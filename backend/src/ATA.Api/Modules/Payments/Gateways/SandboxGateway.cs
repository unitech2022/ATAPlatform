using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ATA.Domain.Payments;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments.Gateways;

/// <summary>
/// Development/test gateway driven by fixed tokens (no PAN ever exists):
/// <c>tok_sandbox_mada|visa|mastercard</c> succeed, <c>tok_sandbox_3ds</c> requires the simulated challenge page,
/// <c>tok_sandbox_declined</c> / <c>tok_sandbox_insufficient</c> fail, <c>tok_sandbox_capture_fail</c> authorizes but its capture/purchase is
/// declined, <c>tok_sandbox_timeout</c> authorizes but its capture/purchase times out (transient), <c>applepay_sandbox</c> is an Apple Pay source.
/// Charges live in memory (singleton) for the process lifetime.
/// </summary>
public sealed class SandboxGateway(IOptions<PaymentsOptions> options) : IPaymentGateway
{
    public const string Mada = "tok_sandbox_mada";
    public const string Visa = "tok_sandbox_visa";
    public const string Mastercard = "tok_sandbox_mastercard";
    public const string ThreeDs = "tok_sandbox_3ds";
    public const string Declined = "tok_sandbox_declined";
    public const string Insufficient = "tok_sandbox_insufficient";
    public const string CaptureFail = "tok_sandbox_capture_fail";
    public const string Timeout = "tok_sandbox_timeout";
    public const string ApplePay = "applepay_sandbox";

    private static readonly Dictionary<string, (string Brand, string Last4)> Cards = new(StringComparer.Ordinal)
    {
        [Mada] = ("mada", "4201"),
        [Visa] = ("visa", "4242"),
        [Mastercard] = ("mastercard", "5454"),
        [ThreeDs] = ("visa", "1091"),
        [Declined] = ("visa", "0002"),
        [Insufficient] = ("mada", "9995"),
        [CaptureFail] = ("visa", "3220"),
        [Timeout] = ("visa", "4000"),
        [ApplePay] = ("visa", "0000"),
    };

    public static readonly string[] PublicTokens = [Mada, Visa, Mastercard, ThreeDs, Declined, Insufficient, CaptureFail, Timeout];

    private readonly ConcurrentDictionary<string, SandboxCharge> _charges = new(StringComparer.Ordinal);
    private readonly PaymentsOptions _options = options.Value;

    public string Provider => PaymentProviders.Sandbox;

    private sealed class SandboxCharge
    {
        public required string Id { get; init; }
        public required string Token { get; init; }
        public required Guid PaymentId { get; init; }
        public decimal Amount { get; init; }
        public GatewayStatus Status { get; set; }
        public decimal Authorized { get; set; }
        public decimal Captured { get; set; }
        public decimal Refunded { get; set; }
        /// <summary>State reached when the 3-D Secure challenge is approved.</summary>
        public GatewayStatus OnApprove { get; init; }
    }

    public Task<GatewayResult> AuthorizeAsync(GatewayChargeRequest request, CancellationToken ct) => Task.FromResult(Charge(request, manual: true));

    public Task<GatewayResult> PurchaseAsync(GatewayChargeRequest request, CancellationToken ct) => Task.FromResult(Charge(request, manual: false));

    public Task<GatewayResult> CaptureAsync(string gatewayPaymentId, decimal amount, CancellationToken ct)
    {
        if (!_charges.TryGetValue(gatewayPaymentId, out var charge))
        {
            return Task.FromResult(GatewayResult.Declined("not_found", "Unknown sandbox payment", gatewayPaymentId));
        }

        lock (charge)
        {
            if (charge.Token == Timeout) return Task.FromResult(GatewayResult.Transient("timeout", "Sandbox gateway timeout", charge.Id));
            if (charge.Token == CaptureFail) return Task.FromResult(GatewayResult.Declined("card_declined", "The card was declined", charge.Id));
            if (charge.Status != GatewayStatus.Authorized || amount > charge.Authorized)
            {
                return Task.FromResult(GatewayResult.Declined("invalid_state", $"Cannot capture {amount} from a {charge.Status} payment", charge.Id));
            }

            charge.Status = GatewayStatus.Captured;
            charge.Captured = amount;
            return Task.FromResult(Result(charge));
        }
    }

