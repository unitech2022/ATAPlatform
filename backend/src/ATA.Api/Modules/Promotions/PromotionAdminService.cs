using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Promotions;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Promotions;

/// <summary>Admin console of promo codes (<c>promotions.manage</c>); every write is audited (<c>promotion.create|update|activate|deactivate</c>).</summary>
public sealed class PromotionAdminService(AtaDbContext db, IClock clock, ICurrentUser currentUser, AuditService audit)
{
    private const string EntityType = "promotion";

    public static string StatusOf(Promotion p, DateTime now) =>
        !p.IsActive ? "inactive" : p.ValidFrom > now ? "scheduled" : p.ValidTo < now ? "expired" : "active";

    public async Task<PagedResult<PromotionListItemDto>> ListAsync(string? status, string? search, Paging paging, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var query = db.Promotions.AsNoTracking().AsQueryable();
        query = status switch
        {
            null or "" or "all" => query,
            "active" => query.Where(p => p.IsActive && p.ValidFrom <= now && p.ValidTo >= now),
            "scheduled" => query.Where(p => p.IsActive && p.ValidFrom > now),
            "expired" => query.Where(p => p.IsActive && p.ValidTo < now),
            "inactive" => query.Where(p => !p.IsActive),
            _ => throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["status"] = "must be active|scheduled|expired|inactive" }),
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var upper = term.ToUpperInvariant();
            query = query.Where(p => p.Code.Contains(upper) || p.NameAr.Contains(term) || p.NameEn.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(p => p.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(p => new PromotionListItemDto(p.Id, p.Code, p.NameAr, p.NameEn, p.Type, p.Value, p.ValidFrom, p.ValidTo, p.UsageCount,
            p.TotalUsageLimit, p.SpentAmount, p.BudgetAmount, p.IsActive, StatusOf(p, now))).ToList(), total);
    }

