using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Safety;

public sealed record QuickReply(TripMessageSender Role, string Code, string Ar, string En);

/// <summary>Quick replies of the in-trip chat (catalogue in code, doc 09 §F12.4).</summary>
public static class QuickReplies
{
    public static readonly IReadOnlyList<QuickReply> All =
    [
        new(TripMessageSender.Driver, "on_my_way", "أنا في الطريق إليك", "I'm on my way"),
        new(TripMessageSender.Driver, "arrived", "وصلت إلى نقطة الالتقاط", "I've arrived at the pickup"),
        new(TripMessageSender.Driver, "cant_find_you", "لا أستطيع إيجادك، أين أنت؟", "I can't find you, where are you?"),
        new(TripMessageSender.Driver, "traffic_delay", "تأخرت قليلاً بسبب الزحام", "Running a bit late due to traffic"),
        new(TripMessageSender.Passenger, "coming_now", "قادم الآن", "Coming now"),
        new(TripMessageSender.Passenger, "wait_please", "انتظرني دقيقتين من فضلك", "Please wait two minutes"),
        new(TripMessageSender.Passenger, "at_pickup", "أنا في نقطة الالتقاط", "I'm at the pickup point"),
        new(TripMessageSender.Passenger, "where_are_you", "أين أنت؟", "Where are you?"),
    ];

    public static QuickReply? Find(TripMessageSender role, string code) => All.FirstOrDefault(q => q.Role == role && q.Code == code);

    public static QuickReply? Find(string code) => All.FirstOrDefault(q => q.Code == code);
}

/// <summary>Masked calls between the trip parties; the real number of the other party is never returned.</summary>
public interface ICallMaskingProvider
{
    Task<MaskedCallDto> CreateSessionAsync(Trip trip, TripMessageSender caller, CancellationToken ct);
}

/// <summary>Default provider (<c>CallMasking:Provider = none</c>): calls are unavailable and the app opens the chat instead.</summary>
public sealed class NoneCallMaskingProvider : ICallMaskingProvider
{
    public Task<MaskedCallDto> CreateSessionAsync(Trip trip, TripMessageSender caller, CancellationToken ct) =>
        Task.FromResult(new MaskedCallDto("unavailable", false, null, null, null));
}