    public Task<GatewayResult> VoidAsync(string gatewayPaymentId, CancellationToken ct)
    {
        if (!_charges.TryGetValue(gatewayPaymentId, out var charge))
        {
            return Task.FromResult(GatewayResult.Declined("not_found", "Unknown sandbox payment", gatewayPaymentId));
        }

        lock (charge)
        {
            if (charge.Status is GatewayStatus.Authorized or GatewayStatus.Initiated or GatewayStatus.RequiresAction)
            {
                charge.Status = GatewayStatus.Voided;
            }

            return Task.FromResult(charge.Status == GatewayStatus.Voided ? Result(charge) : GatewayResult.Declined("invalid_state", $"Cannot void a {charge.Status} payment", charge.Id));
        }
    }

    public Task<GatewayResult> RefundAsync(string gatewayPaymentId, decimal amount, string idempotencyKey, CancellationToken ct)
    {
        if (!_charges.TryGetValue(gatewayPaymentId, out var charge))
        {
            return Task.FromResult(GatewayResult.Declined("not_found", "Unknown sandbox payment", gatewayPaymentId));
        }

        lock (charge)
        {
            if (charge.Status is not (GatewayStatus.Captured or GatewayStatus.PartiallyRefunded) || amount > charge.Captured - charge.Refunded)
            {
                return Task.FromResult(GatewayResult.Declined("invalid_state", "Nothing left to refund", charge.Id));
            }

            charge.Refunded += amount;
            charge.Status = charge.Refunded >= charge.Captured ? GatewayStatus.Refunded : GatewayStatus.PartiallyRefunded;
            return Task.FromResult(Result(charge) with { Success = true });
        }
    }

    public Task<GatewayResult> FetchAsync(string gatewayPaymentId, CancellationToken ct) =>
        Task.FromResult(_charges.TryGetValue(gatewayPaymentId, out var charge)
            ? Result(charge)
            : GatewayResult.Declined("not_found", "Unknown sandbox payment", gatewayPaymentId));

    public Task<GatewayCardResult> VerifyCardTokenAsync(Guid verificationId, string token, string returnUrl, CancellationToken ct)
    {
        if (!Cards.TryGetValue(token, out var card) || token == ApplePay)
        {
            return Task.FromResult(new GatewayCardResult(false, null, null, null, null, null, null, null, "invalid_token", "Unknown sandbox card token"));
        }

        if (token is Declined or Insufficient)
        {
            var code = token == Declined ? "card_declined" : "insufficient_funds";
            return Task.FromResult(new GatewayCardResult(false, null, card.Brand, card.Last4, null, null, null, null, code, "The card was declined"));
        }

        var expiryYear = DateTime.UtcNow.Year + 3;
        var actionUrl = token == ThreeDs
            ? $"{ChallengeUrl(verificationId)}?returnUrl={Uri.EscapeDataString(returnUrl)}"
            : null;
        return Task.FromResult(new GatewayCardResult(true, token, card.Brand, card.Last4, 12, expiryYear, "SANDBOX CARD", actionUrl, null, null, $"fp_{token}"));
    }

    public Task DeleteCardTokenAsync(string token, CancellationToken ct) => Task.CompletedTask;

    public string ChallengeUrl(Guid id) => $"{_options.PublicBaseUrl.TrimEnd('/')}/api/v1/payments/sandbox/challenge/{id}";

    /// <summary>Outcome of the simulated 3-D Secure page for a charge.</summary>
    public GatewayResult CompleteChallenge(string gatewayPaymentId, bool approve)
    {
        if (!_charges.TryGetValue(gatewayPaymentId, out var charge))
        {
            return GatewayResult.Declined("not_found", "Unknown sandbox payment", gatewayPaymentId);
        }

        lock (charge)
        {
            if (charge.Status == GatewayStatus.RequiresAction)
            {
                if (approve)
                {
                    charge.Status = charge.OnApprove;
                    charge.Authorized = charge.Amount;
                    charge.Captured = charge.OnApprove == GatewayStatus.Captured ? charge.Amount : 0m;
                }
                else
                {
                    charge.Status = GatewayStatus.Failed;
                }
            }

            return charge.Status == GatewayStatus.Failed ? GatewayResult.Declined("authentication_failed", "3-D Secure authentication failed", charge.Id) : Result(charge);
        }
    }

