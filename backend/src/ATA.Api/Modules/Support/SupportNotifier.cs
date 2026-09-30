using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Support;
using Microsoft.AspNetCore.SignalR;

namespace ATA.Api.Modules.Support;

/// <summary>SignalR events of F18 on <c>/hubs/trips</c> (doc 11 §F18.3 "SignalR").</summary>
public interface ISupportNotifier
{
    /// <summary><c>SupportTicketUpdated({ ticketId, status, lastMessageAt, unread })</c> to the requester.</summary>
    Task TicketUpdatedForUserAsync(Guid userId, SupportTicketUserEvent update, CancellationToken ct);

    /// <summary><c>SupportTicketUpdated({ ticketId, status, priority, lastMessageBy })</c> to the <c>admins</c> group.</summary>
    Task TicketUpdatedForAdminsAsync(SupportTicketAdminEvent update, CancellationToken ct);

    /// <summary><c>SupportTicketCreated(ticketSummary)</c> to the <c>admins</c> group.</summary>
    Task TicketCreatedForAdminsAsync(AdminTicketListItemDto ticket, CancellationToken ct);
}

public static class SupportHubEvents
{
    public const string SupportTicketUpdated = "SupportTicketUpdated";
    public const string SupportTicketCreated = "SupportTicketCreated";
}

public sealed class SignalRSupportNotifier(IHubContext<TripsHub> hub, ILogger<SignalRSupportNotifier> logger) : ISupportNotifier
{
    public Task TicketUpdatedForUserAsync(Guid userId, SupportTicketUserEvent update, CancellationToken ct) =>
        SendAsync(TripsHub.UserGroup(userId), SupportHubEvents.SupportTicketUpdated, update, ct);

    public Task TicketUpdatedForAdminsAsync(SupportTicketAdminEvent update, CancellationToken ct) =>
        SendAsync(TripsHub.AdminsGroup, SupportHubEvents.SupportTicketUpdated, update, ct);

    public Task TicketCreatedForAdminsAsync(AdminTicketListItemDto ticket, CancellationToken ct) =>
        SendAsync(TripsHub.AdminsGroup, SupportHubEvents.SupportTicketCreated, ticket, ct);

    private async Task SendAsync(string group, string method, object payload, CancellationToken ct)
    {
        try
        {
            await hub.Clients.Group(group).SendAsync(method, payload, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "SignalR {Method} to {Group} failed", method, group);
        }
    }
}

/// <summary>Arabic / English labels used in notification texts and system messages.</summary>
public static class SupportLabels
{
    public const string TeamNameAr = "فريق دعم ATA";
    public const string TeamNameEn = "ATA Support";

    public static (string Ar, string En) Status(SupportTicketStatus status) => status switch
    {
        SupportTicketStatus.Open => ("مفتوحة", "open"),
        SupportTicketStatus.PendingUser => ("بانتظار ردك", "waiting for your reply"),
        SupportTicketStatus.InProgress => ("قيد المعالجة", "in progress"),
        SupportTicketStatus.Resolved => ("تم الحل", "resolved"),
        _ => ("مغلقة", "closed"),
    };

    public static (string Ar, string En) Dispute(DisputeStatus status) => status switch
    {
        DisputeStatus.Approved => ("تمت الموافقة على الاسترداد", "refund approved"),
        DisputeStatus.PartiallyApproved => ("تمت الموافقة على استرداد جزئي", "partial refund approved"),
        DisputeStatus.Rejected => ("تم رفض الاعتراض", "dispute rejected"),
        DisputeStatus.UnderReview => ("قيد المراجعة", "under review"),
        _ => ("مفتوح", "open"),
    };

    public static string Team(Language language) => language == Language.En ? TeamNameEn : TeamNameAr;

    /// <summary>A short plain-text preview for push notifications.</summary>
    public static string Preview(string body, int max = 120)
    {
        var flat = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return flat.Length <= max ? flat : flat[..max].TrimEnd() + "…";
    }
}
