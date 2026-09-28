using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Admin;

/// <summary>Dashboard, passengers, user suspension, ride-category management and audit log browsing.</summary>
public sealed class AdminService(AtaDbContext db, AuditService audit, IClock clock)
{
    public async Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken ct)
    {
        var today = clock.UtcNow.Date;
        return new DashboardSummaryDto(
            await db.Drivers.CountAsync(d => d.ApplicationStatus == ApplicationStatus.Submitted || d.ApplicationStatus == ApplicationStatus.UnderReview, ct),
            await db.Drivers.CountAsync(d => d.ApplicationStatus == ApplicationStatus.Approved, ct),
            await db.Drivers.CountAsync(d => d.IsOnline, ct),
            await db.Passengers.CountAsync(ct),
            await db.Trips.CountAsync(t => t.RequestedAt >= today, ct),
            await db.Users.CountAsync(u => u.CreatedAt >= today, ct));
    }

    public async Task<PagedResult<AdminPassengerListItemDto>> ListPassengersAsync(string? search, Paging paging, CancellationToken ct)
    {
        var query = from p in db.Passengers.AsNoTracking()
                    join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                    select new { Passenger = p, User = u };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phoneTerm = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.User.PhoneNumber.Contains(phoneTerm) || (x.User.FullName != null && x.User.FullName.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.User.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var passengerIds = rows.Select(r => r.Passenger.Id).ToList();
        var tripCounts = await db.Trips.AsNoTracking().Where(t => passengerIds.Contains(t.PassengerId) && t.Status == TripStatus.Completed)
            .GroupBy(t => t.PassengerId).Select(g => new { PassengerId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.PassengerId, x => x.Count, ct);
        var items = rows.Select(x => new AdminPassengerListItemDto(
            x.Passenger.Id, x.User.FullName, x.User.PhoneNumber, x.User.Status, x.User.CreatedAt, tripCounts.GetValueOrDefault(x.Passenger.Id))).ToList();
        return paging.Result(items, total);
    }

    public async Task<UserStatusChangeDto> SuspendUserAsync(Guid userId, ReasonRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Reason), request.Reason, 1000).ThrowIfInvalid();
        var user = Guard.NotFound(await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct));
        if (user.Status != UserStatus.Active)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = user.Status });
        }

        var before = new { status = user.Status };
        user.Status = UserStatus.Suspended;
        var now = clock.UtcNow;
        await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        await db.Drivers.Where(d => d.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(d => d.IsOnline, false), ct);
        audit.Log("user.suspend", "user", user.Id, before, new { status = user.Status, reason = request.Reason!.Trim() });
        await db.SaveChangesAsync(ct);
        return new UserStatusChangeDto(user.Id, user.Status);
    }

    public async Task<UserStatusChangeDto> ReinstateUserAsync(Guid userId, CancellationToken ct)
    {
        var user = Guard.NotFound(await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct));
        if (user.Status != UserStatus.Suspended)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = user.Status });
        }

        var before = new { status = user.Status };
        user.Status = UserStatus.Active;
        audit.Log("user.reinstate", "user", user.Id, before, new { status = user.Status });
        await db.SaveChangesAsync(ct);
        return new UserStatusChangeDto(user.Id, user.Status);
    }

    public async Task<List<RideCategoryAdminDto>> ListRideCategoriesAsync(CancellationToken ct) =>
        await db.RideCategories.AsNoTracking().OrderBy(c => c.SortOrder).Select(c => ToDto(c)).ToListAsync(ct);

    public async Task<RideCategoryAdminDto> CreateRideCategoryAsync(RideCategoryUpsertRequest request, CancellationToken ct)
    {
        ValidateCategory(request, requireCode: true);
        var code = request.Code!.Trim().ToLowerInvariant();
        if (await db.RideCategories.AnyAsync(c => c.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { code = "already exists" });
        }

        var category = new RideCategory { Code = code, NameAr = request.NameAr!.Trim(), NameEn = request.NameEn!.Trim() };
        Apply(category, request);
        db.RideCategories.Add(category);
        audit.Log("ride_category.create", "ride_category", category.Id, null, ToDto(category));
        await db.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task<RideCategoryAdminDto> UpdateRideCategoryAsync(Guid id, RideCategoryUpsertRequest request, CancellationToken ct)
    {
        ValidateCategory(request, requireCode: false);
        var category = Guard.NotFound(await db.RideCategories.FirstOrDefaultAsync(c => c.Id == id, ct));
        var before = ToDto(category);
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim().ToLowerInvariant();
            if (code != category.Code && await db.RideCategories.AnyAsync(c => c.Code == code, ct))
            {
                throw new DomainException(ErrorCodes.Conflict, new { code = "already exists" });
            }

            category.Code = code;
        }

        Apply(category, request);
        audit.Log("ride_category.update", "ride_category", category.Id, before, ToDto(category));
        await db.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task DeleteRideCategoryAsync(Guid id, CancellationToken ct)
    {
        var category = Guard.NotFound(await db.RideCategories.FirstOrDefaultAsync(c => c.Id == id, ct));
        if (await db.Vehicles.AnyAsync(v => v.RideCategoryId == id, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { rideCategoryId = "vehicles reference this category; deactivate it instead" });
        }

        db.RideCategories.Remove(category);
        audit.Log("ride_category.delete", "ride_category", category.Id, ToDto(category), null);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> ListAuditLogsAsync(string? entityType, Guid? entityId, Paging paging, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (entityId is not null) query = query.Where(a => a.EntityId == entityId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(a => a.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var items = rows.Select(a => new AuditLogDto(
            a.Id, a.ActorUserId, a.ActorRole, a.Action, a.EntityType, a.EntityId, Parse(a.BeforeJson), Parse(a.AfterJson), a.IpAddress, a.CreatedAt)).ToList();
        return paging.Result(items, total);
    }

    private static JsonElement? Parse(string? json) => json is null ? null : JsonSerializer.Deserialize<JsonElement>(json);

    private static void ValidateCategory(RideCategoryUpsertRequest request, bool requireCode)
    {
        var v = new Validator();
        if (requireCode)
        {
            v.Require(nameof(request.Code), request.Code, 32).Require(nameof(request.NameAr), request.NameAr, 80).Require(nameof(request.NameEn), request.NameEn, 80);
        }

        v.Rule(nameof(request.Code), request.Code is null || request.Code.Trim().All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_'), "lowercase letters, digits and underscores only")
         .Rule(nameof(request.Seats), request.Seats is null or (>= 1 and <= 12), "must be between 1 and 12")
         .Rule(nameof(request.MaxStops), request.MaxStops is null or <= 5, "must be at most 5")
         .Rule(nameof(request.BaseFare), request.BaseFare is null or >= 0, "must be positive")
         .Rule(nameof(request.PerKm), request.PerKm is null or >= 0, "must be positive")
         .Rule(nameof(request.PerMinute), request.PerMinute is null or >= 0, "must be positive")
         .Rule(nameof(request.BookingFee), request.BookingFee is null or >= 0, "must be positive")
         .Rule(nameof(request.MinFare), request.MinFare is null or >= 0, "must be positive")
         .Rule(nameof(request.DriverSharePercent), request.DriverSharePercent is null or (>= 0 and <= 100), "must be between 0 and 100")
         .ThrowIfInvalid();
    }

    private static void Apply(RideCategory category, RideCategoryUpsertRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.NameAr)) category.NameAr = request.NameAr.Trim();
        if (!string.IsNullOrWhiteSpace(request.NameEn)) category.NameEn = request.NameEn.Trim();
        if (request.DescriptionAr is not null) category.DescriptionAr = request.DescriptionAr.Trim();
        if (request.DescriptionEn is not null) category.DescriptionEn = request.DescriptionEn.Trim();
        if (request.Icon is not null) category.Icon = request.Icon.Trim();
        if (request.Seats is not null) category.Seats = request.Seats.Value;
        if (request.MaxStops is not null) category.MaxStops = request.MaxStops.Value;
        if (request.SortOrder is not null) category.SortOrder = request.SortOrder.Value;
        if (request.IsActive is not null) category.IsActive = request.IsActive.Value;
        if (request.BaseFare is not null) category.BaseFare = request.BaseFare.Value;
        if (request.PerKm is not null) category.PerKm = request.PerKm.Value;
        if (request.PerMinute is not null) category.PerMinute = request.PerMinute.Value;
        if (request.BookingFee is not null) category.BookingFee = request.BookingFee.Value;
        if (request.MinFare is not null) category.MinFare = request.MinFare.Value;
        if (request.DriverSharePercent is not null) category.DriverSharePercent = request.DriverSharePercent.Value;
    }

    private static RideCategoryAdminDto ToDto(RideCategory c) =>
        new(c.Id, c.Code, c.NameAr, c.NameEn, c.DescriptionAr, c.DescriptionEn, c.Icon, c.Seats, c.MaxStops, c.SortOrder, c.IsActive,
            c.BaseFare, c.PerKm, c.PerMinute, c.BookingFee, c.MinFare, c.DriverSharePercent);
}
