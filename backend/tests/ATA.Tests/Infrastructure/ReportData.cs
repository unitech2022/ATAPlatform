using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Passengers;
using ATA.Domain.Payments;
using ATA.Domain.Pricing;
using ATA.Domain.Ratings;
using ATA.Domain.Scheduling;
using ATA.Domain.Support;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Persistence.Seed;

namespace ATA.Tests.Infrastructure;

/// <summary>
/// Hand-built operational rows for the KPI tests (doc 12 §F20.6), inserted directly so every expected value can be computed by hand. Times are UTC;
/// the Riyadh day <see cref="Day"/> (2026-09-20, a Sunday) runs from 2026-09-19 21:00Z to 2026-09-20 21:00Z.
/// </summary>
public sealed class ReportData
{
    public static readonly DateOnly Day = new(2026, 9, 20);
    public static readonly Guid ZoneA = SeedIds.ZoneRiyadhDefault;
    public static readonly Guid Economy = SeedIds.RideCategories.Economy;
    public static readonly Guid Comfort = SeedIds.RideCategories.Comfort;

    private readonly AtaDbContext _db;
    private int _trip;

    private ReportData(AtaDbContext db) => _db = db;

    public Guid ZoneB { get; private set; }
    public User P1 { get; private set; } = null!;
    public User P2 { get; private set; } = null!;
    public User P3 { get; private set; } = null!;
    public User D1User { get; private set; } = null!;
    public User D2User { get; private set; } = null!;
    public PassengerProfile Passenger1 { get; private set; } = null!;
    public PassengerProfile Passenger2 { get; private set; } = null!;
    public PassengerProfile Passenger3 { get; private set; } = null!;
    public DriverProfile Driver1 { get; private set; } = null!;
    public DriverProfile Driver2 { get; private set; } = null!;

    public static DateTime At(int day, int hour, int minute = 0) => new(2026, 9, day, hour, minute, 0, DateTimeKind.Utc);

    /// <summary>
    /// Day D (2026-09-20): T1 (P1/D1 economy zone A, completed 40 + favourite discount 5, earnings 32), T2 (P1/D2 comfort zone B, completed 60, earnings 48,
    /// refund 15), T3 (P2/D1 cancelled by the passenger at fault, fee 10, compensation 7), T4 (P3 no_drivers), T5 (P2/D2 zone B cancelled by the driver at
    /// fault), T6 (P3/D1 scheduled 15:00 completed 50, earnings 40), T7 (P1 scheduled 18:00, free passenger cancellation, driver released its reservation),
    /// T8 (P2/D2 cancelled by the passenger at fault, no fee); T0 (P3/D2 completed on 09-15) and T9 (P1/D1 completed 100 on 09-21). Plus offers, ratings,
    /// incentive 20, online hours (D1 4 h, D2 6 h) and two resolved tickets (3 h and 8 h).
    /// </summary>
    public static async Task<ReportData> SeedAsync(AtaDbContext db)
    {
        var data = new ReportData(db);
        await data.BuildAsync();
        return data;
    }

