using ATA.Api.Modules.Airports;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Scheduling;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Common;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ATA.Tests.Infrastructure;

/// <summary>Hosts the API against a shared SQLite in-memory database with a controllable clock.</summary>
public sealed class AtaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "ata-tests", Guid.NewGuid().ToString("N"));

    private readonly Dictionary<string, string?> _settings;

    private readonly Action<IServiceCollection>? _services;

    public AtaWebApplicationFactory(Dictionary<string, string?>? settings = null, Action<IServiceCollection>? services = null)
    {
        _settings = settings ?? [];
        _services = services;
    }

    public FakeClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused;User=unused;Password=unused;");
        builder.UseSetting("Jwt:Key", "test-only-jwt-signing-key-0123456789abcdef0123456789");
        builder.UseSetting("Jwt:Issuer", "ata-tests");
        builder.UseSetting("Jwt:Audience", "ata-tests");
        builder.UseSetting("Otp:DevMode", "true");
        builder.UseSetting("Payments:SandboxEnabled", "true");
        builder.UseSetting("Storage:Root", _storageRoot);
        builder.UseSetting("RateLimiting:OtpPerIpPerHour", "10000");
        // Background loops are driven explicitly from tests (RunMatcherAsync) so results are deterministic under the fake clock.
        builder.UseSetting("Matching:Enabled", "false");
        builder.UseSetting("Demand:Enabled", "false");
        builder.UseSetting("Realtime:LiveSnapshotEnabled", "false");
        builder.UseSetting("Payments:Provider", "sandbox");
        builder.UseSetting("Payments:JobsEnabled", "false");
        builder.UseSetting("Payments:PublicBaseUrl", "http://localhost");
        builder.UseSetting("Notifications:WorkerEnabled", "false");
        builder.UseSetting("Notifications:JobsEnabled", "false");
        builder.UseSetting("Safety:JobsEnabled", "false");
        builder.UseSetting("Reliability:JobsEnabled", "false");
        builder.UseSetting("Ratings:JobsEnabled", "false");
        builder.UseSetting("Incentives:JobsEnabled", "false");
        builder.UseSetting("Scheduling:JobsEnabled", "false");
        builder.UseSetting("Airport:JobsEnabled", "false");
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AtaDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AtaDbContext>));
            services.RemoveAll<AtaDbContext>();
            services.AddDbContext<AtaDbContext>(o => o.UseSqlite(_connection));
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
            _services?.Invoke(services);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtaDbContext>();
        await db.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
    }

    /// <summary>Runs one matching pass (offers, expiries, no_drivers) exactly like the background service would.</summary>
    public async Task<int> RunMatcherAsync()
    {
        var matcher = Services.GetServices<IHostedService>().OfType<MatchingBackgroundService>().Single();
        return await matcher.RunOnceAsync(CancellationToken.None);
    }

    /// <summary>Runs one pass of <c>ScheduledRideWorker</c> (confirmation requests / timeouts, search start, driver no-show, favourite window).</summary>
    public async Task<int> RunScheduledWorkerAsync()
    {
        var worker = Services.GetServices<IHostedService>().OfType<ScheduledRideWorker>().Single();
        return await worker.RunOnceAsync(CancellationToken.None);
    }

    /// <summary>Runs one pass of <c>ScheduledReminderJob</c>.</summary>
    public async Task<int> RunScheduledRemindersAsync()
    {
        var job = Services.GetServices<IHostedService>().OfType<ScheduledReminderJob>().Single();
        return await job.RunOnceAsync(CancellationToken.None);
    }

    /// <summary>Runs one pass of <c>AirportQueueJob</c> (stale entries leave, positions are broadcast).</summary>
    public async Task<int> RunAirportQueueJobAsync()
    {
        var job = Services.GetServices<IHostedService>().OfType<AirportQueueJob>().Single();
        return await job.RunOnceAsync(CancellationToken.None);
    }

    /// <summary>Runs one demand pass (snapshots per zone + <c>DemandChanged</c>) exactly like the background service would.</summary>
    public async Task<int> RunDemandAsync()
    {
        var demand = Services.GetServices<IHostedService>().OfType<DemandBackgroundService>().Single();
        return await demand.RunOnceAsync(CancellationToken.None);
    }

    /// <summary>Runs <paramref name="action"/> with a service from a fresh scope (payment/notification jobs, scanners…).</summary>
    public async Task<T> WithServiceAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <summary>Runs one pass of the notification delivery worker (queued deliveries and due retries).</summary>
    public Task<int> RunNotificationWorkerAsync() =>
        WithServiceAsync<ATA.Api.Modules.Notifications.NotificationDeliveryProcessor, int>(p => p.ProcessDueAsync(CancellationToken.None));

    /// <summary>One pass of <c>SafetyMonitorJob</c> (anomaly detection on in-trip trips).</summary>
    public Task<int> RunSafetyMonitorAsync() =>
        WithServiceAsync<ATA.Api.Modules.Safety.SafetyMonitor, int>(m => m.RunMonitorAsync(CancellationToken.None));

    /// <summary>One pass of <c>SafetyCheckTimeoutJob</c>.</summary>
    public Task<int> RunSafetyCheckTimeoutsAsync() =>
        WithServiceAsync<ATA.Api.Modules.Safety.SafetyMonitor, int>(m => m.RunCheckTimeoutsAsync(CancellationToken.None));

    /// <summary>One pass of <c>RestrictionExpiryJob</c>.</summary>
    public Task<int> RunRestrictionExpiryAsync() =>
        WithServiceAsync<ATA.Api.Modules.Cancellation.ReliabilityService, int>(s => s.ExpireRestrictionsAsync(CancellationToken.None));

    public async Task<T> WithDbAsync<T>(Func<AtaDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AtaDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }
    }
}

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; private set; } = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);

    /// <summary>Jumps to an absolute instant (e.g. the booking date of a window-boundary scenario).</summary>
    public void Set(DateTime utc) => UtcNow = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
}
