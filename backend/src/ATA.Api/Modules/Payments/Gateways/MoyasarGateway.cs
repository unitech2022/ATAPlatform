using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments.Gateways;

/// <summary>
/// Moyasar-style adapter (placeholder until a merchant account and the provider's final documentation are available). Amounts are sent
/// in halalas (×100); <c>source.manual=true</c> authorizes without capture; 3-D Secure returns <c>source.transaction_url</c>.
/// Without <c>Payments:Moyasar:SecretKey</c> no HTTP call is made and every operation fails with <c>payment_provider_unavailable</c>.
/// </summary>
public sealed class MoyasarGateway(HttpClient http, IOptions<PaymentsOptions> options) : IPaymentGateway
{
    private readonly PaymentsOptions _payments = options.Value;
    private MoyasarOptions Options => _payments.Moyasar;

    public string Provider => PaymentProviders.Moyasar;

    public Task<GatewayResult> AuthorizeAsync(GatewayChargeRequest request, CancellationToken ct) => CreateAsync(request, manual: true, ct);

    public Task<GatewayResult> PurchaseAsync(GatewayChargeRequest request, CancellationToken ct) => CreateAsync(request, manual: false, ct);

    public Task<GatewayResult> CaptureAsync(string gatewayPaymentId, decimal amount, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"v1/payments/{gatewayPaymentId}/capture", new JsonObject { ["amount"] = Halalas(amount) }, ct);

    public Task<GatewayResult> VoidAsync(string gatewayPaymentId, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"v1/payments/{gatewayPaymentId}/void", null, ct);

    public Task<GatewayResult> RefundAsync(string gatewayPaymentId, decimal amount, string idempotencyKey, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"v1/payments/{gatewayPaymentId}/refund", new JsonObject { ["amount"] = Halalas(amount) }, ct, idempotencyKey);

    public Task<GatewayResult> FetchAsync(string gatewayPaymentId, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"v1/payments/{gatewayPaymentId}", null, ct);

    public async Task<GatewayCardResult> VerifyCardTokenAsync(Guid verificationId, string token, string returnUrl, CancellationToken ct)
    {
        EnsureConfigured();
        // Tokens created with the publishable key are verified by fetching them; the provider performs its own 3-D Secure verification.
        using var request = NewRequest(HttpMethod.Get, $"v1/tokens/{Uri.EscapeDataString(token)}", null, null);
        using var response = await http.SendAsync(request, ct);
        var json = await ReadAsync(response, ct);
        if (!response.IsSuccessStatusCode || json is null)
        {
            return new GatewayCardResult(false, null, null, null, null, null, null, null, "card_verification_failed", json?["message"]?.ToString());
        }

        var status = json["status"]?.ToString();
        var action = json["verification_url"]?.ToString();
        return new GatewayCardResult(
            status is "active" or "initiated", json["id"]?.ToString() ?? token, json["brand"]?.ToString()?.ToLowerInvariant(), json["last_four"]?.ToString(),
            int.TryParse(json["month"]?.ToString(), out var m) ? m : null, int.TryParse(json["year"]?.ToString(), out var y) ? y : null,
            json["name"]?.ToString(), status == "initiated" ? action : null, status is "active" or "initiated" ? null : "card_verification_failed", null,
            json["fingerprint"]?.ToString());
    }

    public async Task DeleteCardTokenAsync(string token, CancellationToken ct)
    {
        if (!Options.IsConfigured) return;
        using var request = NewRequest(HttpMethod.Delete, $"v1/tokens/{Uri.EscapeDataString(token)}", null, null);
        using var _ = await http.SendAsync(request, ct);
    }

    public WebhookParseResult ParseWebhook(string rawBody, IHeaderDictionary headers)
    {
        var parsed = ParseVerifiedPayload(rawBody);
        var headerSignature = headers[Options.SignatureHeader].ToString();
        var valid = !string.IsNullOrEmpty(headerSignature)
            ? WebhookSignatures.HmacMatches(rawBody, Options.WebhookSecret, headerSignature)
            : WebhookSignatures.SecretMatches(ReadSecretToken(rawBody), Options.WebhookSecret);
        return parsed with { SignatureValid = valid };
    }

