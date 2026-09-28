using ATA.Api.Common;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

/// <summary>One pass of each payment job (doc 08 §F11.8); tests call these directly.</summary>
public sealed class PaymentJobs(
    AtaDbContext db,
    PaymentService payments,
    CardTripPaymentService cardPayments,
    RefundService refunds,
    SettlementService settlements,
    IPaymentGatewayResolver gateways,
    IClock clock,
    IOptions<SettlementsOptions> settlementOptions,
    ILogger<PaymentJobs> logger)
{
    public const int WebhookMaxAttempts = 10;

    /// <summary><c>PaymentActionExpiryJob</c>: initiated payments past <c>action_expires_at</c> fail (their trip is cancelled with <c>payment_failed</c>).</summary>
    public async Task<int> ExpireActionsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var expired = await db.Payments.Where(p => p.Status == PaymentStatus.Initiated && p.ActionExpiresAt != null && p.ActionExpiresAt <= now).ToListAsync(ct);
        foreach (var payment in expired)
        {
            await db.InTransactionAsync(async () =>
            {
                await payments.TransitionAsync(payment, GatewayStatus.Failed, null, "action_expired", "The payment verification was not completed in time", ct);
                await db.SaveChangesAsync(ct);
            }, ct);
        }

        await payments.PublishPendingAsync(ct);
        return expired.Count;
    }

    public Task<int> RetryCapturesAsync(CancellationToken ct) => cardPayments.RetryPendingCapturesAsync(ct);

    public Task<int> ReconcileAuthorizationsAsync(CancellationToken ct) => cardPayments.ReconcileAuthorizationsAsync(ct);

    /// <summary><c>PaymentWebhookRetryJob</c>: re-processes pending/failed events older than a minute, up to 10 attempts.</summary>
    public async Task<int> RetryWebhooksAsync(CancellationToken ct)
    {
        var before = clock.UtcNow.AddMinutes(-1);
        var events = await db.PaymentWebhookEvents
            .Where(e => e.SignatureValid && (e.ProcessingStatus == WebhookProcessingStatus.Pending || e.ProcessingStatus == WebhookProcessingStatus.Failed)
                        && e.ReceivedAt <= before && e.Attempts < WebhookMaxAttempts)
            .OrderBy(e => e.ReceivedAt).Take(50).ToListAsync(ct);
        foreach (var evt in events)
        {
            var gateway = gateways.Find(evt.Provider);
            if (gateway is null) continue;
            try
            {
                await payments.ProcessWebhookEventAsync(evt, gateway.ParseVerifiedPayload(evt.Payload), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Webhook event {EventId} retry failed", evt.EventId);
            }
        }

        return events.Count;
    }

    public Task<int> ProcessRefundsAsync(CancellationToken ct) => refunds.ProcessApprovedAsync(ct);

    /// <summary><c>SettlementWeeklyJob</c>: on Sunday from 01:00 Riyadh, generates last week's batch per city (<c>Settlements:AutoGenerate</c>).</summary>
    public async Task<int> SettlementWeeklyAsync(CancellationToken ct)
    {
        var local = Formats.ToRiyadh(clock.UtcNow);
        if (!settlementOptions.Value.AutoGenerate || local.DayOfWeek != DayOfWeek.Sunday || local.Hour < 1)
        {
            return 0;
        }

        return await settlements.GenerateWeeklyAsync(ct);
    }
}

/// <summary>Runs the payment jobs on their schedules under <c>lock:job:{name}</c>; disabled with <c>Payments:JobsEnabled=false</c>.</summary>
public sealed class PaymentsBackgroundService(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<PaymentsOptions> options, ILogger<PaymentsBackgroundService> logger) : BackgroundService
{
    private static readonly (string Name, TimeSpan Every, Func<PaymentJobs, CancellationToken, Task<int>> Run)[] Jobs =
    [
        ("payment_action_expiry", TimeSpan.FromSeconds(30), (j, ct) => j.ExpireActionsAsync(ct)),
        ("payment_capture_retry", TimeSpan.FromMinutes(1), (j, ct) => j.RetryCapturesAsync(ct)),
        ("authorization_reconcile", TimeSpan.FromMinutes(10), (j, ct) => j.ReconcileAuthorizationsAsync(ct)),
        ("payment_webhook_retry", TimeSpan.FromMinutes(1), (j, ct) => j.RetryWebhooksAsync(ct)),
        ("refund_processor", TimeSpan.FromSeconds(30), (j, ct) => j.ProcessRefundsAsync(ct)),
        ("settlement_weekly", TimeSpan.FromMinutes(15), (j, ct) => j.SettlementWeeklyAsync(ct)),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        var next = Jobs.ToDictionary(j => j.Name, _ => DateTime.UtcNow);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var job in Jobs.Where(j => next[j.Name] <= DateTime.UtcNow))
            {
                next[job.Name] = DateTime.UtcNow + job.Every;
                try
                {
                    await RunAsync(job.Name, job.Run, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Payment job {Job} failed", job.Name);
                }
            }
        }
    }

    public async Task<int> RunAsync(string name, Func<PaymentJobs, CancellationToken, Task<int>> run, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{name}", TimeSpan.FromMinutes(5), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await run(scope.ServiceProvider.GetRequiredService<PaymentJobs>(), ct);
    }
}
