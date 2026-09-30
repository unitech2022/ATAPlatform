using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Support;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Domain.Support;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ATA.Tests.Infrastructure;

/// <summary>Records the F18 SignalR events instead of sending them.</summary>
public sealed class RecordingSupportNotifier : ISupportNotifier
{
    public ConcurrentQueue<(Guid UserId, SupportTicketUserEvent Event)> UserUpdates { get; } = [];
    public ConcurrentQueue<SupportTicketAdminEvent> AdminUpdates { get; } = [];
    public ConcurrentQueue<AdminTicketListItemDto> Created { get; } = [];

    public Task TicketUpdatedForUserAsync(Guid userId, SupportTicketUserEvent update, CancellationToken ct)
    {
        UserUpdates.Enqueue((userId, update));
        return Task.CompletedTask;
    }

    public Task TicketUpdatedForAdminsAsync(SupportTicketAdminEvent update, CancellationToken ct)
    {
        AdminUpdates.Enqueue(update);
        return Task.CompletedTask;
    }

    public Task TicketCreatedForAdminsAsync(AdminTicketListItemDto ticket, CancellationToken ct)
    {
        Created.Enqueue(ticket);
        return Task.CompletedTask;
    }
}

/// <summary>API host for the F18 tests: recording SignalR notifier and a low refund four-eyes limit (20 SAR) so a full refund of a trip fare (above 20 SAR) needs a second admin while a 10 SAR partial refund does not.</summary>
public sealed class SupportFixture() : ApiFixture(new Dictionary<string, string?>
{
    ["Payments:RefundAutoApproveLimit"] = "20",
}, services =>
{
    services.RemoveAll<ISupportNotifier>();
    services.AddSingleton<RecordingSupportNotifier>();
    services.AddSingleton<ISupportNotifier>(sp => sp.GetRequiredService<RecordingSupportNotifier>());
})
{
    public RecordingSupportNotifier Realtime => Factory.Services.GetRequiredService<RecordingSupportNotifier>();
}

/// <summary>Shared steps for the support tests.</summary>
public static class SupportFlow
{
    public const string Base = "/api/v1";

    /// <summary>A passenger and a driver with a completed cash trip in <paramref name="area"/>.</summary>
    public static async Task<SafetyFlow.Ride> CompletedRideAsync(ApiFixture fixture, (decimal Lat, decimal Lng) area, string paymentMethod = "cash", SafetyFlow.Party? passenger = null)
    {
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area, paymentMethod, passenger);
        await SafetyFlow.CompleteAsync(ride);
        return ride;
    }

    public static async Task<JsonElement> CreateTicketAsync(HttpClient client, string type = "other", string? tripId = null, string subject = "مشكلة في الخدمة", string message = "تفاصيل المشكلة",
        object? dispute = null, IEnumerable<string>? fileIds = null)
    {
        var response = await client.PostAsJsonAsync($"{Base}/support/tickets", new { type, tripId, subject, message, fileIds, dispute });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task<string> UploadAsync(HttpClient client, string fileName = "proof.png", byte[]? bytes = null)
    {
        var response = await UploadRawAsync(client, fileName, bytes);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("fileId").GetString()!;
    }

    public static async Task<HttpResponseMessage> UploadRawAsync(HttpClient client, string fileName, byte[]? bytes = null)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes ?? [0x89, 0x50, 0x4E, 0x47, 1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "file", fileName);
        return await client.PostAsync($"{Base}/support/attachments", content);
    }

    /// <summary>An admin account holding exactly <paramref name="permissions"/>.</summary>
    public static Task<HttpClient> AdminWithAsync(ApiFixture fixture, string username, params string[] permissions) =>
        TestAdmins.LoginWithAsync(fixture, username, "Agent@12345", permissions);

    public static Task<SupportTicket> TicketAsync(ApiFixture fixture, string id) =>
        fixture.Factory.WithDbAsync(db => db.SupportTickets.AsNoTracking().FirstAsync(t => t.Id == Guid.Parse(id)));

    public static Task UpdateTicketAsync(ApiFixture fixture, string id, Action<SupportTicket> change) =>
        fixture.Factory.WithDbAsync(async db =>
        {
            var ticket = await db.SupportTickets.FirstAsync(t => t.Id == Guid.Parse(id));
            change(ticket);
            await db.SaveChangesAsync();
            return true;
        });

    public static async Task<JsonElement> AdminTicketAsync(HttpClient admin, string id) =>
        await (await admin.GetAsync($"{Base}/admin/support/tickets/{id}")).ReadJsonAsync();

    public static async Task<JsonElement> UserTicketAsync(HttpClient client, string id) =>
        await (await client.GetAsync($"{Base}/support/tickets/{id}")).ReadJsonAsync();

    public static Task<HttpResponseMessage> AdminPostAsync(HttpClient admin, string ticketId, string action, object body) =>
        admin.PostAsJsonAsync($"{Base}/admin/support/tickets/{ticketId}/{action}", body);
}
