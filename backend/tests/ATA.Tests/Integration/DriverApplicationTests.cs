using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Drivers;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class DriverApplicationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Submit_lists_missing_steps_until_everything_is_provided()
    {
        var (driver, _) = await fixture.LoginAsync("driver");

        var empty = await driver.PostAsync("/api/v1/driver/application/submit", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
        var error = (await empty.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("validation_failed", error.GetProperty("code").GetString());
        var missing = error.GetProperty("details").GetProperty("missing").EnumerateArray().Select(m => m.GetString()).ToList();
        Assert.Contains("profile", missing);
        Assert.Contains("vehicle", missing);
        Assert.Contains("documents:insurance", missing);
        Assert.Contains("documents:national_id", missing);

        await DriverFlow.CompleteProfileAndVehicleAsync(driver, fixture.NextPhone());
        var afterProfile = await driver.PostAsync("/api/v1/driver/application/submit", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, afterProfile.StatusCode);
        missing = (await afterProfile.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("missing").EnumerateArray().Select(m => m.GetString()).ToList();
        Assert.DoesNotContain("profile", missing);
        Assert.DoesNotContain("vehicle", missing);
        Assert.All(missing, m => Assert.StartsWith("documents:", m));

        await DriverFlow.UploadAllDocumentsAsync(driver);
        var application = await (await driver.GetAsync("/api/v1/driver/application")).ReadJsonAsync();
        Assert.True(application.GetProperty("steps").GetProperty("canSubmit").GetBoolean());
        Assert.Equal(5, application.GetProperty("documents").GetArrayLength());
        Assert.All(application.GetProperty("requiredDocuments").EnumerateArray(), d => Assert.True(d.GetProperty("uploaded").GetBoolean()));

        var submitted = await driver.PostAsync("/api/v1/driver/application/submit", null);
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        Assert.Equal("submitted", (await submitted.ReadJsonAsync()).GetProperty("status").GetString());

        var locked = await driver.PutAsJsonAsync("/api/v1/driver/application/profile", DriverFlow.Profile("Locked"));
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
    }

    [Fact]
    public async Task Profile_validation_rejects_bad_national_id()
    {
        var (driver, _) = await fixture.LoginAsync("driver");
        var response = await driver.PutAsJsonAsync("/api/v1/driver/application/profile", new
        {
            fullName = "Test", nationalId = "12", dateOfBirth = "1990-01-01", cityId = SeedIds.CityRiyadh, gender = "male",
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var details = (await response.ReadJsonAsync()).GetProperty("error").GetProperty("details");
        Assert.True(details.TryGetProperty("nationalId", out _));
    }

    [Fact]
    public async Task Document_upload_rejects_unsupported_type_and_requires_expiry()
    {
        var (driver, _) = await fixture.LoginAsync("driver");

        var bad = await driver.PostAsync("/api/v1/driver/documents", DriverFlow.Multipart(SeedIds.DocumentTypes.ProfilePhoto, "photo.gif", "image/gif", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);
        Assert.Equal("unsupported_file_type", await bad.ErrorCodeAsync());

        var noExpiry = await driver.PostAsync("/api/v1/driver/documents", DriverFlow.Multipart(SeedIds.DocumentTypes.Insurance, "ins.pdf", "application/pdf", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noExpiry.StatusCode);
        Assert.Equal("validation_failed", await noExpiry.ErrorCodeAsync());

        var ok = await driver.PostAsync("/api/v1/driver/documents", DriverFlow.Multipart(SeedIds.DocumentTypes.Insurance, "ins.pdf", "application/pdf", "2030-01-01"));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var doc = await ok.ReadJsonAsync();
        Assert.Equal("pending", doc.GetProperty("status").GetString());
        Assert.Equal("insurance", doc.GetProperty("documentTypeCode").GetString());

        var file = await driver.GetAsync($"/api/v1/files/{doc.GetProperty("fileId").GetString()}");
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("application/pdf", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal("inline", file.Content.Headers.ContentDisposition?.DispositionType);

        var replaced = await driver.PostAsync("/api/v1/driver/documents", DriverFlow.Multipart(SeedIds.DocumentTypes.Insurance, "ins2.pdf", "application/pdf", "2031-01-01"));
        Assert.Equal(HttpStatusCode.Created, replaced.StatusCode);
        var count = await fixture.Factory.WithDbAsync(db => db.DriverDocuments.CountAsync(d => d.DocumentTypeId == SeedIds.DocumentTypes.Insurance && d.Id == Guid.Parse(doc.GetProperty("id").GetString()!)));
        Assert.Equal(0, count);

        var deleted = await driver.DeleteAsync($"/api/v1/driver/documents/{(await replaced.ReadJsonAsync()).GetProperty("id").GetString()}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Going_online_requires_approval()
    {
        var (driver, _) = await fixture.LoginAsync("driver");
        var status = await (await driver.GetAsync("/api/v1/driver/status")).ReadJsonAsync();
        Assert.False(status.GetProperty("canGoOnline").GetBoolean());
        Assert.Equal("driver_not_approved", status.GetProperty("reason").GetString());

        var response = await driver.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("driver_not_approved", await response.ErrorCodeAsync());
    }
}

/// <summary>Shared steps to bring a driver application to the submitted state.</summary>
public static class DriverFlow
{
    public static object Profile(string name) => new
    {
        fullName = name, nationalId = "1012345678", dateOfBirth = "1990-05-20", cityId = SeedIds.CityRiyadh, gender = "male", iban = "SA0380000000608010167519",
    };

    public static async Task CompleteProfileAndVehicleAsync(HttpClient driver, string uniqueSuffix)
    {
        var profile = await driver.PutAsJsonAsync("/api/v1/driver/application/profile", Profile("سائق تجريبي"));
        profile.EnsureSuccessStatusCode();
        var vehicle = await driver.PutAsJsonAsync("/api/v1/driver/application/vehicle", new
        {
            make = "Toyota", model = "Camry", year = 2023, color = "White", plateNumber = $"ABC {uniqueSuffix[^4..]}", seats = 4, rideCategoryId = SeedIds.RideCategories.Economy,
        });
        vehicle.EnsureSuccessStatusCode();
    }

    public static async Task UploadAllDocumentsAsync(HttpClient driver)
    {
        foreach (var (typeId, expiry) in new[]
                 {
                     (SeedIds.DocumentTypes.NationalId, "2030-01-01"),
                     (SeedIds.DocumentTypes.DrivingLicense, "2030-01-01"),
                     (SeedIds.DocumentTypes.VehicleRegistration, "2030-01-01"),
                     (SeedIds.DocumentTypes.Insurance, "2030-01-01"),
                     (SeedIds.DocumentTypes.ProfilePhoto, (string?)null),
                 })
        {
            var response = await driver.PostAsync("/api/v1/driver/documents", Multipart(typeId, "doc.png", "image/png", expiry));
            response.EnsureSuccessStatusCode();
        }
    }

    public static MultipartFormDataContent Multipart(Guid documentTypeId, string fileName, string contentType, string? expiresAt)
    {
        var content = new MultipartFormDataContent { { new StringContent(documentTypeId.ToString()), "documentTypeId" } };
        var file = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 });
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        if (expiresAt is not null)
        {
            content.Add(new StringContent(expiresAt), "expiresAt");
        }

        return content;
    }

    public static async Task<Guid> SubmitAsync(ApiFixture fixture, HttpClient driver)
    {
        await CompleteProfileAndVehicleAsync(driver, fixture.NextPhone());
        await UploadAllDocumentsAsync(driver);
        (await driver.PostAsync("/api/v1/driver/application/submit", null)).EnsureSuccessStatusCode();
        var application = await (await driver.GetAsync("/api/v1/driver/application")).ReadJsonAsync();
        var number = application.GetProperty("applicationNumber").GetString();
        return await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.ApplicationNumber == number).Select(d => d.Id).FirstAsync());
    }

    public static List<Guid> DocumentIds(JsonElement application) =>
        application.GetProperty("documents").EnumerateArray().Select(d => Guid.Parse(d.GetProperty("id").GetString()!)).ToList();
}
