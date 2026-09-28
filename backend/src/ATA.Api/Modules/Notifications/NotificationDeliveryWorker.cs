using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Push;
using ATA.Infrastructure.Sms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Notifications;

/// <summary>
/// Sends queued push/SMS deliveries. A transient error marks the delivery <c>failed</c> with
/// <c>next_attempt_at = now + 30 s × 2^(attempts − 1)</c> until <c>Notifications:MaxAttempts</c>; a permanent error fails it for good;
/// users without an active push subscription (OneSignal <c>invalid_aliases</c>) are <c>skipped (no_subscription)</c>.
/// </summary>
public sealed class NotificationDeliveryProcessor(AtaDbContext db, IPushSender push, ISmsSender sms, IClock clock, IOptions<NotificationsOptions> options)
{
    private const int BatchSize = 100;
    private readonly NotificationsOptions _options = options.Value;

    /// <summary>Processes every due delivery (queued, or failed with a due retry). Returns the number processed.</summary>
    public async Task<int> ProcessDueAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var due = await db.NotificationDeliveries
            .Where(d => (d.Status == DeliveryStatus.Queued && (d.NextAttemptAt == null || d.NextAttemptAt <= now))
                        || (d.Status == DeliveryStatus.Failed && d.NextAttemptAt != null && d.NextAttemptAt <= now))
            .OrderBy(d => d.CreatedAt).Take(BatchSize).ToListAsync(ct);
        foreach (var delivery in due)
        {
            await SendAsync(delivery, ct);
        }

        return due.Count;
    }

    public async Task<bool> ProcessAsync(Guid deliveryId, CancellationToken ct)
    {
        var delivery = await db.NotificationDeliveries.FirstOrDefaultAsync(d => d.Id == deliveryId, ct);
        if (delivery is null || delivery.Status != DeliveryStatus.Queued)
        {
            return false;
        }

        await SendAsync(delivery, ct);
        return true;
    }

    private async Task SendAsync(NotificationDelivery delivery, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<DeliveryPayload>(delivery.Payload, JsonDefaults.Options)!;
        delivery.Attempts++;
        var now = clock.UtcNow;
        if (delivery.Channel == NotificationChannel.Push)
        {
            var message = new PushMessage(
                [delivery.UserId!.Value],
                payload.Headings ?? [],
                payload.Contents ?? [],
                (payload.Data ?? []).ToDictionary(kv => kv.Key, kv => kv.Value is JsonElement e ? Unwrap(e) : kv.Value),
                payload.Category ?? "system",
                payload.Priority ?? "normal",
                payload.TtlSeconds,
                payload.CollapseId,
                payload.Buttons,
                delivery.Id.ToString());
            var result = await push.SendAsync(message, ct);
            if (result.Success && result.InvalidExternalUserIds.Contains(delivery.UserId!.Value))
            {
                delivery.Skip(DeliverySkipReasons.NoSubscription);
            }
            else
            {
                Apply(delivery, result.Success, result.ProviderMessageId, result.ErrorCode, result.ErrorMessage, result.IsTransient, now);
            }
        }
        else
        {
            var result = await sms.SendAsync(payload.Phone ?? delivery.PhoneNumber ?? string.Empty, payload.Body ?? string.Empty, ct);
            Apply(delivery, result.Success, result.ProviderMessageId, result.ErrorCode, result.ErrorMessage, result.IsTransient, now);
        }

        await db.SaveChangesAsync(ct);
    }

    private void Apply(NotificationDelivery delivery, bool success, string? messageId, string? errorCode, string? errorMessage, bool transient, DateTime now)
    {
        if (success)
        {
            delivery.Status = DeliveryStatus.Sent;
            delivery.ProviderMessageId = messageId;
            delivery.SentAt = now;
            delivery.NextAttemptAt = null;
            delivery.ErrorCode = null;
            delivery.ErrorMessage = null;
            return;
        }

        delivery.Status = DeliveryStatus.Failed;
        delivery.ErrorCode = errorCode;
        delivery.ErrorMessage = errorMessage is { Length: > 500 } ? errorMessage[..500] : errorMessage;
        delivery.NextAttemptAt = transient && delivery.Attempts < _options.MaxAttempts
            ? now.AddSeconds(30 * Math.Pow(2, delivery.Attempts - 1))
            : null;
    }

    private static object? Unwrap(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDecimal(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => element,
    };
}

/// <summary>
/// Delivers notifications immediately from <see cref="DeliveryQueue"/> (so <c>offer.received</c> goes out in well under a second) and polls
/// every <c>Notifications:WorkerPollSeconds</c> for queued rows and due retries. Disabled with <c>Notifications:WorkerEnabled=false</c>.
/// </summary>
public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopes, DeliveryQueue queue, IDistributedLock locks, IOptions<NotificationsOptions> options, ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private readonly NotificationsOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.WorkerEnabled)
        {
            return;
        }

        var poll = TimeSpan.FromSeconds(Math.Max(1, _options.WorkerPollSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                wait.CancelAfter(poll);
                try
                {
                    var id = await queue.Reader.ReadAsync(wait.Token);
                    await ProcessOneAsync(id, stoppingToken);
                    continue;
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Poll interval elapsed without queued ids.
                }

                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification delivery pass failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:notification_delivery", TimeSpan.FromMinutes(1), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationDeliveryProcessor>().ProcessDueAsync(ct);
    }

    private async Task ProcessOneAsync(Guid id, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<NotificationDeliveryProcessor>().ProcessAsync(id, ct);
    }
}
