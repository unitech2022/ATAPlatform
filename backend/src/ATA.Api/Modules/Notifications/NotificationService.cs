using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;

namespace ATA.Api.Modules.Notifications;

/// <summary>Creates in-app notification rows (push delivery is out of scope for step 1).</summary>
public sealed class NotificationService(AtaDbContext db)
{
    public Notification Add(Guid userId, string type, (string Ar, string En) title, (string Ar, string En) body, object? data = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            TitleAr = title.Ar,
            TitleEn = title.En,
            BodyAr = body.Ar,
            BodyEn = body.En,
            Data = data is null ? null : JsonSerializer.Serialize(data, JsonDefaults.Options),
        };
        db.Notifications.Add(notification);
        return notification;
    }
}
