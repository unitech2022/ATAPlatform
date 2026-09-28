using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Trips;

/// <summary>Who is looking at a trip; controls whether the PIN is revealed.</summary>
public enum TripViewer { Passenger, Driver, Admin }

/// <summary>
/// Adds <c>trip_events</c> rows. Timestamps are monotonic per process (at least 1 µs apart, which fits MySQL <c>datetime(6)</c>)
/// so the timeline keeps insertion order even when several events are written within the same clock tick.
/// </summary>
public sealed class TripEventRecorder(AtaDbContext db, IClock clock)
{
    private static long _lastTicks;

    public TripEvent Add(Guid tripId, string type, TripActor actor, Guid? actorUserId = null, decimal? lat = null, decimal? lng = null, object? data = null)
    {
        var tripEvent = new TripEvent
        {
            TripId = tripId,
            Type = type,
            Actor = actor,
            ActorUserId = actorUserId,
            Lat = lat,
            Lng = lng,
            Data = data is null ? null : JsonSerializer.Serialize(data, JsonDefaults.Options),
            CreatedAt = NextTimestamp(clock.UtcNow),
        };
        db.TripEvents.Add(tripEvent);
        return tripEvent;
    }

    private static DateTime NextTimestamp(DateTime now)
    {
        while (true)
        {
            var last = Interlocked.Read(ref _lastTicks);
            var next = Math.Max(now.Ticks, last + TimeSpan.TicksPerMicrosecond);
            if (Interlocked.CompareExchange(ref _lastTicks, next, last) == last)
            {
                return new DateTime(next, DateTimeKind.Utc);
            }
        }
    }
}

/// <summary>Generates <c>T-YYYYMMDD-#####</c> numbers, sequential per UTC day.</summary>
public sealed class TripNumberGenerator(AtaDbContext db)
{
    public async Task<string> NextAsync(DateTime now, int offset, CancellationToken ct)
    {
        var prefix = $"T-{now:yyyyMMdd}-";
        var count = await db.Trips.CountAsync(t => t.TripNumber.StartsWith(prefix), ct);
        return $"{prefix}{count + 1 + offset:D5}";
    }
}

/// <summary>
/// Trip PIN handling: the PIN is verified against an HMAC keyed by the trip id (<c>pin_code_hash</c>) and kept in a
/// data-protected form so it can be shown to the passenger once a driver is assigned.
/// </summary>
public sealed class TripPinService(IOtpGenerator generator, IDataProtectionProvider protection)
{
    private readonly IDataProtector _protector = protection.CreateProtector("ATA.Trips.Pin");

    public (string Pin, string Hash, string Protected) Create(Guid tripId)
    {
        var pin = generator.Generate(Trip.PinLength);
        return (pin, generator.Hash(tripId, pin), _protector.Protect(pin));
    }

    public bool Matches(Trip trip, string pin) => string.Equals(generator.Hash(trip.Id, pin), trip.PinCodeHash, StringComparison.Ordinal);

    public string? Reveal(Trip trip)
    {
        try
        {
            return _protector.Unprotect(trip.PinCodeProtected);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }
}

public static class PhoneMasking
{
    /// <summary><c>+966512345678</c> → <c>+96651****678</c>.</summary>
    public static string Mask(string phone)
    {
        if (phone.Length <= 7)
        {
            return new string('*', phone.Length);
        }

        return phone[..6] + new string('*', phone.Length - 9) + phone[^3..];
    }
}
