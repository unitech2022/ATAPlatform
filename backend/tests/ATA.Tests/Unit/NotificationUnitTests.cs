using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Push;
using ATA.Infrastructure.Sms;
using ATA.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ATA.Tests.Unit;

/// <summary>Captures outgoing requests and answers with a scripted response.</summary>
public sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(status) { Content = new StringContent(body) };
    }
}

public class NotificationUnitTests
{
    private static readonly Guid User = Guid.Parse("01998600-0000-7000-8000-000000000001");

    private static OneSignalPushSender Sender(FakeHandler handler) => new(new HttpClient(handler), Options.Create(new OneSignalOptions
    {
        Enabled = true, AppId = "app-123", RestApiKey = "rest-key", ApiBaseUrl = "https://api.onesignal.com",
        AndroidChannels = new Dictionary<string, string> { ["trips"] = "chan-trips" },
    }), NullLogger<OneSignalPushSender>.Instance);

    private static PushMessage Message() => new(
        [User],
        new Dictionary<string, string> { ["ar"] = "وصل الكابتن", ["en"] = "Driver arrived" },
        new Dictionary<string, string> { ["ar"] = "بانتظارك", ["en"] = "Waiting for you" },
        new Dictionary<string, object?> { ["eventCode"] = "trip.driver_arrived", ["deepLink"] = "ata://trip/1" },
        "trips", "high", 600, "trip-1", [new PushButton("ok", "أنا بخير", "I'm OK")], "delivery-1");

