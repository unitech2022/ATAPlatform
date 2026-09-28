using System.Text.Json;
using ATA.Api.Common;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Notifications;

public sealed record NotificationDto(Guid Id, string Type, string Title, string Body, JsonElement? Data, DateTime? ReadAt, DateTime CreatedAt);

public sealed record NotificationsPage(IReadOnlyList<NotificationDto> Items, int Page, int PageSize, int Total, int UnreadCount);

public sealed record MarkReadRequest(Guid[]? Ids);

public static class NotificationEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/notifications").WithTags("Notifications").RequireAuthorization(Policies.Authenticated);

        group.MapGet("/", async (int? page, int? pageSize, AtaDbContext db, ICurrentUser user, HttpContext http, CancellationToken ct) =>
            {
                var paging = Paging.From(page, pageSize);
                var lang = http.GetLanguage();
                var userId = user.UserId;
                var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
                var total = await query.CountAsync(ct);
                var unread = await query.CountAsync(n => n.ReadAt == null, ct);
                var items = await query.OrderByDescending(n => n.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
                var dtos = items.Select(n => new NotificationDto(
                    n.Id, n.Type, lang.Pick(n.TitleAr, n.TitleEn), lang.Pick(n.BodyAr, n.BodyEn),
                    n.Data is null ? null : JsonSerializer.Deserialize<JsonElement>(n.Data), n.ReadAt, n.CreatedAt)).ToList();
                return Results.Ok(new NotificationsPage(dtos, paging.Page, paging.PageSize, total, unread));
            })
            .Produces<NotificationsPage>();

        group.MapPost("/read", async (MarkReadRequest? request, AtaDbContext db, ICurrentUser user, ATA.Domain.Common.IClock clock, CancellationToken ct) =>
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
    }
}
