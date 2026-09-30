using ATA.Infrastructure.Locking;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Corporate;

/// <summary>Shared loop of the corporate jobs: every <c>Corporate:JobIntervalMinutes</c> under a named lock; off with <c>Corporate:JobsEnabled=false</c> (tests call <c>RunOnceAsync</c>).</summary>
public abstract class CorporateJobBase(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<CorporateOptions> options, ILogger logger, string name) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.Value.JobIntervalMinutes)));
        do
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
                logger.LogError(ex, "Corporate job {Job} failed", name);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{name}", TimeSpan.FromMinutes(10), ct);
        if (handle is null)
        {
            return 0;
        }

        using var scope = scopes.CreateScope();
        return await RunAsync(scope.ServiceProvider.GetRequiredService<CorporateInvoiceService>(), ct);
    }

    protected abstract Task<int> RunAsync(CorporateInvoiceService service, CancellationToken ct);
}

/// <summary><c>CorporateInvoiceJob</c> (doc 12 §F19.2): from <c>Corporate:InvoiceDayOfMonth</c> at 04:00 Riyadh, invoices the previous month of every company with movements.</summary>
public sealed class CorporateInvoiceJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<CorporateOptions> options, ILogger<CorporateInvoiceJob> logger)
    : CorporateJobBase(scopes, locks, options, logger, "corporate_invoice")
{
    protected override Task<int> RunAsync(CorporateInvoiceService service, CancellationToken ct) => service.GenerateMonthlyAsync(ct);
}

/// <summary><c>CorporateInvoiceOverdueJob</c>: <c>issued</c> and past due → <c>overdue</c>; optionally suspends accounts late for more than <c>Corporate:SuspendAfterOverdueDays</c>.</summary>
public sealed class CorporateInvoiceOverdueJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<CorporateOptions> options, ILogger<CorporateInvoiceOverdueJob> logger)
    : CorporateJobBase(scopes, locks, options, logger, "corporate_invoice_overdue")
{
    protected override Task<int> RunAsync(CorporateInvoiceService service, CancellationToken ct) => service.MarkOverdueAsync(ct);
}

/// <summary><c>CorporateInvitationExpiryJob</c>: marks invitations past <c>expires_at</c> as expired (the acceptance then answers <c>410 invitation_expired</c>).</summary>
public sealed class CorporateInvitationExpiryJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<CorporateOptions> options, ILogger<CorporateInvitationExpiryJob> logger)
    : CorporateJobBase(scopes, locks, options, logger, "corporate_invitation_expiry")
{
    protected override Task<int> RunAsync(CorporateInvoiceService service, CancellationToken ct) => service.ExpireInvitationsAsync(ct);
}
