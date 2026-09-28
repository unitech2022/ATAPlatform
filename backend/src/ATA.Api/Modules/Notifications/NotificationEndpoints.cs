using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Notifications;

public static class NotificationEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/notifications").WithTags("Notifications").RequireAuthorization(Policies.Authenticated);

        group.MapGet("/", async (int? page, int? pageSize, string? category, AtaDbContext db, ICurrentUser user, HttpContext http, CancellationToken ct) =>
            {
                var paging = Paging.From(page, pageSize);
                var lang = http.GetLanguage();
                var userId = user.UserId;
                var filter = QueryEnum.Parse<NotificationCategory>(category, "category");
                var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
                var unread = await query.CountAsync(n => n.ReadAt == null, ct);
                if (filter is not null) query = query.Where(n => n.Category == filter);
                var total = await query.CountAsync(ct);
                var items = await query.OrderByDescending(n => n.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
                var dtos = items.Select(n => new NotificationDto(
                    n.Id, NotificationEvents.NormalizeLegacy(n.Type), n.Category, lang.Pick(n.TitleAr, n.TitleEn), lang.Pick(n.BodyAr, n.BodyEn),
                    n.Data is null ? null : JsonSerializer.Deserialize<JsonElement>(n.Data), n.ReadAt, n.CreatedAt)).ToList();
                return Results.Ok(new NotificationsPage(dtos, paging.Page, paging.PageSize, total, unread));
            })
            .Produces<NotificationsPage>();

        group.MapGet("/unread-count", async (AtaDbContext db, ICurrentUser user, CancellationToken ct) =>
            {
                var userId = user.UserId;
                return Results.Ok(new UnreadCountDto(await db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, ct)));
            })
            .Produces<UnreadCountDto>();

        group.MapPost("/read", async (MarkReadRequest? request, AtaDbContext db, ICurrentUser user, IClock clock, CancellationToken ct) =>
            {
                var userId = user.UserId;
                var now = clock.UtcNow;
                var query = db.Notifications.Where(n => n.UserId == userId && n.ReadAt == null);
                if (request?.Ids is { Length: > 0 } ids)
                {
                    query = query.Where(n => ids.Contains(n.Id));
                }

                await query.ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now), ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        // Tap tracking from the push/inbox: marks the row read, the deliveries opened and counts the first open of a campaign.
        group.MapPost("/{id:guid}/opened", async (Guid id, AtaDbContext db, ICurrentUser user, IClock clock, CancellationToken ct) =>
            {
                var userId = user.UserId;
                var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
                var now = clock.UtcNow;
                var wasUnread = notification.ReadAt is null;
                notification.ReadAt ??= now;
                var deliveries = await db.NotificationDeliveries.Where(d => d.NotificationId == id && d.OpenedAt == null).ToListAsync(ct);
                var anyDelivery = await db.NotificationDeliveries.AnyAsync(d => d.NotificationId == id, ct);
                var firstOpen = anyDelivery ? !await db.NotificationDeliveries.AnyAsync(d => d.NotificationId == id && d.OpenedAt != null, ct) : wasUnread;
                foreach (var delivery in deliveries)
                {
                    delivery.OpenedAt = now;
                }

                if (firstOpen && notification.CampaignId is { } campaignId)
                {
                    await db.NotificationCampaigns.Where(c => c.Id == campaignId).ExecuteUpdateAsync(s => s.SetProperty(c => c.OpenedCount, c => c.OpenedCount + 1), ct);
                }

                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
    }
}