    public async Task<PromotionDto> GetAsync(Guid id, CancellationToken ct) =>
        await ToDtoAsync(Guard.NotFound(await db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)), ct);

    public async Task<PromotionDto> CreateAsync(PromotionUpsertRequest request, CancellationToken ct)
    {
        var code = await ValidateAsync(request, null, ct);
        var promotion = new Promotion { Code = code, NameAr = string.Empty, NameEn = string.Empty, CreatedBy = currentUser.UserId };
        Apply(promotion, request);
        db.Promotions.Add(promotion);
        audit.Log("promotion.create", EntityType, promotion.Id, null, Snapshot(promotion));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new DomainException(ErrorCodes.Conflict, new { code = "taken" });
        }

        return await ToDtoAsync(promotion, ct);
    }

    /// <summary><c>code</c> and <c>type</c> cannot change after the first reservation (<c>409 conflict</c>).</summary>
    public async Task<PromotionDto> UpdateAsync(Guid id, PromotionUpsertRequest request, CancellationToken ct)
    {
        var promotion = Guard.NotFound(await db.Promotions.FirstOrDefaultAsync(p => p.Id == id, ct));
        var code = await ValidateAsync(request, promotion, ct);
        var used = await db.PromotionRedemptions.AnyAsync(r => r.PromotionId == id, ct);
        if (used && (code != promotion.Code || request.Type != promotion.Type))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "promotion_in_use", fields = new[] { "code", "type" } });
        }

        var before = Snapshot(promotion);
        promotion.Code = code;
        Apply(promotion, request);
        audit.Log("promotion.update", EntityType, promotion.Id, before, Snapshot(promotion));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new DomainException(ErrorCodes.Conflict, new { code = "taken" });
        }

        return await ToDtoAsync(promotion, ct);
    }

    /// <summary>Deactivation keeps existing reservations (they are still applied or released normally).</summary>
    public async Task<PromotionDto> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var promotion = Guard.NotFound(await db.Promotions.FirstOrDefaultAsync(p => p.Id == id, ct));
        if (promotion.IsActive != active)
        {
            promotion.IsActive = active;
            audit.Log(active ? "promotion.activate" : "promotion.deactivate", EntityType, promotion.Id, new { isActive = !active }, new { isActive = active });
            await db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(promotion, ct);
    }

    public async Task<PagedResult<PromotionRedemptionDto>> RedemptionsAsync(Guid? promotionId, RedemptionStatus? status, DateOnly? from, DateOnly? to, Paging paging, CancellationToken ct)
    {
        if (promotionId is { } pid && !await db.Promotions.AnyAsync(p => p.Id == pid, ct))
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        var query = from r in db.PromotionRedemptions.AsNoTracking()
                    join p in db.Promotions.AsNoTracking() on r.PromotionId equals p.Id
                    join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                    join ps in db.Passengers.AsNoTracking() on r.PassengerId equals ps.Id
                    join u in db.Users.AsNoTracking() on ps.UserId equals u.Id
                    select new { r, p.Code, t.TripNumber, u.FullName, u.PhoneNumber };
        if (promotionId is { } id) query = query.Where(x => x.r.PromotionId == id);
        if (status is { } s) query = query.Where(x => x.r.Status == s);
        if (from is { } f)
        {
            var fromAt = Formats.RiyadhMidnightUtc(f);
            query = query.Where(x => x.r.ReservedAt >= fromAt);
        }

        if (to is { } tt)
        {
            var toAt = Formats.RiyadhMidnightUtc(tt.AddDays(1));
            query = query.Where(x => x.r.ReservedAt < toAt);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.r.ReservedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => new PromotionRedemptionDto(x.r.Id, x.FullName, PhoneMasking.Mask(x.PhoneNumber), x.TripNumber, x.r.Status, x.r.ReservedAmount,
            x.r.DiscountAmount, x.r.ReservedAt, x.r.AppliedAt, x.r.ReleaseReason, x.r.TripId, x.r.ReleasedAt, x.r.PromotionId, x.Code)).ToList(), total);
    }

    /// <summary><c>firstTripConversions</c> = applied redemptions on the passenger's first completed trip.</summary>
    public async Task<PromotionStatsDto> StatsAsync(Guid id, CancellationToken ct)
    {
        Guard.NotFound(await db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct));
        var rows = await db.PromotionRedemptions.AsNoTracking().Where(r => r.PromotionId == id)
            .Select(r => new { r.Status, r.DiscountAmount, r.PassengerId, r.TripId }).ToListAsync(ct);
        var applied = rows.Where(r => r.Status == RedemptionStatus.Applied).ToList();
        var appliedTrips = applied.Select(r => r.TripId).ToList();
        var completions = await db.Trips.AsNoTracking().Where(t => appliedTrips.Contains(t.Id)).Select(t => new { t.Id, t.PassengerId, t.CompletedAt }).ToListAsync(ct);
        var conversions = 0;
        foreach (var trip in completions)
        {
            var earlier = await db.Trips.AsNoTracking().AnyAsync(t => t.PassengerId == trip.PassengerId && t.Status == TripStatus.Completed && t.CompletedAt < trip.CompletedAt, ct);
            if (!earlier) conversions++;
        }

        return new PromotionStatsDto(
            rows.Count(r => r.Status == RedemptionStatus.Reserved), applied.Count, rows.Count(r => r.Status == RedemptionStatus.Released),
            applied.Sum(r => r.DiscountAmount ?? 0m), rows.Where(r => r.Status != RedemptionStatus.Released).Select(r => r.PassengerId).Distinct().Count(), conversions);
    }

    private async Task<string> ValidateAsync(PromotionUpsertRequest r, Promotion? existing, CancellationToken ct)
    {
        var code = r.Code is null ? string.Empty : Promotion.Normalize(r.Code);
        var v = new Validator()
            .Require(nameof(r.Code), r.Code, Promotion.CodeMaxLength)
            .Rule(nameof(r.Code), string.IsNullOrEmpty(code) || Promotion.IsValidCode(code), "must be 4-20 latin letters or digits")
            .Require(nameof(r.NameAr), r.NameAr, 120)
            .Require(nameof(r.NameEn), r.NameEn, 120)
            .Rule(nameof(r.DescriptionAr), r.DescriptionAr is null || r.DescriptionAr.Length <= 500, "max_length:500")
            .Rule(nameof(r.DescriptionEn), r.DescriptionEn is null || r.DescriptionEn.Length <= 500, "max_length:500")
            .Require(nameof(r.Type), r.Type)
            .Require(nameof(r.ValidFrom), r.ValidFrom)
            .Require(nameof(r.ValidTo), r.ValidTo)
            .Rule(nameof(r.ValidTo), r.ValidFrom is null || r.ValidTo is null || r.ValidTo > r.ValidFrom, "must be after validFrom")
            .Rule(nameof(r.MaxDiscount), r.MaxDiscount is null or > 0, "must be positive")
            .Rule(nameof(r.MinFare), r.MinFare is null or >= 0, "must be positive")
            .Rule(nameof(r.TotalUsageLimit), r.TotalUsageLimit is null or > 0, "must be positive")
            .Rule(nameof(r.PerUserLimit), r.PerUserLimit is null or > 0, "must be positive")
            .Rule(nameof(r.BudgetAmount), r.BudgetAmount is null or > 0, "must be positive")
            .Rule(nameof(r.NewUserDays), r.NewUserDays is null or > 0, "must be positive");
        switch (r.Type)
        {
            case PromotionType.Percent:
                v.Rule(nameof(r.Value), r.Value is >= 1 and <= 100, "must be between 1 and 100");
                break;
            case PromotionType.Fixed:
                v.Rule(nameof(r.Value), r.Value is > 0, "must be positive");
                break;
            case PromotionType.FreeBookingFee:
                v.Rule(nameof(r.Value), r.Value is null or >= 0, "must be positive");
                break;
        }

        if (existing is not null && r.TotalUsageLimit is { } limit)
        {
            v.Rule(nameof(r.TotalUsageLimit), limit >= existing.UsageCount, $"must be at least the current usage ({existing.UsageCount})");
        }

        v.ThrowIfInvalid();
        if (r.CityId is { } cityId)
        {
            v.Rule(nameof(r.CityId), await db.Cities.AnyAsync(c => c.Id == cityId, ct), "unknown city");
        }

        if (r.RideCategoryIds is { Count: > 0 } categories)
        {
            var known = await db.RideCategories.CountAsync(c => categories.Contains(c.Id), ct);
            v.Rule(nameof(r.RideCategoryIds), known == categories.Distinct().Count(), "unknown ride category");
        }

        if (r.ZoneIds is { Count: > 0 } zones)
        {
            var known = await db.Zones.CountAsync(z => zones.Contains(z.Id), ct);
            v.Rule(nameof(r.ZoneIds), known == zones.Distinct().Count(), "unknown zone");
        }

        v.ThrowIfInvalid();
        if (await db.Promotions.AnyAsync(p => p.Code == code && (existing == null || p.Id != existing.Id), ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { code = "taken" });
        }

        return code;
    }

    private static void Apply(Promotion p, PromotionUpsertRequest r)
    {
        p.NameAr = r.NameAr!.Trim();
        p.NameEn = r.NameEn!.Trim();
        p.DescriptionAr = string.IsNullOrWhiteSpace(r.DescriptionAr) ? null : r.DescriptionAr.Trim();
        p.DescriptionEn = string.IsNullOrWhiteSpace(r.DescriptionEn) ? null : r.DescriptionEn.Trim();
        p.Type = r.Type!.Value;
        p.Value = p.Type == PromotionType.FreeBookingFee ? r.Value ?? 0m : r.Value!.Value;
        p.MaxDiscount = r.MaxDiscount;
        p.MinFare = r.MinFare;
        p.ValidFrom = r.ValidFrom!.Value.ToUniversalTime();
        p.ValidTo = r.ValidTo!.Value.ToUniversalTime();
        p.TotalUsageLimit = r.TotalUsageLimit;
        p.PerUserLimit = r.PerUserLimit ?? 1;
        p.BudgetAmount = r.BudgetAmount;
        p.FirstTripOnly = r.FirstTripOnly ?? false;
        p.NewUsersOnly = r.NewUsersOnly ?? false;
        p.NewUserDays = r.NewUserDays ?? 30;
        p.CityId = r.CityId;
        p.RideCategoryIds = JsonLists.Serialize(r.RideCategoryIds);
        p.ZoneIds = JsonLists.Serialize(r.ZoneIds);
        p.PaymentMethods = JsonLists.Serialize(r.PaymentMethods);
        p.BookingTypes = JsonLists.Serialize(r.BookingTypes);
        p.IsStackable = r.IsStackable ?? false;
        p.IsPublic = r.IsPublic ?? false;
        p.IsActive = r.IsActive ?? true;
    }

    private static object Snapshot(Promotion p) => new
    {
        p.Code, p.NameAr, p.NameEn, p.Type, p.Value, p.MaxDiscount, p.MinFare, p.ValidFrom, p.ValidTo, p.TotalUsageLimit, p.PerUserLimit, p.BudgetAmount,
        p.FirstTripOnly, p.NewUsersOnly, p.NewUserDays, p.CityId, p.RideCategoryIds, p.ZoneIds, p.PaymentMethods, p.BookingTypes, p.IsStackable, p.IsPublic, p.IsActive,
    };

    private async Task<PromotionDto> ToDtoAsync(Promotion p, CancellationToken ct)
    {
        var createdByName = p.CreatedBy is { } by ? await db.Users.AsNoTracking().Where(u => u.Id == by).Select(u => u.FullName).FirstOrDefaultAsync(ct) : null;
        return new PromotionDto(p.Id, p.Code, p.NameAr, p.NameEn, p.DescriptionAr, p.DescriptionEn, p.Type, p.Value, p.MaxDiscount, p.MinFare, p.ValidFrom, p.ValidTo,
            p.TotalUsageLimit, p.PerUserLimit, p.UsageCount, p.BudgetAmount, p.SpentAmount, p.FirstTripOnly, p.NewUsersOnly, p.NewUserDays, p.CityId,
            JsonLists.Parse<Guid>(p.RideCategoryIds), JsonLists.Parse<Guid>(p.ZoneIds), JsonLists.Parse<PaymentMethodKind>(p.PaymentMethods),
            JsonLists.Parse<BookingType>(p.BookingTypes), p.IsStackable, p.IsPublic, p.IsActive, StatusOf(p, clock.UtcNow), p.CreatedBy, createdByName, p.CreatedAt, p.UpdatedAt);
    }
}
