using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ATA.Api.Modules.Trips;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>F19 trips: corporate payment at completion (ledger), no discounts, cancellation fees billed to the company, guest and employee bookings from the portal.</summary>
public class CorporateTripTests(CorporateFixture fixture) : IClassFixture<CorporateFixture>
{
    [Fact]
    public async Task Completed_corporate_trip_posts_a_balanced_charge_against_the_company_receivable_and_pays_the_driver()
    {
        var area = TripFlow.Area(30);
        var company = await CorporateFlow.OnboardAsync(fixture, "شركة القيد");
        var costCenter = await CorporateFlow.CreateCostCenterAsync(company, "FIN-01", "المالية");
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف القيد", b => { b["department"] = "المالية"; b["employeeNumber"] = "E-7"; });
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var rider = CorporateFlow.Party(employee);

        var tripId = await CorporateFlow.CompleteCorporateTripAsync(fixture, area, rider, driver, CorporateFlow.TripRequest(area, "اجتماع عميل", Guid.Parse(costCenter.GetProperty("id").GetString()!)));
        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        Assert.Equal(PaymentMethodKind.Corporate, trip.PaymentMethod);
        Assert.Equal(company.AccountId, trip.CorporateAccountId);
        Assert.Equal(employee.MemberId, trip.CorporateUserId);
        Assert.Equal(employee.UserId, trip.BookedByUserId);
        Assert.False(trip.IsGuest);
        Assert.Equal("اجتماع عميل", trip.TripPurpose);
        var fare = trip.FinalFare!.Value;

        // trip_corporate_charge: corporate_receivable:{id} ← trip_revenue; the driver share is paid out of trip_revenue.
        var journals = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().Where(j => j.ReferenceId == trip.Id).ToListAsync());
        var charge = Assert.Single(journals);
        Assert.Equal(JournalType.TripCorporateCharge, charge.Type);
        Assert.Equal($"trip:{trip.Id}:corporate", charge.IdempotencyKey);
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => e.JournalId == charge.Id).ToListAsync());
        Assert.Equal(fare, entries.Single(e => e.Account == LedgerAccounts.CorporateReceivable(company.AccountId)).Debit);
        Assert.Equal(fare, entries.Single(e => e.Account == LedgerAccounts.TripRevenue).Credit);
        var earning = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.AsNoTracking().SingleAsync(t => t.ReferenceId == trip.Id && t.Type == TransactionType.TripEarning));
        Assert.Equal(trip.DriverEarnings, earning.Amount);
        var earningEntries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => e.TransactionId == earning.Id).ToListAsync());
        Assert.Equal(trip.DriverEarnings, earningEntries.Single(e => e.Account == LedgerAccounts.TripRevenue).Debit);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
        Assert.Equal(-fare, await CorporateFlow.LedgerBalanceAsync(fixture, LedgerAccounts.CorporateReceivable(company.AccountId)));

        // Nothing touches the rider's wallet or cards; the trip event records the company payment.
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.WalletTransactions.AsNoTracking().Where(t => db.Wallets.Any(w => w.Id == t.WalletId && w.UserId == employee.UserId)).ToListAsync()));
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.Payments.AsNoTracking().Where(p => p.TripId == trip.Id).ToListAsync()));
        var payment = await fixture.Factory.WithDbAsync(db => db.TripEvents.AsNoTracking().SingleAsync(e => e.TripId == trip.Id && e.Type == TripEventTypes.PaymentRecorded));
        Assert.Contains("corporate", payment.Data);
        Assert.Equal(0m, trip.DiscountTotal);

        // The rider's trip and receipt carry the corporate block; the receipt has no refund or wallet lines.
        var riderTrip = await (await employee.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        Assert.Equal("corporate", riderTrip.GetProperty("paymentMethod").GetString());
        var corporate = riderTrip.GetProperty("corporate");
        Assert.Equal("شركة القيد", corporate.GetProperty("companyName").GetString());
        Assert.Equal("اجتماع عميل", corporate.GetProperty("purpose").GetString());
        Assert.Equal("FIN-01", corporate.GetProperty("costCenter").GetString());
        Assert.False(corporate.GetProperty("isGuest").GetBoolean());
        Assert.Equal("موظف القيد", corporate.GetProperty("employeeName").GetString());
        Assert.Equal(JsonValueKind.Null, corporate.GetProperty("guestName").ValueKind);
        Assert.Equal(JsonValueKind.Null, corporate.GetProperty("guestPhone").ValueKind);
        var receipt = await (await employee.Client.GetAsync($"/api/v1/passenger/trips/{tripId}/receipt")).ReadJsonAsync();
        Assert.Equal("corporate", receipt.GetProperty("payment").GetProperty("method").GetString());
        Assert.Equal(fare, receipt.GetProperty("total").GetDecimal());
        Assert.Equal(0, receipt.GetProperty("refunds").GetArrayLength());
        Assert.Equal("شركة القيد", receipt.GetProperty("corporate").GetProperty("companyName").GetString());
        // The driver's view names neither the purpose nor the cost centre.
        var driverView = await fixture.Factory.WithServiceAsync<TripReadService, TripDto>(async reads => await reads.BuildAsync((await reads.FindAsync(trip.Id, default))!, TripViewer.Driver, ATA.Domain.Common.Language.Ar, default));
        Assert.Equal("شركة القيد", driverView.Corporate!.CompanyName);
        Assert.Null(driverView.Corporate.Purpose);
        Assert.Null(driverView.Corporate.CostCenter);
        Assert.Null(driverView.Corporate.EmployeeName);

        // Portal: live detail with the receipt, list, dashboard and reports.
        var detail = await (await company.Portal.GetAsync($"/api/v1/corporate/bookings/{tripId}")).ReadJsonAsync();
        Assert.Equal("completed", detail.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("pin").ValueKind);
        Assert.Equal(fare, detail.GetProperty("receipt").GetProperty("total").GetDecimal());
        Assert.Equal("E-7", detail.GetProperty("corporate").GetProperty("employeeNumber").GetString());
        Assert.NotNull(detail.GetProperty("driver").GetProperty("fullName").GetString());
        Assert.Equal("Camry", detail.GetProperty("vehicle").GetProperty("model").GetString());
        var list = await (await company.Portal.GetAsync("/api/v1/corporate/bookings?status=completed")).ReadJsonAsync();
        var row = list.GetProperty("items")[0];
        Assert.Equal("موظف القيد", row.GetProperty("employeeName").GetString());
        Assert.Equal(fare, row.GetProperty("amount").GetDecimal());
        Assert.False(row.GetProperty("isGuest").GetBoolean());
        var dashboard = await (await company.Portal.GetAsync("/api/v1/corporate/dashboard")).ReadJsonAsync();
        Assert.Equal(1, dashboard.GetProperty("monthToDate").GetProperty("trips").GetInt32());
        Assert.Equal(fare, dashboard.GetProperty("monthToDate").GetProperty("spend").GetDecimal());
        Assert.Equal(fare, dashboard.GetProperty("creditUsed").GetDecimal());
        Assert.Equal(1, dashboard.GetProperty("recentTrips").GetArrayLength());
        var roster = await (await company.Portal.GetAsync($"/api/v1/corporate/employees/{employee.MemberId}")).ReadJsonAsync();
        Assert.Equal(fare, roster.GetProperty("spentThisMonth").GetDecimal());

        // Admin views: trip detail with the corporate block and the receivables.
        using var admin = await fixture.LoginAdminAsync();
        var adminTrip = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}")).ReadJsonAsync();
        var adminCorporate = adminTrip.GetProperty("corporate");
        Assert.Equal(company.AccountId.ToString(), adminCorporate.GetProperty("accountId").GetString());
        Assert.Equal("المالية", adminCorporate.GetProperty("department").GetString());
        var receivables = await (await admin.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync();
        Assert.Equal(fare, receivables.EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString()).GetProperty("unbilled").GetDecimal());
        var companyTrips = await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/trips")).ReadJsonAsync();
        Assert.Equal(tripId, companyTrips.GetProperty("items")[0].GetProperty("tripId").GetString());
        Assert.Equal("FIN-01", companyTrips.GetProperty("items")[0].GetProperty("costCenter").GetString());
    }

    [Fact]
    public async Task Completing_above_the_policy_is_allowed_and_recorded_as_a_trip_event()
    {
        var area = TripFlow.Area(31);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company);
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var quote = await (await employee.Client.PostAsJsonAsync("/api/v1/pricing/quote", new
        {
            pickup = new { name = "a", address = "b", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "c", address = "d", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m }, stops = Array.Empty<object>(),
            rideCategoryId = SeedIds.RideCategories.Economy, bookingType = "now", paymentMethod = "corporate",
        })).ReadJsonAsync();
        var estimate = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy").GetProperty("total").GetDecimal();
        await CorporateFlow.CreatePolicyAsync(company, new { name = "سقف", maxFarePerTrip = estimate });

        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var assigned = await TripFlow.RequestAndAssignAsync(fixture, employee.Client, driver.Client, CorporateFlow.TripRequest(area));
        var tripId = assigned.GetProperty("id").GetString()!;
        var pin = (await (await employee.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{tripId}/en-route", null)).EnsureSuccessStatusCode();
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{tripId}/arrived", null)).EnsureSuccessStatusCode();
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(12)); // billable waiting pushes the fare above the estimate
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/verify-pin", new { pin })).EnsureSuccessStatusCode();
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{tripId}/start", null)).EnsureSuccessStatusCode();
        var completed = await (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).ReadJsonAsync();
        Assert.Equal("completed", completed.GetProperty("status").GetString());
        Assert.True(completed.GetProperty("finalFare").GetDecimal() > estimate);

        var exceeded = await fixture.Factory.WithDbAsync(db => db.TripEvents.AsNoTracking().SingleAsync(e => e.TripId == Guid.Parse(tripId) && e.Type == TripEventTypes.CorporatePolicyExceeded));
        Assert.Contains("max_fare", exceeded.Data);
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.LedgerJournals.CountAsync(j => j.ReferenceId == Guid.Parse(tripId) && j.Type == JournalType.TripCorporateCharge)));
    }

    [Fact]
    public async Task Corporate_payment_gets_no_promo_or_offer_discounts_but_allows_offer_your_price()
    {
        var area = TripFlow.Area(32);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company);
        using var admin = await fixture.LoginAdminAsync();
        await RewardsFlow.CreatePromotionAsync(admin, "CORP10", b => { b["type"] = "percent"; b["value"] = 10m; });

        // A code with corporate payment is refused (payment_method), at request time…
        var refused = await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, promoCode: "CORP10"));
        await RewardsFlow.AssertErrorAsync(refused, "promo_not_eligible", reason: "payment_method");
        // …while a quote silently ignores it (no discount, no promotion block).
        var quote = await (await employee.Client.PostAsJsonAsync("/api/v1/pricing/quote", new
        {
            pickup = new { name = "a", address = "b", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "c", address = "d", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m }, stops = Array.Empty<object>(),
            rideCategoryId = SeedIds.RideCategories.Economy, bookingType = "now", paymentMethod = "corporate", promoCode = "CORP10",
        })).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, quote.GetProperty("promotion").ValueKind);
        var economy = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");
        Assert.Equal(JsonValueKind.Null, economy.GetProperty("totalBeforeDiscount").ValueKind);
        Assert.Equal(0m, economy.GetProperty("breakdown").GetProperty("discount").GetDecimal());
        Assert.True(quote.GetProperty("corporate").GetProperty("allowed").GetBoolean());

        // "Offer your price" stays available: the offered price is the estimate and the fare charged to the company.
        var total = economy.GetProperty("total").GetDecimal();
        var offered = Math.Ceiling(total * 0.9m);
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var tripId = await CorporateFlow.CompleteCorporateTripAsync(fixture, area, CorporateFlow.Party(employee), driver,
            CorporateFlow.TripRequest(area, pricingMode: "offer", offeredPrice: offered, quoteId: quote.GetProperty("quoteId").GetString()));
        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        Assert.Equal(PricingMode.Offer, trip.PricingMode);
        Assert.Equal(offered, trip.EstimatedFare);
        Assert.Equal(offered, trip.FinalFare);
        Assert.Equal(0m, trip.DiscountTotal);
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.PromotionRedemptions.AsNoTracking().Where(r => r.TripId == trip.Id).ToListAsync()));
        Assert.Equal(offered, (await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => db.LedgerJournals.Any(j => j.Id == e.JournalId && j.ReferenceId == trip.Id)).ToListAsync()))
            .Single(e => e.Account == LedgerAccounts.CorporateReceivable(company.AccountId)).Debit);
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().Where(j => j.ReferenceId == trip.Id && j.Type == JournalType.TripDiscount).ToListAsync()));
    }

    [Fact]
    public async Task Cancellation_fee_is_billed_to_the_company_through_the_f14_engine()
    {
        var area = TripFlow.Area(33);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف الإلغاء");
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var assigned = await TripFlow.RequestAndAssignAsync(fixture, employee.Client, driver.Client, CorporateFlow.TripRequest(area));
        var tripId = assigned.GetProperty("id").GetString()!;

        // Inside the free window the preview is free; afterwards the stage fee (5 SAR) applies — to the company.
        var free = await (await employee.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.True(free.GetProperty("isFree").GetBoolean());
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(200));
        var preview = await (await company.Portal.PostAsJsonAsync($"/api/v1/corporate/bookings/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal(5m, preview.GetProperty("fee").GetDecimal());
        Assert.False(preview.GetProperty("isFree").GetBoolean());
        var stale = await company.Portal.PostAsJsonAsync($"/api/v1/corporate/bookings/{tripId}/cancel", new { reasonCode = "changed_mind", expectedFee = 1m });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("cancellation_fee_changed", await stale.ErrorCodeAsync());
        // Another company cannot cancel it.
        var other = await CorporateFlow.OnboardAsync(fixture);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Portal.PostAsJsonAsync($"/api/v1/corporate/bookings/{tripId}/cancel", new { reasonCode = "changed_mind" })).StatusCode);

        var cancelled = await (await employee.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind", expectedFee = 5m })).ReadJsonAsync();
        Assert.Equal("cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal(5m, cancelled.GetProperty("cancellation").GetProperty("feeCharged").GetDecimal());
        var cancellation = await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AsNoTracking().SingleAsync(e => e.TripId == Guid.Parse(tripId)));
        Assert.Equal(CancellationFeeMethod.Corporate, cancellation.FeeMethod);
        Assert.Equal(CancellationFeeStatus.Charged, cancellation.FeeStatus);
        Assert.Equal(5m, cancellation.FeeCharged);
        Assert.Equal(AtFault.Passenger, cancellation.AtFault);

        var journal = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().SingleAsync(j => j.ReferenceId == cancellation.Id));
        Assert.Equal(JournalType.TripCorporateCharge, journal.Type);
        Assert.Equal($"cancellation:{cancellation.Id}:fee", journal.IdempotencyKey);
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => e.JournalId == journal.Id).ToListAsync());
        Assert.Equal(5m, entries.Single(e => e.Account == LedgerAccounts.CorporateReceivable(company.AccountId)).Debit);
        Assert.Equal(5m, entries.Single(e => e.Account == LedgerAccounts.CancellationFees).Credit);
        // The driver's compensation (50 %) is still paid out of cancellation_fees; the rider's wallet is untouched and is not notified of a charge.
        var compensation = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.AsNoTracking().SingleAsync(t => t.ReferenceId == cancellation.Id && t.Type == TransactionType.CancellationCompensation));
        Assert.Equal(2.5m, compensation.Amount);
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.WalletTransactions.AsNoTracking().Where(t => t.Type == TransactionType.CancellationFee && t.ReferenceId == cancellation.Id).ToListAsync()));
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().Where(n => n.UserId == employee.UserId && n.Type == "cancellation.fee_charged").ToListAsync()));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        // It shows in the company's exposure as unbilled and on the company's side of the trip.
        using var admin = await fixture.LoginAdminAsync();
        var receivables = await (await admin.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync();
        Assert.Equal(5m, receivables.EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString()).GetProperty("unbilled").GetDecimal());
        var detail = await (await company.Portal.GetAsync($"/api/v1/corporate/bookings/{tripId}")).ReadJsonAsync();
        Assert.Equal(5m, detail.GetProperty("cancellation").GetProperty("feeCharged").GetDecimal());
        Assert.Equal("غيرت رأيي", detail.GetProperty("cancellation").GetProperty("reasonName").GetString());
    }

    [Fact]
    public async Task Portal_books_a_trip_for_an_employee_who_rides_and_is_notified_in_the_app()
    {
        var area = TripFlow.Area(34);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف الحجز");
        var costCenter = await CorporateFlow.CreateCostCenterAsync(company, "OPS", "العمليات");
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);

        var quote = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings/quote", new
        {
            pickup = new { name = "a", address = "b", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "c", address = "d", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m }, stops = Array.Empty<object>(),
            rideCategoryId = SeedIds.RideCategories.Economy, bookingType = "now", employeeId = employee.MemberId, purpose = "زيارة", costCenterId = costCenter.GetProperty("id").GetString(),
        });
        Assert.Equal(HttpStatusCode.OK, quote.StatusCode);
        var quoted = await quote.ReadJsonAsync();
        Assert.True(quoted.GetProperty("corporate").GetProperty("allowed").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, quoted.GetProperty("quoteId").ValueKind);

        var booked = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, employee.MemberId, purpose: "زيارة", costCenterId: Guid.Parse(costCenter.GetProperty("id").GetString()!), quoteId: quoted.GetProperty("quoteId").GetString()));
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var trip = await booked.ReadJsonAsync();
        var tripId = trip.GetProperty("id").GetString()!;
        Assert.Equal("searching", trip.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, trip.GetProperty("pin").ValueKind);
        Assert.Equal("موظف الحجز", trip.GetProperty("corporate").GetProperty("employeeName").GetString());
        var dbTrip = await RewardsFlow.TripAsync(fixture, tripId);
        Assert.Equal(company.AdminUserId, dbTrip.BookedByUserId);
        Assert.Equal(employee.MemberId, dbTrip.CorporateUserId);
        var riderProfile = await fixture.Factory.WithDbAsync(db => db.Passengers.AsNoTracking().SingleAsync(p => p.UserId == employee.UserId));
        Assert.Equal(riderProfile.Id, dbTrip.PassengerId);

        // The employee sees it as their active trip in the app (with the PIN once a driver is assigned).
        var active = await (await employee.Client.GetAsync("/api/v1/passenger/trips/active")).ReadJsonAsync();
        Assert.Equal(tripId, active.GetProperty("id").GetString());
        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal("corporate", offer.GetProperty("paymentMethod").GetString());
        Assert.Equal("موظف", offer.GetProperty("passenger").GetProperty("firstName").GetString());
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        var riderView = await (await employee.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        Assert.Matches("^[0-9]{4}$", riderView.GetProperty("pin").GetString());
        var live = await (await company.Portal.GetAsync($"/api/v1/corporate/bookings/{tripId}")).ReadJsonAsync();
        Assert.Equal("driver_assigned", live.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, live.GetProperty("pin").ValueKind);
        Assert.NotNull(live.GetProperty("driver").GetProperty("fullName").GetString());
        Assert.Contains("****", live.GetProperty("driver").GetProperty("phoneMasked").GetString());

        // One active trip per employee still applies to employee bookings.
        var second = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, employee.MemberId));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("trip_active_exists", await second.ErrorCodeAsync());

        // Portal cancellation goes through the engine; an unknown / inactive employee and bad input are refused.
        var cancelled = await company.Portal.PostAsJsonAsync($"/api/v1/corporate/bookings/{tripId}/cancel", new { reasonCode = "changed_mind" });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("cancelled", (await cancelled.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, Guid.NewGuid()))).StatusCode);
        var invited = await CorporateFlow.AddEmployeeAsync(fixture, company, "غير مفعل", accept: false);
        var inactive = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, invited.MemberId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inactive.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area))).StatusCode);
        var both = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, employee.MemberId, "ضيف", "0551234567"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, both.StatusCode);
        Assert.Contains("corporate_booking.create", await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityId == company.AccountId).Select(a => a.Action).ToListAsync()));
    }

    [Fact]
    public async Task Guest_booking_is_recorded_on_the_admin_profile_and_the_guest_gets_the_link_and_pin_by_sms()
    {
        var area = TripFlow.Area(35);
        var company = await CorporateFlow.OnboardAsync(fixture, "شركة الضيوف", creditLimit: 100000m);
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var guestPhone = fixture.NextPhone();

        var booked = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ضيف محمد العلي", guestPhone: guestPhone, purpose: "استقبال ضيف"));
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var trip = await booked.ReadJsonAsync();
        var tripId = Guid.Parse(trip.GetProperty("id").GetString()!);
        Assert.True(trip.GetProperty("corporate").GetProperty("isGuest").GetBoolean());
        Assert.Equal("ضيف محمد العلي", trip.GetProperty("corporate").GetProperty("guestName").GetString());
        Assert.Equal(JsonValueKind.Null, trip.GetProperty("pin").ValueKind);

        // Recorded on the booking admin's rider profile (created on demand) with the guest's name and phone.
        var dbTrip = await RewardsFlow.TripAsync(fixture, tripId.ToString());
        var adminProfile = await fixture.Factory.WithDbAsync(db => db.Passengers.AsNoTracking().SingleAsync(p => p.UserId == company.AdminUserId));
        Assert.Equal(adminProfile.Id, dbTrip.PassengerId);
        Assert.True(dbTrip.IsGuest);
        Assert.Equal("ضيف محمد العلي", dbTrip.GuestName);
        Assert.Equal(CorporateFlow.Normalize(guestPhone), dbTrip.GuestPhone);
        Assert.Null(dbTrip.CorporateUserId);
        Assert.Equal(company.AdminUserId, dbTrip.BookedByUserId);

        // The SMS (corporate.guest_trip) carries the company, the pickup, the tracking link (F12) and the real trip PIN.
        await fixture.Factory.RunNotificationWorkerAsync();
        var sms = fixture.Sms.Sent.Single(m => m.Phone == CorporateFlow.Normalize(guestPhone));
        Assert.Contains("شركة الضيوف", sms.Message);
        Assert.Contains("المنزل", sms.Message);
        var link = Regex.Match(sms.Message, @"https?://\S+/t/([A-Za-z0-9_\-]+)");
        Assert.True(link.Success, sms.Message);
        var pin = await fixture.Factory.WithServiceAsync<TripPinService, string?>(pins => Task.FromResult<string?>(pins.Reveal(dbTrip)));
        Assert.Matches("^[0-9]{4}$", pin);
        Assert.Contains($"{pin}", sms.Message);
        var delivery = await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.AsNoTracking().SingleAsync(d => d.EventCode == "corporate.guest_trip" && d.PhoneNumber == CorporateFlow.Normalize(guestPhone)));
        Assert.Equal(ATA.Domain.Notifications.DeliveryStatus.Sent, delivery.Status);

        // The public tracking page names the guest (first name), never the admin.
        using var anonymous = fixture.CreateClient();
        var tracking = await (await anonymous.GetAsync($"/api/v1/public/trip-shares/{link.Groups[1].Value}")).ReadJsonAsync();
        Assert.Equal("ضيف", tracking.GetProperty("passengerFirstName").GetString());

        // The driver sees the guest's first name and the company payment.
        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal("ضيف", offer.GetProperty("passenger").GetProperty("firstName").GetString());
        Assert.Equal("corporate", offer.GetProperty("paymentMethod").GetString());

        // trip_active_exists does not apply to guest trips: a second guest trip is fine, up to Corporate:MaxActiveGuestTripsPerAdmin (3 in this host).
        Assert.Equal(HttpStatusCode.Created, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ضيفة ثانية", guestPhone: fixture.NextPhone()))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ضيف ثالث", guestPhone: fixture.NextPhone()))).StatusCode);
        var over = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ضيف رابع", guestPhone: fixture.NextPhone()));
        Assert.Equal(HttpStatusCode.Conflict, over.StatusCode);
        var limit = (await over.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("trip_active_exists", limit.GetProperty("code").GetString());
        Assert.Equal(3, limit.GetProperty("details").GetProperty("limit").GetInt32());
        Assert.Equal(3, (await (await company.Portal.GetAsync("/api/v1/corporate/bookings?isGuest=true")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(0, (await (await company.Portal.GetAsync("/api/v1/corporate/bookings?isGuest=false")).ReadJsonAsync()).GetProperty("total").GetInt32());

        // The guest trip is billed to the company like any other: complete it.
        var assigned = await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null);
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        await TripFlow.DriveAsync(driver.Client, tripId.ToString(), pin!);
        Assert.Equal(HttpStatusCode.OK, (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).StatusCode);
        var completed = await (await company.Portal.GetAsync($"/api/v1/corporate/bookings/{tripId}")).ReadJsonAsync();
        Assert.Equal("completed", completed.GetProperty("status").GetString());
        Assert.Equal("ضيف محمد العلي", completed.GetProperty("corporate").GetProperty("guestName").GetString());
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.LedgerJournals.CountAsync(j => j.ReferenceId == tripId && j.Type == JournalType.TripCorporateCharge)));
    }

    [Fact]
    public async Task Guest_bookings_follow_the_policy_flag_and_never_block_the_admins_own_rides()
    {
        var area = TripFlow.Area(36);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var policy = await CorporateFlow.CreatePolicyAsync(company, new { name = "بلا ضيوف", allowGuestBooking = false });
        var refused = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ضيف", guestPhone: fixture.NextPhone()));
        Assert.Equal("guest_booking", await CorporateFlow.ViolationRuleAsync(refused));
        var quote = await (await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings/quote", new
        {
            pickup = new { name = "a", address = "b", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "c", address = "d", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m }, stops = Array.Empty<object>(),
            rideCategoryId = SeedIds.RideCategories.Economy, bookingType = "now", purpose = "x",
        })).ReadJsonAsync();
        Assert.False(quote.GetProperty("corporate").GetProperty("allowed").GetBoolean());
        Assert.Equal("guest_booking", quote.GetProperty("corporate").GetProperty("violations")[0].GetProperty("rule").GetString());

        // Invalid guest data is a validation error.
        await company.Portal.PutAsJsonAsync($"/api/v1/corporate/policies/{policy.GetProperty("id").GetString()}", new { name = "بلا ضيوف", allowGuestBooking = true });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ض", guestPhone: fixture.NextPhone()))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ضيف", guestPhone: "123"))).StatusCode);

        // Suspended companies cannot book from the portal either.
        using var admin = await fixture.LoginAdminAsync();
        await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/suspend", new { reason = "test" });
        var inactive = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: "ضيف", guestPhone: fixture.NextPhone()));
        Assert.Equal(HttpStatusCode.Forbidden, inactive.StatusCode);
        Assert.Equal("corporate_account_inactive", await inactive.ErrorCodeAsync());
        // …but can still read (invoices, dashboard).
        Assert.Equal(HttpStatusCode.OK, (await company.Portal.GetAsync("/api/v1/corporate/dashboard")).StatusCode);
    }
}
