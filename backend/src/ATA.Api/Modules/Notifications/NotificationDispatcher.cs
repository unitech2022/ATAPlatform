using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using ATA.Api.Common;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Push;
using ATA.Infrastructure.Sms;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Notifications;

/// <summary>
/// A notification to dispatch. <paramref name="Placeholders"/> may carry language-specific values as <c>name:ar</c> / <c>name:en</c>
/// (see <see cref="NotificationPlaceholders"/>). Deep-link variables resolve from placeholders, <paramref name="ExtraData"/> and
/// <c>{entityType}Id</c> = <paramref name="EntityId"/>. <paramref name="ExtraChannels"/> turns on catalogue-allowed channels that are not on by
/// default (e.g. SMS for <c>document.expiring</c> one day before). <paramref name="RecipientUserId"/> may be empty for an SMS to a phone only.
/// </summary>
public sealed record NotificationRequest(
    string EventCode,
    Guid RecipientUserId,
    IReadOnlyDictionary<string, string?> Placeholders,
    string? EntityType = null,
    Guid? EntityId = null,
    IReadOnlyDictionary<string, object?>? ExtraData = null,
    string? RecipientPhoneOverride = null,
    Guid? CampaignId = null,
    IReadOnlyCollection<NotificationChannel>? ExtraChannels = null,
    bool IsTest = false);

public sealed record DispatchResult(Notification? Notification, IReadOnlyList<NotificationDelivery> Deliveries);

/// <summary>The single entry point every module uses to notify users (doc 08 §F13.4).</summary>
public interface INotificationDispatcher
{
    /// <summary>Adds the inbox row and delivery rows to the caller's unit of work; they are sent after the caller saves.</summary>
    Task<DispatchResult> DispatchAsync(NotificationRequest request, CancellationToken ct);
}

public static class NotificationPlaceholders
{
    public static Dictionary<string, string?> Of(params (string Key, object? Value)[] values) =>
        values.ToDictionary(v => v.Key, v => v.Value switch
        {
            null => null,
            decimal d => Formats.Money(d),
            DateTime t => Formats.LocalTime(t),
            _ => v.Value.ToString(),
        }, StringComparer.Ordinal);

    /// <summary>Adds an amount rendered as <c>46.00 ر.س</c> (ar) and <c>SAR 46.00</c> (en).</summary>
    public static Dictionary<string, string?> Money(this Dictionary<string, string?> values, string key, decimal amount)
    {
        values[key] = Formats.Money(amount);
        values[$"{key}:ar"] = Formats.MoneyAr(amount);
        values[$"{key}:en"] = Formats.MoneyEn(amount);
        return values;
    }

    /// <summary>Adds a language-specific value.</summary>
    public static Dictionary<string, string?> Localized(this Dictionary<string, string?> values, string key, string ar, string en)
    {
        values[key] = ar;
        values[$"{key}:ar"] = ar;
        values[$"{key}:en"] = en;
        return values;
    }
}

public static partial class TemplateRenderer
{
    [GeneratedRegex(@"\{([a-zA-Z][a-zA-Z0-9_]*)\}")]
    private static partial Regex Placeholder();

    public static IReadOnlyList<string> PlaceholdersIn(string? template) =>
        template is null ? [] : Placeholder().Matches(template).Select(m => m.Groups[1].Value).Distinct().ToList();

    /// <summary>Replaces <c>{name}</c> with <c>name:{lang}</c> or <c>name</c>; a missing value renders as empty and is reported in <paramref name="missing"/>.</summary>
    public static string Render(string? template, IReadOnlyDictionary<string, string?> values, string language, ICollection<string>? missing = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        return Placeholder().Replace(template, m =>
        {
            var name = m.Groups[1].Value;
            if (values.TryGetValue($"{name}:{language}", out var localized) && localized is not null) return localized;
            if (values.TryGetValue(name, out var value) && value is not null) return value;
            missing?.Add(name);
            return string.Empty;
        });
    }

    /// <summary>SMS segments: GSM-7 text uses 160 (single) / 153 (multi-part) characters, anything else UCS-2 70 / 67.</summary>
    public static int SmsSegments(string text)
    {
        if (text.Length == 0) return 0;
        var gsm = text.All(c => c < 128);
        var (single, multi) = gsm ? (160, 153) : (70, 67);
        return text.Length <= single ? 1 : (int)Math.Ceiling(text.Length / (double)multi);
    }
}

/// <summary>Process-wide cache of <c>notification_templates</c>; invalidated by the admin template endpoints.</summary>
public sealed class NotificationTemplateCache
{
    private IReadOnlyList<NotificationTemplate>? _rows;

    public async Task<IReadOnlyList<NotificationTemplate>> GetAsync(AtaDbContext db, CancellationToken ct)
    {
        var rows = Volatile.Read(ref _rows);
        if (rows is null)
        {
            rows = await db.NotificationTemplates.AsNoTracking().ToListAsync(ct);
            Volatile.Write(ref _rows, rows);
        }

        return rows;
    }

    public void Invalidate() => Volatile.Write(ref _rows, null);
}

