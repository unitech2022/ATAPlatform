using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Push;
using ATA.Infrastructure.Sms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Notifications;

/// <summary>
/// Resolves a campaign audience (doc 08 §F13.5): every key is optional and combined with AND; <c>userIds</c> alone ignores the rest. Only active
/// users are targeted. A driver's city is <c>drivers.city_id</c>; a passenger's city is the city nearest to the pickup of their last trip.
/// </summary>
public sealed class AudienceResolver(AtaDbContext db, IClock clock)
{
    public async Task<List<Guid>> ResolveAsync(CampaignAudience audience, CancellationToken ct)
    {
        var users = db.Users.AsNoTracking().Where(u => u.Status == UserStatus.Active);
        if (audience.UserIds is { Count: > 0 } ids)
        {
            return await users.Where(u => ids.Contains(u.Id)).OrderBy(u => u.Id).Select(u => u.Id).ToListAsync(ct);
        }

        if (audience.Roles is { Count: > 0 })
        {
            var roles = audience.Roles.Select(r => QueryEnum.Parse<Role>(r, "audience.roles")!.Value).ToList();
            users = users.Where(u => u.Roles.Any(r => roles.Contains(r.Role)));
        }

        if (audience.Languages is { Count: > 0 })
        {
            var languages = audience.Languages.Select(l => QueryEnum.Parse<Language>(l, "audience.languages")!.Value).ToList();
            users = users.Where(u => languages.Contains(u.Language));
        }

        if (audience.Genders is { Count: > 0 })
        {
            var genders = audience.Genders.Select(g => QueryEnum.Parse<Gender>(g, "audience.genders")!.Value).ToList();
            users = users.Where(u => genders.Contains(u.Gender) || db.Drivers.Any(d => d.UserId == u.Id && genders.Contains(d.Gender)));
        }

        if (audience.DriverTiers is { Count: > 0 })
        {
            var tiers = audience.DriverTiers.Select(t => QueryEnum.Parse<DriverTier>(t, "audience.driverTiers")!.Value).ToList();
            users = users.Where(u => db.Drivers.Any(d => d.UserId == u.Id && tiers.Contains(d.Tier)));
        }

        if (audience.LastActiveWithinDays is { } days)
        {
            var since = clock.UtcNow.AddDays(-days);
            users = users.Where(u => u.LastLoginAt != null && u.LastLoginAt >= since);
        }

        if (audience.HasCompletedTrip is { } completed)
        {
            var passengerIds = db.Trips.Where(t => t.Status == TripStatus.Completed).Join(db.Passengers, t => t.PassengerId, p => p.Id, (t, p) => p.UserId);
            var driverIds = db.Trips.Where(t => t.Status == TripStatus.Completed && t.DriverId != null).Join(db.Drivers, t => t.DriverId, d => (Guid?)d.Id, (t, d) => d.UserId);
            users = completed
                ? users.Where(u => passengerIds.Contains(u.Id) || driverIds.Contains(u.Id))
                : users.Where(u => !passengerIds.Contains(u.Id) && !driverIds.Contains(u.Id));
        }

        var candidates = await users.OrderBy(u => u.Id).Select(u => u.Id).ToListAsync(ct);
        if (audience.CityIds is not { Count: > 0 } cityIds || candidates.Count == 0)
        {
            return candidates;
        }

        var driverCities = await db.Drivers.AsNoTracking().Where(d => candidates.Contains(d.UserId) && d.CityId != null)
            .Select(d => new { d.UserId, CityId = d.CityId!.Value }).ToListAsync(ct);
        var inCity = driverCities.Where(d => cityIds.Contains(d.CityId)).Select(d => d.UserId).ToHashSet();
        var cities = await db.Cities.AsNoTracking().Where(c => c.IsActive).Select(c => new { c.Id, c.CenterLat, c.CenterLng }).ToListAsync(ct);
        var lastTrips = await (from t in db.Trips.AsNoTracking()
                               join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id
                               where candidates.Contains(p.UserId)
                               select new { p.UserId, t.PickupLat, t.PickupLng, t.RequestedAt }).ToListAsync(ct);
        foreach (var trip in lastTrips.GroupBy(t => t.UserId).Select(g => g.OrderByDescending(t => t.RequestedAt).First()))
        {
            var nearest = cities.OrderBy(c => Geo.HaversineMeters(trip.PickupLat, trip.PickupLng, c.CenterLat, c.CenterLng)).FirstOrDefault();
            if (nearest is not null && cityIds.Contains(nearest.Id)) inCity.Add(trip.UserId);
        }

        return candidates.Where(inCity.Contains).ToList();
    }
}

