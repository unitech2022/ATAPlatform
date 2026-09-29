using ATA.Api.Common;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Payments;
using ATA.Domain.Common;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Promotions;

public static class DiscountSources
{
    public const string Promotion = "promotion";
    public const string FavoriteDriver = "favorite_driver";
}

/// <summary>One discount offered for a fare: a promo code (F15) or the favourite-driver rule (F16, not wired yet).</summary>
public sealed record DiscountCandidate(string Source, string? Reference, decimal Amount, bool Stackable);

public sealed record AppliedDiscount(string Source, string? Reference, decimal Amount);

/// <summary>
/// The combined discount of a fare. <see cref="PromotionDropped"/> is set when a non-stackable promotion lost to the favourite discount
/// (its reservation is then released with <c>not_stacked</c>).
/// </summary>
public sealed record DiscountOutcome(decimal Base, decimal Discount, decimal Total, IReadOnlyList<AppliedDiscount> Applied, bool PromotionDropped)
{
    public decimal AmountOf(string source) => Applied.Where(a => a.Source == source).Sum(a => a.Amount);
}

/// <summary>The unified discount engine of doc 10 §1, used by the quote, the trip request (reservation) and the completion.</summary>
public interface IDiscountEngine
{
    /// <summary>
    /// <code>
    /// promo + fav: both stackable → [promo, fav]; otherwise the larger one (a tie goes to fav, the promotion is dropped)
    /// discount = min(Σ, base − Promotions:MinPayableFare);  total = round(base − discount, 0.5)
    /// </code>
    /// </summary>
    DiscountOutcome Combine(decimal baseFare, DiscountCandidate? promotion, DiscountCandidate? favorite = null);
}

public sealed class DiscountEngine(IOptions<PromotionsOptions> options) : IDiscountEngine
{
    public DiscountOutcome Combine(decimal baseFare, DiscountCandidate? promotion, DiscountCandidate? favorite = null) =>
        Compute(baseFare, promotion, favorite, options.Value.MinPayableFare);

    public static DiscountOutcome Compute(decimal baseFare, DiscountCandidate? promotion, DiscountCandidate? favorite, decimal minPayableFare)
    {
        var chosen = new List<DiscountCandidate>(2);
        var dropped = false;
        if (promotion is { Amount: > 0 } && favorite is { Amount: > 0 })
        {
            if (promotion.Stackable && favorite.Stackable)
            {
                chosen.Add(promotion);
                chosen.Add(favorite);
            }
            else if (favorite.Amount >= promotion.Amount)
            {
                chosen.Add(favorite);
                dropped = true;
            }
            else
            {
                chosen.Add(promotion);
            }
        }
        else if (promotion is { Amount: > 0 })
        {
            chosen.Add(promotion);
        }
        else if (favorite is { Amount: > 0 })
        {
            chosen.Add(favorite);
        }

        var cap = Math.Max(0m, baseFare - Math.Max(0m, minPayableFare));
        var remaining = cap;
        var applied = new List<AppliedDiscount>(chosen.Count);
        foreach (var candidate in chosen)
        {
            var amount = PricingMath.Round2(Math.Min(candidate.Amount, remaining));
            remaining -= amount;
            applied.Add(new AppliedDiscount(candidate.Source, candidate.Reference, amount));
        }

        var discount = applied.Sum(a => a.Amount);
        return new DiscountOutcome(baseFare, discount, Math.Max(0m, PricingMath.RoundToHalf(baseFare - discount)), applied.Where(a => a.Amount > 0).ToList(), dropped);
    }

    /// <summary>Receipt / breakdown label of a discount line in the viewer's language.</summary>
    public static string Label(string source, string? reference, Language lang) => source switch
    {
        DiscountSources.Promotion => lang.Pick($"خصم {reference}", $"{reference} discount"),
        DiscountSources.FavoriteDriver => lang.Pick("خصم الكابتن المفضل", "Favourite driver discount"),
        _ => lang.Pick("خصم", "Discount"),
    };

    public static List<DiscountDto> Lines(DiscountOutcome outcome, Language lang) =>
        outcome.Applied.Select(a => new DiscountDto(a.Source, a.Reference, Label(a.Source, a.Reference, lang), a.Amount)).ToList();
}
