using Microsoft.EntityFrameworkCore;
using ATA.Domain.Common;

namespace ATA.Api.Common;

/// <summary>Admin permission names (full role/permission management arrives with F20; the seeded admin holds <c>*</c>).</summary>
public static class Permissions
{
    public const string PaymentsView = "payments.view";
    public const string PaymentsRefund = "payments.refund";
    public const string PaymentsRefundApprove = "payments.refund_approve";
    public const string PayoutsApprove = "payouts.approve";
    public const string SettlementsManage = "settlements.manage";
    public const string WalletsAdjust = "wallets.adjust";
    public const string NotificationsView = "notifications.view";
    public const string NotificationsManage = "notifications.manage";
    public const string NotificationsSmsBroadcast = "notifications.sms_broadcast";
    public const string SafetyManage = "safety.manage";
    public const string SupportManage = "support.manage";
    public const string TripsView = "trips.view";
    public const string TripsCancel = "trips.cancel";
    public const string CancellationManage = "cancellation.manage";
    public const string CancellationReview = "cancellation.review";
    public const string ReliabilityManage = "reliability.manage";
    public const string ReportsView = "reports.view";
    public const string RatingsManage = "ratings.manage";
    public const string PromotionsManage = "promotions.manage";
    public const string IncentivesManage = "incentives.manage";
    public const string FavoritesManage = "favorites.manage";

    /// <summary>Whether a stored permission list (JSON array of <c>admin_accounts.permissions</c>) grants <paramref name="permission"/>.</summary>
    public static bool Grants(string? permissionsJson, string permission)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return false;
        }

        try
        {
            var values = System.Text.Json.JsonSerializer.Deserialize<string[]>(permissionsJson) ?? [];
            return values.Contains("*") || values.Contains(permission);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    /// <summary>Rejects the request with <c>403 forbidden</c> unless the caller holds at least one of <paramref name="permissions"/> (or <c>*</c>).</summary>
    public static TBuilder RequireAnyPermission<TBuilder>(this TBuilder builder, params string[] permissions) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
            if (!permissions.Any(user.HasPermission))
            {
                throw new DomainException(ErrorCodes.Forbidden, new { permissions });
            }

            return await next(context);
        });

    /// <summary>Rejects the request with <c>403 forbidden</c> unless the caller's JWT carries <paramref name="permission"/> (or <c>*</c>).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
            if (!user.HasPermission(permission))
            {
                throw new DomainException(ErrorCodes.Forbidden, new { permission });
            }

            return await next(context);
        });
}

/// <summary>Money and time formatting for notification texts: <c>46.00 ر.س</c> / <c>SAR 46.00</c>, Riyadh local <c>HH:mm dd/MM</c>.</summary>
public static class Formats
{
    public const int RiyadhOffsetMinutes = 180;

    public static string MoneyAr(decimal amount) => $"{amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} ر.س";

    public static string MoneyEn(decimal amount) => $"SAR {amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}";

    public static string Money(decimal amount) => amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    public static DateTime ToRiyadh(DateTime utc) => DateTime.SpecifyKind(utc.AddMinutes(RiyadhOffsetMinutes), DateTimeKind.Unspecified);

    public static DateOnly RiyadhDate(DateTime utc) => DateOnly.FromDateTime(ToRiyadh(utc));

    /// <summary>UTC instant of local (Riyadh) midnight at the start of <paramref name="date"/>.</summary>
    public static DateTime RiyadhMidnightUtc(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddMinutes(-RiyadhOffsetMinutes), DateTimeKind.Utc);

    public static string LocalTime(DateTime utc) => ToRiyadh(utc).ToString("HH:mm dd/MM", System.Globalization.CultureInfo.InvariantCulture);
}

public static class DbTransactions
{
    /// <summary>
    /// Runs <paramref name="action"/> in a database transaction under the provider's execution strategy (money movements), or directly
    /// when a transaction is already open. The action must call <c>SaveChangesAsync</c>.
    /// </summary>
    public static async Task InTransactionAsync(this ATA.Infrastructure.Persistence.AtaDbContext db, Func<Task> action, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            await action();
            return;
        }

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await action();
            await tx.CommitAsync(ct);
        });
    }

    public static async Task<T> InTransactionAsync<T>(this ATA.Infrastructure.Persistence.AtaDbContext db, Func<Task<T>> action, CancellationToken ct)
    {
        T result = default!;
        await db.InTransactionAsync(async () => { result = await action(); }, ct);
        return result;
    }
}

/// <summary>Human-readable sequential numbers such as <c>R-YYYYMMDD-#####</c> (sequential per UTC day; retried on a unique conflict).</summary>
public static class SequenceNumbers
{
    public static async Task<string> NextAsync(IQueryable<string> existing, string prefix, int digits, int offset, CancellationToken ct)
    {
        var count = await existing.CountAsync(n => n.StartsWith(prefix), ct);
        return prefix + (count + 1 + offset).ToString(new string('0', digits), System.Globalization.CultureInfo.InvariantCulture);
    }
}
