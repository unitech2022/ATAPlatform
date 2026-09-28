using System.Net;
using System.Net.Http.Json;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class AdminReviewTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Approve_requires_every_required_document_verified_and_audits_and_notifies()
    {
        var (driver, auth) = await fixture.LoginAsync("driver");
        var driverId = await DriverFlow.SubmitAsync(fixture, driver);
        using var admin = await fixture.LoginAdminAsync();

        var review = await admin.PostAsJsonAsync($"/api/v1/admin/drivers/{driverId}/review", new { action = "start_review" });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        Assert.Equal("under_review", (await review.ReadJsonAsync()).GetProperty("status").GetString());

        var premature = await admin.PostAsync($"/api/v1/admin/drivers/{driverId}/approve", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, premature.StatusCode);
        var missing = (await premature.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("missing").EnumerateArray().Select(m => m.GetString()).ToList();
        Assert.Equal(5, missing.Count);
        Assert.Contains("documents:insurance", missing);

        var detail = await (await admin.GetAsync($"/api/v1/admin/drivers/{driverId}")).ReadJsonAsync();
        var documentIds = DriverFlow.DocumentIds(detail);
        foreach (var documentId in documentIds.Take(4))
        {
            var verified = await admin.PostAsJsonAsync($"/api/v1/admin/documents/{documentId}/verify", new { status = "verified", note = "ok" });
            Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
            Assert.Equal("verified", (await verified.ReadJsonAsync()).GetProperty("status").GetString());
        }

        var stillMissing = await admin.PostAsync($"/api/v1/admin/drivers/{driverId}/approve", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, stillMissing.StatusCode);
        Assert.Single((await stillMissing.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("missing").EnumerateArray());

        (await admin.PostAsJsonAsync($"/api/v1/admin/documents/{documentIds[4]}/verify", new { status = "verified" })).EnsureSuccessStatusCode();

        var approved = await admin.PostAsync($"/api/v1/admin/drivers/{driverId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal("approved", (await approved.ReadJsonAsync()).GetProperty("status").GetString());

        var userId = Guid.Parse(auth.GetProperty("user").GetProperty("id").GetString()!);
        var notification = await fixture.Factory.WithDbAsync(db => db.Notifications.FirstOrDefaultAsync(n => n.UserId == userId && n.Type == NotificationTypes.DriverApplicationApproved));
        Assert.NotNull(notification);

        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "driver" && a.EntityId == driverId).OrderBy(a => a.CreatedAt).Select(a => a.Action).ToListAsync());
        Assert.Equal(["driver.start_review", "driver.approve"], audits);

        var history = (await (await admin.GetAsync($"/api/v1/admin/drivers/{driverId}")).ReadJsonAsync()).GetProperty("statusHistory");
        Assert.Equal(2, history.GetArrayLength());
        Assert.Equal("approved", history[1].GetProperty("toStatus").GetString());
        Assert.Equal("under_review", history[1].GetProperty("fromStatus").GetString());
        Assert.False(string.IsNullOrEmpty(history[1].GetProperty("id").GetString()));

        var status = await driver.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true, latitude = 24.7, longitude = 46.6 });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.True((await status.ReadJsonAsync()).GetProperty("isOnline").GetBoolean());

        var notifications = await (await driver.GetAsync("/api/v1/notifications")).ReadJsonAsync();
        Assert.True(notifications.GetProperty("unreadCount").GetInt32() >= 2);
        Assert.Equal(HttpStatusCode.NoContent, (await driver.PostAsJsonAsync("/api/v1/notifications/read", new { ids = (Guid[]?)null })).StatusCode);
        Assert.Equal(0, (await (await driver.GetAsync("/api/v1/notifications")).ReadJsonAsync()).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Reject_requires_reason_and_reopens_application_for_editing()
    {
        var (driver, _) = await fixture.LoginAsync("driver");
        var driverId = await DriverFlow.SubmitAsync(fixture, driver);
        using var admin = await fixture.LoginAdminAsync();

        var noReason = await admin.PostAsJsonAsync($"/api/v1/admin/drivers/{driverId}/reject", new { reason = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);

        var rejected = await admin.PostAsJsonAsync($"/api/v1/admin/drivers/{driverId}/reject", new { reason = "صورة الرخصة غير واضحة" });
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);

        var application = await (await driver.GetAsync("/api/v1/driver/application")).ReadJsonAsync();
        Assert.Equal("rejected", application.GetProperty("status").GetString());
        Assert.Equal("صورة الرخصة غير واضحة", application.GetProperty("rejectionReason").GetString());

        var edit = await driver.PutAsJsonAsync("/api/v1/driver/application/profile", DriverFlow.Profile("سائق معدل"));
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        var list = await (await admin.GetAsync("/api/v1/admin/drivers?status=rejected&pageSize=50")).ReadJsonAsync();
        Assert.Contains(list.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetString() == driverId.ToString());
    }

    [Fact]
    public async Task Admin_actions_are_forbidden_for_passengers_and_ride_categories_are_managed()
    {
        var (passenger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/v1/admin/ride-categories")).StatusCode);

        using var admin = await fixture.LoginAdminAsync();
        var created = await admin.PostAsJsonAsync("/api/v1/admin/ride-categories", new { code = "test_cat", nameAr = "تجريبي", nameEn = "Test", seats = 4, maxStops = 2, sortOrder = 99, isActive = false });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.ReadJsonAsync()).GetProperty("id").GetString();

        var dup = await admin.PostAsJsonAsync("/api/v1/admin/ride-categories", new { code = "test_cat", nameAr = "x", nameEn = "y" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/ride-categories/{id}", new { nameEn = "Test 2", isActive = true });
        Assert.Equal("Test 2", (await updated.ReadJsonAsync()).GetProperty("nameEn").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/ride-categories/{id}")).StatusCode);

        var logs = await (await admin.GetAsync($"/api/v1/admin/audit-logs?entityType=ride_category&entityId={id}")).ReadJsonAsync();
        Assert.Equal(3, logs.GetProperty("total").GetInt32());

        var summary = await (await admin.GetAsync("/api/v1/admin/dashboard/summary")).ReadJsonAsync();
        Assert.True(summary.GetProperty("passengers").GetInt32() >= 1);
    }
}