/// <summary>In-trip chat (doc 09 §F12.4): open from <c>driver_assigned</c> to <c>in_trip</c>, read-only afterwards, phone numbers masked.</summary>
public sealed partial class TripChatService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    INotificationDispatcher notifications,
    ISafetyNotifier realtime,
    ICallMaskingProvider calls,
    AuditService audit,
    IOptions<SafetyOptions> options)
{
    public const int MaxMessages = 500;
    public const string Mask = "••••";

    public static readonly TripStatus[] OpenStatuses =
        [TripStatus.DriverAssigned, TripStatus.DriverEnRoute, TripStatus.DriverArrived, TripStatus.Waiting, TripStatus.PinVerified, TripStatus.InTrip];

    [GeneratedRegex(@"\+?[0-9٠-٩۰-۹](?:[\s\-.]*[0-9٠-٩۰-۹]){7,}")]
    private static partial Regex PhoneLike();

    /// <summary>Replaces every run of 8 or more digits (separators allowed) with <see cref="Mask"/>.</summary>
    public static string MaskNumbers(string text) => PhoneLike().Replace(text, Mask);

    public async Task<IReadOnlyList<TripMessageDto>> ListAsync(Guid tripId, TripMessageSender role, Guid? after, Language lang, CancellationToken ct)
    {
        var (trip, _) = await LoadPartyAsync(tripId, role, ct);
        var userId = currentUser.UserId;
        var rows = await QueryAfterAsync(trip.Id, after, ct);
        return rows.Select(m => ToDto(m, userId, lang)).ToList();
    }

    public async Task<TripMessageDto> SendAsync(Guid tripId, TripMessageSender role, SendMessageRequest request, Language lang, CancellationToken ct)
    {
        var hasBody = !string.IsNullOrWhiteSpace(request.Body);
        var hasCode = !string.IsNullOrWhiteSpace(request.QuickReplyCode);
        new Validator()
            .Rule("body", hasBody ^ hasCode, "exactly one of body or quickReplyCode is required")
            .Rule("body", request.Body is null || request.Body.Trim().Length <= TripMessage.MaxBodyLength, $"max_length:{TripMessage.MaxBodyLength}")
            .ThrowIfInvalid();

        var (trip, otherUserId) = await LoadPartyAsync(tripId, role, ct);
        if (!OpenStatuses.Contains(trip.Status))
        {
            throw new DomainException(ErrorCodes.ChatClosed, new { status = trip.Status });
        }

        QuickReply? quick = null;
        if (hasCode)
        {
            quick = QuickReplies.Find(role, request.QuickReplyCode!.Trim());
            new Validator().Rule("quickReplyCode", quick is not null, "unknown quick reply for this role").ThrowIfInvalid();
        }

        var body = quick?.Ar ?? request.Body!.Trim();
        if (quick is null && options.Value.ChatMaskPhoneNumbers)
        {
            body = MaskNumbers(body);
        }

        var userId = currentUser.UserId;
        var message = new TripMessage
        {
            TripId = trip.Id, SenderUserId = userId, SenderRole = role, Kind = quick is null ? TripMessageKind.Text : TripMessageKind.QuickReply,
            Body = body, QuickReplyCode = quick?.Code, CreatedAt = clock.UtcNow,
        };
        db.TripMessages.Add(message);
        if (otherUserId is { } recipient)
        {
            var senderName = SafetyLabels.FirstName(await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct));
            var preview = quick is null ? (body.Length > 80 ? body[..80] + "…" : body) : quick.Ar;
            var values = NotificationPlaceholders.Of(("senderName", senderName ?? (role == TripMessageSender.Driver ? "الكابتن" : "الراكب")));
            values.Localized("preview", preview, quick?.En ?? preview);
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.TripMessage, recipient, values, "trip", trip.Id,
                new Dictionary<string, object?> { ["tripId"] = trip.Id, ["messageId"] = message.Id, ["collapseId"] = $"chat-{trip.Id}" }), ct);
        }

        await db.SaveChangesAsync(ct);
        if (otherUserId is { } other)
        {
            var recipientLanguage = await db.Users.AsNoTracking().Where(u => u.Id == other).Select(u => u.Language).FirstOrDefaultAsync(ct);
            await realtime.TripMessageAsync(other, ToDto(message, other, recipientLanguage), ct);
        }

        return ToDto(message, userId, lang);
    }

    public async Task MarkReadAsync(Guid tripId, TripMessageSender role, MarkMessagesReadRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.UpToId), request.UpToId).ThrowIfInvalid();
        var (trip, otherUserId) = await LoadPartyAsync(tripId, role, ct);
        var upTo = Guard.NotFound(await db.TripMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.UpToId && m.TripId == trip.Id, ct));
        var userId = currentUser.UserId;
        var unread = await db.TripMessages
            .Where(m => m.TripId == trip.Id && m.ReadAt == null && m.SenderUserId != userId && m.CreatedAt <= upTo.CreatedAt)
            .ToListAsync(ct);
        var now = clock.UtcNow;
        foreach (var message in unread)
        {
            message.ReadAt = now;
        }

        await db.SaveChangesAsync(ct);
        if (otherUserId is { } other)
        {
            await realtime.TripMessagesReadAsync(other, new TripMessagesReadEvent(trip.Id, upTo.Id), ct);
        }
    }

    public async Task<MaskedCallDto> CallAsync(Guid tripId, TripMessageSender role, CancellationToken ct)
    {
        var (trip, _) = await LoadPartyAsync(tripId, role, ct);
        if (!OpenStatuses.Contains(trip.Status))
        {
            throw new DomainException(ErrorCodes.ChatClosed, new { status = trip.Status });
        }

        return await calls.CreateSessionAsync(trip, role, ct);
    }

    /// <summary><c>GET /admin/trips/{id}/messages</c>: every read is audited as <c>trip_messages.view</c>.</summary>
    public async Task<IReadOnlyList<AdminTripMessageDto>> ListForAdminAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var trip = Guard.NotFound(await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct));
        var rows = await QueryAfterAsync(trip.Id, null, ct);
        var senderIds = rows.Where(m => m.SenderUserId != null).Select(m => m.SenderUserId!.Value).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => senderIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        audit.Log("trip_messages.view", "trip", trip.Id, null, new { messages = rows.Count });
        await db.SaveChangesAsync(ct);
        return rows.Select(m => new AdminTripMessageDto(m.Id, m.TripId, m.SenderRole, m.SenderUserId is { } s ? names.GetValueOrDefault(s) : null, m.Kind,
            LocalizedBody(m, lang), m.QuickReplyCode, m.ReadAt, m.CreatedAt)).ToList();
    }

    private async Task<List<TripMessage>> QueryAfterAsync(Guid tripId, Guid? after, CancellationToken ct)
    {
        var query = db.TripMessages.AsNoTracking().Where(m => m.TripId == tripId);
        if (after is { } afterId)
        {
            var anchor = await db.TripMessages.AsNoTracking().Where(m => m.Id == afterId && m.TripId == tripId).Select(m => (DateTime?)m.CreatedAt).FirstOrDefaultAsync(ct);
            if (anchor is { } at)
            {
                query = query.Where(m => m.CreatedAt > at || (m.CreatedAt == at && m.Id != afterId));
            }
        }

        return (await query.OrderBy(m => m.CreatedAt).Take(MaxMessages).ToListAsync(ct)).OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).ToList();
    }

    private static string LocalizedBody(TripMessage m, Language lang) =>
        m.Kind == TripMessageKind.QuickReply && m.QuickReplyCode is { } code && QuickReplies.Find(m.SenderRole, code) is { } q ? lang.Pick(q.Ar, q.En) : m.Body;

    private static TripMessageDto ToDto(TripMessage m, Guid viewerUserId, Language lang) =>
        new(m.Id, m.TripId, m.SenderRole, m.Kind, LocalizedBody(m, lang), m.QuickReplyCode, m.SenderUserId == viewerUserId, m.ReadAt, m.CreatedAt);

    /// <summary>The trip must exist (404) and the caller must be its passenger / driver for the route's role (403 otherwise).</summary>
    private async Task<(Trip Trip, Guid? OtherUserId)> LoadPartyAsync(Guid tripId, TripMessageSender role, CancellationToken ct)
    {
        var trip = Guard.NotFound(await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct));
        var userId = currentUser.UserId;
        var passengerUserId = await db.Passengers.AsNoTracking().Where(p => p.Id == trip.PassengerId).Select(p => p.UserId).FirstAsync(ct);
        Guid? driverUserId = trip.DriverId is { } driverId && trip.HasDriver
            ? await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => (Guid?)d.UserId).FirstOrDefaultAsync(ct)
            : null;
        var isParty = role == TripMessageSender.Passenger ? passengerUserId == userId : driverUserId == userId;
        if (!isParty)
        {
            throw new DomainException(ErrorCodes.Forbidden);
        }

        return (trip, role == TripMessageSender.Passenger ? driverUserId : passengerUserId);
    }
}
