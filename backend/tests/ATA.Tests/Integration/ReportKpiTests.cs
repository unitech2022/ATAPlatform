using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ATA.Domain.Rbac;
using ATA.Domain.Reporting;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>A small export limit so the row cap is testable; the hand-built dataset is seeded once per class.</summary>
public sealed class ReportFixture() : ApiFixture(new Dictionary<string, string?> { ["Reports:MaxExportRows"] = "5" })
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ReportData? _data;

    public async Task<ReportData> DataAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _data ??= await Factory.WithDbAsync(ReportData.SeedAsync);
        }
        finally
        {
            _gate.Release();
        }
    }
}

/// <summary>F20 KPIs (doc 12 §F20.6 / §F20.7) against hand-built data: every metric, scopes, multi-day aggregation, comparison, series, breakdown, exports.</summary>
public sealed class ReportKpiTests(ReportFixture fixture) : IClassFixture<ReportFixture>
{
    private const string Api = "/api/v1";
    private const string D = "2026-09-20";
    private const string D1 = "2026-09-21";

    /// <summary>Expected values of day D for the <c>all</c> scope (percent metrics are shares 0..1).</summary>
    private static readonly Dictionary<string, decimal?> ExpectedDay = new()
    {
        ["completed_trips"] = 3, ["requested_trips"] = 8, ["completion_rate"] = 0.375m, ["trips_per_active_rider"] = 1.5m, ["average_eta"] = 480m,
        ["average_time_to_assign"] = 96m, ["no_drivers_rate"] = 0.125m, ["driver_acceptance_rate"] = 0.5m, ["driver_cancellation_rate"] = 0.1667m,
        ["passenger_cancellation_rate"] = 0.3333m, ["average_fare"] = 50m, ["driver_earnings_per_online_hour"] = 14m, ["online_hours"] = 10m, ["gmv"] = 165m,
        ["platform_revenue"] = 18m, ["take_rate"] = 0.1091m, ["incentives_paid"] = 20m, ["refunds_amount"] = 15m, ["active_riders"] = 2, ["active_drivers"] = 2,
        ["new_riders"] = 1, ["repeat_rate"] = 0.5m, ["customer_rating"] = 4.5m, ["support_resolution_time"] = 5.5m, ["cancellation_fee_revenue"] = 10m,
        ["repeat_cancellation_rate"] = 0.5m, ["driver_reliability_rate"] = 0.4286m, ["passenger_reliability_rate"] = 0.5m, ["favorite_driver_booking_rate"] = 0.3333m,
        ["favorite_driver_discount_usage"] = 5m, ["scheduled_ride_completion_rate"] = 1m, ["scheduled_ride_cancellation_rate"] = 0.5m,
    };

    private static Dictionary<string, JsonElement> ByCode(JsonElement report) =>
        report.GetProperty("metrics").EnumerateArray().ToDictionary(m => m.GetProperty("code").GetString()!);

    private static decimal? Value(JsonElement metric, string property = "value") =>
        metric.GetProperty(property).ValueKind == JsonValueKind.Null ? null : metric.GetProperty(property).GetDecimal();

    private async Task<HttpClient> AdminAsync()
    {
        await fixture.DataAsync();
        return await fixture.LoginAdminAsync();
    }

    private static void AssertDay(Dictionary<string, JsonElement> metrics)
    {
        Assert.Equal(32, metrics.Count);
        foreach (var (code, expected) in ExpectedDay)
        {
            Assert.True(expected == Value(metrics[code]), $"{code}: expected {expected}, got {Value(metrics[code])}");
        }
    }

