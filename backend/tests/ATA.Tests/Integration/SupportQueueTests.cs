using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Tests.Infrastructure;

namespace ATA.Tests.Integration;

/// <summary>The admin queue over a known set of tickets (its own host, so the totals are exact).</summary>
public class SupportQueueTests(SupportFixture fixture) : IClassFixture<SupportFixture>
{
    private const string Api = SupportFlow.Base;

    private static string Str(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    private static List<string> Ids(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(i => Str(i, "id")).ToList();

    [Fact]
    public async Task The_queue_filters_by_status_type_priority_channel_requester_assignee_search_and_dates_and_orders_by_urgency()
    {
        var passengerOne = await SafetyFlow.PassengerAsync(fixture, "منى القحطاني");
        var passengerTwo = await SafetyFlow.PassengerAsync(fixture, "هدى الشمري");
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(0), passenger: passengerOne);
        var rideTwo = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(1), passenger: passengerTwo);
        var admin = await fixture.LoginAdminAsync();
        var agentId = await SafetyFlow.AdminUserIdAsync(fixture);

        var a = Str(await SupportFlow.CreateTicketAsync(passengerOne.Client, "trip_issue", ride.TripId, "مسار مختلف عن الخريطة", "تفاصيل"), "id");
        var b = Str(await SupportFlow.CreateTicketAsync(ride.Driver.Client, "safety", null, "موقف مقلق في الطريق", "تفاصيل"), "id");
        var c = Str(await SupportFlow.CreateTicketAsync(passengerTwo.Client, "payment_issue", rideTwo.TripId, "خصم مكرر من البطاقة", "تفاصيل"), "id");
        var phoneResponse = await admin.PostAsJsonAsync($"{Api}/admin/support/tickets", new { requesterUserId = passengerOne.UserId, type = "account", subject = "تغيير رقم الجوال", message = "اتصال هاتفي", channel = "phone" });
        var d = Str(await phoneResponse.ReadJsonAsync(), "id");
        (await SupportFlow.AdminPostAsync(admin, b, "assign", new { userId = agentId })).EnsureSuccessStatusCode();
        (await SupportFlow.AdminPostAsync(admin, c, "status", new { status = "pending_user" })).EnsureSuccessStatusCode();
        (await SupportFlow.AdminPostAsync(admin, a, "status", new { status = "resolved" })).EnsureSuccessStatusCode();
        var numberA = (await SupportFlow.TicketAsync(fixture, a)).TicketNumber;

        async Task<JsonElement> ListAsync(string query) => await (await admin.GetAsync($"{Api}/admin/support/tickets?pageSize=100&{query}")).ReadJsonAsync();

        Assert.Equal(4, (await ListAsync("")).GetProperty("total").GetInt32());
        Assert.Equal([c], Ids(await ListAsync("status=pending_user")));
        Assert.Equal([a], Ids(await ListAsync("status=resolved")));
        Assert.Equal([b], Ids(await ListAsync("type=safety")));
        Assert.Equal([b], Ids(await ListAsync("priority=urgent")));
        Assert.Equal([c], Ids(await ListAsync("priority=high")));
        Assert.Equal([d], Ids(await ListAsync("channel=phone")));
        Assert.Equal(3, (await ListAsync("channel=app")).GetProperty("total").GetInt32());
        Assert.Equal([b], Ids(await ListAsync("assignedTo=me")));
        Assert.Equal([b], Ids(await ListAsync($"assignedTo={agentId}")));
        // "Unassigned" means an active ticket nobody took (resolved ones are left out unless a status is asked for).
        Assert.Equal([c, d], Ids(await ListAsync("assignedTo=unassigned")));
        Assert.Equal([a], Ids(await ListAsync("assignedTo=unassigned&status=resolved")));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/support/tickets?assignedTo=nobody")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/support/tickets?status=bogus")).StatusCode);

        // By requester and by free-text search (ticket number, subject, trip number, phone, name).
        Assert.Equal([d, a], Ids(await ListAsync($"requesterUserId={passengerOne.UserId}")));
        Assert.Equal([a], Ids(await ListAsync($"search={numberA}")));
        Assert.Equal([c], Ids(await ListAsync($"search={Uri.EscapeDataString("خصم مكرر")}")));
        Assert.Equal([a], Ids(await ListAsync($"search={ride.TripNumber}")));
        Assert.Equal([c], Ids(await ListAsync($"search={rideTwo.TripNumber}")));
        Assert.Equal(2, (await ListAsync($"search={Uri.EscapeDataString(passengerOne.Phone)}")).GetProperty("total").GetInt32());
        Assert.Equal([c], Ids(await ListAsync($"search={Uri.EscapeDataString("هدى")}")));
        Assert.Empty(Ids(await ListAsync("search=zzz-none")));

        // Dates (Riyadh days of the creation time) and paging.
        var today = DateOnly.FromDateTime(fixture.Factory.Clock.UtcNow.AddHours(3));
        Assert.Equal(4, (await ListAsync($"from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await ListAsync($"to={today.AddDays(-1):yyyy-MM-dd}")).GetProperty("total").GetInt32());
        Assert.Equal(0, (await ListAsync($"from={today.AddDays(1):yyyy-MM-dd}")).GetProperty("total").GetInt32());
        var second = await (await admin.GetAsync($"{Api}/admin/support/tickets?pageSize=2&page=2")).ReadJsonAsync();
        Assert.Equal(2, second.GetProperty("items").GetArrayLength());
        Assert.Equal(4, second.GetProperty("total").GetInt32());

        // Queue order: active first, urgent → high → normal, the resolved ticket last.
        var all = Ids(await ListAsync(""));
        Assert.Equal([b, c, d, a], all);

        // Row fields.
        var row = (await ListAsync("")).GetProperty("items").EnumerateArray().First(i => Str(i, "id") == c);
        Assert.Equal("هدى الشمري", Str(row, "requesterName"));
        Assert.Equal("passenger", Str(row, "requesterRole"));
        Assert.Equal(rideTwo.TripNumber, Str(row, "tripNumber"));
        Assert.Equal("pending_user", Str(row, "status"));
        Assert.Equal("user", Str(row, "lastMessageBy"));
        Assert.Equal("app", Str(row, "channel"));
        Assert.Equal(passengerTwo.UserId.ToString(), Str(row, "requesterUserId"));
        Assert.Equal(JsonValueKind.Null, row.GetProperty("assignedToName").ValueKind);
        var assignedRow = (await ListAsync("")).GetProperty("items").EnumerateArray().First(i => Str(i, "id") == b);
        Assert.False(string.IsNullOrEmpty(Str(assignedRow, "assignedToName")));
        Assert.Equal("driver", Str(assignedRow, "requesterRole"));
    }
}
