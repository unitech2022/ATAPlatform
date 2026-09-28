using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;

namespace ATA.Api.Modules.Trips.Realtime;

/// <summary>Real-time fan-out of trip events; the SignalR implementation is the only transport in F8.</summary>
public interface ITripNotifier
{
    Task TripUpdatedAsync(Guid userId, TripDto trip, CancellationToken ct);

    Task TripUpdatedForAdminsAsync(TripDto trip, CancellationToken ct);

    Task DriverLocationAsync(Guid userId, DriverLocationEvent location, CancellationToken ct);

    Task OfferReceivedAsync(Guid userId, OfferDto offer, CancellationToken ct);

    Task OfferExpiredAsync(Guid userId, Guid offerId, CancellationToken ct);

    Task LiveSnapshotAsync(LiveSnapshotDto snapshot, CancellationToken ct);

    /// <summary>A zone's demand level moved (admins group).</summary>
    Task DemandChangedAsync(DemandChangedEvent change, CancellationToken ct);

    /// <summary>A payment of the user changed state (F11).</summary>
    Task PaymentUpdatedAsync(Guid userId, PaymentUpdatedEvent payment, CancellationToken ct);

    /// <summary>A driver requested a payout (admins group, F11).</summary>
    Task PayoutRequestedAsync(PayoutRequestedEvent payout, CancellationToken ct);

    /// <summary>A new inbox notification for the user (F13).</summary>
    Task NotificationCreatedAsync(Guid userId, NotificationDto notification, CancellationToken ct);
}
