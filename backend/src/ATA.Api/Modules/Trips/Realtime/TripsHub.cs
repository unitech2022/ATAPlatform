using Microsoft.Extensions.Options;
using ATA.Api.Common;
using ATA.Api.Modules.Pricing;
using ATA.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ATA.Api.Modules.Trips.Realtime;

/// <summary>
/// <c>/hubs/trips</c>: every connection joins its own user group; admins/operations also join <see cref="AdminsGroup"/>.
/// Clients authenticate with the JWT in the <c>access_token</c> query string.
/// </summary>
[Authorize(Policy = Policies.Authenticated)]
public sealed class TripsHub : Hub
{
    public const string AdminsGroup = "admins";

    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var sub = Context.User?.FindFirst(AtaClaims.Subject)?.Value;
        if (Guid.TryParse(sub, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        if (Context.User?.IsInRole(RoleNames.Admin) == true || Context.User?.IsInRole(RoleNames.Operations) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }

        await base.OnConnectedAsync();
    }
}

public static class TripHubEvents
{
    public const string TripUpdated = "TripUpdated";
    public const string DriverLocation = "DriverLocation";
    public const string OfferReceived = "OfferReceived";
    public const string OfferExpired = "OfferExpired";
    public const string LiveSnapshot = "LiveSnapshot";
    public const string DemandChanged = "DemandChanged";
}

public sealed class SignalRTripNotifier(IHubContext<TripsHub> hub) : ITripNotifier
{
    public Task TripUpdatedAsync(Guid userId, TripDto trip, CancellationToken ct) =>
        hub.Clients.Group(TripsHub.UserGroup(userId)).SendAsync(TripHubEvents.TripUpdated, trip, ct);

    public Task TripUpdatedForAdminsAsync(TripDto trip, CancellationToken ct) =>
        hub.Clients.Group(TripsHub.AdminsGroup).SendAsync(TripHubEvents.TripUpdated, trip, ct);

    public Task DriverLocationAsync(Guid userId, DriverLocationEvent location, CancellationToken ct) =>
        hub.Clients.Group(TripsHub.UserGroup(userId)).SendAsync(TripHubEvents.DriverLocation, location, ct);

    public Task OfferReceivedAsync(Guid userId, OfferDto offer, CancellationToken ct) =>
        hub.Clients.Group(TripsHub.UserGroup(userId)).SendAsync(TripHubEvents.OfferReceived, offer, ct);

    public Task OfferExpiredAsync(Guid userId, Guid offerId, CancellationToken ct) =>
        hub.Clients.Group(TripsHub.UserGroup(userId)).SendAsync(TripHubEvents.OfferExpired, offerId, ct);

    public Task LiveSnapshotAsync(LiveSnapshotDto snapshot, CancellationToken ct) =>
        hub.Clients.Group(TripsHub.AdminsGroup).SendAsync(TripHubEvents.LiveSnapshot, snapshot, ct);

    public Task DemandChangedAsync(DemandChangedEvent change, CancellationToken ct) =>
        hub.Clients.Group(TripsHub.AdminsGroup).SendAsync(TripHubEvents.DemandChanged, change, ct);
}

/// <summary>Pushes <c>LiveSnapshot</c> to the admins group every <see cref="RealtimeOptions.LiveSnapshotSeconds"/> seconds.</summary>
public sealed class LiveSnapshotService(IServiceScopeFactory scopes, IOptions<RealtimeOptions> options, ILogger<LiveSnapshotService> logger) : BackgroundService
{
    private readonly RealtimeOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.LiveSnapshotEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.LiveSnapshotSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PublishOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Live snapshot publish failed");
            }
        }
    }

    internal async Task PublishOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var snapshot = await scope.ServiceProvider.GetRequiredService<AdminTripService>().GetLiveAsync(ct);
        await scope.ServiceProvider.GetRequiredService<ITripNotifier>().LiveSnapshotAsync(snapshot, ct);
    }
}
