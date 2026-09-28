using ATA.Api.Common;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

public sealed record AddCardOutcome(bool Pending, SavedCardDto Card, PaymentActionDto? Action);

/// <summary>
/// Saved cards (<c>/passenger/payment-methods</c>): the client tokenizes the card with the provider SDK and the server only verifies and stores
/// the token (never a PAN). A 3-D Secure verification leaves the card <c>pending_verification</c> until the challenge/return completes.
/// </summary>
public sealed class PaymentMethodService(AtaDbContext db, IPaymentGatewayResolver gateways, ICurrentUser currentUser, IClock clock, IOptions<PaymentsOptions> options)
{
    private readonly PaymentsOptions _options = options.Value;

    public async Task<IReadOnlyList<SavedCardDto>> ListAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var cards = await db.PaymentMethods.AsNoTracking()
            .Where(m => m.UserId == userId && (m.Status == SavedCardStatus.Active || m.Status == SavedCardStatus.PendingVerification))
            .OrderByDescending(m => m.IsDefault).ThenByDescending(m => m.CreatedAt).ToListAsync(ct);
        var now = clock.UtcNow;
        return cards.Select(c => ToDto(c, now)).ToList();
    }

    public async Task<AddCardOutcome> AddAsync(AddCardRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Token), request.Token, 255)
            .Rule(nameof(request.ReturnUrl), request.ReturnUrl is null || request.ReturnUrl.Length <= 500, "max_length:500")
            .ThrowIfInvalid();

        var userId = currentUser.UserId;
        var passenger = await db.Passengers.FirstOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        var cards = await db.PaymentMethods.Where(m => m.UserId == userId).ToListAsync(ct);
        var live = cards.Where(c => c.Status is SavedCardStatus.Active or SavedCardStatus.PendingVerification).ToList();
        new Validator().Rule(nameof(request.Token), live.Count < _options.MaxCardsPerUser, "limit").ThrowIfInvalid();

        var gateway = gateways.Current;
        var verificationId = Guid.CreateVersion7();
        var returnUrl = request.ReturnUrl ?? _options.DefaultReturnUrl;
        GatewayCardResult result;
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.GatewayTimeoutSeconds)));
            try
            {
                result = await gateway.VerifyCardTokenAsync(verificationId, request.Token!.Trim(), returnUrl, timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new DomainException(ErrorCodes.PaymentProviderUnavailable);
            }
        }

        if (!result.Success || result.Token is null)
        {
            throw new DomainException(ErrorCodes.PaymentFailed, new { failureCode = result.FailureCode, failureMessage = result.FailureMessage });
        }

        var fingerprint = result.Fingerprint;
        if (fingerprint is not null && live.Any(c => c.Fingerprint == fingerprint))
        {
            throw new DomainException(ErrorCodes.Conflict, new { token = "card_already_saved" });
        }

        var now = clock.UtcNow;
        // A card removed earlier keeps its row (UNIQUE(user_id, fingerprint)): reuse it.
        var card = fingerprint is null ? null : cards.FirstOrDefault(c => c.Fingerprint == fingerprint);
        if (card is null)
        {
            card = new PaymentMethod { Id = verificationId, UserId = userId, Provider = gateway.Provider, GatewayToken = result.Token, Brand = "card", Last4 = "0000" };
            db.PaymentMethods.Add(card);
        }
        else if (card.Id != verificationId)
        {
            result = result with { ActionUrl = result.ActionUrl?.Replace(verificationId.ToString(), card.Id.ToString(), StringComparison.Ordinal) };
        }

        card.Provider = gateway.Provider;
        card.GatewayToken = result.Token;
        card.Brand = result.Brand ?? "card";
        card.Last4 = result.Last4 ?? "0000";
        card.ExpiryMonth = (byte)(result.ExpiryMonth ?? 12);
        card.ExpiryYear = (short)(result.ExpiryYear ?? now.Year);
        card.HolderName = result.HolderName;
        card.Fingerprint = fingerprint;
        card.RemovedAt = null;
        var pending = result.ActionUrl is not null;
        card.Status = pending ? SavedCardStatus.PendingVerification : SavedCardStatus.Active;
        card.VerifiedAt = pending ? null : now;
        if (!pending && (request.SetDefault == true || !live.Any(c => c.Status == SavedCardStatus.Active)))
        {
            // The first card becomes the default card; the default payment method only switches to card when asked.
            MakeDefault(card, cards, passenger, switchMethod: request.SetDefault == true);
        }

        await db.SaveChangesAsync(ct);
        return new AddCardOutcome(pending, ToDto(card, now), pending ? new PaymentActionDto("redirect", result.ActionUrl!, now.AddSeconds(_options.ActionTimeoutSeconds)) : null);
    }

    public async Task<SavedCardDto> SetDefaultAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var cards = await db.PaymentMethods.Where(m => m.UserId == userId).ToListAsync(ct);
        var card = cards.FirstOrDefault(c => c.Id == id && c.Status != SavedCardStatus.Removed) ?? throw new DomainException(ErrorCodes.NotFound);
        var now = clock.UtcNow;
        card.EnsureUsable(now);
        var passenger = await db.Passengers.FirstOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        MakeDefault(card, cards, passenger, switchMethod: true);
        await db.SaveChangesAsync(ct);
        return ToDto(card, now);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var card = await db.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.Status != SavedCardStatus.Removed, ct)
                   ?? throw new DomainException(ErrorCodes.NotFound);
        var inUse = await db.Trips.AnyAsync(t => t.PaymentMethodId == id && Trip.ActiveStatuses.Contains(t.Status), ct);
        if (inUse)
        {
            throw new DomainException(ErrorCodes.PaymentMethodInUse);
        }

        await gateways.Get(card.Provider).DeleteCardTokenAsync(card.GatewayToken, ct);
        card.Status = SavedCardStatus.Removed;
        card.RemovedAt = clock.UtcNow;
        if (card.IsDefault)
        {
            card.IsDefault = false;
            var passenger = await db.Passengers.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (passenger is not null && passenger.DefaultPaymentMethodId == id)
            {
                passenger.DefaultPaymentMethodId = null;
                if (passenger.DefaultPaymentMethod == PaymentMethodKind.Card)
                {
                    passenger.DefaultPaymentMethod = PaymentMethodKind.Cash;
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Completes a pending card verification (sandbox challenge or provider return). Returns the redirect URL, or <c>null</c> if unknown.</summary>
    public async Task<string?> CompleteVerificationAsync(Guid id, bool approve, string? returnUrl, CancellationToken ct)
    {
        var card = await db.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (card is null)
        {
            return null;
        }

        if (card.Status == SavedCardStatus.PendingVerification)
        {
            var now = clock.UtcNow;
            card.Status = approve ? SavedCardStatus.Active : SavedCardStatus.Failed;
            card.VerifiedAt = approve ? now : null;
            if (approve)
            {
                var cards = await db.PaymentMethods.Where(m => m.UserId == card.UserId).ToListAsync(ct);
                var passenger = await db.Passengers.FirstOrDefaultAsync(p => p.UserId == card.UserId, ct);
                if (passenger is not null && !cards.Any(c => c.Id != card.Id && c.Status == SavedCardStatus.Active && c.IsDefault))
                {
                    MakeDefault(card, cards, passenger, switchMethod: false);
                }
            }

            await db.SaveChangesAsync(ct);
        }

        return QueryHelpers.AddQueryString(returnUrl ?? _options.DefaultReturnUrl, new Dictionary<string, string?>
        {
            ["paymentMethodId"] = card.Id.ToString(),
            ["status"] = System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(card.Status.ToString()),
        });
    }

    public static string Label(PaymentMethod card, Language lang)
    {
        var brand = card.Brand switch
        {
            "mada" => lang.Pick("مدى", "mada"),
            "visa" => "Visa",
            "mastercard" => "Mastercard",
            _ => card.Brand,
        };
        return $"{brand} •••• {card.Last4}";
    }

    public static SavedCardDto ToDto(PaymentMethod card, DateTime now) =>
        new(card.Id, card.Type, card.Brand, card.Last4, card.ExpiryMonth, card.ExpiryYear, card.HolderName, card.Status, card.IsDefault, card.IsExpiredAt(now), card.CreatedAt);

    private static void MakeDefault(PaymentMethod card, IEnumerable<PaymentMethod> all, Domain.Passengers.PassengerProfile passenger, bool switchMethod)
    {
        foreach (var other in all.Where(c => c.Id != card.Id))
        {
            other.IsDefault = false;
        }

        card.IsDefault = true;
        passenger.DefaultPaymentMethodId = card.Id;
        if (switchMethod)
        {
            passenger.DefaultPaymentMethod = PaymentMethodKind.Card;
        }
    }
}
