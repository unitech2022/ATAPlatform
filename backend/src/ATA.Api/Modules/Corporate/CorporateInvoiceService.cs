using System.Security.Cryptography;
using System.Text;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Files;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Email;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Corporate;

/// <summary>A generated PDF (or CSV) download: bytes plus the file name sent in <c>Content-Disposition</c>.</summary>
public sealed record InvoiceFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Monthly corporate invoicing (doc 12 §F19.2): generation of the lines (trips, cancellation fees, adjustments) with VAT split per line
/// (<c>excl = round(incl / 1.15, 2)</c>), issuing with the PDF (QR code) stored in <c>stored_files</c>, the notification + e-mail, the payment journal
/// (<c>platform_cash ← corporate_receivable</c>), void (frees the lines), overdue marking and the optional suspension of late accounts.
/// </summary>
public sealed class CorporateInvoiceService(
    AtaDbContext db,
    IClock clock,
    LedgerService ledger,
    AuditService audit,
    INotificationDispatcher notifications,
    IInvoicePdfRenderer pdf,
    IFileStorage storage,
    IEmailSender email,
    IOptions<CorporateOptions> options,
    ILogger<CorporateInvoiceService> logger)
{
    private readonly CorporateOptions _options = options.Value;

    public const string EntityType = "corporate_account";
    private const int NumberRetries = 3;

    // ----- generation -----

    /// <summary>
    /// Builds the invoice of the month starting at <paramref name="periodStart"/> for one company. <c>409 conflict { reason: invoice_exists }</c> when a non-void invoice of the
    /// period exists, <c>409 conflict { reason: nothing_to_bill }</c> when there is nothing to bill. <paramref name="createdBy"/> is the admin (null for the job).
    /// </summary>
    public async Task<CorporateInvoice> GenerateAsync(Guid accountId, DateOnly periodStart, Guid? createdBy, CancellationToken ct)
    {
        new Validator().Rule("periodStart", periodStart.Day == 1, "must be the first day of a month").ThrowIfInvalid();
        var account = Guard.NotFound(await db.CorporateAccounts.FirstOrDefaultAsync(a => a.Id == accountId, ct));
        var now = clock.UtcNow;
        var today = Formats.RiyadhDate(now);
        new Validator().Rule("periodStart", periodStart <= today, "must not be in the future").ThrowIfInvalid();
        if (await db.CorporateInvoices.AnyAsync(i => i.CorporateAccountId == accountId && i.PeriodStart == periodStart && i.Status != CorporateInvoiceStatus.Void, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "invoice_exists" });
        }

        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var start = Formats.RiyadhMidnightUtc(periodStart);
        var end = Formats.RiyadhMidnightUtc(periodStart.AddMonths(1));
        var lines = await BuildLinesAsync(accountId, start, end, ct);
        if (lines.Count == 0)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "nothing_to_bill" });
        }

        var invoice = new CorporateInvoice
        {
            InvoiceNumber = string.Empty, CorporateAccountId = accountId, PeriodStart = periodStart, PeriodEnd = periodEnd, IssueDate = today, DueDate = today.AddDays(account.PaymentTermsDays),
            Currency = "SAR", TripsCount = lines.Count(l => l.Line.LineType == InvoiceLineType.Trip), VatRate = CorporateVat.Rate,
            SubtotalExclVat = lines.Sum(l => l.Line.AmountExclVat), VatAmount = lines.Sum(l => l.Line.VatAmount), TotalInclVat = lines.Sum(l => l.Line.AmountInclVat),
            Status = CorporateInvoiceStatus.Draft, SellerSnapshot = Json.Serialize(Seller()), BuyerSnapshot = Json.Serialize(Buyer(account)), PeriodActive = true,
        };
        foreach (var (line, _) in lines)
        {
            line.InvoiceId = invoice.Id;
        }

        db.CorporateInvoices.Add(invoice);
        db.CorporateInvoiceLines.AddRange(lines.Select(l => l.Line));
        foreach (var adjustmentId in lines.Where(l => l.AdjustmentId is not null).Select(l => l.AdjustmentId!.Value))
        {
            (await db.CorporateAdjustments.FirstAsync(a => a.Id == adjustmentId, ct)).InvoiceId = invoice.Id;
        }

        audit.Log("corporate_invoice.generate", EntityType, accountId, null, new { invoiceId = invoice.Id, invoice.PeriodStart, invoice.TripsCount, invoice.TotalInclVat, system = createdBy is null }, createdBy is null ? "system" : null);
        for (var attempt = 0; ; attempt++)
        {
            invoice.InvoiceNumber = await NextNumberAsync(periodStart, attempt, ct);
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException) when (attempt < NumberRetries)
            {
                // Another invoice took the same sequence number: retry with the next one.
            }
        }

        if (_options.AutoIssueInvoices)
        {
            await IssueAsync(invoice.Id, createdBy, systemActor: createdBy is null, ct);
        }

        return invoice;
    }

    private async Task<string> NextNumberAsync(DateOnly periodStart, int offset, CancellationToken ct)
    {
        var prefix = $"INV-{periodStart:yyyyMM}-";
        var count = await db.CorporateInvoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix), ct);
        return $"{prefix}{count + 1 + offset:D5}";
    }

    private sealed record BuiltLine(CorporateInvoiceLine Line, Guid? AdjustmentId);

    private async Task<List<BuiltLine>> BuildLinesAsync(Guid accountId, DateTime start, DateTime end, CancellationToken ct)
    {
        var lines = new List<BuiltLine>();
        var trips = await db.Trips.AsNoTracking()
            .Where(t => t.CorporateAccountId == accountId && t.PaymentMethod == PaymentMethodKind.Corporate && t.Status == TripStatus.Completed && t.FinalFare != null
                        && t.CompletedAt >= start && t.CompletedAt < end && !(
                            from l in db.CorporateInvoiceLines
                            join i in db.CorporateInvoices on l.InvoiceId equals i.Id
                            where l.TripId == t.Id && l.LineType == InvoiceLineType.Trip && i.Status != CorporateInvoiceStatus.Void
                            select l.Id).Any())
            .OrderBy(t => t.CompletedAt).ToListAsync(ct);
        var fees = await (from e in db.CancellationEvents.AsNoTracking()
                          join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                          where t.CorporateAccountId == accountId && e.FeeMethod == CancellationFeeMethod.Corporate && e.FeeStatus == CancellationFeeStatus.Charged && e.FeeCharged > 0
                                && e.CreatedAt >= start && e.CreatedAt < end && !(
                                    from l in db.CorporateInvoiceLines
                                    join i in db.CorporateInvoices on l.InvoiceId equals i.Id
                                    where l.TripId == t.Id && l.LineType == InvoiceLineType.CancellationFee && i.Status != CorporateInvoiceStatus.Void
                                    select l.Id).Any()
                          orderby e.CreatedAt
                          select new { Trip = t, e.FeeCharged, e.CreatedAt }).ToListAsync(ct);
        var adjustments = await db.CorporateAdjustments.AsNoTracking().Where(a => a.CorporateAccountId == accountId && a.InvoiceId == null && a.CreatedAt < end).OrderBy(a => a.CreatedAt).ToListAsync(ct);

        var tripRows = trips.Concat(fees.Select(f => f.Trip)).ToList();
        var memberIds = tripRows.Where(t => t.CorporateUserId != null).Select(t => t.CorporateUserId!.Value).Distinct().ToList();
        var members = await (from m in db.CorporateUsers.AsNoTracking()
                             join u in db.Users.AsNoTracking() on m.UserId equals u.Id into users
                             from u in users.DefaultIfEmpty()
                             where memberIds.Contains(m.Id)
                             select new { m.Id, Name = m.FullName ?? (u == null ? null : u.FullName), m.EmployeeNumber, m.Department }).ToDictionaryAsync(x => x.Id, ct);
        var costCenterIds = tripRows.Where(t => t.CostCenterId != null).Select(t => t.CostCenterId!.Value).Distinct().ToList();
        var costCenters = await db.CorporateCostCenters.AsNoTracking().Where(c => costCenterIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Code, ct);

        CorporateInvoiceLine Line(Trip t, InvoiceLineType type, decimal incl, DateTime date, string description)
        {
            var member = t.CorporateUserId is { } id ? members.GetValueOrDefault(id) : null;
            var (excl, vat) = CorporateVat.Split(incl);
            return new CorporateInvoiceLine
            {
                LineType = type, TripId = t.Id, TripNumber = t.TripNumber, TripDate = date, EmployeeName = member?.Name, EmployeeNumber = member?.EmployeeNumber, Department = member?.Department,
                CostCenterCode = t.CostCenterId is { } c ? costCenters.GetValueOrDefault(c) : null, GuestName = t.IsGuest ? t.GuestName : null, Purpose = t.TripPurpose,
                PickupName = t.PickupName, DropoffName = t.DropoffName, Description = Truncate(description, 255), AmountExclVat = excl, VatAmount = vat, AmountInclVat = incl,
            };
        }

        foreach (var t in trips)
        {
            lines.Add(new BuiltLine(Line(t, InvoiceLineType.Trip, t.FinalFare!.Value, t.CompletedAt!.Value, $"Trip {t.TripNumber}: {t.PickupName} → {t.DropoffName}"), null));
        }

        foreach (var f in fees)
        {
            lines.Add(new BuiltLine(Line(f.Trip, InvoiceLineType.CancellationFee, f.FeeCharged, f.CreatedAt, $"Cancellation fee {f.Trip.TripNumber}"), null));
        }

        foreach (var a in adjustments)
        {
            var (excl, vat) = CorporateVat.Split(a.Amount);
            lines.Add(new BuiltLine(new CorporateInvoiceLine
            {
                LineType = InvoiceLineType.Adjustment, TripDate = a.CreatedAt, Description = Truncate(a.Description, 255), AmountExclVat = excl, VatAmount = vat, AmountInclVat = a.Amount,
            }, a.Id));
        }

        return lines;
    }

    // ----- issue / pay / void -----

    /// <summary>
    /// <c>draft → issued</c>: refreshes the seller / buyer snapshots, dates the invoice today (<c>due_date = issue_date + payment_terms_days</c>), renders and stores the PDF,
    /// notifies the company admins (<c>corporate.invoice_issued</c>) and e-mails the PDF to <c>billing_email</c>.
    /// </summary>
    public async Task<CorporateInvoice> IssueAsync(Guid invoiceId, Guid? issuedBy, bool systemActor, CancellationToken ct)
    {
        var invoice = Guard.NotFound(await db.CorporateInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct));
        if (invoice.Status != CorporateInvoiceStatus.Draft)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "not_draft", status = invoice.Status });
        }

        var account = await db.CorporateAccounts.FirstAsync(a => a.Id == invoice.CorporateAccountId, ct);
        var now = clock.UtcNow;
        var today = Formats.RiyadhDate(now);
        invoice.Status = CorporateInvoiceStatus.Issued;
        invoice.IssuedAt = now;
        invoice.IssuedBy = issuedBy;
        invoice.IssueDate = today;
        invoice.DueDate = today.AddDays(account.PaymentTermsDays);
        invoice.SellerSnapshot = Json.Serialize(Seller());
        invoice.BuyerSnapshot = Json.Serialize(Buyer(account));
        var file = await RenderAndStoreAsync(invoice, account, isDraft: false, ct);
        invoice.PdfFileId = file.Id;

        var admins = await db.CorporateUsers.AsNoTracking().Where(m => m.CorporateAccountId == account.Id && m.Role == CorporateRole.CorporateAdmin && m.Status == CorporateUserStatus.Active && m.UserId != null)
            .Select(m => m.UserId!.Value).ToListAsync(ct);
        foreach (var adminUserId in admins)
        {
            await notifications.DispatchAsync(new NotificationRequest("corporate.invoice_issued", adminUserId,
                NotificationPlaceholders.Of(("invoiceNumber", invoice.InvoiceNumber)).Money("total", invoice.TotalInclVat), "invoice", invoice.Id,
                new Dictionary<string, object?> { ["invoiceId"] = invoice.Id, ["accountId"] = account.Id }), ct);
        }

        audit.Log("corporate_invoice.issue", EntityType, account.Id, new { invoiceId, status = CorporateInvoiceStatus.Draft }, new { invoiceId, status = invoice.Status, invoice.InvoiceNumber, invoice.TotalInclVat }, systemActor ? "system" : null);
        await db.SaveChangesAsync(ct);
        await SendEmailAsync(invoice, account, file, ct);
        return invoice;
    }

    /// <summary>Posts <c>corporate_invoice_payment</c> (<c>platform_cash ← corporate_receivable:{account}</c>) for the paid amount; a partial payment keeps the status.</summary>
    public async Task<CorporateInvoice> MarkPaidAsync(Guid invoiceId, decimal? amount, string? reference, DateTime? paidAt, CancellationToken ct)
    {
        var invoice = Guard.NotFound(await db.CorporateInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct));
        if (!invoice.IsOpen)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "not_open", status = invoice.Status });
        }

        new Validator()
            .Rule(nameof(amount), amount is > 0 && decimal.Round(amount.Value, 2) == amount.Value, "must be a positive amount with at most 2 decimals")
            .Require(nameof(reference), reference, 100)
            .Rule(nameof(amount), amount is null || amount <= invoice.Outstanding, $"exceeds the outstanding amount {invoice.Outstanding:0.00}")
            .ThrowIfInvalid();

        var before = new { invoice.Status, invoice.PaidAmount };
        var now = clock.UtcNow;
        var paid = (invoice.PaidAmount ?? 0m) + amount!.Value;
        await db.InTransactionAsync(async () =>
        {
            invoice.PaidAmount = paid;
            invoice.PaymentReference = reference!.Trim();
            if (paid >= invoice.TotalInclVat)
            {
                invoice.Status = CorporateInvoiceStatus.Paid;
                invoice.PaidAt = paidAt?.ToUniversalTime() ?? now;
            }

            await ledger.JournalAsync(JournalType.CorporateInvoicePayment, LedgerAccounts.PlatformCash, LedgerAccounts.CorporateReceivable(invoice.CorporateAccountId), amount.Value,
                "corporate_invoice", invoice.Id, $"corporate_invoice:{invoice.Id}:payment:{paid:0.00}", $"Invoice {invoice.InvoiceNumber} payment {reference!.Trim()}", ct);
            audit.Log("corporate_invoice.mark_paid", EntityType, invoice.CorporateAccountId, before, new { invoiceId, amount, reference = reference.Trim(), invoice.Status, invoice.PaidAmount });
            await db.SaveChangesAsync(ct);
        }, ct);
        return invoice;
    }

    /// <summary>Only an invoice without payments can be voided; its lines become billable again and its adjustments are released.</summary>
    public async Task<CorporateInvoice> VoidAsync(Guid invoiceId, string? reason, CancellationToken ct)
    {
        new Validator().Require(nameof(reason), reason, 500).ThrowIfInvalid();
        var invoice = Guard.NotFound(await db.CorporateInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct));
        if (invoice.Status is CorporateInvoiceStatus.Paid or CorporateInvoiceStatus.Void || (invoice.PaidAmount ?? 0m) > 0m)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "not_voidable", status = invoice.Status });
        }

        var before = new { invoice.Status };
        invoice.Status = CorporateInvoiceStatus.Void;
        invoice.VoidReason = reason!.Trim();
        invoice.PeriodActive = null;
        await db.CorporateAdjustments.Where(a => a.InvoiceId == invoiceId).ExecuteUpdateAsync(s => s.SetProperty(a => a.InvoiceId, (Guid?)null), ct);
        audit.Log("corporate_invoice.void", EntityType, invoice.CorporateAccountId, before, new { invoiceId, invoice.Status, reason = invoice.VoidReason });
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    // ----- adjustments (platform admin) -----

    public async Task<AdjustmentDto> AddAdjustmentAsync(Guid accountId, AdjustmentRequest request, Guid createdBy, CancellationToken ct)
    {
        new Validator()
            .Rule(nameof(request.Amount), request.Amount is not null and not 0 && decimal.Round(request.Amount.Value, 2) == request.Amount, "must be a non-zero amount with at most 2 decimals")
            .Require(nameof(request.Description), request.Description, 255)
            .ThrowIfInvalid();
        Guard.NotFound(await db.CorporateAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct));
        var adjustment = new CorporateAdjustment { CorporateAccountId = accountId, Amount = request.Amount!.Value, Description = request.Description!.Trim(), CreatedBy = createdBy };
        db.CorporateAdjustments.Add(adjustment);
        // The receivable follows the adjustment in the ledger: a charge is receivable ← adjustments, a credit is adjustments ← receivable.
        var amount = adjustment.Amount;
        await ledger.JournalAsync(JournalType.Manual, amount > 0 ? LedgerAccounts.CorporateReceivable(accountId) : LedgerAccounts.Adjustments, amount > 0 ? LedgerAccounts.Adjustments : LedgerAccounts.CorporateReceivable(accountId),
            Math.Abs(amount), "corporate_adjustment", adjustment.Id, $"corporate_adjustment:{adjustment.Id}", adjustment.Description, ct, createdBy);
        audit.Log("corporate_adjustment.create", EntityType, accountId, null, new { adjustmentId = adjustment.Id, adjustment.Amount, adjustment.Description });
        await db.SaveChangesAsync(ct);
        return new AdjustmentDto(adjustment.Id, adjustment.Amount, adjustment.Description, adjustment.InvoiceId, adjustment.CreatedBy, adjustment.CreatedAt);
    }

    public async Task<IReadOnlyList<AdjustmentDto>> ListAdjustmentsAsync(Guid accountId, CancellationToken ct) =>
        await db.CorporateAdjustments.AsNoTracking().Where(a => a.CorporateAccountId == accountId).OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdjustmentDto(a.Id, a.Amount, a.Description, a.InvoiceId, a.CreatedBy, a.CreatedAt)).ToListAsync(ct);

    // ----- reads -----

    public async Task<PagedResult<CorporateInvoiceDto>> ListAsync(Guid? accountId, CorporateInvoiceStatus? status, DateOnly? from, DateOnly? to, bool includeDrafts, Paging paging, CancellationToken ct)
    {
        var query = from i in db.CorporateInvoices.AsNoTracking() join a in db.CorporateAccounts.AsNoTracking() on i.CorporateAccountId equals a.Id select new { Invoice = i, Account = a };
        if (accountId is { } id) query = query.Where(x => x.Invoice.CorporateAccountId == id);
        if (!includeDrafts) query = query.Where(x => x.Invoice.Status != CorporateInvoiceStatus.Draft);
        if (status is not null) query = query.Where(x => x.Invoice.Status == status);
        if (from is { } f) query = query.Where(x => x.Invoice.IssueDate >= f);
        if (to is { } t) query = query.Where(x => x.Invoice.IssueDate <= t);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Invoice.IssueDate).ThenByDescending(x => x.Invoice.InvoiceNumber).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(r => CorporateDtos.ToDto(r.Invoice, r.Account.DisplayName, r.Account.AccountNumber)).ToList(), total);
    }

    /// <summary>One invoice with a page of its lines. <paramref name="accountId"/> scopes the read to one company (drafts are hidden from companies: <paramref name="includeDrafts"/> false).</summary>
    public async Task<CorporateInvoiceDetailDto> GetAsync(Guid invoiceId, Guid? accountId, bool includeDrafts, Paging paging, CancellationToken ct)
    {
        var invoice = await FindAsync(invoiceId, accountId, includeDrafts, ct);
        var account = await db.CorporateAccounts.AsNoTracking().FirstAsync(a => a.Id == invoice.CorporateAccountId, ct);
        var query = db.CorporateInvoiceLines.AsNoTracking().Where(l => l.InvoiceId == invoiceId);
        var total = await query.CountAsync(ct);
        var lines = (await query.OrderBy(l => l.LineType).ThenBy(l => l.TripDate).ThenBy(l => l.TripNumber).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct)).Select(CorporateDtos.ToDto).ToList();
        return new CorporateInvoiceDetailDto(invoice.Id, invoice.InvoiceNumber, invoice.CorporateAccountId, account.DisplayName, account.AccountNumber, invoice.PeriodStart, invoice.PeriodEnd,
            invoice.IssueDate, invoice.DueDate, invoice.Currency, invoice.TripsCount, invoice.SubtotalExclVat, invoice.VatRate, invoice.VatAmount, invoice.TotalInclVat, invoice.Status,
            invoice.PaidAmount, invoice.PaidAt, invoice.PaymentReference, invoice.VoidReason, invoice.IssuedAt, invoice.PdfFileId, paging.Result(lines, total));
    }

    public async Task<CorporateInvoiceDto> GetDtoAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = Guard.NotFound(await db.CorporateInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId, ct));
        var account = await db.CorporateAccounts.AsNoTracking().FirstAsync(a => a.Id == invoice.CorporateAccountId, ct);
        return CorporateDtos.ToDto(invoice, account.DisplayName, account.AccountNumber);
    }

    /// <summary>The PDF: the stored file of an issued invoice (rendered again when the file is gone), an on-the-fly "draft" rendering for a draft.</summary>
    public async Task<InvoiceFile> GetPdfAsync(Guid invoiceId, Guid? accountId, bool includeDrafts, CancellationToken ct)
    {
        var invoice = await FindAsync(invoiceId, accountId, includeDrafts, ct, tracking: true);
        var account = await db.CorporateAccounts.AsNoTracking().FirstAsync(a => a.Id == invoice.CorporateAccountId, ct);
        var name = $"{invoice.InvoiceNumber}.pdf";
        if (invoice.Status == CorporateInvoiceStatus.Draft)
        {
            return new InvoiceFile(name, "application/pdf", pdf.Render(await ModelAsync(invoice, account, isDraft: true, ct)));
        }

        if (invoice.PdfFileId is { } fileId && await db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fileId, ct) is { } stored
            && await storage.OpenReadAsync(stored.StorageKey, ct) is { } stream)
        {
            await using (stream)
            {
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, ct);
                return new InvoiceFile(name, "application/pdf", buffer.ToArray());
            }
        }

        var file = await RenderAndStoreAsync(invoice, account, isDraft: false, ct);
        invoice.PdfFileId = file.Id;
        await db.SaveChangesAsync(ct);
        return new InvoiceFile(name, "application/pdf", await ReadAsync(file, ct));
    }

    /// <summary><c>GET …/export?format=csv</c>: the invoice lines (UTF-8 with BOM).</summary>
    public async Task<InvoiceFile> ExportCsvAsync(Guid invoiceId, Guid? accountId, bool includeDrafts, CancellationToken ct)
    {
        var invoice = await FindAsync(invoiceId, accountId, includeDrafts, ct);
        var lines = await db.CorporateInvoiceLines.AsNoTracking().Where(l => l.InvoiceId == invoiceId).OrderBy(l => l.LineType).ThenBy(l => l.TripDate).ThenBy(l => l.TripNumber).ToListAsync(ct);
        var csv = new StringBuilder();
        csv.Append(Csv.Row("invoice_number", "line_type", "trip_number", "date", "employee", "employee_number", "department", "cost_center", "guest", "purpose", "pickup", "dropoff",
            "description", "amount_excl_vat", "vat", "amount_incl_vat"));
        foreach (var l in lines)
        {
            csv.Append(Csv.Row(invoice.InvoiceNumber, SnakeCase(l.LineType.ToString()), l.TripNumber, l.TripDate?.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture), l.EmployeeName,
                l.EmployeeNumber, l.Department, l.CostCenterCode, l.GuestName, l.Purpose, l.PickupName, l.DropoffName, l.Description, Money(l.AmountExclVat), Money(l.VatAmount), Money(l.AmountInclVat)));
        }

        return new InvoiceFile($"{invoice.InvoiceNumber}.csv", "text/csv; charset=utf-8", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray());
    }

    // ----- jobs -----

    /// <summary>
    /// <c>CorporateInvoiceJob</c>: from <c>Corporate:InvoiceDayOfMonth</c> at <c>Corporate:InvoiceHourLocal</c> (Riyadh) the previous month is invoiced for every company with movements
    /// and no invoice for it yet (idempotent, so a later pass the same month only catches up). Returns the number of invoices created.
    /// </summary>
    public async Task<int> GenerateMonthlyAsync(CancellationToken ct)
    {
        var local = Formats.ToRiyadh(clock.UtcNow);
        var day = Math.Clamp(_options.InvoiceDayOfMonth, 1, 28);
        var due = new DateTime(local.Year, local.Month, day, Math.Clamp(_options.InvoiceHourLocal, 0, 23), 0, 0);
        if (local < due)
        {
            return 0;
        }

        var periodStart = new DateOnly(local.Year, local.Month, 1).AddMonths(-1);
        var accountIds = await db.CorporateAccounts.AsNoTracking().Where(a => a.Status != CorporateAccountStatus.Pending).Select(a => a.Id).ToListAsync(ct);
        var created = 0;
        foreach (var accountId in accountIds)
        {
            if (await db.CorporateInvoices.AnyAsync(i => i.CorporateAccountId == accountId && i.PeriodStart == periodStart && i.Status != CorporateInvoiceStatus.Void, ct))
            {
                continue;
            }

            try
            {
                await GenerateAsync(accountId, periodStart, null, ct);
                created++;
            }
            catch (DomainException ex) when (ex.Code == ErrorCodes.Conflict)
            {
                // Nothing to bill for this company in the period.
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Monthly corporate invoice of account {AccountId} for {Period} failed", accountId, periodStart);
                db.ChangeTracker.Clear();
            }
        }

        return created;
    }

    /// <summary>
    /// <c>CorporateInvoiceOverdueJob</c>: <c>issued</c> invoices past their due date (Riyadh) become <c>overdue</c>; with <c>Corporate:SuspendAfterOverdueDays</c> &gt; 0 a company whose
    /// oldest overdue invoice is older than that many days is suspended (audit, actor system). Returns the number of changes.
    /// </summary>
    public async Task<int> MarkOverdueAsync(CancellationToken ct)
    {
        var today = Formats.RiyadhDate(clock.UtcNow);
        var changes = 0;
        foreach (var invoice in await db.CorporateInvoices.Where(i => i.Status == CorporateInvoiceStatus.Issued && i.DueDate < today).ToListAsync(ct))
        {
            invoice.Status = CorporateInvoiceStatus.Overdue;
            audit.Log("corporate_invoice.overdue", EntityType, invoice.CorporateAccountId, new { invoiceId = invoice.Id, status = CorporateInvoiceStatus.Issued }, new { invoiceId = invoice.Id, status = invoice.Status, invoice.DueDate }, "system");
            changes++;
        }

        if (_options.SuspendAfterOverdueDays > 0)
        {
            var cutoff = today.AddDays(-_options.SuspendAfterOverdueDays);
            var late = await db.CorporateInvoices.AsNoTracking().Where(i => i.Status == CorporateInvoiceStatus.Overdue && i.DueDate < cutoff).Select(i => i.CorporateAccountId).Distinct().ToListAsync(ct);
            foreach (var accountId in late)
            {
                var account = await db.CorporateAccounts.FirstAsync(a => a.Id == accountId, ct);
                if (account.Status != CorporateAccountStatus.Active)
                {
                    continue;
                }

                account.Status = CorporateAccountStatus.Suspended;
                audit.Log("corporate_account.suspend", EntityType, account.Id, new { status = CorporateAccountStatus.Active }, new { status = account.Status, reason = "overdue_invoice" }, "system");
                changes++;
            }
        }

        if (changes > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return changes;
    }

    /// <summary><c>CorporateInvitationExpiryJob</c>: open invitations past <c>expires_at</c> are marked expired once (audit, actor system). Returns how many.</summary>
    public async Task<int> ExpireInvitationsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var due = await db.CorporateInvitations.Where(i => i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null && i.ExpiredAt == null && i.ExpiresAt <= now).Take(500).ToListAsync(ct);
        foreach (var invitation in due)
        {
            invitation.ExpiredAt = now;
            audit.Log("corporate_invitation.expire", EntityType, invitation.CorporateAccountId, null, new { invitationId = invitation.Id, memberId = invitation.CorporateUserId, invitation.ExpiresAt }, "system");
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return due.Count;
    }

    // ----- helpers -----

    private async Task<CorporateInvoice> FindAsync(Guid invoiceId, Guid? accountId, bool includeDrafts, CancellationToken ct, bool tracking = false)
    {
        var query = tracking ? db.CorporateInvoices.AsQueryable() : db.CorporateInvoices.AsNoTracking();
        var invoice = await query.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null || (accountId is { } id && invoice.CorporateAccountId != id) || (!includeDrafts && invoice.Status == CorporateInvoiceStatus.Draft))
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        return invoice;
    }

    private InvoiceParty Seller() => new(_options.SellerLegalNameAr, _options.SellerLegalNameEn, _options.SellerVatNumber, null);

    private static InvoiceParty Buyer(CorporateAccount account) =>
        new(account.LegalNameAr, account.LegalNameEn, account.VatNumber, CorporateRules.ParseAddress(account.BillingAddress), account.CrNumber);

    private async Task<InvoicePdfModel> ModelAsync(CorporateInvoice invoice, CorporateAccount account, bool isDraft, CancellationToken ct)
    {
        var seller = Json.Deserialize<InvoiceParty>(invoice.SellerSnapshot) ?? Seller();
        var buyer = Json.Deserialize<InvoiceParty>(invoice.BuyerSnapshot) ?? Buyer(account);
        var lines = (await db.CorporateInvoiceLines.AsNoTracking().Where(l => l.InvoiceId == invoice.Id).OrderBy(l => l.LineType).ThenBy(l => l.TripDate).ThenBy(l => l.TripNumber).ToListAsync(ct))
            .Select(CorporateDtos.ToDto).ToList();
        return new InvoicePdfModel(invoice.InvoiceNumber, isDraft, invoice.IssueDate, invoice.DueDate, invoice.PeriodStart, invoice.PeriodEnd, invoice.Currency, invoice.VatRate, invoice.SubtotalExclVat,
            invoice.VatAmount, invoice.TotalInclVat, seller, buyer, _options.SellerAddress, CorporateRules.FormatAddress(buyer.Address), invoice.IssuedAt ?? clock.UtcNow, lines);
    }

    private async Task<StoredFile> RenderAndStoreAsync(CorporateInvoice invoice, CorporateAccount account, bool isDraft, CancellationToken ct)
    {
        var bytes = pdf.Render(await ModelAsync(invoice, account, isDraft, ct));
        var key = $"corporate-invoices/{account.Id:N}/{invoice.InvoiceNumber}.pdf";
        using (var stream = new MemoryStream(bytes))
        {
            await storage.SaveAsync(key, stream, ct);
        }

        var file = new StoredFile
        {
            OwnerUserId = account.CreatedBy, StorageKey = key, OriginalName = $"{invoice.InvoiceNumber}.pdf", ContentType = "application/pdf", SizeBytes = bytes.Length,
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
        };
        db.StoredFiles.Add(file);
        return file;
    }

    private async Task<byte[]> ReadAsync(StoredFile file, CancellationToken ct)
    {
        await using var stream = await storage.OpenReadAsync(file.StorageKey, ct) ?? throw new InvalidOperationException("Stored invoice PDF is missing");
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    /// <summary>Mails the PDF to <c>billing_email</c>; a failing mail never undoes the issue (the PDF stays downloadable).</summary>
    private async Task SendEmailAsync(CorporateInvoice invoice, CorporateAccount account, StoredFile file, CancellationToken ct)
    {
        try
        {
            var bytes = await ReadAsync(file, ct);
            var body = $"Dear {account.ContactName},\n\nTax invoice {invoice.InvoiceNumber} for {invoice.PeriodStart:yyyy-MM-dd} to {invoice.PeriodEnd:yyyy-MM-dd} is attached.\n" +
                       $"Total (incl. VAT): SAR {Money(invoice.TotalInclVat)} — due {invoice.DueDate:yyyy-MM-dd}.\n\nATA\n\n" +
                       $"السادة {account.ContactName}،\nمرفق الفاتورة الضريبية {invoice.InvoiceNumber} للفترة من {invoice.PeriodStart:yyyy-MM-dd} إلى {invoice.PeriodEnd:yyyy-MM-dd}. الإجمالي شامل الضريبة {Money(invoice.TotalInclVat)} ر.س، تستحق في {invoice.DueDate:yyyy-MM-dd}.";
            var result = await email.SendAsync(new EmailMessage(account.BillingEmail, $"ATA invoice {invoice.InvoiceNumber} · فاتورة {invoice.InvoiceNumber}", body,
                [new EmailAttachment($"{invoice.InvoiceNumber}.pdf", "application/pdf", bytes)]), ct);
            if (!result.Success)
            {
                logger.LogWarning("Invoice {InvoiceNumber} e-mail failed: {Code}", invoice.InvoiceNumber, result.ErrorCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Invoice {InvoiceNumber} e-mail failed", invoice.InvoiceNumber);
        }
    }

    private static string Money(decimal value) => value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static string Truncate(string value, int max) => value.Length > max ? value[..max] : value;

    private static string SnakeCase(string pascal) => System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(pascal);

    private static class Json
    {
        public static string Serialize<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value, JsonDefaults.Options);

        public static T? Deserialize<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, JsonDefaults.Options);
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }
    }
}
