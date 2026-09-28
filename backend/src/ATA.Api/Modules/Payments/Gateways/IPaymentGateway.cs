using ATA.Domain.Common;

namespace ATA.Api.Modules.Payments.Gateways;

/// <summary>
/// Payment gateway abstraction (doc 08 §F11.1). Every call runs outside a database transaction with a timeout and an idempotency key.
/// Deviation: card verification takes a <c>verificationId</c> (the <c>payment_methods</c> row) so 3-D Secure pages can refer back to it,
/// and <see cref="ParseVerifiedPayload"/> re-reads a stored webhook body for the retry job without its (already checked) signature.
/// </summary>
public interface IPaymentGateway
{
    /// <summary><c>sandbox</c> | <c>moyasar</c>.</summary>
    string Provider { get; }

    /// <summary>Manual capture (card trips).</summary>
    Task<GatewayResult> AuthorizeAsync(GatewayChargeRequest request, CancellationToken ct);

    /// <summary>Immediate capture (top-ups, purchases after an expired/insufficient authorization).</summary>
    Task<GatewayResult> PurchaseAsync(GatewayChargeRequest request, CancellationToken ct);

    Task<GatewayResult> CaptureAsync(string gatewayPaymentId, decimal amount, CancellationToken ct);

    Task<GatewayResult> VoidAsync(string gatewayPaymentId, CancellationToken ct);

    Task<GatewayResult> RefundAsync(string gatewayPaymentId, decimal amount, string idempotencyKey, CancellationToken ct);

    Task<GatewayResult> FetchAsync(string gatewayPaymentId, CancellationToken ct);

    Task<GatewayCardResult> VerifyCardTokenAsync(Guid verificationId, string token, string returnUrl, CancellationToken ct);

    Task DeleteCardTokenAsync(string token, CancellationToken ct);

    /// <summary>Verifies the signature and extracts the event (never throws for a bad signature: <c>SignatureValid=false</c>).</summary>
    WebhookParseResult ParseWebhook(string rawBody, IHeaderDictionary headers);

    WebhookParseResult ParseVerifiedPayload(string rawBody);
}

public sealed record GatewayChargeRequest(
    Guid PaymentId, decimal Amount, string Currency, GatewaySource Source, string Description,
    string ReturnUrl, string IdempotencyKey, IReadOnlyDictionary<string, string> Metadata);

/// <param name="Type"><c>token</c> | <c>apple_pay</c> | <c>sandbox</c>.</param>
public sealed record GatewaySource(string Type, string Value);

public sealed record GatewayResult(
    bool Success, string? GatewayPaymentId, GatewayStatus Status, decimal? AuthorizedAmount, decimal? CapturedAmount,
    string? ActionUrl, string? FailureCode, string? FailureMessage, bool IsTransient, string? RawStatus)
{
    public static GatewayResult Transient(string code, string message, string? gatewayPaymentId = null) =>
        new(false, gatewayPaymentId, GatewayStatus.Failed, null, null, null, code, message, true, null);

    public static GatewayResult Declined(string code, string message, string? gatewayPaymentId = null) =>
        new(false, gatewayPaymentId, GatewayStatus.Failed, null, null, null, code, message, false, "failed");
}

public enum GatewayStatus { Initiated, RequiresAction, Authorized, Captured, Failed, Voided, Refunded, PartiallyRefunded }

public sealed record GatewayCardResult(bool Success, string? Token, string? Brand, string? Last4, int? ExpiryMonth, int? ExpiryYear,
    string? HolderName, string? ActionUrl, string? FailureCode, string? FailureMessage, string? Fingerprint = null);

public sealed record WebhookParseResult(bool SignatureValid, string? EventId, string? EventType, string? GatewayPaymentId, GatewayStatus? Status,
    decimal? Amount, string? FailureCode);

public interface IPaymentGatewayResolver
{
    /// <summary>The configured provider (<c>Payments:Provider</c>) used for new payments.</summary>
    IPaymentGateway Current { get; }

    /// <summary>The gateway of an existing payment (payments keep their provider).</summary>
    IPaymentGateway Get(string provider);

    IPaymentGateway? Find(string provider);
}

public sealed class PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways, Microsoft.Extensions.Options.IOptions<PaymentsOptions> options) : IPaymentGatewayResolver
{
    private readonly Dictionary<string, IPaymentGateway> _gateways = gateways.ToDictionary(g => g.Provider, StringComparer.OrdinalIgnoreCase);

    public IPaymentGateway Current => Get(options.Value.Provider);

    public IPaymentGateway Get(string provider) => Find(provider) ?? throw new DomainException(ErrorCodes.PaymentProviderUnavailable, new { provider });

    public IPaymentGateway? Find(string provider) => _gateways.GetValueOrDefault(provider);
}