/// <summary>In-process queue feeding <see cref="NotificationDeliveryWorker"/> right after the unit of work is saved.</summary>
public sealed class DeliveryQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public void Enqueue(Guid deliveryId) => _channel.Writer.TryWrite(deliveryId);
}

/// <summary>
/// Collects what the dispatcher added and publishes it once the caller's <c>SaveChanges</c> succeeds: delivery ids go to the worker queue and
/// inbox rows are pushed as <c>NotificationCreated</c> over SignalR.
/// </summary>
public sealed class NotificationOutbox
{
    private readonly List<Guid> _deliveries = [];
    private readonly List<Notification> _notifications = [];
    private readonly DeliveryQueue _queue;
    private readonly ITripNotifier _realtime;
    private readonly ILogger<NotificationOutbox> _logger;

    public NotificationOutbox(AtaDbContext db, DeliveryQueue queue, ITripNotifier realtime, ILogger<NotificationOutbox> logger)
    {
        _queue = queue;
        _realtime = realtime;
        _logger = logger;
        db.SavedChanges += (_, _) => Flush();
    }

    public void Add(Notification? notification, IEnumerable<NotificationDelivery> deliveries)
    {
        lock (_deliveries)
        {
            if (notification is not null) _notifications.Add(notification);
            _deliveries.AddRange(deliveries.Where(d => d.Status == DeliveryStatus.Queued).Select(d => d.Id));
        }
    }

    private void Flush()
    {
        List<Guid> deliveries;
        List<Notification> notifications;
        lock (_deliveries)
        {
            deliveries = [.. _deliveries];
            notifications = [.. _notifications];
            _deliveries.Clear();
            _notifications.Clear();
        }

        foreach (var id in deliveries)
        {
            _queue.Enqueue(id);
        }

        foreach (var n in notifications)
        {
            var dto = new NotificationDto(n.Id, n.Type, n.Category, n.TitleAr, n.BodyAr,
                n.Data is null ? null : JsonSerializer.Deserialize<JsonElement>(n.Data), n.ReadAt, n.CreatedAt);
            _ = PublishAsync(n.UserId, dto);
        }
    }

    private async Task PublishAsync(Guid userId, NotificationDto dto)
    {
        try
        {
            await _realtime.NotificationCreatedAsync(userId, dto, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotificationCreated push failed for {UserId}", userId);
        }
    }
}

/// <summary>Serialized push/SMS message kept in <c>notification_deliveries.payload</c>.</summary>
public sealed record DeliveryPayload(
    Dictionary<string, string>? Headings,
    Dictionary<string, string>? Contents,
    Dictionary<string, object?>? Data,
    string? Category,
    string? Priority,
    int? TtlSeconds,
    string? CollapseId,
    List<PushButton>? Buttons,
    string? Phone,
    string? Body,
    string? Language);

public sealed class NotificationDispatcher(
    AtaDbContext db,
    NotificationTemplateCache templates,
    NotificationOutbox outbox,
    IPushSender push,
    ISmsSender sms,
    IClock clock,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task<DispatchResult> DispatchAsync(NotificationRequest request, CancellationToken ct)
    {
        var definition = NotificationEvents.Find(request.EventCode) ?? throw new DomainException(ErrorCodes.UnknownEventCode, new { eventCode = request.EventCode });
        var rows = (await templates.GetAsync(db, ct)).Where(t => t.Code == definition.Code).ToList();

        User? user = null;
        NotificationPreference? preference = null;
        if (request.RecipientUserId != Guid.Empty)
        {
            user = db.Users.Local.FirstOrDefault(u => u.Id == request.RecipientUserId)
                   ?? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.RecipientUserId, ct);
            preference = await db.NotificationPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == request.RecipientUserId, ct);
        }

        var values = new Dictionary<string, string?>(request.Placeholders, StringComparer.Ordinal);
        if (request.EntityType is not null && request.EntityId is { } entityId)
        {
            values.TryAdd($"{request.EntityType}Id", entityId.ToString());
        }

        foreach (var (key, value) in request.ExtraData ?? new Dictionary<string, object?>())
        {
            if (value is not null) values.TryAdd(key, value.ToString());
        }

