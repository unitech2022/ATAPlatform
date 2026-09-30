namespace ATA.Api.Modules.Corporate;

/// <summary>Configuration section <c>Corporate</c> (doc 12 §F19 / §F21.10).</summary>
public sealed class CorporateOptions
{
    public const string Section = "Corporate";

    /// <summary>Base of the SMS invitation link <c>{PortalBaseUrl}/business/join/{token}</c>.</summary>
    public string PortalBaseUrl { get; set; } = "https://ata.sa";
    public int InvitationDays { get; set; } = 7;
    /// <summary>Day of the month (1–28) on which <c>CorporateInvoiceJob</c> bills the previous month (at 04:00 Riyadh).</summary>
    public int InvoiceDayOfMonth { get; set; } = 1;
    /// <summary><c>true</c>: generated invoices are issued at once (PDF, notification, e-mail); otherwise they stay <c>draft</c> for review (default).</summary>
    public bool AutoIssueInvoices { get; set; }
    /// <summary>&gt; 0: an account with an invoice overdue for more days than this becomes <c>suspended</c> (0 = never).</summary>
    public int SuspendAfterOverdueDays { get; set; }
    /// <summary>Concurrent guest trips one company admin may have open (the <c>trip_active_exists</c> rule does not apply to guest bookings).</summary>
    public int MaxActiveGuestTripsPerAdmin { get; set; } = 10;
    /// <summary><c>false</c>: <c>/corporate/api-keys</c> answers <c>404</c> (keys are for future extensibility; nothing authenticates with them in v1).</summary>
    public bool ApiKeysEnabled { get; set; }
    public string SellerLegalNameAr { get; set; } = "شركة أتا للنقل";
    public string SellerLegalNameEn { get; set; } = "ATA Transport Co.";
    /// <summary>15 digits starting and ending with 3 (placeholder until the platform's tax registration is configured).</summary>
    public string SellerVatNumber { get; set; } = "300000000000003";
    public string SellerAddress { get; set; } = "Riyadh, Saudi Arabia";
    /// <summary>Hour (Riyadh) from which the monthly invoice job runs on <see cref="InvoiceDayOfMonth"/>.</summary>
    public int InvoiceHourLocal { get; set; } = 4;
    public int MaxImportRows { get; set; } = 500;
    /// <summary>Runs <c>CorporateInvoiceJob</c>, <c>CorporateInvoiceOverdueJob</c> and <c>CorporateInvitationExpiryJob</c> (<c>false</c> in tests, which call <c>RunOnceAsync</c>).</summary>
    public bool JobsEnabled { get; set; } = true;
    public int JobIntervalMinutes { get; set; } = 15;
}
