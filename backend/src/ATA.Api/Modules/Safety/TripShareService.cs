using System.Collections.Concurrent;
using System.Security.Cryptography;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Safety;

/// <summary>Counts a public view at most once per 60 s per (token, IP).</summary>
public sealed class ShareViewThrottle
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<string, DateTime> _seen = new(StringComparer.Ordinal);

    public bool ShouldCount(string token, string? ip, DateTime now)
    {
        var key = $"{token}|{ip ?? "unknown"}";
        if (_seen.Count > 50_000)
        {
            foreach (var stale in _seen.Where(p => now - p.Value > Window).Select(p => p.Key).ToList())
            {
                _seen.TryRemove(stale, out _);
            }
        }

        var counted = false;
        _seen.AddOrUpdate(key, _ => { counted = true; return now; }, (_, last) =>
        {
            if (now - last < Window) return last;
            counted = true;
            return now;
        });
        return counted;
    }
}

/// <summary>Trip sharing (doc 09 §F12.2): links for the passenger, automatic shares on assignment, expiry at trip end and the public tracking read.</summary>
public sealed class TripShareService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IPricingService pricing,
    INotificationDispatcher notifications,
    IFileStorage storage,
    ShareViewThrottle throttle,
    IOptions<SafetyOptions> options)
{
    public const int MaxTravelledPoints = 300;
    private readonly SafetyOptions _options = options.Value;

    public static readonly TripStatus[] ShareableStatuses =
        [TripStatus.DriverAssigned, TripStatus.DriverEnRoute, TripStatus.DriverArrived, TripStatus.Waiting, TripStatus.PinVerified, TripStatus.InTrip];

    public string UrlOf(string token) => $"{_options.ShareBaseUrl.TrimEnd('/')}/t/{token}";

    public static string NewToken() => System.Buffers.Text.Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));

    public async Task<CreateSharesResponse> CreateAsync(Guid tripId, CreateShareRequest request, CancellationToken ct)
    {
        var channel = request.Channel ?? TripShareChannel.Link;
        new Validator()
            .Rule(nameof(request.Channel), channel is TripShareChannel.Link or TripShareChannel.Sms, "must be link|sms")
            .Rule(nameof(request.ContactIds), channel != TripShareChannel.Sms || request.ContactIds is { Count: > 0 }, "required for channel=sms")
            .ThrowIfInvalid();

        var userId = currentUser.UserId;
        var trip = await LoadPassengerTripAsync(tripId, userId, ct);
        if (!ShareableStatuses.Contains(trip.Status))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        }

        var contacts = new List<TrustedContact>();
        if (channel == TripShareChannel.Sms)
        {
            var ids = request.ContactIds!.Distinct().ToList();
            contacts = await db.TrustedContacts.AsNoTracking().Where(c => c.UserId == userId && ids.Contains(c.Id)).ToListAsync(ct);
            new Validator().Rule(nameof(request.ContactIds), contacts.Count == ids.Count, "unknown contact").ThrowIfInvalid();
        }

        var existing = await db.TripShares.CountAsync(s => s.TripId == trip.Id, ct);
        var adding = channel == TripShareChannel.Sms ? contacts.Count : 1;
        if (existing + adding > _options.MaxSharesPerTrip)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["shares"] = $"limit:{_options.MaxSharesPerTrip}" });
        }

        var created = new List<TripShare>();
        if (channel == TripShareChannel.Link)
        {
            created.Add(Add(trip.Id, userId, null, TripShareChannel.Link));
        }
        else
        {
            var userName = await UserNameAsync(userId, ct);
            foreach (var contact in contacts)
            {
                var share = Add(trip.Id, userId, contact.Id, TripShareChannel.Sms);
                created.Add(share);
                await SendShareSmsAsync(contact.PhoneNumber, userName, share, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        return new CreateSharesResponse(created.Select(s => new CreatedShareDto(s.Id, UrlOf(s.Token), s.Channel, s.TrustedContactId, s.ExpiresAt)).ToList());
    }

    public async Task<IReadOnlyList<TripShareDto>> ListAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await LoadPassengerTripAsync(tripId, currentUser.UserId, ct);
        return await ListForTripAsync(trip.Id, ct);
    }

    public async Task<IReadOnlyList<TripShareDto>> ListForTripAsync(Guid tripId, CancellationToken ct)
    {
        var rows = await (from s in db.TripShares.AsNoTracking()
                          join c in db.TrustedContacts.AsNoTracking() on s.TrustedContactId equals c.Id into contacts
                          from c in contacts.DefaultIfEmpty()
                          where s.TripId == tripId
                          orderby s.CreatedAt
                          select new { s, ContactName = c == null ? null : c.Name }).ToListAsync(ct);
        return rows.Select(r => new TripShareDto(r.s.Id, UrlOf(r.s.Token), r.s.Channel, r.s.TrustedContactId, r.ContactName, r.s.ViewCount, r.s.ExpiresAt, r.s.RevokedAt, r.s.LastViewedAt, r.s.CreatedAt)).ToList();
    }

    public async Task RevokeAsync(Guid shareId, CancellationToken ct)
    {
        var share = Guard.NotFound(await db.TripShares.FirstOrDefaultAsync(s => s.Id == shareId && s.CreatedByUserId == currentUser.UserId, ct));
        share.RevokedAt ??= clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Automatic sharing when the trip moves to <c>driver_assigned</c>: one <c>auto</c> link per trusted contact with <c>auto_share</c>, plus an SMS
    /// when <c>Safety:AutoShareSmsEnabled</c>. Adds to the caller's unit of work.
    /// </summary>
    public async Task AutoShareOnAssignAsync(Trip trip, Guid passengerUserId, CancellationToken ct)
    {
        var contacts = await db.TrustedContacts.AsNoTracking().Where(c => c.UserId == passengerUserId && c.AutoShare).OrderBy(c => c.CreatedAt).ToListAsync(ct);
        if (contacts.Count == 0)
        {
            return;
        }

        var userName = await UserNameAsync(passengerUserId, ct);
        foreach (var contact in contacts.Take(_options.MaxSharesPerTrip))
        {
            var share = Add(trip.Id, passengerUserId, contact.Id, TripShareChannel.Auto);
            if (_options.AutoShareSmsEnabled)
            {
                await SendShareSmsAsync(contact.PhoneNumber, userName, share, ct);
            }
        }
    }

    /// <summary>Sets <c>expires_at</c> (end + <c>Safety:ShareExpiryMinutesAfterEnd</c>) on the trip's open links; the caller saves.</summary>
    public async Task ExpireForTripAsync(Guid tripId, DateTime endedAt, CancellationToken ct)
    {
        var expiresAt = endedAt.AddMinutes(_options.ShareExpiryMinutesAfterEnd);
        var open = await db.TripShares.Where(s => s.TripId == tripId && s.ExpiresAt == null).ToListAsync(ct);
        foreach (var share in open)
        {
            share.ExpiresAt = expiresAt;
        }
    }

    /// <summary><c>TripShareExpiryJob</c>: links of ended trips that missed the end event.</summary>
    public async Task<int> ExpireEndedAsync(CancellationToken ct)
    {
        var rows = await (from s in db.TripShares
                          join t in db.Trips on s.TripId equals t.Id
                          where s.ExpiresAt == null && Trip.TerminalStatuses.Contains(t.Status)
                          select new { Share = s, t.CompletedAt, t.CancelledAt, t.UpdatedAt }).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.Share.ExpiresAt = (row.CompletedAt ?? row.CancelledAt ?? row.UpdatedAt).AddMinutes(_options.ShareExpiryMinutesAfterEnd);
        }

        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    /// <summary>A link created for an SOS (trusted contacts); adds to the caller's unit of work.</summary>
    public TripShare Add(Guid tripId, Guid createdBy, Guid? contactId, TripShareChannel channel)
    {
        var share = new TripShare { TripId = tripId, Token = NewToken(), CreatedByUserId = createdBy, TrustedContactId = contactId, Channel = channel };
        db.TripShares.Add(share);
        return share;
    }

    public async Task<PublicTripShareDto> GetPublicAsync(string token, string? ip, Language lang, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var share = await LoadPublicAsync(token, ct);
        if (throttle.ShouldCount(share.Token, ip, now))
        {
            share.ViewCount++;
            share.LastViewedAt = now;
            await db.SaveChangesAsync(ct);
        }

        var trip = await db.Trips.AsNoTracking().Include(t => t.Stops).FirstAsync(t => t.Id == share.TripId, ct);
        var passengerName = await (from p in db.Passengers.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where p.Id == trip.PassengerId select u.FullName).FirstOrDefaultAsync(ct);
        var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == trip.RideCategoryId, ct);

        PublicDriverDto? driver = null;
        TripVehicleDto? vehicle = null;
        PublicDriverLocationDto? location = null;
        int? eta = null;
        string? etaTarget = null;
        if (trip.DriverId is { } driverId && trip.HasDriver)
        {
            var row = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == driverId select new { u.FullName, d.RatingAvg }).FirstAsync(ct);
            var hasPhoto = await DriverPhotoFileIdAsync(driverId, ct) is not null;
            driver = new PublicDriverDto(SafetyLabels.FirstName(row.FullName) ?? string.Empty, row.RatingAvg, hasPhoto ? $"/api/v1/public/trip-shares/{share.Token}/driver-photo" : null);
            if (trip.VehicleId is { } vehicleId)
            {
                vehicle = await db.Vehicles.AsNoTracking().Where(v => v.Id == vehicleId).Select(v => new TripVehicleDto(v.Make, v.Model, v.Color, v.PlateNumber)).FirstOrDefaultAsync(ct);
            }

            if (!trip.IsTerminal && await db.DriverLocations.AsNoTracking().FirstOrDefaultAsync(l => l.DriverId == driverId, ct) is { } loc)
            {
                location = new PublicDriverLocationDto(loc.Lat, loc.Lng, loc.Heading, loc.UpdatedAt);
                (etaTarget, var targetLat, var targetLng) = trip.Status switch
                {
                    TripStatus.DriverAssigned or TripStatus.DriverEnRoute => ("pickup", trip.PickupLat, trip.PickupLng),
                    TripStatus.InTrip => ("dropoff", trip.DropoffLat, trip.DropoffLng),
                    _ => ((string?)null, 0m, 0m),
                };
                if (etaTarget is not null)
                {
                    eta = pricing.EtaSeconds(Geo.HaversineMeters(loc.Lat, loc.Lng, targetLat, targetLng) * FlatPricing.RoadFactor);
                }
            }
        }

        var travelled = new List<decimal[]>();
        if (trip.StartedAt is { } startedAt)
        {
            travelled = (await db.DriverLocationHistory.AsNoTracking().Where(h => h.TripId == trip.Id && h.RecordedAt >= startedAt).OrderBy(h => h.RecordedAt)
                .Select(h => new { h.Lat, h.Lng }).ToListAsync(ct)).Select(h => new[] { h.Lat, h.Lng }).ToList();
        }

        var status = trip.Status == TripStatus.PinVerified ? TripStatus.Waiting : trip.Status;
        return new PublicTripShareDto(
            SafetyLabels.Snake(status),
            SafetyLabels.FirstName(passengerName),
            driver,
            vehicle,
            category is null ? null : new PublicCategoryDto(category.Code, lang.Pick(category.NameAr, category.NameEn)),
            new PublicPlaceDto(trip.PickupName, trip.PickupAddress, trip.PickupLat, trip.PickupLng),
            new PublicPlaceDto(trip.DropoffName, trip.DropoffAddress, trip.DropoffLat, trip.DropoffLng),
            trip.Stops.OrderBy(s => s.Sequence).Select(s => new PublicPlaceDto(s.Name, s.Address, s.Lat, s.Lng)).ToList(),
            location,
            new PublicRouteDto(PlannedRoutes.Of(trip, trip.Stops), PlannedRoutes.Downsample(travelled, MaxTravelledPoints)),
            eta,
            etaTarget,
            new PublicTimelineDto(trip.AssignedAt, trip.ArrivedAt, trip.StartedAt, trip.CompletedAt, trip.CancelledAt),
            share.ExpiresAt,
            _options.PublicShareRefreshSeconds);
    }

    /// <summary>The driver's profile photo for a valid link (same validity rules as the tracking read).</summary>
    public async Task<(Stream Content, string ContentType)> GetDriverPhotoAsync(string token, CancellationToken ct)
    {
        var share = await LoadPublicAsync(token, ct);
        var driverId = await db.Trips.AsNoTracking().Where(t => t.Id == share.TripId).Select(t => t.DriverId).FirstOrDefaultAsync(ct);
        var fileId = driverId is { } id ? await DriverPhotoFileIdAsync(id, ct) : null;
        var file = fileId is { } f ? await db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == f, ct) : null;
        var stream = file is null ? null : await storage.OpenReadAsync(file.StorageKey, ct);
        if (stream is null)
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        return (stream, file!.ContentType);
    }

    private async Task<TripShare> LoadPublicAsync(string token, CancellationToken ct)
    {
        var share = string.IsNullOrWhiteSpace(token) || token.Length > 32 ? null : await db.TripShares.FirstOrDefaultAsync(s => s.Token == token, ct);
        if (share is null)
        {
            throw new DomainException(ErrorCodes.ShareNotFound);
        }

        if (!share.IsUsableAt(clock.UtcNow))
        {
            throw new DomainException(ErrorCodes.ShareExpired, new { share.ExpiresAt, share.RevokedAt });
        }

        return share;
    }

    private async Task<Guid?> DriverPhotoFileIdAsync(Guid driverId, CancellationToken ct) =>
        await (from doc in db.DriverDocuments.AsNoTracking()
               join type in db.DocumentTypes.AsNoTracking() on doc.DocumentTypeId equals type.Id
               where doc.DriverId == driverId && type.Code == "profile_photo" && doc.Status != Domain.Drivers.DocumentStatus.Rejected
               select (Guid?)doc.FileId).FirstOrDefaultAsync(ct);

    private async Task SendShareSmsAsync(string phone, string? userName, TripShare share, CancellationToken ct) =>
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SafetyTripShared, Guid.Empty,
            NotificationPlaceholders.Of(("userName", userName ?? string.Empty), ("shareUrl", UrlOf(share.Token))), "share", share.Id,
            RecipientPhoneOverride: phone), ct);

    private async Task<string?> UserNameAsync(Guid userId, CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct);

    private async Task<Trip> LoadPassengerTripAsync(Guid tripId, Guid userId, CancellationToken ct)
    {
        var trip = await (from t in db.Trips
                          join p in db.Passengers on t.PassengerId equals p.Id
                          where t.Id == tripId && p.UserId == userId
                          select t).FirstOrDefaultAsync(ct);
        return trip ?? throw new DomainException(ErrorCodes.NotFound);
    }
}
