using System.Text;
using ATA.Api.Modules.Corporate;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;

namespace ATA.Tests.Unit;

public class CorporateUnitTests
{
    [Theory]
    [InlineData(115.00, 100.00, 15.00)]
    [InlineData(10.00, 8.70, 1.30)]
    [InlineData(46.50, 40.43, 6.07)]
    [InlineData(0.01, 0.01, 0.00)]
    [InlineData(-2.30, -2.00, -0.30)]
    [InlineData(0, 0, 0)]
    public void Vat_is_split_per_line_as_round_of_incl_over_1_15(double incl, double excl, double vat)
    {
        var (e, v) = CorporateVat.Split((decimal)incl);
        Assert.Equal((decimal)excl, e);
        Assert.Equal((decimal)vat, v);
        Assert.Equal((decimal)incl, e + v);
    }

    [Fact]
    public void Vat_split_always_adds_up_and_stays_within_a_halala_of_the_exact_rate()
    {
        for (var cents = -5000; cents <= 100000; cents += 37)
        {
            var incl = cents / 100m;
            var (excl, vat) = CorporateVat.Split(incl);
            Assert.Equal(incl, excl + vat);
            Assert.True(Math.Abs(excl - incl / 1.15m) <= 0.005m + 0.0000001m);
        }
    }

    [Fact]
    public void Zatca_qr_payload_is_tlv_with_seller_vat_timestamp_total_and_vat()
    {
        var payload = ZatcaQr.Payload("شركة أتا للنقل", "300000000000003", new DateTime(2026, 10, 1, 4, 30, 15, DateTimeKind.Utc), 1150.5m, 150.07m);
        var fields = ZatcaQr.Decode(payload);
        Assert.Equal("شركة أتا للنقل", fields[1]);
        Assert.Equal("300000000000003", fields[2]);
        Assert.Equal("2026-10-01T04:30:15Z", fields[3]);
        Assert.Equal("1150.50", fields[4]);
        Assert.Equal("150.07", fields[5]);
        var raw = Convert.FromBase64String(payload);
        Assert.Equal(1, raw[0]);
        Assert.Equal(Encoding.UTF8.GetByteCount("شركة أتا للنقل"), raw[1]);
        var svg = ZatcaQr.Svg(payload);
        Assert.StartsWith("<svg", svg);
        Assert.Contains("<path", svg);
    }

    [Fact]
    public void Csv_reader_handles_quotes_bom_crlf_and_embedded_separators()
    {
        var text = "﻿a,b,c\r\n1,\"x, y\",\"he said \"\"hi\"\"\"\r\n\"multi\nline\",,last";
        var rows = CsvReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        Assert.Equal(3, rows.Count);
        Assert.Equal(["a", "b", "c"], rows[0]);
        Assert.Equal(["1", "x, y", "he said \"hi\""], rows[1]);
        Assert.Equal(["multi\nline", "", "last"], rows[2]);
        Assert.Empty(CsvReader.Read(new MemoryStream()));
    }

    [Theory]
    [InlineData("07:00", "22:00", "07:00", true)]
    [InlineData("07:00", "22:00", "22:00", true)]
    [InlineData("07:00", "22:00", "06:59", false)]
    [InlineData("07:00", "22:00", "22:01", false)]
    [InlineData("22:00", "05:00", "23:30", true)]
    [InlineData("22:00", "05:00", "04:59", true)]
    [InlineData("22:00", "05:00", "12:00", false)]
    [InlineData("bad", "22:00", "12:00", false)]
    public void Time_windows_are_inclusive_and_may_wrap_past_midnight(string from, string to, string at, bool expected) =>
        Assert.Equal(expected, CorporatePolicyEvaluator.InWindow(TimeOnly.Parse(at), new TimeWindowDto(from, to)));

    private static readonly Guid Economy = Guid.NewGuid();
    private static readonly Guid Comfort = Guid.NewGuid();
    private static readonly Dictionary<Guid, string> Codes = new() { [Economy] = "economy", [Comfort] = "comfort" };