    [Fact]
    public async Task Every_kpi_of_the_day_matches_the_hand_computed_value_live_and_from_snapshots()
    {
        using var admin = await AdminAsync();
        var live = ByCode(await (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D}")).ReadJsonAsync());
        AssertDay(live);
        Assert.Equal("الرحلات المكتملة", live["completed_trips"].GetProperty("name").GetString());
        Assert.Equal("count", live["completed_trips"].GetProperty("unit").GetString());
        Assert.Equal("v1.1", live["scheduled_ride_cancellation_rate"].GetProperty("group").GetString());
        Assert.Equal(3m, Value(live["completion_rate"], "numerator"));
        Assert.Equal(8m, Value(live["completion_rate"], "denominator"));
        Assert.Equal(1m, Value(live["favorite_driver_discount_usage"], "numerator"));
        Assert.Equal(JsonValueKind.Null, live["completed_trips"].GetProperty("numerator").ValueKind);

        var rebuilt = await admin.PostAsJsonAsync($"{Api}/admin/reports/snapshots/rebuild", new { from = "2026-09-15", to = D1 });
        Assert.Equal(HttpStatusCode.Accepted, rebuilt.StatusCode);
        var summary = await rebuilt.ReadJsonAsync();
        Assert.Equal(7, summary.GetProperty("days").GetInt32());
        var fromSnapshots = ByCode(await (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D}")).ReadJsonAsync());
        AssertDay(fromSnapshots);

        await fixture.Factory.WithDbAsync(async db =>
        {
            var day = ReportData.Day;
            Assert.Equal(32, await db.ReportSnapshots.CountAsync(s => s.SnapshotDate == day && s.ScopeKey == ReportSnapshot.AllScope));
            var zone = await db.ReportSnapshots.FirstAsync(s => s.SnapshotDate == day && s.ScopeKey == $"zone:{ReportData.ZoneA}" && s.MetricCode == "completed_trips");
            Assert.Equal(2m, zone.Value);
            Assert.Equal(ReportData.ZoneA, zone.ZoneId);
            var rate = await db.ReportSnapshots.FirstAsync(s => s.SnapshotDate == day && s.ScopeKey == ReportSnapshot.AllScope && s.MetricCode == "completion_rate");
            Assert.Equal((0.375m, 3m, 8m), (rate.Value, rate.Numerator!.Value, rate.Denominator!.Value));
            var quiet = new DateOnly(2026, 9, 17);
            Assert.Equal(0m, (await db.ReportSnapshots.FirstAsync(s => s.SnapshotDate == quiet && s.ScopeKey == ReportSnapshot.AllScope && s.MetricCode == "gmv")).Value);
            return true;
        });
        Assert.Contains("report_snapshots.rebuild", await fixture.Factory.WithDbAsync(db => db.AuditLogs.Select(a => a.Action).ToListAsync()));
    }

    [Fact]
    public async Task Scopes_filter_by_zone_city_and_category()
    {
        using var admin = await AdminAsync();
        var data = await fixture.DataAsync();
        async Task<Dictionary<string, JsonElement>> Kpis(string filter) =>
            ByCode(await (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D}&{filter}")).ReadJsonAsync());

        var zoneA = await Kpis($"zoneId={ReportData.ZoneA}");
        Assert.Equal(2m, Value(zoneA["completed_trips"]));
        Assert.Equal(6m, Value(zoneA["requested_trips"]));
        Assert.Equal(JsonValueKind.Null, zoneA["online_hours"].GetProperty("value").ValueKind);
        var zoneB = await Kpis($"zoneId={data.ZoneB}");
        Assert.Equal(1m, Value(zoneB["completed_trips"]));
        Assert.Equal(15m, Value(zoneB["refunds_amount"]));
        Assert.Equal(1m, Value(zoneB["driver_cancellation_rate"], "numerator"));
        var city = await Kpis($"cityId={SeedIds.CityRiyadh}");
        Assert.Equal(3m, Value(city["completed_trips"]));
        Assert.Equal(165m, Value(city["gmv"]));
        var comfort = await Kpis($"rideCategoryId={ReportData.Comfort}");
        Assert.Equal(1m, Value(comfort["completed_trips"]));
        Assert.Equal(60m, Value(comfort["average_fare"]));
        var zoneEconomy = await Kpis($"zoneId={ReportData.ZoneA}&rideCategoryId={ReportData.Economy}");
        Assert.Equal(2m, Value(zoneEconomy["completed_trips"]));
        var mismatch = await admin.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D}&zoneId={data.ZoneB}&cityId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mismatch.StatusCode);
    }

    [Fact]
    public async Task Several_days_aggregate_ratios_as_sums_distinct_directly_and_the_comparison_uses_the_previous_period()
    {
        using var admin = await AdminAsync();
        var two = ByCode(await (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D1}")).ReadJsonAsync());
        Assert.Equal(4m, Value(two["completed_trips"]));
        Assert.Equal(62.5m, Value(two["average_fare"]));
        Assert.Equal(0.4444m, Value(two["completion_rate"]));
        Assert.Equal(2m, Value(two["active_riders"]));
        Assert.Equal(0.5m, Value(two["repeat_rate"]));
        Assert.Equal(2m, Value(two["trips_per_active_rider"]));

        var compared = await (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D1}&to={D1}&compare=previous_period&metrics=completed_trips,gmv")).ReadJsonAsync();
        Assert.Equal(D, compared.GetProperty("previousFrom").GetString());
        var metrics = ByCode(compared);
        Assert.Equal(2, metrics.Count);
        Assert.Equal((1m, 3m, -66.7m), (Value(metrics["completed_trips"])!.Value, Value(metrics["completed_trips"], "previousValue")!.Value, Value(metrics["completed_trips"], "changePercent")!.Value));
        Assert.Equal((100m, 165m, -39.4m), (Value(metrics["gmv"])!.Value, Value(metrics["gmv"], "previousValue")!.Value, Value(metrics["gmv"], "changePercent")!.Value));

        var tooLarge = await admin.GetAsync($"{Api}/admin/reports/kpis?from=2025-01-01&to={D}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooLarge.StatusCode);
        var rangeError = (await tooLarge.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("report_range_too_large", rangeError.GetProperty("code").GetString());
        Assert.Equal(366, rangeError.GetProperty("details").GetProperty("maxDays").GetInt32());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D}&metrics=nope")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D}&compare=last_year")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/reports/kpis?from={D1}&to={D}")).StatusCode);
    }

    [Fact]
    public async Task Series_by_day_week_and_month_and_breakdown_by_zone_and_category()
    {
        using var admin = await AdminAsync();
        var data = await fixture.DataAsync();
        List<string> Points(JsonElement series) =>
            series.GetProperty("points").EnumerateArray().Select(p => $"{p.GetProperty("periodStart").GetString()}={Value(p)}").ToList();
        List<string> Rows(JsonElement breakdown, bool label = false) =>
            breakdown.GetProperty("rows").EnumerateArray().Select(r => $"{r.GetProperty("key").GetString()}{(label ? "|" + r.GetProperty("label").GetString() : string.Empty)}={Value(r)}").ToList();

        var daily = await (await admin.GetAsync($"{Api}/admin/reports/kpis/completed_trips/series?from=2026-09-19&to={D1}&granularity=day")).ReadJsonAsync();
        Assert.Equal(["2026-09-19=0", $"{D}=3", $"{D1}=1"], Points(daily));
        var weekly = await (await admin.GetAsync($"{Api}/admin/reports/kpis/completed_trips/series?from=2026-09-14&to={D1}&granularity=week")).ReadJsonAsync();
        Assert.Equal(["2026-09-13=1", $"{D}=4"], Points(weekly));
        var monthly = await (await admin.GetAsync($"{Api}/admin/reports/kpis/completed_trips/series?from=2026-09-14&to={D1}&granularity=month")).ReadJsonAsync();
        Assert.Equal(["2026-09-01=5"], Points(monthly));
        var fare = await (await admin.GetAsync($"{Api}/admin/reports/kpis/average_fare/series?from={D}&to={D1}&granularity=week")).ReadJsonAsync();
        Assert.Equal([$"{D}=62.5"], Points(fare));
        var riders = await (await admin.GetAsync($"{Api}/admin/reports/kpis/active_riders/series?from={D}&to={D1}&granularity=week")).ReadJsonAsync();
        Assert.Equal([$"{D}=2"], Points(riders));
        var zoneSeries = await (await admin.GetAsync($"{Api}/admin/reports/kpis/completed_trips/series?from={D}&to={D}&zoneId={data.ZoneB}")).ReadJsonAsync();
        Assert.Equal([$"{D}=1"], Points(zoneSeries));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Api}/admin/reports/kpis/unknown/series?from={D}&to={D}")).StatusCode);

        var byZone = await (await admin.GetAsync($"{Api}/admin/reports/breakdown?metric=completed_trips&from={D}&to={D}&groupBy=zone")).ReadJsonAsync();
        Assert.Equal([$"{ReportData.ZoneA}|الرياض (افتراضي)=2", $"{data.ZoneB}|المنطقة ب=1"], Rows(byZone, label: true));
        var byCategory = await (await admin.GetAsync($"{Api}/admin/reports/breakdown?metric=average_fare&from={D}&to={D}&groupBy=category")).ReadJsonAsync();
        var categoryRows = byCategory.GetProperty("rows").EnumerateArray().ToDictionary(r => r.GetProperty("key").GetString()!, r => Value(r));
        Assert.Equal(60m, categoryRows[ReportData.Comfort.ToString()]);
        Assert.Equal(45m, categoryRows[ReportData.Economy.ToString()]);
        var zoneEconomy = await (await admin.GetAsync($"{Api}/admin/reports/breakdown?metric=completed_trips&from={D}&to={D}&groupBy=zone&rideCategoryId={ReportData.Economy}")).ReadJsonAsync();
        Assert.Equal([$"{ReportData.ZoneA}=2"], Rows(zoneEconomy));
        var zoneCategories = await (await admin.GetAsync($"{Api}/admin/reports/breakdown?metric=completed_trips&from={D}&to={D}&groupBy=category&zoneId={data.ZoneB}")).ReadJsonAsync();
        Assert.Equal([$"{ReportData.Comfort}=1"], Rows(zoneCategories));
        var byCity = await (await admin.GetAsync($"{Api}/admin/reports/breakdown?metric=active_riders&from={D}&to={D1}&groupBy=city")).ReadJsonAsync();
        Assert.Equal(2m, Value(byCity.GetProperty("rows")[0]));
        Assert.Equal(0, (await (await admin.GetAsync($"{Api}/admin/reports/breakdown?metric=online_hours&from={D}&to={D}&groupBy=zone")).ReadJsonAsync()).GetProperty("rows").GetArrayLength());
    }

    [Fact]
    public async Task Csv_exports_start_with_a_bom_follow_the_columns_respect_the_row_limit_and_need_reports_export()
    {
        using var admin = await AdminAsync();
        var response = await admin.GetAsync($"{Api}/admin/reports/export?dataset=trips&from={D1}&to={D1}&format=csv");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("trip_number,requested_at,completed_at,status,category,city,zone,passenger_id,driver_id,booking_type,payment_method,distance_km,duration_min,"
                     + "estimated_fare,final_fare,discount_total,driver_earnings,cancelled_by,cancellation_reason,corporate_account", lines[0]);
        Assert.Equal(2, lines.Length);
        var row = lines[1].Split(',');
        Assert.Equal(("completed", "economy", "city_default", "100", "80"), (row[3], row[4], row[6], row[14], row[16]));
        Assert.True(Guid.TryParse(row[7], out _));
        Assert.DoesNotContain("+9665", lines[1]);

        var tooMany = await admin.GetAsync($"{Api}/admin/reports/export?dataset=trips&from={D}&to={D}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMany.StatusCode);
        var rowsError = (await tooMany.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("report_range_too_large", rowsError.GetProperty("code").GetString());
        Assert.Equal(5, rowsError.GetProperty("details").GetProperty("maxRows").GetInt32());

        var zoneTrips = await admin.GetAsync($"{Api}/admin/reports/export?dataset=trips&from={D}&to={D}&zoneId={(await fixture.DataAsync()).ZoneB}");
        Assert.Equal(3, (await zoneTrips.Content.ReadAsStringAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);

        foreach (var dataset in new[] { "cancellations", "ratings", "support_tickets", "payments", "payouts", "drivers", "incentives", "kpis" })
        {
            var range = dataset == "kpis" ? "from=2026-09-10&to=2026-09-10" : $"from={D}&to={D}";
            var export = await admin.GetAsync($"{Api}/admin/reports/export?dataset={dataset}&{range}");
            Assert.True(export.StatusCode == HttpStatusCode.OK, $"{dataset}: {(int)export.StatusCode} {await export.Content.ReadAsStringAsync()}");
        }

        var cancellations = (await (await admin.GetAsync($"{Api}/admin/reports/export?dataset=cancellations&from={D}&to={D}")).Content.ReadAsStringAsync())
            .TrimStart('﻿').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("trip_number,created_at,actor,at_fault,stage,reason_code,fee_amount,fee_charged,fee_status,compensation,penalty_points,excuse_status", cancellations[0]);
        Assert.Equal(6, cancellations.Length);
        var drivers = (await (await admin.GetAsync($"{Api}/admin/reports/export?dataset=drivers&from={D}&to={D}")).Content.ReadAsStringAsync())
            .TrimStart('﻿').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var driver1 = drivers.Single(l => l.Contains("RPT-0001")).Split(',');
        Assert.Equal(("2", "4"), (driver1[7], driver1[8]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/reports/export?dataset=secrets&from={D}&to={D}")).StatusCode);

        using var viewer = await TestAdmins.LoginWithAsync(fixture, "report.viewer", "Viewer#Pass123", PermissionCatalog.ReportsView);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Api}/admin/reports/kpis?from={D}&to={D}&metrics=gmv")).StatusCode);
        var denied = await viewer.GetAsync($"{Api}/admin/reports/export?dataset=trips&from={D1}&to={D1}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(PermissionCatalog.ReportsExport, (await denied.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("permission").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"{Api}/admin/reports/snapshots/rebuild", new { from = D, to = D })).StatusCode);
    }

    [Fact]
    public async Task Dashboard_summary_has_the_live_today_block()
    {
        using var admin = await AdminAsync();
        var summary = await (await admin.GetAsync($"{Api}/admin/dashboard/summary")).ReadJsonAsync();
        var today = summary.GetProperty("today");
        Assert.Equal(0, today.GetProperty("completedTrips").GetInt32());
        Assert.Equal(0m, today.GetProperty("gmv").GetDecimal());
        Assert.True(today.TryGetProperty("onlineDrivers", out _));
        Assert.True(today.TryGetProperty("openSafetyCases", out _));
        Assert.True(today.TryGetProperty("openTickets", out _));
        Assert.Equal(JsonValueKind.Null, today.GetProperty("avgTimeToAssignSeconds").ValueKind);
    }
}
