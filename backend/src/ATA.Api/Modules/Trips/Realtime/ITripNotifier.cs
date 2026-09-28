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
}