    // 2026-09-28 is a Monday; 12:00 UTC is 15:00 in Riyadh.
    private static PolicyFacts Facts(Guid? category = null, Guid? pickupZone = null, Guid? dropoffZone = null, DateTime? at = null, decimal fare = 40m, BookingType type = BookingType.Now,
        string? purpose = "x", bool costCenter = true, bool costCenterActive = true, bool guest = false) =>
        new(category ?? Economy, pickupZone, dropoffZone, at ?? new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc), fare, type, purpose, costCenter, costCenterActive, guest);

    private static CorporatePolicy Policy(Action<CorporatePolicy>? configure = null)
    {
        var policy = new CorporatePolicy { CorporateAccountId = Guid.NewGuid(), Name = "p" };
        configure?.Invoke(policy);
        return policy;
    }

    private static string Rules(CorporatePolicy? policy, PolicyFacts facts) =>
        string.Join(',', CorporatePolicyEvaluator.Evaluate(policy, facts, Codes).Select(v => v.Rule));

    [Fact]
    public void No_policy_or_an_empty_policy_allows_everything() =>
        Assert.Equal(string.Empty, Rules(null, Facts()) + Rules(Policy(), Facts(fare: 100000m, purpose: null, costCenter: false, guest: true, type: BookingType.Scheduled)));

    [Fact]
    public void Each_rule_yields_its_own_violation_in_the_documented_order()
    {
        Assert.Equal("category", Rules(Policy(p => p.AllowedRideCategoryIds = $"[\"{Comfort}\"]"), Facts()));
        Assert.Equal(string.Empty, Rules(Policy(p => p.AllowedRideCategoryIds = $"[\"{Comfort}\",\"{Economy}\"]"), Facts()));
        Assert.Equal("day", Rules(Policy(p => p.AllowedDays = "[0,2,3]"), Facts())); // Monday = 1
        Assert.Equal(string.Empty, Rules(Policy(p => p.AllowedDays = "[1]"), Facts()));
        Assert.Equal("day", Rules(Policy(p => p.AllowedDays = "[1]"), Facts(at: new DateTime(2026, 9, 28, 22, 0, 0, DateTimeKind.Utc)))); // 01:00 Tuesday in Riyadh
        Assert.Equal("time_window", Rules(Policy(p => p.TimeWindows = "[{\"from\":\"07:00\",\"to\":\"10:00\"}]"), Facts()));
        Assert.Equal(string.Empty, Rules(Policy(p => p.TimeWindows = "[{\"from\":\"07:00\",\"to\":\"10:00\"},{\"from\":\"14:00\",\"to\":\"16:00\"}]"), Facts()));
        Assert.Equal("max_fare", Rules(Policy(p => p.MaxFarePerTrip = 39.99m), Facts()));
        Assert.Equal(string.Empty, Rules(Policy(p => p.MaxFarePerTrip = 40m), Facts()));
        Assert.Equal("scheduled", Rules(Policy(p => p.AllowScheduled = false), Facts(type: BookingType.Scheduled)));
        Assert.Equal("purpose_required", Rules(Policy(p => p.RequirePurpose = true), Facts(purpose: " ")));
        Assert.Equal("cost_center_required", Rules(Policy(p => p.RequireCostCenter = true), Facts(costCenter: false)));
        Assert.Equal("cost_center_required", Rules(Policy(p => p.RequireCostCenter = true), Facts(costCenterActive: false)));
        Assert.Equal("guest_booking", Rules(Policy(p => p.AllowGuestBooking = false), Facts(guest: true)));
        Assert.Equal(string.Empty, Rules(Policy(p => p.AllowGuestBooking = false), Facts(guest: false)));
        Assert.Equal("guest_booking,category,max_fare,scheduled,purpose_required,cost_center_required", Rules(Policy(p =>
        {
            p.AllowGuestBooking = false;
            p.AllowedRideCategoryIds = $"[\"{Comfort}\"]";
            p.MaxFarePerTrip = 1m;
            p.AllowScheduled = false;
            p.RequirePurpose = true;
            p.RequireCostCenter = true;
        }), Facts(type: BookingType.Scheduled, purpose: null, costCenter: false, guest: true)));
    }

    [Fact]
    public void Zone_rules_follow_the_zone_match_mode()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var other = Guid.NewGuid();
        var both = Policy(p => { p.AllowedZoneIds = $"[\"{a}\",\"{b}\"]"; p.ZoneMatch = ZoneMatch.PickupAndDropoff; });
        var either = Policy(p => { p.AllowedZoneIds = $"[\"{a}\",\"{b}\"]"; p.ZoneMatch = ZoneMatch.PickupOrDropoff; });
        Assert.Equal(string.Empty, Rules(both, Facts(pickupZone: a, dropoffZone: b)));
        Assert.Equal("zone", Rules(both, Facts(pickupZone: a, dropoffZone: other)));
        Assert.Equal("zone", Rules(both, Facts(pickupZone: null, dropoffZone: b)));
        Assert.Equal(string.Empty, Rules(either, Facts(pickupZone: a, dropoffZone: other)));
        Assert.Equal(string.Empty, Rules(either, Facts(pickupZone: other, dropoffZone: b)));
        Assert.Equal("zone", Rules(either, Facts(pickupZone: other, dropoffZone: other)));
        Assert.Equal("zone", Rules(either, Facts()));
    }

    [Fact]
    public void Category_violation_lists_the_allowed_codes_and_fare_violation_the_limit()
    {
        var violations = CorporatePolicyEvaluator.Evaluate(Policy(p => { p.AllowedRideCategoryIds = $"[\"{Comfort}\"]"; p.MaxFarePerTrip = 10m; }), Facts(), Codes);
        Assert.Equal(["comfort"], Assert.IsAssignableFrom<IEnumerable<string>>(violations[0].Allowed));
        Assert.Equal(10m, violations[1].Limit);
    }

    [Fact]
    public void Policy_resolution_prefers_the_members_policy_then_the_default_and_ignores_inactive_ones()
    {
        var account = new CorporateAccount { AccountNumber = "CA-1", LegalNameAr = "a", LegalNameEn = "a", DisplayName = "a", CrNumber = "1010101010", BillingEmail = "a@a.sa", ContactName = "a", ContactPhone = "+966500000000" };
        var own = Policy(p => p.Name = "own");
        var def = Policy(p => { p.Name = "default"; p.IsDefault = true; });
        var inactive = Policy(p => { p.Name = "inactive"; p.IsActive = false; });
        account.DefaultPolicyId = def.Id;
        var policies = new[] { own, def, inactive };
        Assert.Equal("own", CorporateTripPolicyService.Resolve(policies, account, new CorporateUser { CorporateAccountId = account.Id, PhoneNumber = "+966500000001", PolicyId = own.Id })!.Name);
        Assert.Equal("default", CorporateTripPolicyService.Resolve(policies, account, new CorporateUser { CorporateAccountId = account.Id, PhoneNumber = "+966500000001", PolicyId = inactive.Id })!.Name);
        Assert.Equal("default", CorporateTripPolicyService.Resolve(policies, account, null)!.Name);
        account.DefaultPolicyId = null;
        Assert.Equal("default", CorporateTripPolicyService.Resolve(policies, account, null)!.Name);
        Assert.Null(CorporateTripPolicyService.Resolve([], account, null));
        // A personal budget beats the policy's; without either there is no budget.
        var member = new CorporateUser { CorporateAccountId = account.Id, PhoneNumber = "+966500000001", MonthlyBudget = 50m };
        Assert.Equal(50m, CorporateTripPolicyService.BudgetOf(member, Policy(p => p.MonthlyBudgetPerEmployee = 500m)));
        Assert.Equal(500m, CorporateTripPolicyService.BudgetOf(new CorporateUser { CorporateAccountId = account.Id, PhoneNumber = "+966500000001" }, Policy(p => p.MonthlyBudgetPerEmployee = 500m)));
        Assert.Null(CorporateTripPolicyService.BudgetOf(null, null));
    }

    [Theory]
    [InlineData("1010101010", true)]
    [InlineData("101010101", false)]
    [InlineData("10101010101", false)]
    [InlineData("10101O1010", false)]
    public void Commercial_registration_is_ten_digits(string value, bool valid) => Assert.Equal(valid, CorporateRules.IsCrNumber(value));

    [Theory]
    [InlineData("300000000000003", true)]
    [InlineData("310122393500003", true)]
    [InlineData("200000000000003", false)]
    [InlineData("300000000000002", false)]
    [InlineData("30000000000003", false)]
    public void Vat_number_is_fifteen_digits_starting_and_ending_with_three(string value, bool valid) => Assert.Equal(valid, CorporateRules.IsVatNumber(value));

    [Theory]
    [InlineData("0551234567", "+966551234567", true)]
    [InlineData("+966551234567", "+966551234567", true)]
    [InlineData("+971501234567", "+971501234567", true)]
    [InlineData("+966 11 234 5678", "+966112345678", true)]
    [InlineData("12345", "", false)]
    [InlineData("", "", false)]
    public void Contact_phones_are_e164(string input, string expected, bool valid)
    {
        Assert.Equal(valid, CorporateRules.TryNormalizePhone(input, out var normalized));
        if (valid) Assert.Equal(expected, normalized);
    }

    [Fact]
    public void National_address_is_formatted_on_one_line()
    {
        var address = new NationalAddress("1234", "King Fahd Road", "Al Olaya", "Riyadh", "12211", "5678");
        Assert.Equal("1234 King Fahd Road, Al Olaya, Riyadh 12211-5678, SA", CorporateRules.FormatAddress(address));
        Assert.Equal(string.Empty, CorporateRules.FormatAddress(null));
    }

    [Fact]
    public void Month_bounds_are_riyadh_calendar_months()
    {
        var (start, end) = CorporateRules.MonthBounds(new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc)); // 01:00 on 1 October in Riyadh
        Assert.Equal(new DateTime(2026, 9, 30, 21, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 10, 31, 21, 0, 0, DateTimeKind.Utc), end);
    }

    [Fact]
    public void Invoice_pdf_renders_an_arabic_and_english_document_with_many_lines()
    {
        var lines = Enumerable.Range(1, 120).Select(i => new InvoiceLineDto(Guid.NewGuid(), i % 10 == 0 ? InvoiceLineType.CancellationFee : InvoiceLineType.Trip, Guid.NewGuid(), $"T-20260928-{i:D5}",
            new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc), i % 3 == 0 ? null : "أحمد القحطاني", "E-1", "المالية", "FIN-01", i % 3 == 0 ? "ضيف" : null, "اجتماع عميل", "المنزل", "العمل",
            $"Trip {i}", 40m, 6m, 46m)).ToList();
        var seller = new InvoiceParty("شركة أتا للنقل", "ATA Transport Co.", "300000000000003", null);
        var buyer = new InvoiceParty("شركة ATA التجريبية المحدودة", "ATA Demo Company Ltd", "310000000000003", new NationalAddress("1234", "King Fahd Road", "Al Olaya", "Riyadh", "12211", "5678"), "1010000001");
        var model = new InvoicePdfModel("INV-202609-00001", false, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), "SAR", 15m, 4800m, 720m, 5520m,
            seller, buyer, "Riyadh, Saudi Arabia", CorporateRules.FormatAddress(buyer.Address), new DateTime(2026, 10, 1, 1, 0, 0, DateTimeKind.Utc), lines);
        var renderer = new QuestPdfInvoiceRenderer();
        var bytes = renderer.Render(model);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes[..5]));
        Assert.Contains("%%EOF", Encoding.ASCII.GetString(bytes[^32..]));
        Assert.True(bytes.Length > 20_000, $"multi-page PDF with embedded fonts, got {bytes.Length} bytes");
        var draft = renderer.Render(model with { IsDraft = true, Lines = lines.Take(1).ToList() });
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(draft[..5]));
        Assert.True(bytes.Length > draft.Length);
    }
}
