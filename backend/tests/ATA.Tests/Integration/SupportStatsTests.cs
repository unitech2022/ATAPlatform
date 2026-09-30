using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Tests.Infrastructure;

namespace ATA.Tests.Integration;

/// <summary>The support KPIs of doc 11 §F18.5 over a known set of tickets (its own host).</summary>
public class SupportStatsTests(SupportFixture fixture) : IClassFixture<SupportFixture>
{
    private const string Api = SupportFlow.Base;

    private static string Str(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    [Fact]
    public async Task Stats_report_resolution_and_first_response_times_without_the_paused_time_sla_compliance_and_csat()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var clock = fixture.Factory.Clock;

        var empty = await (await admin.GetAsync($"{Api}/admin/support/stats")).ReadJsonAsync();
        Assert.Equal(0, empty.GetProperty("created").GetInt32());
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("avgResolutionMinutes").ValueKind);
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("slaCompliance").ValueKind);
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("csatAvg").ValueKind);

        // t0: T1 and T3 are created. +10: T1 answered and put on hold (pending_user). +20: the user answers (10 minutes of pause).
        // +30: T1 resolved (resolution time 30 − 10 = 20 minutes). T2 is created at +30, answered at +50, resolved at +55 (25 minutes).
        var t1 = Str(await SupportFlow.CreateTicketAsync(passenger.Client), "id");
        await SupportFlow.CreateTicketAsync(passenger.Client, "account");
        clock.Advance(TimeSpan.FromMinutes(10));
        (await SupportFlow.AdminPostAsync(admin, t1, "messages", new { body = "مرحباً", isInternal = false })).EnsureSuccessStatusCode();
        (await SupportFlow.AdminPostAsync(admin, t1, "status", new { status = "pending_user" })).EnsureSuccessStatusCode();
        clock.Advance(TimeSpan.FromMinutes(10));
        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{t1}/messages", new { body = "رد" })).EnsureSuccessStatusCode();
        clock.Advance(TimeSpan.FromMinutes(10));
        (await SupportFlow.AdminPostAsync(admin, t1, "status", new { status = "resolved" })).EnsureSuccessStatusCode();
        var t2 = Str(await SupportFlow.CreateTicketAsync(passenger.Client), "id");
        clock.Advance(TimeSpan.FromMinutes(20));
        (await SupportFlow.AdminPostAsync(admin, t2, "messages", new { body = "مرحباً", isInternal = false })).EnsureSuccessStatusCode();
        clock.Advance(TimeSpan.FromMinutes(5));
        (await SupportFlow.AdminPostAsync(admin, t2, "status", new { status = "resolved" })).EnsureSuccessStatusCode();
        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{t1}/csat", new { score = 5 })).EnsureSuccessStatusCode();
        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{t2}/csat", new { score = 3 })).EnsureSuccessStatusCode();
        Assert.Equal(600, (await SupportFlow.TicketAsync(fixture, t1)).SlaPausedSeconds);

        var stats = await (await admin.GetAsync($"{Api}/admin/support/stats")).ReadJsonAsync();
        Assert.Equal(3, stats.GetProperty("created").GetInt32());
        Assert.Equal(2, stats.GetProperty("resolved").GetInt32());
        Assert.Equal(0, stats.GetProperty("closed").GetInt32());
        Assert.Equal(1, stats.GetProperty("openNow").GetInt32());
        Assert.Equal(22.5, stats.GetProperty("avgResolutionMinutes").GetDouble(), 1);
        Assert.Equal(22.5, stats.GetProperty("medianResolutionMinutes").GetDouble(), 1);
        Assert.Equal(15.0, stats.GetProperty("avgFirstResponseMinutes").GetDouble(), 1);
        Assert.Equal(15.0, stats.GetProperty("medianFirstResponseMinutes").GetDouble(), 1);
        Assert.Equal(1.0, stats.GetProperty("slaCompliance").GetDouble(), 3);
        Assert.Equal(4.0, stats.GetProperty("csatAvg").GetDouble(), 2);
        Assert.Equal(2, stats.GetProperty("csatCount").GetInt32());
        var byType = stats.GetProperty("byType").EnumerateArray().ToDictionary(t => Str(t, "type"));
        Assert.Equal(2, byType["other"].GetProperty("created").GetInt32());
        Assert.Equal(2, byType["other"].GetProperty("resolved").GetInt32());
        Assert.Equal(1, byType["account"].GetProperty("created").GetInt32());
        Assert.Equal(0, byType["account"].GetProperty("resolved").GetInt32());

        // The summary counters.
        var summary = await (await admin.GetAsync($"{Api}/admin/support/summary")).ReadJsonAsync();
        Assert.Equal(1, summary.GetProperty("open").GetInt32());
        Assert.Equal(1, summary.GetProperty("unassigned").GetInt32());
        Assert.Equal(0, summary.GetProperty("pendingUser").GetInt32());
        Assert.Equal(0, summary.GetProperty("breachingFirstResponse").GetInt32());
        Assert.Equal(0, summary.GetProperty("breachingResolution").GetInt32());
        Assert.Equal(15.0, summary.GetProperty("avgFirstResponseMinutes").GetDouble(), 1);
        Assert.Equal(0.375, summary.GetProperty("avgResolutionHours").GetDouble(), 2);
        Assert.Equal(4.0, summary.GetProperty("csatAvg").GetDouble(), 2);

        // A range in another day, the same day, and an invalid range; the SLA compliance drops when a ticket was resolved late.
        var today = DateOnly.FromDateTime(clock.UtcNow.AddHours(3));
        var sameDay = await (await admin.GetAsync($"{Api}/admin/support/stats?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}")).ReadJsonAsync();
        Assert.Equal(3, sameDay.GetProperty("created").GetInt32());
        Assert.Equal(2, sameDay.GetProperty("resolved").GetInt32());
        var tomorrow = await (await admin.GetAsync($"{Api}/admin/support/stats?from={today.AddDays(1):yyyy-MM-dd}")).ReadJsonAsync();
        Assert.Equal(0, tomorrow.GetProperty("created").GetInt32());
        Assert.Equal(JsonValueKind.Null, tomorrow.GetProperty("avgResolutionMinutes").ValueKind);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/support/stats?from=2026-09-29&to=2026-09-28")).StatusCode);

        await SupportFlow.UpdateTicketAsync(fixture, t2, t => t.ResolutionDueAt = t.ResolvedAt!.Value.AddMinutes(-1));
        var late = await (await admin.GetAsync($"{Api}/admin/support/stats")).ReadJsonAsync();
        Assert.Equal(0.5, late.GetProperty("slaCompliance").GetDouble(), 3);
    }
}