        var missing = new HashSet<string>(StringComparer.Ordinal);
        var deepLink = definition.DeepLink is null ? null : TemplateRenderer.Render(definition.DeepLink, values, "ar", missing);
        var notificationId = Guid.CreateVersion7();
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["eventCode"] = definition.Code,
            ["notificationId"] = notificationId,
            ["deepLink"] = deepLink,
            ["entityType"] = request.EntityType,
            ["entityId"] = request.EntityId,
        };
        foreach (var (key, value) in request.ExtraData ?? new Dictionary<string, object?>())
        {
            data[key] = value;
        }

        if (request.IsTest) data["test"] = true;

        Notification? notification = null;
        var deliveries = new List<NotificationDelivery>();
        var active = user is { Status: UserStatus.Active };
        var preferenceOn = preference is null || NotificationEvents.PreferenceOf(definition.Category) is not { } flag || flag(preference);

        if (Text(NotificationChannel.Inapp) is { } inapp && definition.CreatesInbox && user is not null)
        {
            notification = new Notification
            {
                Id = notificationId,
                UserId = user.Id,
                Type = definition.Code,
                Category = definition.Category,
                CampaignId = request.CampaignId,
                TitleAr = Truncate(TemplateRenderer.Render(inapp.TitleAr, values, "ar", missing), 200),
                TitleEn = Truncate(TemplateRenderer.Render(inapp.TitleEn, values, "en", missing), 200),
                BodyAr = Truncate(TemplateRenderer.Render(inapp.BodyAr, values, "ar", missing), 1000),
                BodyEn = Truncate(TemplateRenderer.Render(inapp.BodyEn, values, "en", missing), 1000),
                Data = JsonSerializer.Serialize(data, JsonDefaults.Options),
                CreatedAt = clock.UtcNow,
            };
            db.Notifications.Add(notification);
        }
        else
        {
            data["notificationId"] = null;
        }

        if (Text(NotificationChannel.Push) is { } pushText && user is not null)
        {
            var payload = new DeliveryPayload(
                new Dictionary<string, string> { ["ar"] = TemplateRenderer.Render(pushText.TitleAr, values, "ar", missing), ["en"] = TemplateRenderer.Render(pushText.TitleEn, values, "en", missing) },
                new Dictionary<string, string> { ["ar"] = TemplateRenderer.Render(pushText.BodyAr, values, "ar", missing), ["en"] = TemplateRenderer.Render(pushText.BodyEn, values, "en", missing) },
                data,
                JsonNamingPolicy.SnakeCaseLower.ConvertName(definition.Category.ToString()),
                definition.Priority,
                request.ExtraData?.GetValueOrDefault("ttlSeconds") is int ttl ? ttl : definition.TtlSeconds,
                request.ExtraData?.GetValueOrDefault("collapseId")?.ToString() ?? (request.EntityType == "trip" && request.EntityId is { } tripId ? $"trip-{tripId}" : null),
                definition.Buttons?.Select(b => new PushButton(b.Id, b.TextAr, b.TextEn)).ToList(),
                null, null, null);
            var delivery = NewDelivery(definition.Code, NotificationChannel.Push, push.Provider, payload, request, notification, user.Id, null);
            if (!active && definition.Category != NotificationCategory.System) delivery.Skip(DeliverySkipReasons.UserInactive);
            else if (!preferenceOn && !definition.IsCritical) delivery.Skip(DeliverySkipReasons.PreferenceOff);
            deliveries.Add(delivery);
        }

        if (Text(NotificationChannel.Sms) is { } smsText)
        {
            var phone = request.RecipientPhoneOverride ?? user?.PhoneNumber;
            var language = user?.Language == Language.En ? "en" : "ar";
            var body = TemplateRenderer.Render(language == "en" ? smsText.BodyEn : smsText.BodyAr, values, language, missing);
            var payload = new DeliveryPayload(null, null, null, null, null, null, null, null, phone, body, language);
            var delivery = NewDelivery(definition.Code, NotificationChannel.Sms, sms.Provider, payload, request, notification, user?.Id, phone);
            if (string.IsNullOrWhiteSpace(phone)) delivery.Skip(DeliverySkipReasons.NoPhone);
            else if (user is not null && !active && definition.Category != NotificationCategory.System) delivery.Skip(DeliverySkipReasons.UserInactive);
            else if (user is not null && !preferenceOn && !definition.IsCritical) delivery.Skip(DeliverySkipReasons.PreferenceOff);
            deliveries.Add(delivery);
        }

        if (missing.Count > 0)
        {
            logger.LogWarning("Notification {EventCode}: missing placeholders {Placeholders}", definition.Code, string.Join(", ", missing));
        }

        db.NotificationDeliveries.AddRange(deliveries);
        outbox.Add(notification, deliveries);
        return new DispatchResult(notification, deliveries);

        // Channel text: the active template row, or the code default when the event enables the channel and no row exists.
        NotificationText? Text(NotificationChannel channel)
        {
            if (!definition.AllowedChannels.Contains(channel)) return null;
            var row = rows.FirstOrDefault(t => t.Channel == channel);
            if (row is not null)
            {
                return row.IsActive ? new NotificationText(row.TitleAr ?? string.Empty, row.TitleEn ?? string.Empty, row.BodyAr, row.BodyEn) : null;
            }

            var enabled = definition.DefaultChannels.Contains(channel) || request.ExtraChannels?.Contains(channel) == true;
            return enabled ? definition.Text : null;
        }
    }

    private NotificationDelivery NewDelivery(string code, NotificationChannel channel, string provider, DeliveryPayload payload, NotificationRequest request,
        Notification? notification, Guid? userId, string? phone) => new()
    {
        NotificationId = notification?.Id,
        UserId = userId,
        PhoneNumber = phone,
        EventCode = code,
        Channel = channel,
        CampaignId = request.CampaignId,
        Provider = provider,
        Payload = JsonSerializer.Serialize(payload, JsonDefaults.Options),
        CreatedAt = clock.UtcNow,
    };

    private static string Truncate(string value, int max) => value.Length > max ? value[..max] : value;
}