    public WebhookParseResult ParseVerifiedPayload(string rawBody)
    {
        var json = TryParse(rawBody);
        var data = json?["data"];
        var amount = decimal.TryParse(data?["amount"]?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var halalas) ? halalas / 100m : (decimal?)null;
        return new WebhookParseResult(true, json?["id"]?.ToString(), json?["type"]?.ToString(), data?["id"]?.ToString(),
            data is null ? null : MapStatus(data), amount, data?["source"]?["message"]?.ToString());
    }

    /// <summary><c>initiated→initiated</c>, <c>authorized→authorized</c>, <c>captured|paid→captured</c>, <c>failed</c>, <c>voided</c>, <c>refunded→refunded|partially_refunded</c>.</summary>
    public static GatewayStatus MapStatus(JsonNode payment)
    {
        var status = payment["status"]?.ToString();
        return status switch
        {
            "initiated" => payment["source"]?["transaction_url"] is not null ? GatewayStatus.RequiresAction : GatewayStatus.Initiated,
            "authorized" => GatewayStatus.Authorized,
            "captured" or "paid" => GatewayStatus.Captured,
            "voided" => GatewayStatus.Voided,
            "refunded" => ToDecimal(payment["refunded"]) < ToDecimal(payment["captured"]) ? GatewayStatus.PartiallyRefunded : GatewayStatus.Refunded,
            _ => GatewayStatus.Failed,
        };
    }

    private async Task<GatewayResult> CreateAsync(GatewayChargeRequest request, bool manual, CancellationToken ct)
    {
        var source = new JsonObject { ["type"] = request.Source.Type == "apple_pay" ? "applepay" : "token", ["token"] = request.Source.Value };
        if (manual) source["manual"] = "true";
        var metadata = new JsonObject { ["paymentId"] = request.PaymentId.ToString() };
        foreach (var (key, value) in request.Metadata) metadata[key] = value;
        var body = new JsonObject
        {
            ["amount"] = Halalas(request.Amount),
            ["currency"] = request.Currency,
            ["description"] = request.Description,
            ["callback_url"] = $"{_payments.PublicBaseUrl.TrimEnd('/')}/api/v1/payments/return/moyasar",
            ["source"] = source,
            ["metadata"] = metadata,
        };
        return await SendAsync(HttpMethod.Post, "v1/payments", body, ct, request.IdempotencyKey);
    }

    private async Task<GatewayResult> SendAsync(HttpMethod method, string path, JsonObject? body, CancellationToken ct, string? idempotencyKey = null)
    {
        EnsureConfigured();
        using var request = NewRequest(method, path, body, idempotencyKey);
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            return GatewayResult.Transient("network_error", ex.Message);
        }

        using (response)
        {
            var json = await ReadAsync(response, ct);
            var status = (int)response.StatusCode;
            if (status >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return GatewayResult.Transient($"http_{status}", json?["message"]?.ToString() ?? "Gateway error");
            }

            if (!response.IsSuccessStatusCode || json is null)
            {
                return GatewayResult.Declined(json?["type"]?.ToString() ?? $"http_{status}", json?["message"]?.ToString() ?? "Request rejected");
            }

            var mapped = MapStatus(json);
            var amount = ToDecimal(json["amount"]) / 100m;
            var captured = ToDecimal(json["captured"]) / 100m;
            return new GatewayResult(
                mapped != GatewayStatus.Failed, json["id"]?.ToString(), mapped,
                mapped is GatewayStatus.Authorized or GatewayStatus.Captured ? amount : null, captured > 0 ? captured : null,
                json["source"]?["transaction_url"]?.ToString(), mapped == GatewayStatus.Failed ? "card_declined" : null,
                json["source"]?["message"]?.ToString(), false, json["status"]?.ToString());
        }
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string path, JsonObject? body, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(Options.BaseUrl.TrimEnd('/') + "/"), path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Options.SecretKey}:")));
        if (idempotencyKey is not null) request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        if (body is not null) request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        return request;
    }

    private void EnsureConfigured()
    {
        if (!Options.IsConfigured)
        {
            throw new DomainException(ErrorCodes.PaymentProviderUnavailable, new { provider = Provider });
        }
    }

    private static async Task<JsonNode?> ReadAsync(HttpResponseMessage response, CancellationToken ct) => TryParse(await response.Content.ReadAsStringAsync(ct));

    private static JsonNode? TryParse(string text)
    {
        try
        {
            return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadSecretToken(string rawBody) => TryParse(rawBody)?["secret_token"]?.ToString();

    private static long Halalas(decimal amount) => (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

    private static decimal ToDecimal(JsonNode? node) =>
        decimal.TryParse(node?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m;
}
