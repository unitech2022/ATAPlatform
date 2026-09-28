using ATA.Api.Common;
using ATA.Api.Modules.Payments;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Wallet;

namespace ATA.Api.Modules.Wallet;

/// <summary>A wallet payment option: <c>wallet</c>, <c>cash</c> or a saved <c>card</c> (with id, brand and last 4 digits).</summary>
public sealed record PaymentMethodDto(string Type, string Label, bool IsDefault, Guid? Id = null, string? Brand = null, string? Last4 = null);

/// <summary><c>CashDebt</c> is set for driver wallets (<c>max(0, −balance)</c>).</summary>
public sealed record WalletDto(Guid Id, WalletKind Kind, string Currency, decimal Balance, IReadOnlyList<PaymentMethodDto> PaymentMethods, decimal? CashDebt = null,
    WalletStatus Status = WalletStatus.Active, decimal? CashDebtLimit = null);

public sealed record WalletTransactionDto(Guid Id, TransactionType Type, TransactionDirection Direction, decimal Amount, decimal BalanceAfter, string? Description, DateTime CreatedAt);

public sealed record TopupRequest(decimal? Amount, string? Method, Guid? PaymentMethodId = null, string? ApplePayToken = null, string? ReturnUrl = null);

/// <summary><c>201</c>: <c>transactionId</c>, <c>balance</c>, <c>paymentId</c> (null for sandbox), <c>status</c>. <c>202</c>: <c>paymentId</c>, <c>status</c>, <c>action</c>.</summary>
public sealed record TopupResponse(Guid? TransactionId, decimal? Balance, Guid? PaymentId, PaymentStatus? Status, PaymentActionDto? Action);

public static class WalletEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/wallet").WithTags("Wallet").RequireAuthorization(Policies.Authenticated);

        group.MapGet("/", async (string? kind, WalletService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(QueryEnum.Parse<WalletKind>(kind, "kind"), http.GetLanguage(), ct)))
            .Produces<WalletDto>();

        group.MapGet("/transactions", async (string? kind, int? page, int? pageSize, WalletService service, CancellationToken ct) =>
                Results.Ok(await service.GetTransactionsAsync(QueryEnum.Parse<WalletKind>(kind, "kind"), Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<WalletTransactionDto>>();

        group.MapPost("/topups", async (string? kind, TopupRequest request, WalletService service, HttpContext http, CancellationToken ct) =>
            {
                var key = http.Request.Headers["Idempotency-Key"].ToString();
                if (string.IsNullOrWhiteSpace(key) || key.Length > 60)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new { idempotencyKey = "Idempotency-Key header is required (max 60 chars)" });
                }

                var result = await service.TopupAsync(QueryEnum.Parse<WalletKind>(kind, "kind"), request, key, ct);
                return result.Accepted ? Results.Accepted("/api/v1/wallet/transactions", result.Body) : Results.Created("/api/v1/wallet/transactions", result.Body);
            })
            .Produces<TopupResponse>(StatusCodes.Status201Created)
            .Produces<TopupResponse>(StatusCodes.Status202Accepted)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
    }
}
