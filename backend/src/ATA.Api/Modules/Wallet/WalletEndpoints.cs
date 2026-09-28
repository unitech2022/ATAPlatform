using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Wallet;

namespace ATA.Api.Modules.Wallet;

public sealed record PaymentMethodDto(string Type, string Label, bool IsDefault);

public sealed record WalletDto(Guid Id, WalletKind Kind, string Currency, decimal Balance, IReadOnlyList<PaymentMethodDto> PaymentMethods);

public sealed record WalletTransactionDto(Guid Id, TransactionType Type, TransactionDirection Direction, decimal Amount, decimal BalanceAfter, string? Description, DateTime CreatedAt);

public sealed record TopupRequest(decimal? Amount, string? Method);

public sealed record TopupResponse(Guid TransactionId, decimal Balance);

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
                if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new { idempotencyKey = "Idempotency-Key header is required (max 128 chars)" });
                }

                var result = await service.TopupAsync(QueryEnum.Parse<WalletKind>(kind, "kind"), request, key, ct);
                return Results.Created($"/api/v1/wallet/transactions", result);
            })
            .Produces<TopupResponse>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
    }
}
