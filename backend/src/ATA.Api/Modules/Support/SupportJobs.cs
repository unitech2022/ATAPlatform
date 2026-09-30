using System.Collections.Concurrent;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Support;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Support;

/// <summary>The SLA state last broadcast per ticket, so <c>SupportSlaMonitorJob</c> only pushes transitions.</summary>
public sealed class SupportSlaBroadcastState
{
    private readonly ConcurrentDictionary<Guid, SlaState> _states = new();

    /// <summary>Records the state and reports whether it differs from the last one seen (a ticket unseen before is assumed <c>ok</c>).</summary>
    public bool Changed(Guid ticketId, SlaState state)
    {
        var previous = _states.GetOrAdd(ticketId, SlaState.Ok);
        _states[ticketId] = state;
        return previous != state;
    }

    public void Forget(IEnumerable<Guid> ticketIds)
    {
        foreach (var id in ticketIds)
        {
            _states.TryRemove(id, out _);
        }
    }

    public IReadOnlyCollection<Guid> Known => _states.Keys.ToList();
}

/// <summary>The work of the two support jobs (doc 11 §F18.4), separated from the hosted services so tests can call it directly.</summary>
public sealed class SupportMaintenance(
    AtaDbContext db,
    IClock clock,
    INotificationDispatcher notifications,
    ISupportNotifier realtime,
    SupportSlaBroadcastState broadcast,
    IOptions<SupportOptions> options)
{
    private const int BatchSize = 200;

    /// <summary><c>SupportAutoCloseJob</c>: a <c>resolved</c> ticket without a reply for <c>Support:AutoCloseDays</c> becomes <c>closed</c>; the requester gets <c>support.status</c>.</summary>
    public async Task<int> AutoCloseAsync(CancellationToken ct)
    {
        var cutoff = clock.UtcNow.AddDays(-options.Value.AutoCloseDays);
        var due = await db.SupportTickets.Where(t => t.Status == SupportTicketStatus.Resolved && t.ResolvedAt != null && t.ResolvedAt <= cutoff)
            .OrderBy(t => t.ResolvedAt).Take(BatchSize).ToListAsync(ct);
        foreach (var ticket in due)
        {
            ticket.Status = SupportTicketStatus.Closed;
            ticket.ClosedAt = clock.UtcNow;
            var (ar, en) = SupportLabels.Status(SupportTicketStatus.Closed);
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SupportStatus, ticket.RequesterUserId,
                NotificationPlaceholders.Of(("ticketNumber", ticket.TicketNumber)).Localized("status", ar, en), "ticket", ticket.Id,
                new Dictionary<string, object?> { ["ticketId"] = ticket.Id, ["status"] = "closed" }), ct);
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            foreach (var ticket in due)
            {
                await realtime.TicketUpdatedForUserAsync(ticket.RequesterUserId, new SupportTicketUserEvent(ticket.Id, ticket.Status, ticket.LastMessageAt, ticket.UnreadByUser), ct);
                await realtime.TicketUpdatedForAdminsAsync(new SupportTicketAdminEvent(ticket.Id, ticket.Status, ticket.Priority, ticket.LastMessageBy), ct);
            }

            broadcast.Forget(due.Select(t => t.Id));
        }

        return due.Count;
    }

    /// <summary>
    /// <c>SupportSlaMonitorJob</c>: pushes <c>SupportTicketUpdated</c> to the admins when an active ticket's <c>slaState</c> changed since the last pass so the queue can recolour
    /// (<c>ok → due_soon → breached</c>); tickets that left the queue are forgotten.
    /// </summary>
    public async Task<int> MonitorSlaAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var active = await db.SupportTickets.AsNoTracking().Where(t => t.Status != SupportTicketStatus.Resolved && t.Status != SupportTicketStatus.Closed).ToListAsync(ct);
        var changed = 0;
        foreach (var ticket in active)
        {
            var state = ticket.SlaStateAt(now);
            if (broadcast.Changed(ticket.Id, state))
            {
                changed++;
                await realtime.TicketUpdatedForAdminsAsync(new SupportTicketAdminEvent(ticket.Id, ticket.Status, ticket.Priority, ticket.LastMessageBy, state), ct);
            }
        }

        var activeIds = active.Select(t => t.Id).ToHashSet();
        broadcast.Forget(broadcast.Known.Where(id => !activeIds.Contains(id)));
        return changed;
    }
}

/// <summary><c>SupportAutoCloseJob</c> (hourly): closes resolved tickets nobody answered; off with <c>Support:JobsEnabled=false</c>.</summary>
public sealed class SupportAutoCloseJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<SupportOptions> options, ILogger<SupportAutoCloseJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.Value.AutoCloseIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Support auto-close job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:support_auto_close", TimeSpan.FromMinutes(10), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SupportMaintenance>().AutoCloseAsync(ct);
    }
}

/// <summary><c>SupportSlaMonitorJob</c> (every 5 minutes): broadcasts SLA state transitions to the admins group; off with <c>Support:JobsEnabled=false</c>.</summary>
public sealed class SupportSlaMonitorJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<SupportOptions> options, ILogger<SupportSlaMonitorJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.Value.SlaMonitorIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Support SLA monitor job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:support_sla_monitor", TimeSpan.FromMinutes(5), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SupportMaintenance>().MonitorSlaAsync(ct);
    }
}
