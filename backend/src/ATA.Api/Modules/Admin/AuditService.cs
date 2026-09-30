using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Admin;
using ATA.Infrastructure.Persistence;

namespace ATA.Api.Modules.Admin;

/// <summary>Records sensitive administrative actions in <c>audit_logs</c>; the caller saves within its own unit of work.</summary>
public sealed class AuditService(AtaDbContext db, ICurrentUser currentUser)
{
    /// <param name="actorRole">Overrides the caller's first role (<c>corporate_admin</c> for portal actions, <c>system</c> for jobs).</param>
    /// <param name="actorUserId">Overrides the caller (F20 security events of the anonymous login endpoints name the admin who signed in).</param>
    public AuditLog Log(string action, string entityType, Guid? entityId, object? before = null, object? after = null, string? actorRole = null, Guid? actorUserId = null)
    {
        var log = new AuditLog
        {
            ActorUserId = actorUserId ?? (currentUser.IsAuthenticated ? currentUser.UserId : null),
            ActorRole = actorRole ?? currentUser.Roles.FirstOrDefault(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonDefaults.Options),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonDefaults.Options),
            IpAddress = currentUser.IpAddress,
        };
        db.AuditLogs.Add(log);
        return log;
    }
}
