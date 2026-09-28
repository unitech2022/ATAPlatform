namespace ATA.Api.Modules.Payments;

public sealed class PaymentsOptions
{
    public const string Section = "Payments";
    /// <summary><c>sandbox</c> (development/tests) or <c>moyasar</c>.</summary>
    public string Provider { get; set; } = "sandbox";
    /// <summary>Keeps the legacy <c>POST /wallet/topups { method: "sandbox" }</c> (no <c>payments</c> row).</summary>
    public bool SandboxEnabled { get; set; }
    /// <summary>Card trips are not authorized at request time and are charged with a purchase on completion.</summary>
    public bool SandboxSkipAuthorization { get; set; }
    public bool CardAuthorizeOnRequest { get; set; } = true;
    public decimal AuthBufferPercent { get; set; } = 30m;
    public int ActionTimeoutSeconds { get; set; } = 300;
    public int GatewayTimeoutSeconds { get; set; } = 15;
    public int CaptureTimeoutSeconds { get; set; } = 10;
    public int CaptureMaxAttempts { get; set; } = 5;
    public decimal MinTopup { get; set; } = 10m;
    public decimal MaxTopup { get; set; } = 5000m;
    public decimal MaxTopupPerDay { get; set; } = 10000m;
    public int MaxCardsPerUser { get; set; } = 5;
    public decimal RefundAutoApproveLimit { get; set; } = 50m;
    public bool BlockOnOutstandingBalance { get; set; } = true;
    /// <summary>Public base URL of this API (gateway callbacks and the sandbox challenge page).</summary>
    public string PublicBaseUrl { get; set; } = "https://api.ata.sa";
    public string DefaultReturnUrl { get; set; } = "ata://payments/return";
    /// <summary>Runs the payment background jobs (<c>false</c> in tests, which call <c>PaymentJobs</c> directly).</summary>
    public bool JobsEnabled { get; set; } = true;
    public SandboxGatewayOptions Sandbox { get; set; } = new();
    public MoyasarOptions Moyasar { get; set; } = new();

    public bool AuthorizeCardTrips => CardAuthorizeOnRequest && !SandboxSkipAuthorization;
}

public sealed class SandboxGatewayOptions
{
    /// <summary>HMAC-SHA256 key of sandbox webhooks (<c>X-Sandbox-Signature</c> = hex HMAC of the raw body).</summary>
    public string WebhookSecret { get; set; } = "sandbox-webhook-secret";
    public string SignatureHeader { get; set; } = "X-Sandbox-Signature";
    public string PublishableKey { get; set; } = "pk_test_sandbox";
}

public sealed class MoyasarOptions
{
    public string BaseUrl { get; set; } = "https://api.moyasar.com";
    public string? PublishableKey { get; set; }
    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string SignatureHeader { get; set; } = "X-Moyasar-Signature";
    public string? ApplePayMerchantId { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}

public sealed class PayoutsOptions
{
    public const string Section = "Payouts";
    public decimal MinAmount { get; set; } = 100m;
    public decimal MaxCashDebt { get; set; } = 500m;
    public bool AutoApprove { get; set; }
    public decimal AutoApproveLimit { get; set; } = 1000m;
}

public sealed class SettlementsOptions
{
    public const string Section = "Settlements";
    public int PeriodDays { get; set; } = 7;
    public bool AutoGenerate { get; set; } = true;
    public bool AutoCreatePayouts { get; set; }
}
