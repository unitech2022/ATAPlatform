using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Cancellation;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Passengers;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>
/// Bookings made by a company admin in the portal (doc 12 §F19.2 "الحجز", §F19.4): for an employee (the trip belongs to the employee's rider profile, who is notified in the app)
/// or for a guest (recorded on the admin's own rider profile, the guest gets the tracking link and PIN by SMS). Every trip is checked against the company's policy, budget and credit.
/// </summary>
public sealed class CorporateBookingService(
    AtaDbContext db,
    QuoteService quotes,
    PassengerTripService trips,
    TripReadService reads,
    TripCancellationService cancellationPreviews,
    CancellationEngine cancellations,
    CorporateRiderService riders,
    CorporateTripSummaries summaries,
    ReceiptService receipts,
    AuditService audit,
    IClock clock)
{
    public async Task<QuoteResponse> QuoteAsync(CorporateScope scope, CorporateQuoteRequest request, Language lang, CancellationToken ct)
    {
        CorporateContext.EnsureBookable(scope.Account);
        new Validator()
            .Route(request.Pickup, request.Dropoff, request.Stops, requireLabels: false)
            .Booking(request.BookingType, request.ScheduledAt)
            .Rule(nameof(request.Purpose), request.Purpose is null || request.Purpose.Length <= 200, "max_length:200")
            .ThrowIfInvalid();
        var booking = await ResolveRiderAsync(scope, request.EmployeeId, null, ct);
        var estimate = new EstimateRequest(request.Pickup, request.Dropoff, request.Stops, request.RideCategoryId, request.BookingType, request.ScheduledAt,
            PaymentMethod: PaymentMethodKind.Corporate, TripPurpose: request.Purpose, CostCenterId: request.CostCenterId);
        var response = await quotes.QuoteAsync(estimate, booking.Passenger.Id, lang, ct, corporate: true);
        return response with { Corporate = await riders.QuoteBlockAsync(booking, estimate, request.Purpose, request.CostCenterId, response, clock.UtcNow, ct) };
    }

    public async Task<TripDto> BookAsync(CorporateScope scope, CorporateBookingRequest request, Language lang, CancellationToken ct)
    {
        CorporateContext.EnsureBookable(scope.Account);
        var v = new Validator()
            .Rule("employeeId", (request.EmployeeId is null) != (request.Guest is null), "provide exactly one of employeeId or guest");
        string? guestPhone = null;
        if (request.Guest is { } guest)
        {
            v.Require("guest.name", guest.Name, 80).Rule("guest.name", guest.Name is null || guest.Name.Trim().Length >= 2, "min_length:2");
            v.Rule("guest.phoneNumber", PhoneNumber.TryNormalize(guest.PhoneNumber, out guestPhone), "invalid phone number");
        }

        v.Require(nameof(request.RideCategoryId), request.RideCategoryId).ThrowIfInvalid();
        var booking = await ResolveRiderAsync(scope, request.EmployeeId, request.Guest is null ? null : (request.Guest.Name!.Trim(), guestPhone!), ct);
        var create = new CreateTripRequest(request.Pickup, request.Dropoff, request.Stops, request.RideCategoryId, request.BookingType, request.ScheduledAt, PaymentMethodKind.Corporate,
            null, PricingMode.Fixed, null, request.RiderNote, request.QuoteId, TripPurpose: request.TripPurpose, CostCenterId: request.CostCenterId);
        var trip = await trips.CreateForCorporateAsync(create, booking, lang, ct);
        audit.Log("corporate_booking.create", "corporate_account", scope.Account.Id,
            null, new { tripId = trip.Id, trip.TripNumber, employeeId = request.EmployeeId, guest = request.Guest is not null, trip.EstimatedFare }, CorporateMembershipService.AdminActor);
        await db.SaveChangesAsync(ct);
        return trip;
    }

    public async Task<PagedResult<CorporateTripSummaryDto>> ListAsync(
        CorporateAccount account, string? status, DateOnly? from, DateOnly? to, Guid? employeeId, bool? isGuest, Paging paging, Language lang, CancellationToken ct)
    {
        var statuses = TripReadService.StatusesFor(status);
        var (fromAt, toAt) = CorporateRules.DayRange(from, to);
        var query = db.Trips.AsNoTracking().Where(t => t.CorporateAccountId == account.Id && statuses.Contains(t.Status));
        if (fromAt is { } f) query = query.Where(t => t.RequestedAt >= f);
        if (toAt is { } u) query = query.Where(t => t.RequestedAt < u);
        if (employeeId is { } e) query = query.Where(t => t.CorporateUserId == e);
        if (isGuest is { } g) query = query.Where(t => t.IsGuest == g);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(t => t.RequestedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await summaries.ToSummariesAsync(rows, lang, ct), total);
    }

    /// <summary>The live trip (status, driver, vehicle, timeline) with the receipt once it completed; the PIN is never part of the company's view.</summary>
    public async Task<TripDto> GetAsync(CorporateAccount account, Guid tripId, Language lang, CancellationToken ct)
    {
        var trip = await LoadAsync(account, tripId, ct);
        var dto = await reads.BuildAsync(trip, TripViewer.Corporate, lang, ct);
        return trip.Status == TripStatus.Completed ? dto with { Receipt = await receipts.ForCorporateTripAsync(trip, lang, ct) } : dto;
    }

    public async Task<CancelPreviewDto> CancelPreviewAsync(CorporateAccount account, Guid tripId, CancelPreviewRequest? request, Language lang, CancellationToken ct) =>
        await cancellationPreviews.PreviewAsync(await LoadAsync(account, tripId, ct), request, lang, ct);

    /// <summary>Cancels through the F14 engine as the booker: the stage fee (if any) is billed to the company.</summary>
    public async Task<TripDto> CancelAsync(CorporateScope scope, Guid tripId, CorporateCancelRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ReasonCode), request.ReasonCode, 60)
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 500, "max_length:500")
            .Rule(nameof(request.ExpectedFee), request.ExpectedFee is null or >= 0, "must be positive")
            .ThrowIfInvalid();
        var trip = await LoadAsync(scope.Account, tripId, ct);
        await cancellations.CancelAsync(trip, new CancelCommand(TripActor.Passenger, scope.UserId, request.ReasonCode!.Trim(), request.Note, request.ExpectedFee), ct);
        audit.Log("corporate_booking.cancel", "corporate_account", scope.Account.Id, null, new { tripId = trip.Id, trip.TripNumber, request.ReasonCode }, CorporateMembershipService.AdminActor);
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Corporate, lang, ct);
    }

    private async Task<Trip> LoadAsync(CorporateAccount account, Guid tripId, CancellationToken ct)
    {
        var trip = await reads.FindAsync(tripId, ct);
        return trip is null || trip.CorporateAccountId != account.Id ? throw new DomainException(ErrorCodes.NotFound) : trip;
    }

    /// <summary>The rider of a booking: an active member (their own rider profile, created on demand for admins who never used the app) or the booking admin for a guest.</summary>
    private async Task<CorporateBooking> ResolveRiderAsync(CorporateScope scope, Guid? employeeId, (string Name, string Phone)? guest, CancellationToken ct)
    {
        if (employeeId is { } id)
        {
            var member = await db.CorporateUsers.FirstOrDefaultAsync(m => m.Id == id && m.CorporateAccountId == scope.Account.Id, ct)
                ?? throw new DomainException(ErrorCodes.NotFound);
            if (member.Status != CorporateUserStatus.Active || member.UserId is not { } userId)
            {
                throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["employeeId"] = "not_active" });
            }

            return new CorporateBooking(scope.Account, member, await EnsurePassengerAsync(userId, ct), scope.UserId, false, null, null);
        }

        return new CorporateBooking(scope.Account, null, await EnsurePassengerAsync(scope.UserId, ct), scope.UserId, true, guest?.Name, guest?.Phone);
    }

    private async Task<PassengerProfile> EnsurePassengerAsync(Guid userId, CancellationToken ct)
    {
        var profile = await db.Passengers.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is not null)
        {
            return profile;
        }

        profile = new PassengerProfile { UserId = userId };
        db.Passengers.Add(profile);
        await db.SaveChangesAsync(ct);
        db.Entry(profile).State = EntityState.Detached;
        return profile;
    }
}