/// <summary>Broadcast campaigns: admin CRUD (editable only as drafts), scheduling, and the sender (<c>CampaignSenderJob</c>).</summary>
public sealed class CampaignService(
    AtaDbContext db,
    AudienceResolver audiences,
    IPushSender push,
    ISmsSender sms,
    AuditService audit,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<NotificationsOptions> options,
    ILogger<CampaignService> logger)
{
    public const string EntityType = "notification_campaign";
    private readonly NotificationsOptions _options = options.Value;

    public async Task<PagedResult<CampaignDto>> ListAsync(CampaignStatus? status, Paging paging, CancellationToken ct)
    {
        var query = db.NotificationCampaigns.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(c => c.Status == status);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(c => c.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await ToDtosAsync(rows, ct), total);
    }

    public async Task<CampaignDto> GetAsync(Guid id, CancellationToken ct) => (await ToDtosAsync([await LoadAsync(id, ct)], ct))[0];

    public async Task<CampaignDto> CreateAsync(CampaignUpsertRequest request, CancellationToken ct)
    {
        Validate(request);
        var campaign = new NotificationCampaign
        {
            Name = string.Empty, TitleAr = string.Empty, TitleEn = string.Empty, BodyAr = string.Empty, BodyEn = string.Empty, CreatedBy = currentUser.UserId,
        };
        Apply(campaign, request);
        db.NotificationCampaigns.Add(campaign);
        audit.Log("notification_campaign.create", EntityType, campaign.Id, null, Snapshot(campaign));
        await db.SaveChangesAsync(ct);
        return await GetAsync(campaign.Id, ct);
    }

    public async Task<CampaignDto> UpdateAsync(Guid id, CampaignUpsertRequest request, CancellationToken ct)
    {
        Validate(request);
        var campaign = await LoadAsync(id, ct);
        campaign.EnsureEditable();
        var before = Snapshot(campaign);
        Apply(campaign, request);
        campaign.UpdatedBy = currentUser.UserId;
        audit.Log("notification_campaign.update", EntityType, campaign.Id, before, Snapshot(campaign));
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var campaign = await LoadAsync(id, ct);
        campaign.EnsureEditable();
        db.NotificationCampaigns.Remove(campaign);
        audit.Log("notification_campaign.delete", EntityType, campaign.Id, Snapshot(campaign));
        await db.SaveChangesAsync(ct);
    }

    public async Task<CampaignDto> ScheduleAsync(Guid id, ScheduleCampaignRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ScheduledAt), request.ScheduledAt)
            .Rule(nameof(request.ScheduledAt), request.ScheduledAt is null || request.ScheduledAt.Value.ToUniversalTime() > clock.UtcNow, "must be in the future")
            .ThrowIfInvalid();
        var campaign = await LoadAsync(id, ct);
        campaign.EnsureEditable();
        campaign.Status = CampaignStatus.Scheduled;
        campaign.ScheduledAt = request.ScheduledAt!.Value.ToUniversalTime();
        campaign.UpdatedBy = currentUser.UserId;
        audit.Log("notification_campaign.schedule", EntityType, campaign.Id, null, new { campaign.ScheduledAt });
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Queues the campaign for the next sender pass (scheduled now).</summary>
    public async Task<CampaignDto> SendNowAsync(Guid id, CancellationToken ct)
    {
        var campaign = await LoadAsync(id, ct);
        if (campaign.Status is not (CampaignStatus.Draft or CampaignStatus.Scheduled))
        {
            throw new DomainException(ErrorCodes.CampaignNotEditable, new { status = campaign.Status });
        }

        campaign.Status = CampaignStatus.Scheduled;
        campaign.ScheduledAt = clock.UtcNow;
        campaign.UpdatedBy = currentUser.UserId;
        audit.Log("notification_campaign.send", EntityType, campaign.Id, null, new { campaign.ScheduledAt });
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<CampaignDto> CancelAsync(Guid id, CancellationToken ct)
    {
        var campaign = await LoadAsync(id, ct);
        if (campaign.Status is not (CampaignStatus.Draft or CampaignStatus.Scheduled or CampaignStatus.Sending))
        {
            throw new DomainException(ErrorCodes.CampaignNotEditable, new { status = campaign.Status });
        }

        var before = campaign.Status;
        campaign.Status = CampaignStatus.Cancelled;
        campaign.CompletedAt = clock.UtcNow;
        campaign.UpdatedBy = currentUser.UserId;
        audit.Log("notification_campaign.cancel", EntityType, campaign.Id, new { status = before }, new { campaign.Status });
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<AudiencePreviewDto> PreviewAsync(AudiencePreviewRequest request, CancellationToken ct)
    {
        var ids = await audiences.ResolveAsync(request.Audience ?? EmptyAudience, ct);
        var sampleIds = ids.Take(10).ToList();
        var sample = await db.Users.AsNoTracking().Where(u => sampleIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.PhoneNumber }).ToListAsync(ct);
        return new AudiencePreviewDto(ids.Count, sample.Select(u => new AudienceSampleDto(u.Id, u.FullName, PhoneMasking.Mask(u.PhoneNumber))).ToList());
    }

    /// <summary>
    /// <c>CampaignSenderJob</c> pass: starts due scheduled campaigns and sends pages of <c>Notifications:PushBatchSize</c> users (one OneSignal request
    /// per page, at most <c>Notifications:CampaignPushPerMinute</c> pages per pass). Users already reached are skipped, so a pass can resume a
    /// campaign; a cancellation stops the remaining pages.
    /// </summary>
    public async Task<int> RunDueAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var due = await db.NotificationCampaigns
            .Where(c => (c.Status == CampaignStatus.Scheduled && c.ScheduledAt <= now) || c.Status == CampaignStatus.Sending)
            .OrderBy(c => c.ScheduledAt).ToListAsync(ct);
        var budget = Math.Max(1, _options.CampaignPushPerMinute);
        foreach (var campaign in due)
        {
            if (budget <= 0) break;
            budget -= await SendAsync(campaign, budget, ct);
        }

        return due.Count;
    }

    private async Task<int> SendAsync(NotificationCampaign campaign, int maxPages, CancellationToken ct)
    {
        var audience = ParseAudience(campaign.Audience);
        var channels = ParseChannels(campaign.Channels);
        var all = await audiences.ResolveAsync(audience, ct);
        if (campaign.Status == CampaignStatus.Scheduled)
        {
            campaign.Status = CampaignStatus.Sending;
            campaign.StartedAt = clock.UtcNow;
            campaign.TargetCount = all.Count;
            await db.SaveChangesAsync(ct);
        }

        var reached = (await db.Notifications.AsNoTracking().Where(n => n.CampaignId == campaign.Id).Select(n => n.UserId).ToListAsync(ct))
            .Concat(await db.NotificationDeliveries.AsNoTracking().Where(d => d.CampaignId == campaign.Id && d.UserId != null).Select(d => d.UserId!.Value).ToListAsync(ct))
            .ToHashSet();
        var remaining = all.Where(id => !reached.Contains(id)).ToList();
        var pageSize = Math.Max(1, _options.PushBatchSize);
        var pages = 0;
        foreach (var page in remaining.Chunk(pageSize))
        {
            if (pages >= maxPages) return pages;
            await db.Entry(campaign).ReloadAsync(ct);
            if (campaign.Status == CampaignStatus.Cancelled)
            {
                return pages;
            }

            await SendPageAsync(campaign, page, channels, ct);
            pages++;
        }

        campaign.Status = CampaignStatus.Sent;
        campaign.CompletedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return Math.Max(1, pages);
    }

    private async Task SendPageAsync(NotificationCampaign campaign, IReadOnlyList<Guid> userIds, IReadOnlyList<NotificationChannel> channels, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.PhoneNumber, u.Language }).ToDictionaryAsync(u => u.Id, ct);
        var preferences = await db.NotificationPreferences.AsNoTracking().Where(p => userIds.Contains(p.UserId)).ToDictionaryAsync(p => p.UserId, ct);
        var flag = NotificationEvents.PreferenceOf(campaign.Category);
        bool Allowed(Guid userId) => flag is null || !preferences.TryGetValue(userId, out var p) || flag(p);
        var category = JsonNamingPolicy.SnakeCaseLower.ConvertName(campaign.Category.ToString());
        var inbox = new Dictionary<Guid, Guid>();
        if (channels.Contains(NotificationChannel.Inapp))
        {
            foreach (var userId in userIds)
            {
                var notification = new Notification
                {
                    UserId = userId, Type = NotificationTypes.CampaignBroadcast, Category = campaign.Category, CampaignId = campaign.Id,
                    TitleAr = campaign.TitleAr, TitleEn = campaign.TitleEn, BodyAr = campaign.BodyAr, BodyEn = campaign.BodyEn,
                    Data = JsonSerializer.Serialize(Data(campaign, null), JsonDefaults.Options), CreatedAt = now,
                };
                notification.Data = JsonSerializer.Serialize(Data(campaign, notification.Id), JsonDefaults.Options);
                db.Notifications.Add(notification);
                inbox[userId] = notification.Id;
            }

            campaign.InappCreated += userIds.Count;
        }

        if (channels.Contains(NotificationChannel.Push))
        {
            var eligible = userIds.Where(Allowed).ToList();
            var payload = new DeliveryPayload(
                new Dictionary<string, string> { ["ar"] = campaign.TitleAr, ["en"] = campaign.TitleEn },
                new Dictionary<string, string> { ["ar"] = campaign.BodyAr, ["en"] = campaign.BodyEn },
                Data(campaign, null), category, "normal", null, null, null, null, null, null);
            var json = JsonSerializer.Serialize(payload, JsonDefaults.Options);
            PushSendResult? result = null;
            if (eligible.Count > 0)
            {
                result = await push.SendAsync(new PushMessage(eligible, payload.Headings!, payload.Contents!, payload.Data!, category, "normal", null, null, null,
                    $"campaign:{campaign.Id}:{eligible[0]}"), ct);
            }

            foreach (var userId in userIds)
            {
                var delivery = new NotificationDelivery
                {
                    NotificationId = inbox.TryGetValue(userId, out var nid) ? nid : null, UserId = userId, EventCode = NotificationTypes.CampaignBroadcast,
                    Channel = NotificationChannel.Push, CampaignId = campaign.Id, Provider = push.Provider, Payload = json, CreatedAt = now, Attempts = 1,
                };
                if (!Allowed(userId))
                {
                    delivery.Attempts = 0;
                    delivery.Skip(DeliverySkipReasons.PreferenceOff);
                    campaign.PushSkipped++;
                }
                else if (result is { Success: true } && result.InvalidExternalUserIds.Contains(userId))
                {
                    delivery.Skip(DeliverySkipReasons.NoSubscription);
                    campaign.PushSkipped++;
                }
                else if (result is { Success: true })
                {
                    delivery.Status = DeliveryStatus.Sent;
                    delivery.ProviderMessageId = result.ProviderMessageId;
                    delivery.SentAt = now;
                    campaign.PushSent++;
                }
                else
                {
                    delivery.Status = DeliveryStatus.Failed;
                    delivery.ErrorCode = result?.ErrorCode;
                    delivery.ErrorMessage = result?.ErrorMessage;
                    delivery.NextAttemptAt = result?.IsTransient == true ? now.AddSeconds(30) : null;
                    campaign.PushFailed++;
                }

                db.NotificationDeliveries.Add(delivery);
            }
        }

        if (channels.Contains(NotificationChannel.Sms))
        {
            foreach (var userId in userIds)
            {
                var user = users.GetValueOrDefault(userId);
                var body = user?.Language == Language.En ? campaign.BodyEn : campaign.BodyAr;
                var delivery = new NotificationDelivery
                {
                    UserId = userId, PhoneNumber = user?.PhoneNumber, EventCode = NotificationTypes.CampaignBroadcast, Channel = NotificationChannel.Sms,
                    CampaignId = campaign.Id, Provider = sms.Provider, CreatedAt = now,
                    Payload = JsonSerializer.Serialize(new DeliveryPayload(null, null, null, null, null, null, null, null, user?.PhoneNumber, body, user?.Language == Language.En ? "en" : "ar"), JsonDefaults.Options),
                };
                if (!Allowed(userId))
                {
                    delivery.Skip(DeliverySkipReasons.PreferenceOff);
                }
                else if (string.IsNullOrWhiteSpace(user?.PhoneNumber))
                {
                    delivery.Skip(DeliverySkipReasons.NoPhone);
                }
                else
                {
                    delivery.Attempts = 1;
                    var result = await sms.SendAsync(user.PhoneNumber, body, ct);
                    delivery.Status = result.Success ? DeliveryStatus.Sent : DeliveryStatus.Failed;
                    delivery.ProviderMessageId = result.ProviderMessageId;
                    delivery.ErrorCode = result.ErrorCode;
                    delivery.ErrorMessage = result.ErrorMessage;
                    delivery.SentAt = result.Success ? now : null;
                    if (result.Success) campaign.SmsSent++; else campaign.SmsFailed++;
                }

                db.NotificationDeliveries.Add(delivery);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Campaign {CampaignId}: page of {Count} users sent", campaign.Id, userIds.Count);
    }

    private static Dictionary<string, object?> Data(NotificationCampaign campaign, Guid? notificationId) => new()
    {
        ["eventCode"] = NotificationTypes.CampaignBroadcast,
        ["notificationId"] = notificationId,
        ["deepLink"] = campaign.DeepLink,
        ["campaignId"] = campaign.Id,
    };

    private static readonly CampaignAudience EmptyAudience = new(null, null, null, null, null, null, null, null);

    private void Validate(CampaignUpsertRequest request)
    {
        var channels = request.Channels ?? [];
        new Validator()
            .Require(nameof(request.Name), request.Name, 150)
            .Require(nameof(request.Category), request.Category)
            .Rule(nameof(request.Category), request.Category is null or NotificationCategory.Promotions or NotificationCategory.System or NotificationCategory.Trips or NotificationCategory.Wallet, "must be promotions|system|trips|wallet")
            .Rule(nameof(request.Channels), channels.Count > 0, "required")
            .Require(nameof(request.TitleAr), request.TitleAr, 120)
            .Require(nameof(request.TitleEn), request.TitleEn, 120)
            .Require(nameof(request.BodyAr), request.BodyAr, 1000)
            .Require(nameof(request.BodyEn), request.BodyEn, 1000)
            .Rule(nameof(request.DeepLink), request.DeepLink is null || (request.DeepLink.Length <= 255 && request.DeepLink.StartsWith("ata://", StringComparison.Ordinal)), "must be an ata:// link")
            .ThrowIfInvalid();
        if (channels.Contains(NotificationChannel.Sms) && !currentUser.HasPermission(Common.Permissions.NotificationsSmsBroadcast))
        {
            throw new DomainException(ErrorCodes.Forbidden, new { permission = Common.Permissions.NotificationsSmsBroadcast });
        }

        // Validates the audience keys (unknown enum values → 422).
        _ = request.Audience?.Roles?.Select(r => QueryEnum.Parse<Role>(r, "audience.roles")).ToList();
        _ = request.Audience?.Languages?.Select(l => QueryEnum.Parse<Language>(l, "audience.languages")).ToList();
        _ = request.Audience?.Genders?.Select(g => QueryEnum.Parse<Gender>(g, "audience.genders")).ToList();
        _ = request.Audience?.DriverTiers?.Select(t => QueryEnum.Parse<DriverTier>(t, "audience.driverTiers")).ToList();
    }

    private static void Apply(NotificationCampaign campaign, CampaignUpsertRequest request)
    {
        campaign.Name = request.Name!.Trim();
        campaign.Category = request.Category!.Value;
        campaign.Channels = JsonSerializer.Serialize(request.Channels!.Distinct().ToList(), JsonDefaults.Options);
        campaign.Audience = JsonSerializer.Serialize(request.Audience ?? EmptyAudience, JsonDefaults.Options);
        campaign.TitleAr = request.TitleAr!.Trim();
        campaign.TitleEn = request.TitleEn!.Trim();
        campaign.BodyAr = request.BodyAr!.Trim();
        campaign.BodyEn = request.BodyEn!.Trim();
        campaign.DeepLink = string.IsNullOrWhiteSpace(request.DeepLink) ? null : request.DeepLink.Trim();
    }

    public static CampaignAudience ParseAudience(string json) => JsonSerializer.Deserialize<CampaignAudience>(json, JsonDefaults.Options) ?? EmptyAudience;

    public static IReadOnlyList<NotificationChannel> ParseChannels(string json) => JsonSerializer.Deserialize<List<NotificationChannel>>(json, JsonDefaults.Options) ?? [];

    private async Task<NotificationCampaign> LoadAsync(Guid id, CancellationToken ct) => Guard.NotFound(await db.NotificationCampaigns.FirstOrDefaultAsync(c => c.Id == id, ct));

    private async Task<IReadOnlyList<CampaignDto>> ToDtosAsync(IReadOnlyList<NotificationCampaign> rows, CancellationToken ct)
    {
        var ids = rows.Select(c => c.CreatedBy).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        return rows.Select(c => new CampaignDto(c.Id, c.Name, c.Category, ParseChannels(c.Channels), ParseAudience(c.Audience), c.TitleAr, c.TitleEn, c.BodyAr, c.BodyEn,
            c.DeepLink, c.Status, c.ScheduledAt, c.StartedAt, c.CompletedAt, c.TargetCount, c.InappCreated, c.PushSent, c.PushFailed, c.PushSkipped, c.SmsSent,
            c.SmsFailed, c.OpenedCount, c.CreatedAt, c.UpdatedAt) { CreatedByName = names.GetValueOrDefault(c.CreatedBy) }).ToList();
    }

    private static object Snapshot(NotificationCampaign c) => new { c.Name, c.Category, c.Channels, c.Audience, c.Status, c.ScheduledAt };
}
