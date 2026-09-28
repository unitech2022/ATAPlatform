using System.Net;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

public static class PaymentEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        MapPublic(api);
        MapPassenger(api);
        MapDriver(api);
        MapAdmin(api);
    }

    private static void MapPublic(IEndpointRouteBuilder api)
    {
        var payments = api.MapGroup("/payments").WithTags("Payments");

        payments.MapGet("/config", (PaymentService service) => Results.Ok(service.GetConfig()))
            .RequireAuthorization(Policies.Authenticated)
            .Produces<PaymentConfigDto>();

        payments.MapGet("/{id:guid}", async (Guid id, PaymentService service, CancellationToken ct) => Results.Ok(await service.GetOwnAsync(id, ct)))
            .RequireAuthorization(Policies.Authenticated)
            .Produces<PaymentDto>();

        // Gateway webhooks: no authentication; the signature is verified before anything is processed. Always 200 once stored.
        payments.MapPost("/webhooks/{provider}", async (string provider, HttpContext http, PaymentService service, CancellationToken ct) =>
            {
                using var reader = new StreamReader(http.Request.Body);
                var raw = await reader.ReadToEndAsync(ct);
                await service.HandleWebhookAsync(provider, raw, http.Request.Headers, ct);
                return Results.Ok(new { });
            })
            .AllowAnonymous()
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized);

        payments.MapGet("/return/{provider}", async (string provider, string? id, PaymentService service, CancellationToken ct) =>
            {
                new Validator().Require(nameof(id), id, 100).ThrowIfInvalid();
                return Results.Redirect(await service.HandleReturnAsync(provider, id!, ct));
            })
            .AllowAnonymous()
            .Produces(StatusCodes.Status302Found);

        payments.MapGet("/sandbox/challenge/{id:guid}", (Guid id, string? returnUrl, IOptions<PaymentsOptions> options) =>
            {
                EnsureSandbox(options.Value);
                var action = $"/api/v1/payments/sandbox/challenge/{id}" + (returnUrl is null ? string.Empty : $"?returnUrl={Uri.EscapeDataString(returnUrl)}");
                var html = $$"""
                    <!doctype html>
                    <html lang="ar" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>ATA Sandbox 3-D Secure</title>
                    <style>body{font-family:system-ui,sans-serif;background:#F4F7F9;color:#123650;display:flex;min-height:100vh;align-items:center;justify-content:center;margin:0}
                    .card{background:#fff;border-radius:16px;padding:24px;max-width:360px;width:100%;box-shadow:0 8px 24px rgba(18,54,80,.12);text-align:center}
                    button{width:100%;padding:14px;border:0;border-radius:12px;font-size:16px;margin-top:12px;cursor:pointer}
                    .ok{background:#19B7A5;color:#fff}.no{background:#F1E3E5;color:#C23B4A}</style></head>
                    <body><div class="card"><h2>تحقق الدفع (بيئة تجريبية)</h2><p>Sandbox 3-D Secure challenge</p><p><small>{{WebUtility.HtmlEncode(id.ToString())}}</small></p>
                    <form method="post" action="{{WebUtility.HtmlEncode(action)}}"><button class="ok" name="outcome" value="approve">موافقة · Approve</button>
                    <button class="no" name="outcome" value="decline">رفض · Decline</button></form></div></body></html>
                    """;
                return Results.Content(html, "text/html; charset=utf-8");
            })
            .AllowAnonymous()
            .ExcludeFromDescription();

        payments.MapPost("/sandbox/challenge/{id:guid}", async (Guid id, string? returnUrl, HttpContext http, PaymentService service, PaymentMethodService cards,
                IOptions<PaymentsOptions> options, CancellationToken ct) =>
            {
                EnsureSandbox(options.Value);
                var outcome = http.Request.HasFormContentType ? (await http.Request.ReadFormAsync(ct))["outcome"].ToString() : http.Request.Query["outcome"].ToString();
                new Validator().Rule("outcome", outcome is "approve" or "decline", "must be approve|decline").ThrowIfInvalid();
                var approve = outcome == "approve";
                var redirect = await service.CompleteSandboxChallengeAsync(id, approve, ct)
                               ?? await cards.CompleteVerificationAsync(id, approve, returnUrl, ct)
                               ?? throw new DomainException(ErrorCodes.NotFound);
                return Results.Redirect(redirect);
            })
            .AllowAnonymous()
            .DisableAntiforgery()
            .ExcludeFromDescription();
    }

    private static void MapPassenger(IEndpointRouteBuilder api)
    {
        var methods = api.MapGroup("/passenger/payment-methods").WithTags("Passenger").RequireAuthorization(Policies.Passenger);
        methods.MapGet("/", async (PaymentMethodService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
            .Produces<List<SavedCardDto>>();
        methods.MapPost("/", async (AddCardRequest request, PaymentMethodService service, CancellationToken ct) =>
            {
                var result = await service.AddAsync(request, ct);
                return result.Pending
                    ? Results.Accepted($"/api/v1/passenger/payment-methods/{result.Card.Id}", new AddCardPendingDto(result.Card, result.Action!))
                    : Results.Created($"/api/v1/passenger/payment-methods/{result.Card.Id}", result.Card);
            })
            .Produces<SavedCardDto>(StatusCodes.Status201Created)
            .Produces<AddCardPendingDto>(StatusCodes.Status202Accepted)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        methods.MapPost("/{id:guid}/default", async (Guid id, PaymentMethodService service, CancellationToken ct) => Results.Ok(await service.SetDefaultAsync(id, ct)))
            .Produces<SavedCardDto>();
        methods.MapDelete("/{id:guid}", async (Guid id, PaymentMethodService service, CancellationToken ct) =>
            {
                await service.RemoveAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);

        api.MapGet("/passenger/trips/{id:guid}/receipt", async (Guid id, ReceiptService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ForPassengerAsync(id, http.GetLanguage(), ct)))
            .WithTags("Passenger")
            .RequireAuthorization(Policies.Passenger)
            .Produces<ReceiptDto>();
    }

    private static void MapDriver(IEndpointRouteBuilder api)
    {
        var driver = api.MapGroup("/driver").WithTags("Driver").RequireAuthorization(Policies.Driver);

        driver.MapGet("/earnings/statement", async (DateOnly? from, DateOnly? to, DriverEarningsService service, CancellationToken ct) =>
                Results.Ok(await service.StatementAsync(from, to, ct)))
            .Produces<EarningsStatementDto>();
        driver.MapGet("/trips/{id:guid}/earnings", async (Guid id, DriverEarningsService service, CancellationToken ct) => Results.Ok(await service.TripAsync(id, ct)))
            .Produces<TripEarningsDto>();

        driver.MapGet("/payouts/summary", async (PayoutService service, CancellationToken ct) => Results.Ok(await service.SummaryAsync(ct)))
            .Produces<PayoutSummaryDto>();
        driver.MapPost("/payouts", async (PayoutRequest request, PayoutService service, HttpContext http, CancellationToken ct) =>
            {
                var payout = await service.RequestAsync(request, IdempotencyKey(http), ct);
                return Results.Created($"/api/v1/driver/payouts/{payout.Id}", payout);
            })
            .Produces<PayoutDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        driver.MapGet("/payouts", async (int? page, int? pageSize, PayoutService service, CancellationToken ct) =>
                Results.Ok(await service.ListOwnAsync(Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<PayoutDto>>();
        driver.MapPost("/payouts/{id:guid}/cancel", async (Guid id, PayoutService service, CancellationToken ct) => Results.Ok(await service.CancelOwnAsync(id, ct)))
            .Produces<PayoutDto>();

        driver.MapGet("/settlements", async (int? page, int? pageSize, SettlementService service, AtaDbContext db, ICurrentUser user, CancellationToken ct) =>
                Results.Ok(await service.ListForDriverAsync(await DriverIdAsync(db, user, ct), Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<DriverSettlementItemDto>>();
        driver.MapGet("/settlements/{id:guid}", async (Guid id, SettlementService service, AtaDbContext db, ICurrentUser user, CancellationToken ct) =>
                Results.Ok(await service.GetSettlementAsync(id, await DriverIdAsync(db, user, ct), ct)))
            .Produces<SettlementDto>();
    }

    private static void MapAdmin(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin payments").RequireAuthorization(Policies.Admin);

        admin.MapGet("/payments", async (string? status, string? purpose, string? provider, string? method, DateOnly? from, DateOnly? to, string? search, int? page, int? pageSize,
                AdminPaymentService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<PaymentStatus>(status, "status"), QueryEnum.Parse<PaymentPurpose>(purpose, "purpose"), provider,
                    QueryEnum.Parse<PaymentInstrument>(method, "method"), from, to, search, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<PagedResult<AdminPaymentListItemDto>>();
        admin.MapGet("/payments/{id:guid}", async (Guid id, AdminPaymentService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<AdminPaymentDetailDto>();
        admin.MapPost("/payments/{id:guid}/refunds", async (Guid id, CreateRefundRequest request, RefundService service, CancellationToken ct) =>
            {
                var refund = await service.CreateForPaymentAsync(id, request, ct);
                return Results.Created($"/api/v1/admin/refunds/{refund.Id}", refund);
            })
            .RequirePermission(Permissions.PaymentsRefund)
            .Produces<RefundDto>(StatusCodes.Status201Created);
        admin.MapPost("/trips/{id:guid}/refunds", async (Guid id, CreateRefundRequest request, RefundService service, CancellationToken ct) =>
            {
                var refund = await service.CreateForTripAsync(id, request, ct);
                return Results.Created($"/api/v1/admin/refunds/{refund.Id}", refund);
            })
            .RequirePermission(Permissions.PaymentsRefund)
            .Produces<RefundDto>(StatusCodes.Status201Created);
        admin.MapGet("/trips/{id:guid}/receipt", async (Guid id, ReceiptService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ForAdminAsync(id, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<ReceiptDto>();

        admin.MapGet("/refunds", async (string? status, DateOnly? from, DateOnly? to, int? page, int? pageSize, RefundService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<RefundStatus>(status, "status"), from, to, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<PagedResult<RefundDto>>();
        admin.MapPost("/refunds/{id:guid}/approve", async (Guid id, RefundService service, CancellationToken ct) => Results.Ok(await service.ApproveAsync(id, ct)))
            .RequirePermission(Permissions.PaymentsRefundApprove)
            .Produces<RefundDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapPost("/refunds/{id:guid}/reject", async (Guid id, ReasonBody request, RefundService service, CancellationToken ct) => Results.Ok(await service.RejectAsync(id, request, ct)))
            .RequirePermission(Permissions.PaymentsRefundApprove)
            .Produces<RefundDto>();
        admin.MapPost("/refunds/{id:guid}/retry", async (Guid id, RefundService service, CancellationToken ct) => Results.Ok(await service.RetryAsync(id, ct)))
            .RequirePermission(Permissions.PaymentsRefundApprove)
            .Produces<RefundDto>();

        admin.MapGet("/payouts", async (string? status, Guid? driverId, DateOnly? from, DateOnly? to, int? page, int? pageSize, PayoutService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<PayoutStatus>(status, "status"), driverId, from, to, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<PagedResult<AdminPayoutDto>>();
        admin.MapPost("/payouts/{id:guid}/approve", async (Guid id, PayoutService service, CancellationToken ct) => Results.Ok(await service.ApproveAsync(id, ct)))
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces<AdminPayoutDto>();
        admin.MapPost("/payouts/{id:guid}/reject", async (Guid id, ReasonBody request, PayoutService service, CancellationToken ct) => Results.Ok(await service.RejectAsync(id, request, ct)))
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces<AdminPayoutDto>();
        admin.MapPost("/payouts/{id:guid}/mark-paid", async (Guid id, MarkPaidRequest request, PayoutService service, CancellationToken ct) => Results.Ok(await service.MarkPaidAsync(id, request, ct)))
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces<AdminPayoutDto>();

        admin.MapPost("/payout-batches", async (CreatePayoutBatchRequest request, PayoutService service, CancellationToken ct) =>
            {
                var batch = await service.CreateBatchAsync(request, ct);
                return Results.Created($"/api/v1/admin/payout-batches/{batch.Id}", batch);
            })
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces<PayoutBatchDto>(StatusCodes.Status201Created);
        admin.MapGet("/payout-batches", async (int? page, int? pageSize, PayoutService service, CancellationToken ct) =>
                Results.Ok(await service.ListBatchesAsync(Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces<PagedResult<PayoutBatchDto>>();
        admin.MapGet("/payout-batches/{id:guid}", async (Guid id, PayoutService service, CancellationToken ct) => Results.Ok(await service.GetBatchAsync(id, ct)))
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces<PayoutBatchDto>();
        admin.MapGet("/payout-batches/{id:guid}/export", async (Guid id, string? format, PayoutService service, CancellationToken ct) =>
            {
                EnsureCsv(format);
                var file = await service.ExportBatchAsync(id, ct);
                return Results.File(file.Content, "text/csv; charset=utf-8", file.FileName);
            })
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");
        admin.MapPost("/payout-batches/{id:guid}/mark-paid", async (Guid id, MarkPaidRequest request, PayoutService service, CancellationToken ct) =>
                Results.Ok(await service.MarkBatchPaidAsync(id, request, ct)))
            .RequirePermission(Permissions.PayoutsApprove)
            .Produces<PayoutBatchDto>();

        admin.MapPost("/settlement-batches", async (GenerateSettlementRequest request, SettlementService service, CancellationToken ct) =>
            {
                var batch = await service.GenerateAsync(request, ct);
                return Results.Accepted($"/api/v1/admin/settlement-batches/{batch.Id}", batch);
            })
            .RequirePermission(Permissions.SettlementsManage)
            .Produces<SettlementBatchDto>(StatusCodes.Status202Accepted)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapGet("/settlement-batches", async (int? page, int? pageSize, SettlementService service, CancellationToken ct) =>
                Results.Ok(await service.ListBatchesAsync(Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.SettlementsManage)
            .Produces<PagedResult<SettlementBatchDto>>();
        admin.MapGet("/settlement-batches/{id:guid}", async (Guid id, SettlementService service, CancellationToken ct) => Results.Ok(await service.GetBatchAsync(id, ct)))
            .RequirePermission(Permissions.SettlementsManage)
            .Produces<SettlementBatchDto>();
        admin.MapGet("/settlement-batches/{id:guid}/settlements", async (Guid id, string? direction, string? search, int? page, int? pageSize, SettlementService service, CancellationToken ct) =>
                Results.Ok(await service.ListSettlementsAsync(id, QueryEnum.Parse<SettlementDirection>(direction, "direction"), search, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.SettlementsManage)
            .Produces<PagedResult<SettlementDto>>();
        admin.MapGet("/settlement-batches/{id:guid}/export", async (Guid id, string? format, SettlementService service, CancellationToken ct) =>
            {
                EnsureCsv(format);
                var file = await service.ExportAsync(id, ct);
                return Results.File(file.Content, "text/csv; charset=utf-8", file.FileName);
            })
            .RequirePermission(Permissions.SettlementsManage)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");
        admin.MapPost("/settlement-batches/{id:guid}/finalize", async (Guid id, SettlementService service, CancellationToken ct) => Results.Ok(await service.FinalizeAsync(id, ct)))
            .RequirePermission(Permissions.SettlementsManage)
            .Produces<SettlementBatchDto>();
        admin.MapPost("/settlement-batches/{id:guid}/regenerate", async (Guid id, SettlementService service, CancellationToken ct) => Results.Ok(await service.RegenerateAsync(id, ct)))
            .RequirePermission(Permissions.SettlementsManage)
            .Produces<SettlementBatchDto>();
        admin.MapGet("/settlements/{id:guid}", async (Guid id, SettlementService service, CancellationToken ct) => Results.Ok(await service.GetSettlementAsync(id, null, ct)))
            .RequirePermission(Permissions.SettlementsManage)
            .Produces<SettlementDto>();

        admin.MapGet("/wallets", async (string? kind, string? search, bool? negativeOnly, int? page, int? pageSize, AdminPaymentService service, CancellationToken ct) =>
                Results.Ok(await service.ListWalletsAsync(QueryEnum.Parse<WalletKind>(kind, "kind"), search, negativeOnly, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<PagedResult<AdminWalletDto>>();
        admin.MapGet("/wallets/{id:guid}", async (Guid id, AdminPaymentService service, CancellationToken ct) => Results.Ok(await service.GetWalletAsync(id, ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<AdminWalletDetailDto>();
        admin.MapPost("/wallets/{id:guid}/adjustments", async (Guid id, WalletAdjustmentRequest request, AdminPaymentService service, CancellationToken ct) =>
            {
                var transaction = await service.AdjustAsync(id, request, ct);
                return Results.Created($"/api/v1/admin/wallets/{id}", transaction);
            })
            .RequirePermission(Permissions.WalletsAdjust)
            .Produces<AdminWalletTransactionDto>(StatusCodes.Status201Created);
        admin.MapPost("/wallets/{id:guid}/freeze", async (Guid id, ReasonBody? request, AdminPaymentService service, CancellationToken ct) =>
                Results.Ok(await service.SetStatusAsync(id, WalletStatus.Frozen, request, ct)))
            .RequirePermission(Permissions.WalletsAdjust)
            .Produces<AdminWalletDetailDto>();
        admin.MapPost("/wallets/{id:guid}/unfreeze", async (Guid id, ReasonBody? request, AdminPaymentService service, CancellationToken ct) =>
                Results.Ok(await service.SetStatusAsync(id, WalletStatus.Active, request, ct)))
            .RequirePermission(Permissions.WalletsAdjust)
            .Produces<AdminWalletDetailDto>();
        admin.MapGet("/ledger/balances", async (DateOnly? from, DateOnly? to, AdminPaymentService service, CancellationToken ct) =>
                Results.Ok(await service.LedgerBalancesAsync(from, to, ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<List<LedgerBalanceDto>>();
    }

    private static string IdempotencyKey(HttpContext http)
    {
        var key = http.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 64)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { idempotencyKey = "Idempotency-Key header is required (max 64 chars)" });
        }

        return key;
    }

    private static void EnsureCsv(string? format)
    {
        new Validator().Rule(nameof(format), format is null or "csv", "must be csv").ThrowIfInvalid();
    }

    private static void EnsureSandbox(PaymentsOptions options)
    {
        if (!string.Equals(options.Provider, PaymentProviders.Sandbox, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(ErrorCodes.NotFound);
        }
    }

    private static async Task<Guid> DriverIdAsync(AtaDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var userId = user.UserId;
        return await db.Drivers.AsNoTracking().Where(d => d.UserId == userId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}