    [Fact]
    public async Task OneSignal_request_uses_external_id_aliases_and_the_rest_key()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, """{"id":"os-notification-1","external_id":null}""");
        var result = await Sender(handler).SendAsync(Message(), CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("os-notification-1", result.ProviderMessageId);
        Assert.Equal(1, result.Recipients);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.onesignal.com/notifications", request.RequestUri!.ToString());
        Assert.Equal("Key", request.Headers.Authorization!.Scheme);
        Assert.Equal("rest-key", request.Headers.Authorization.Parameter);
        var json = JsonNode.Parse(body)!;
        Assert.Equal("app-123", json["app_id"]!.GetValue<string>());
        Assert.Equal("push", json["target_channel"]!.GetValue<string>());
        Assert.Equal(User.ToString(), json["include_aliases"]!["external_id"]![0]!.GetValue<string>());
        Assert.Equal("Driver arrived", json["headings"]!["en"]!.GetValue<string>());
        Assert.Equal("وصل الكابتن", json["headings"]!["ar"]!.GetValue<string>());
        Assert.Equal("Waiting for you", json["contents"]!["en"]!.GetValue<string>());
        Assert.Equal("trip.driver_arrived", json["data"]!["eventCode"]!.GetValue<string>());
        Assert.Equal("chan-trips", json["android_channel_id"]!.GetValue<string>());
        Assert.Equal(10, json["priority"]!.GetValue<int>());
        Assert.Equal(600, json["ttl"]!.GetValue<int>());
        Assert.Equal("trip-1", json["collapse_id"]!.GetValue<string>());
        Assert.Equal("ok", json["buttons"]![0]!["id"]!.GetValue<string>());
        Assert.Equal("delivery-1", json["idempotency_key"]!.GetValue<string>());
    }

    [Fact]
    public async Task OneSignal_server_errors_are_transient_and_invalid_aliases_are_reported()
    {
        var failing = await Sender(new FakeHandler(HttpStatusCode.InternalServerError, "oops")).SendAsync(Message(), CancellationToken.None);
        Assert.False(failing.Success);
        Assert.True(failing.IsTransient);
        Assert.Equal("http_500", failing.ErrorCode);

        var rejected = await Sender(new FakeHandler(HttpStatusCode.BadRequest, """{"errors":["app_id not found"]}""")).SendAsync(Message(), CancellationToken.None);
        Assert.False(rejected.Success);
        Assert.False(rejected.IsTransient);

        var invalid = await Sender(new FakeHandler(HttpStatusCode.OK, "{\"id\":\"\",\"errors\":{\"invalid_aliases\":{\"external_id\":[\"" + User + "\"]}}}"))
            .SendAsync(Message(), CancellationToken.None);
        Assert.True(invalid.Success);
        Assert.Equal([User], invalid.InvalidExternalUserIds);
        Assert.Equal(0, invalid.Recipients);
    }

    [Fact]
    public void Renderer_substitutes_language_specific_values_and_counts_sms_segments()
    {
        var values = NotificationPlaceholders.Of(("tripNumber", "T-1")).Money("fare", 46m);
        var missing = new List<string>();
        Assert.Equal("أجرة T-1: 46.00 ر.س", TemplateRenderer.Render("أجرة {tripNumber}: {fare}", values, "ar"));
        Assert.Equal("Fare T-1: SAR 46.00", TemplateRenderer.Render("Fare {tripNumber}: {fare}", values, "en"));
        Assert.Equal("Hello ", TemplateRenderer.Render("Hello {name}", values, "en", missing));
        Assert.Equal(["name"], missing);
        Assert.Equal(["driverName", "plateNumber"], TemplateRenderer.PlaceholdersIn("{driverName} · {plateNumber} · {driverName}"));
        Assert.Equal(1, TemplateRenderer.SmsSegments(new string('a', 160)));
        Assert.Equal(2, TemplateRenderer.SmsSegments(new string('a', 161)));
        Assert.Equal(1, TemplateRenderer.SmsSegments(new string('ع', 70)));
        Assert.Equal(2, TemplateRenderer.SmsSegments(new string('ع', 71)));
    }

    [Fact]
    public void Catalogue_maps_categories_to_preferences_and_normalizes_legacy_types()
    {
        Assert.Equal("trip.completed", NotificationEvents.NormalizeLegacy("trip_completed"));
        Assert.Equal("driver.application.approved", NotificationEvents.NormalizeLegacy("driver_application_approved"));
        Assert.Null(NotificationEvents.PreferenceOf(NotificationCategory.System));
        Assert.Null(NotificationEvents.PreferenceOf(NotificationCategory.Offers));
        Assert.False(NotificationEvents.PreferenceOf(NotificationCategory.Promotions)!(new NotificationPreference { Offers = false }));
        var offer = NotificationEvents.Find(NotificationTypes.OfferReceived)!;
        Assert.False(offer.CreatesInbox);
        Assert.Equal([NotificationChannel.Sms], NotificationEvents.Find("safety.sos_contact")!.AllowedChannels);
        Assert.All(NotificationEvents.All, e => Assert.All(TemplateRenderer.PlaceholdersIn(e.Text.BodyAr + e.Text.BodyEn + e.Text.TitleAr + e.Text.TitleEn), p => Assert.Contains(p, e.Placeholders)));
    }

    [Fact]
    public async Task Sms_sandbox_never_calls_http_and_taqnyat_adapter_sends_the_expected_request()
    {
        using var factory = new AtaWebApplicationFactory(new Dictionary<string, string?> { ["Sms:Provider"] = "taqnyat", ["Sms:Sandbox"] = "true", ["Sms:Taqnyat:BearerToken"] = "t" });
        Assert.IsType<LoggingSmsSender>(factory.Services.GetRequiredService<ISmsSender>());

        var handler = new FakeHandler(HttpStatusCode.Created, """{"messageId":"m-1"}""");
        var taqnyat = new TaqnyatSmsSender(new HttpClient(handler), Options.Create(new SmsOptions { Sandbox = false, Provider = "taqnyat", Taqnyat = new() { BaseUrl = "https://api.taqnyat.sa", BearerToken = "secret" } }));
        var result = await taqnyat.SendAsync("+966512345678", "مرحبا");
        Assert.True(result.Success);
        Assert.Equal("m-1", result.ProviderMessageId);
        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal("https://api.taqnyat.sa/v1/messages", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        var json = JsonNode.Parse(body)!;
        Assert.Equal("966512345678", json["recipients"]![0]!.GetValue<string>());
        Assert.Equal("ATA", json["sender"]!.GetValue<string>());
    }

    [Fact]
    public async Task Moyasar_adapter_maps_statuses_and_verifies_webhook_signatures()
    {
        Assert.Equal(GatewayStatus.Captured, MoyasarGateway.MapStatus(JsonNode.Parse("""{"status":"paid"}""")!));
        Assert.Equal(GatewayStatus.RequiresAction, MoyasarGateway.MapStatus(JsonNode.Parse("""{"status":"initiated","source":{"transaction_url":"https://3ds"}}""")!));
        Assert.Equal(GatewayStatus.PartiallyRefunded, MoyasarGateway.MapStatus(JsonNode.Parse("""{"status":"refunded","refunded":1000,"captured":5000}""")!));
        Assert.Equal(GatewayStatus.Refunded, MoyasarGateway.MapStatus(JsonNode.Parse("""{"status":"refunded","refunded":5000,"captured":5000}""")!));

        var gateway = new MoyasarGateway(new HttpClient(new FakeHandler(HttpStatusCode.OK, "{}")), Options.Create(new PaymentsOptions { Moyasar = new() { WebhookSecret = "whsec" } }));
        var body = """{"id":"evt_1","type":"payment_paid","secret_token":"whsec","data":{"id":"pay_1","status":"paid","amount":4650}}""";
        var parsed = gateway.ParseWebhook(body, new Microsoft.AspNetCore.Http.HeaderDictionary());
        Assert.True(parsed.SignatureValid);
        Assert.Equal(46.5m, parsed.Amount);
        Assert.Equal(GatewayStatus.Captured, parsed.Status);
        Assert.False(gateway.ParseWebhook(body.Replace("whsec", "nope"), new Microsoft.AspNetCore.Http.HeaderDictionary()).SignatureValid);
        var unavailable = await Assert.ThrowsAsync<ATA.Domain.Common.DomainException>(() => gateway.FetchAsync("pay_1", CancellationToken.None));
        Assert.Equal("payment_provider_unavailable", unavailable.Code);
    }
}