    public WebhookParseResult ParseWebhook(string rawBody, IHeaderDictionary headers)
    {
        var signature = headers[_options.Sandbox.SignatureHeader].ToString();
        var valid = WebhookSignatures.HmacMatches(rawBody, _options.Sandbox.WebhookSecret, signature);
        var parsed = ParseVerifiedPayload(rawBody);
        return parsed with { SignatureValid = valid };
    }

    public WebhookParseResult ParseVerifiedPayload(string rawBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : default;
            string? Str(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            decimal? amount = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number ? a.GetDecimal() : null;
            return new WebhookParseResult(true, Str(root, "id"), Str(root, "type"), Str(data, "id"), MapStatus(Str(data, "status")), amount, Str(data, "failureCode"));
        }
        catch (JsonException)
        {
            return new WebhookParseResult(true, null, null, null, null, null, null);
        }
    }

    public static GatewayStatus? MapStatus(string? status) => status switch
    {
        "initiated" => GatewayStatus.Initiated,
        "requires_action" => GatewayStatus.RequiresAction,
        "authorized" => GatewayStatus.Authorized,
        "captured" or "paid" => GatewayStatus.Captured,
        "failed" => GatewayStatus.Failed,
        "voided" => GatewayStatus.Voided,
        "refunded" => GatewayStatus.Refunded,
        "partially_refunded" => GatewayStatus.PartiallyRefunded,
        _ => null,
    };

    /// <summary>Test hook: sets a sandbox charge's state as if it changed at the provider (used with a signed webhook).</summary>
    public void SetStatus(string gatewayPaymentId, GatewayStatus status)
    {
        if (_charges.TryGetValue(gatewayPaymentId, out var charge))
        {
            lock (charge)
            {
                charge.Status = status;
                if (status == GatewayStatus.Captured) charge.Captured = charge.Amount;
                if (status is GatewayStatus.Authorized or GatewayStatus.Captured) charge.Authorized = charge.Amount;
            }
        }
    }

    private GatewayResult Charge(GatewayChargeRequest request, bool manual)
    {
        var token = request.Source.Value;
        if (!Cards.ContainsKey(token))
        {
            return GatewayResult.Declined("invalid_token", "Unknown sandbox token");
        }

        if (token is Declined) return GatewayResult.Declined("card_declined", "The card was declined");
        if (token is Insufficient) return GatewayResult.Declined("insufficient_funds", "Insufficient funds");
        if (!manual && token == Timeout) return GatewayResult.Transient("timeout", "Sandbox gateway timeout");
        if (!manual && token == CaptureFail) return GatewayResult.Declined("card_declined", "The card was declined");

        var id = $"sbx_{Guid.NewGuid():N}";
        var target = manual ? GatewayStatus.Authorized : GatewayStatus.Captured;
        var charge = new SandboxCharge
        {
            Id = id, Token = token, PaymentId = request.PaymentId, Amount = request.Amount, OnApprove = target,
            Status = token == ThreeDs ? GatewayStatus.RequiresAction : target,
        };
        if (charge.Status != GatewayStatus.RequiresAction)
        {
            charge.Authorized = request.Amount;
            charge.Captured = manual ? 0m : request.Amount;
        }

        _charges[id] = charge;
        var result = Result(charge);
        return charge.Status == GatewayStatus.RequiresAction ? result with { ActionUrl = ChallengeUrl(request.PaymentId) } : result;
    }

    private static GatewayResult Result(SandboxCharge charge) => new(
        charge.Status is not GatewayStatus.Failed,
        charge.Id,
        charge.Status,
        charge.Authorized == 0 ? null : charge.Authorized,
        charge.Captured == 0 ? null : charge.Captured,
        null,
        charge.Status == GatewayStatus.Failed ? "card_declined" : null,
        null,
        false,
        JsonNamingPolicy.SnakeCaseLower.ConvertName(charge.Status.ToString()));
}

public static class WebhookSignatures
{
    /// <summary>Constant-time comparison of a hex (or <c>sha256=</c>-prefixed) HMAC-SHA256 of the raw body.</summary>
    public static bool HmacMatches(string rawBody, string? secret, string? signature)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var provided = signature.Trim();
        if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)) provided = provided[7..];
        var expected = Compute(rawBody, secret);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(provided.ToLowerInvariant()));
    }

    public static string Compute(string rawBody, string secret) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody)));

    public static bool SecretMatches(string? provided, string? secret) =>
        !string.IsNullOrEmpty(provided) && !string.IsNullOrEmpty(secret)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(secret));
}
