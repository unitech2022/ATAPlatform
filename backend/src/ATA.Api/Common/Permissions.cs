using Microsoft.EntityFrameworkCore;
using ATA.Domain.Common;
using ATA.Domain.Rbac;

namespace ATA.Api.Common;

/// <summary>
/// Admin permissions (doc 12 §F20.2 — the catalogue itself is <see cref="PermissionCatalog"/>). <see cref="RequirePermission{TBuilder}"/> attaches a
/// <see cref="RequiredPermissionMetadata"/> that <see cref="AdminAccessMiddleware"/> checks against the JWT <c>perm</c> claims before the endpoint binds its
/// parameters (<c>403 forbidden { permission }</c>); <c>*</c> grants everything.
/// </summary>
public static class Permissions
{
    public const string DashboardView = PermissionCatalog.DashboardView;
    public const string DriversView = PermissionCatalog.DriversView;
    public const string DriversReview = PermissionCatalog.DriversReview;
    public const string PassengersView = PermissionCatalog.PassengersView;
    public const string UsersSuspend = PermissionCatalog.UsersSuspend;
    public const string CatalogManage = PermissionCatalog.CatalogManage;
    public const string TripsView = PermissionCatalog.TripsView;
    public const string TripsCancel = PermissionCatalog.TripsCancel;
    public const string LiveView = PermissionCatalog.LiveView;
    public const string PricingView = PermissionCatalog.PricingView;
    public const string PricingEdit = PermissionCatalog.PricingEdit;
    public const string MatchingEdit = PermissionCatalog.MatchingEdit;
    public const string PaymentsView = PermissionCatalog.PaymentsView;
    public const string PaymentsRefund = PermissionCatalog.PaymentsRefund;
    public const string PaymentsRefundApprove = PermissionCatalog.PaymentsRefundApprove;
    public const string PayoutsApprove = PermissionCatalog.PayoutsApprove;
    public const string SettlementsManage = PermissionCatalog.SettlementsManage;
    public const string WalletsAdjust = PermissionCatalog.WalletsAdjust;
    public const string NotificationsView = PermissionCatalog.NotificationsView;
    public const string NotificationsManage = PermissionCatalog.NotificationsManage;
    public const string NotificationsSmsBroadcast = PermissionCatalog.NotificationsSmsBroadcast;
    public const string SafetyManage = PermissionCatalog.SafetyManage;
    public const string SupportView = PermissionCatalog.SupportView;
    public const string SupportManage = PermissionCatalog.SupportManage;
    public const string SupportDisputes = PermissionCatalog.SupportDisputes;
    public const string HelpManage = PermissionCatalog.HelpManage;
    public const string CancellationManage = PermissionCatalog.CancellationManage;
    public const string CancellationReview = PermissionCatalog.CancellationReview;
    public const string ReliabilityManage = PermissionCatalog.ReliabilityManage;
    public const string ReportsView = PermissionCatalog.ReportsView;
    public const string ReportsExport = PermissionCatalog.ReportsExport;
    public const string RatingsManage = PermissionCatalog.RatingsManage;
    public const string PromotionsManage = PermissionCatalog.PromotionsManage;
    public const string IncentivesManage = PermissionCatalog.IncentivesManage;
    public const string FavoritesManage = PermissionCatalog.FavoritesManage;
    public const string SchedulingManage = PermissionCatalog.SchedulingManage;
    public const string AirportManage = PermissionCatalog.AirportManage;
    public const string CorporateManage = PermissionCatalog.CorporateManage;
    public const string AdminUsersManage = PermissionCatalog.AdminUsersManage;
    public const string AdminRolesManage = PermissionCatalog.AdminRolesManage;
    public const string AuditView = PermissionCatalog.AuditView;

    /// <summary>Rejects the request with <c>403 forbidden { permissions }</c> unless the caller holds at least one of <paramref name="permissions"/> (or <c>*</c>).</summary>
    public static TBuilder RequireAnyPermission<TBuilder>(this TBuilder builder, params string[] permissions) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new RequiredPermissionMetadata(permissions));

    /// <summary>Rejects the request with <c>403 forbidden { permission }</c> unless the caller's JWT carries <paramref name="permission"/> (or <c>*</c>).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new RequiredPermissionMetadata([permission]));

    /// <summary>Marks an <c>/admin/*</c> endpoint open to every admin (no catalogue permission): <c>/admin/me*</c>, reading ride categories.</summary>
    public static TBuilder AllowAnyAdmin<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(AnyAdminMetadata.Instance);

    /// <summary>The only admin endpoint reachable while a temporary password must be changed (<c>POST /admin/me/password</c>).</summary>
    public static TBuilder AllowDuringPasswordChange<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(PasswordChangeEndpointMetadata.Instance);
}

/// <summary>Endpoint metadata: the caller needs one of <see cref="AnyOf"/>; several metadata entries (group + endpoint) must all be satisfied.</summary>
public sealed class RequiredPermissionMetadata(IReadOnlyList<string> anyOf)
{
    public IReadOnlyList<string> AnyOf { get; } = anyOf;
}

public sealed class AnyAdminMetadata
{
    public static readonly AnyAdminMetadata Instance = new();
}

public sealed class PasswordChangeEndpointMetadata
{
    public static readonly PasswordChangeEndpointMetadata Instance = new();
}

/// <summary>
/// Runs after authorization (the <c>Admin</c> policy) and before parameter binding: enforces <see cref="RequiredPermissionMetadata"/> and, for admin tokens
/// carrying the <c>pwdc</c> claim, the temporary-password gate (<c>403 password_change_required</c> while <c>admin_accounts.must_change_password</c> is still set).
/// </summary>
public sealed class AdminAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser user)
    {
        var endpoint = context.GetEndpoint();
        var requirements = endpoint?.Metadata.GetOrderedMetadata<RequiredPermissionMetadata>() ?? [];
        var isAdminEndpoint = requirements.Count > 0 || endpoint?.Metadata.GetMetadata<AnyAdminMetadata>() is not null;
        if (isAdminEndpoint && context.User.Identity?.IsAuthenticated == true)
        {
            if (context.User.HasClaim(ATA.Infrastructure.Security.AtaClaims.PasswordChangeRequired, "true")
                && endpoint!.Metadata.GetMetadata<PasswordChangeEndpointMetadata>() is null)
            {
                var db = context.RequestServices.GetRequiredService<ATA.Infrastructure.Persistence.AtaDbContext>();
                var userId = user.UserId;
                if (await db.AdminAccounts.AsNoTracking().AnyAsync(a => a.UserId == userId && a.MustChangePassword, context.RequestAborted))
                {
                    await context.WriteErrorAsync(ErrorCodes.PasswordChangeRequired, context.GetLanguage());
                    return;
                }
            }

            foreach (var requirement in requirements)
            {
                if (!requirement.AnyOf.Any(user.HasPermission))
                {
                    object details = requirement.AnyOf.Count == 1 ? new { permission = requirement.AnyOf[0] } : new { permissions = requirement.AnyOf };
                    await context.WriteErrorAsync(ErrorCodes.Forbidden, context.GetLanguage(), details);
                    return;
                }
            }
        }

        await next(context);
    }
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