    private async Task BuildAsync()
    {
        var zoneB = new Zone
        {
            CityId = SeedIds.CityRiyadh, Code = "rpt_zone_b", NameAr = "المنطقة ب", NameEn = "Zone B", Polygon = "[[10.0,10.0],[10.0,10.1],[10.1,10.1],[10.1,10.0]]",
            CenterLat = 10.05m, CenterLng = 10.05m, Priority = 5,
        };
        _db.Zones.Add(zoneB);
        ZoneB = zoneB.Id;

        (P1, Passenger1) = Passenger("+966511100001", "P1");
        (P2, Passenger2) = Passenger("+966511100002", "P2");
        (P3, Passenger3) = Passenger("+966511100003", "P3");
        (D1User, Driver1) = Driver("+966511100011", "RPT-0001");
        (D2User, Driver2) = Driver("+966511100012", "RPT-0002");
        await _db.SaveChangesAsync();

        // T0 (09-15): P3's first completed trip (so P3 is not a new rider on D).
        Trip(Passenger3, Driver2, Economy, ZoneA, At(15, 10), assigned: At(15, 10, 1), arrived: At(15, 10, 5), completed: At(15, 10, 30), fare: 30, earnings: 24);

        var t1 = Trip(Passenger1, Driver1, Economy, ZoneA, At(20, 6), assigned: At(20, 6, 2), arrived: At(20, 6, 7), completed: At(20, 6, 30), fare: 40, earnings: 32, discount: 5,
            favorite: FavoriteStatus.Accepted);
        var t2 = Trip(Passenger1, Driver2, Comfort, ZoneB, At(20, 7), assigned: At(20, 7, 1), arrived: At(20, 7, 5), completed: At(20, 7, 40), fare: 60, earnings: 48);
        var t3 = Trip(Passenger2, Driver1, Economy, ZoneA, At(20, 8), assigned: At(20, 8, 3), cancelled: At(20, 8, 10), cancelledBy: CancelledBy.Passenger);
        var t4 = Trip(Passenger3, null, Economy, ZoneA, At(20, 9), cancelled: At(20, 9, 2), cancelledBy: CancelledBy.System, noDrivers: true);
        var t5 = Trip(Passenger2, Driver2, Economy, ZoneB, At(20, 10), assigned: At(20, 10, 1), cancelled: At(20, 10, 5), cancelledBy: CancelledBy.Driver);
        var t6 = Trip(Passenger3, Driver1, Economy, ZoneA, At(20, 11), assigned: At(20, 14, 40), arrived: At(20, 14, 55), completed: At(20, 15, 30), fare: 50, earnings: 40,
            scheduledAt: At(20, 15));
        var t7 = Trip(Passenger1, null, Economy, ZoneA, At(20, 12), cancelled: At(20, 13), cancelledBy: CancelledBy.Passenger, scheduledAt: At(20, 18));
        var t8 = Trip(Passenger2, Driver2, Economy, ZoneA, At(20, 16), assigned: At(20, 16, 1), cancelled: At(20, 16, 5), cancelledBy: CancelledBy.Passenger);
        Trip(Passenger1, Driver1, Economy, ZoneA, At(21, 10), assigned: At(21, 10, 1), arrived: At(21, 10, 5), completed: At(21, 10, 30), fare: 100, earnings: 80);
        await _db.SaveChangesAsync();

        _db.TripOffers.AddRange(
            new TripOffer { TripId = t1.Id, DriverId = Driver1.Id, Status = OfferStatus.Accepted, SentAt = At(20, 6, 1), RespondedAt = At(20, 6, 2), ExpiresAt = At(20, 6, 3) },
            new TripOffer { TripId = t2.Id, DriverId = Driver1.Id, Status = OfferStatus.Rejected, SentAt = At(20, 6, 58), RespondedAt = At(20, 6, 59), ExpiresAt = At(20, 7) },
            new TripOffer { TripId = t2.Id, DriverId = Driver2.Id, Status = OfferStatus.Accepted, SentAt = At(20, 7), RespondedAt = At(20, 7, 1), ExpiresAt = At(20, 7, 2) },
            new TripOffer { TripId = t4.Id, DriverId = Driver2.Id, Status = OfferStatus.Expired, SentAt = At(20, 9), RespondedAt = At(20, 9, 1), ExpiresAt = At(20, 9, 1) });

        _db.CancellationEvents.AddRange(
            Event(t3, TripActor.Passenger, P2.Id, AtFault.Passenger, At(20, 8, 10), counts: true, fee: 10m, compensation: 7m),
            Event(t4, TripActor.System, null, AtFault.None, At(20, 9, 2), counts: false, reason: "no_drivers"),
            Event(t5, TripActor.Driver, D2User.Id, AtFault.Driver, At(20, 10, 5), counts: true),
            Event(t7, TripActor.Passenger, P1.Id, AtFault.None, At(20, 13), counts: false),
            Event(t8, TripActor.Passenger, P2.Id, AtFault.Passenger, At(20, 16, 5), counts: true));

        _db.Ratings.AddRange(
            new Rating { TripId = t1.Id, RaterUserId = P1.Id, RaterRole = RatingRole.Passenger, RateeUserId = D1User.Id, RateeRole = RatingRole.Driver, Stars = 5, CreatedAt = At(20, 7) },
            new Rating { TripId = t2.Id, RaterUserId = P1.Id, RaterRole = RatingRole.Passenger, RateeUserId = D2User.Id, RateeRole = RatingRole.Driver, Stars = 4, CreatedAt = At(20, 8) },
            new Rating { TripId = t2.Id, RaterUserId = D2User.Id, RaterRole = RatingRole.Driver, RateeUserId = P1.Id, RateeRole = RatingRole.Passenger, Stars = 3, CreatedAt = At(20, 8) });

        _db.Refunds.Add(new Refund
        {
            RefundNumber = "RF-RPT-0001", TripId = t2.Id, UserId = P1.Id, Amount = 15m, Type = RefundType.Partial, Destination = RefundDestination.Wallet,
            ReasonCode = RefundReasonCode.Goodwill, Reason = "goodwill", Status = RefundStatus.Succeeded, RequestedBy = P1.Id, ProcessedAt = At(20, 18), CreatedAt = At(20, 17),
        });

        var (journal, entries) = LedgerJournal.Create(JournalType.TripDiscount, LedgerAccounts.DiscountFavoriteDriver, LedgerAccounts.TripRevenue, 5m, "trip", t1.Id, $"trip:{t1.Id}:discount", "favourite discount");
        _db.LedgerJournals.Add(journal);
        _db.LedgerEntries.AddRange(entries);

        var wallet = new Wallet { UserId = D1User.Id, Kind = WalletKind.Driver, Balance = 20m };
        _db.Wallets.Add(wallet);
        _db.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id, Type = TransactionType.Incentive, Direction = TransactionDirection.Credit, Amount = 20m, BalanceAfter = 20m, CreatedAt = At(20, 19),
        });

        _db.DriverStatusLogs.AddRange(
            new DriverStatusLog { DriverId = Driver1.Id, IsOnline = false, ChangedAt = At(19, 10) },
            new DriverStatusLog { DriverId = Driver1.Id, IsOnline = true, ChangedAt = At(20, 2) },
            new DriverStatusLog { DriverId = Driver1.Id, IsOnline = false, ChangedAt = At(20, 6) },
            new DriverStatusLog { DriverId = Driver2.Id, IsOnline = true, ChangedAt = At(20, 6, 30) },
            new DriverStatusLog { DriverId = Driver2.Id, IsOnline = false, ChangedAt = At(20, 12, 30) });

        _db.SupportTickets.AddRange(
            Ticket("ST-RPT-1", P1.Id, At(20, 1), At(20, 5), pausedSeconds: 3600),
            Ticket("ST-RPT-2", P2.Id, At(19, 20), At(20, 4), pausedSeconds: 0));

        _db.ScheduledRideReservations.Add(new ScheduledRideReservation
        {
            TripId = t7.Id, DriverId = Driver1.Id, Source = ReservationSource.Marketplace, Status = ReservationStatus.Released, ReservedAt = At(20, 12, 10),
            ReleasedAt = At(20, 12, 30), ReleaseReason = ReservationReleaseReason.DriverReleased,
        });
        await _db.SaveChangesAsync();
    }

    private (User, PassengerProfile) Passenger(string phone, string name)
    {
        var user = new User { PhoneNumber = phone, FullName = name, PhoneVerifiedAt = At(1, 0) };
        user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.Passenger, GrantedAt = At(1, 0) });
        var profile = new PassengerProfile { UserId = user.Id };
        _db.Users.Add(user);
        _db.Passengers.Add(profile);
        return (user, profile);
    }

    private (User, DriverProfile) Driver(string phone, string applicationNumber)
    {
        var user = new User { PhoneNumber = phone, FullName = applicationNumber, PhoneVerifiedAt = At(1, 0) };
        user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.Driver, GrantedAt = At(1, 0) });
        var profile = new DriverProfile { UserId = user.Id, ApplicationNumber = applicationNumber, ApplicationStatus = ApplicationStatus.Approved, CityId = SeedIds.CityRiyadh, ApprovedAt = At(1, 0) };
        _db.Users.Add(user);
        _db.Drivers.Add(profile);
        return (user, profile);
    }

    public Trip Trip(PassengerProfile passenger, DriverProfile? driver, Guid category, Guid zone, DateTime requested, DateTime? assigned = null, DateTime? arrived = null,
        DateTime? completed = null, DateTime? cancelled = null, CancelledBy? cancelledBy = null, bool noDrivers = false, decimal? fare = null, decimal? earnings = null,
        decimal discount = 0m, FavoriteStatus? favorite = null, DateTime? scheduledAt = null)
    {
        var trip = new Trip
        {
            TripNumber = $"R-RPT-{++_trip:D4}", PassengerId = passenger.Id, DriverId = driver?.Id, RideCategoryId = category,
            Status = completed is not null ? TripStatus.Completed : noDrivers ? TripStatus.NoDrivers : cancelled is not null ? TripStatus.Cancelled : TripStatus.InTrip,
            BookingType = scheduledAt is null ? BookingType.Now : BookingType.Scheduled, ScheduledAt = scheduledAt,
            PickupName = "A", PickupAddress = "A", PickupLat = 24.7m, PickupLng = 46.7m, DropoffName = "B", DropoffAddress = "B", DropoffLat = 24.8m, DropoffLng = 46.8m,
            EstimatedDistanceM = 5000, EstimatedDurationS = 900, EstimatedFare = fare ?? 30m, FinalFare = fare, DriverEarnings = earnings, DiscountTotal = discount,
            FavoriteStatus = favorite, FavoriteDriverId = favorite is null ? null : driver?.Id, PinCodeHash = "x", PinCodeProtected = "x",
            RequestedAt = requested, AssignedAt = assigned, ArrivedAt = arrived, CompletedAt = completed, CancelledAt = cancelled, CancelledBy = cancelledBy,
            CancellationReason = noDrivers ? "no_drivers" : cancelled is null ? null : "changed_mind", CreatedAt = requested,
        };
        _db.Trips.Add(trip);
        _db.FareQuotes.Add(new FareQuote
        {
            GroupId = Guid.NewGuid(), PassengerId = passenger.Id, RideCategoryId = category, PickupZoneId = zone, DistanceM = 5000, DurationS = 900, Breakdown = "{}",
            DemandLevelCode = "normal", Total = fare ?? 30m, BaseAmount = fare ?? 30m, ExpiresAt = requested.AddMinutes(5), UsedTripId = trip.Id, CreatedAt = requested,
        });
        return trip;
    }

    public static CancellationEvent Event(Trip trip, TripActor actor, Guid? userId, AtFault atFault, DateTime at, bool counts, decimal fee = 0m, decimal compensation = 0m,
        string reason = "changed_mind") => new()
    {
        TripId = trip.Id, Actor = actor, UserId = userId, AtFault = atFault, Stage = CancellationStage.AfterAccept, BookingType = trip.BookingType, ReasonCode = reason,
        EstimatedFare = trip.EstimatedFare, FeeAmount = fee, FeeCharged = fee, FeeStatus = fee > 0 ? CancellationFeeStatus.Charged : CancellationFeeStatus.None,
        CompensationAmount = compensation, CountsTowardRate = counts, CreatedAt = at,
    };

    private static SupportTicket Ticket(string number, Guid requester, DateTime created, DateTime resolved, int pausedSeconds) => new()
    {
        TicketNumber = number, RequesterUserId = requester, RequesterRole = SupportRequesterRole.Passenger, Type = SupportTicketType.Other, Subject = "help",
        Status = SupportTicketStatus.Resolved, FirstResponseDueAt = created.AddHours(4), ResolutionDueAt = created.AddHours(48), FirstResponseAt = created.AddMinutes(30),
        SlaPausedSeconds = pausedSeconds, ResolvedAt = resolved, LastMessageAt = resolved, CreatedAt = created,
    };
}
