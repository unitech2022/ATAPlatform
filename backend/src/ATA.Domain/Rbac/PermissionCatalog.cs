namespace ATA.Domain.Rbac;

/// <summary>One entry of the permission catalogue (doc 12 §F20.2): synced into <c>permissions</c> at startup (read-only there).</summary>
public sealed record PermissionDefinition(string Code, string Module, string NameAr, string NameEn, string? Description, int SortOrder);

/// <summary>A role created (once, when missing) by the startup sync; <see cref="Permissions"/> = <c>["*"]</c> for <c>super_admin</c>.</summary>
public sealed record SystemRoleDefinition(string Code, string NameAr, string NameEn, string Description, IReadOnlyList<string> Permissions);

/// <summary>
/// The single reference for admin permissions (doc 12 §F20.2). Every <c>/admin/*</c> endpoint requires one of these codes (the route-table test enforces it);
/// <c>*</c> grants everything and belongs to the <c>super_admin</c> role only.
/// </summary>
public static class PermissionCatalog
{
    public const string All = "*";

    public const string DashboardView = "dashboard.view";
    public const string DriversView = "drivers.view";
    public const string DriversReview = "drivers.review";
    public const string PassengersView = "passengers.view";
    public const string UsersSuspend = "users.suspend";
    public const string CatalogManage = "catalog.manage";
    public const string TripsView = "trips.view";
    public const string TripsCancel = "trips.cancel";
    public const string LiveView = "live.view";
    public const string PricingView = "pricing.view";
    public const string PricingEdit = "pricing.edit";
    public const string MatchingEdit = "matching.edit";
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
    public const string CancellationManage = "cancellation.manage";
    public const string CancellationReview = "cancellation.review";
    public const string ReliabilityManage = "reliability.manage";
    public const string RatingsManage = "ratings.manage";
    public const string PromotionsManage = "promotions.manage";
    public const string IncentivesManage = "incentives.manage";
    public const string FavoritesManage = "favorites.manage";
    public const string SchedulingManage = "scheduling.manage";
    public const string AirportManage = "airport.manage";
    public const string SupportView = "support.view";
    public const string SupportManage = "support.manage";
    public const string SupportDisputes = "support.disputes";
    public const string HelpManage = "help.manage";
    public const string CorporateManage = "corporate.manage";
    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";
    public const string AdminUsersManage = "admin.users.manage";
    public const string AdminRolesManage = "admin.roles.manage";
    public const string AuditView = "audit.view";

    public const string SuperAdminRole = "super_admin";

    public static readonly IReadOnlyList<PermissionDefinition> Definitions = Build(
    [
        (DashboardView, "dashboard", "عرض لوحة المعلومات", "View dashboard", "/admin/dashboard/summary"),
        (DriversView, "drivers", "عرض السائقين", "View drivers", "Driver lists and details"),
        (DriversReview, "drivers", "مراجعة السائقين", "Review drivers", "Start review, approve, reject, suspend, reinstate and verify documents"),
        (PassengersView, "passengers", "عرض الركاب", "View passengers", "Passenger list"),
        (UsersSuspend, "users", "تعليق المستخدمين", "Suspend users", "Suspend / reinstate users"),
        (CatalogManage, "catalog", "إدارة الكتالوج", "Manage catalog", "Write ride categories and document types"),
        (TripsView, "trips", "عرض الرحلات", "View trips", "Trips, details, matching and the cancellation log"),
        (TripsCancel, "trips", "إلغاء الرحلات", "Cancel trips", "Cancel a trip administratively"),
        (LiveView, "trips", "الخريطة المباشرة", "Live map", "The live operations map"),
        (PricingView, "pricing", "عرض التسعير", "View pricing", "Read zones, pricing, demand and matching settings"),
        (PricingEdit, "pricing", "تعديل التسعير", "Edit pricing", "Write zones, pricing rules, demand rules, overrides and run simulations"),
        (MatchingEdit, "matching", "تعديل المطابقة", "Edit matching", "Write matching settings"),
        (PaymentsView, "payments", "عرض المدفوعات", "View payments", "Payments, refunds, payouts, wallets and the ledger"),
        (PaymentsRefund, "payments", "إنشاء استرداد", "Create refunds", "Create a refund"),
        (PaymentsRefundApprove, "payments", "اعتماد الاستردادات", "Approve refunds", "Approve / reject / retry refunds"),
        (PayoutsApprove, "payments", "اعتماد السحوبات", "Approve payouts", "Approve payouts and payout batches"),
        (SettlementsManage, "payments", "إدارة التسويات", "Manage settlements", "Settlement batches"),
        (WalletsAdjust, "payments", "تعديل المحافظ", "Adjust wallets", "Manual adjustments and wallet freezing"),
        (NotificationsView, "notifications", "عرض الإشعارات", "View notifications", "Catalogue, templates (read) and the delivery log"),
        (NotificationsManage, "notifications", "إدارة الإشعارات", "Manage notifications", "Edit templates, campaigns and resend"),
        (NotificationsSmsBroadcast, "notifications", "حملات الرسائل النصية", "SMS broadcasts", "SMS campaigns"),
        (SafetyManage, "safety", "إدارة السلامة", "Manage safety", "Safety cases, alerts, lost items and duty"),
        (CancellationManage, "cancellation", "إدارة الإلغاء", "Manage cancellation", "Reasons, rules and thresholds"),
        (CancellationReview, "cancellation", "مراجعة الأعذار", "Review excuses", "Review cancellation excuses"),
        (ReliabilityManage, "cancellation", "إدارة الموثوقية", "Manage reliability", "Reliability profiles and manual adjustments"),
        (RatingsManage, "ratings", "إدارة التقييمات", "Manage ratings", "Ratings and flags"),
        (PromotionsManage, "promotions", "إدارة العروض", "Manage promotions", "Promotions"),
        (IncentivesManage, "incentives", "إدارة الحوافز", "Manage incentives", "Incentives and driver tiers"),
        (FavoritesManage, "favorites", "إدارة المفضلة", "Manage favourites", "Favourite driver discount rules"),
        (SchedulingManage, "scheduling", "إدارة الجدولة", "Manage scheduling", "Scheduling rules and scheduled trips"),
        (AirportManage, "airport", "إدارة المطارات", "Manage airports", "Airports and the driver queue"),
        (SupportView, "support", "عرض الدعم", "View support", "Read tickets and disputes"),
        (SupportManage, "support", "إدارة الدعم", "Manage support", "Reply, assign, statuses, canned responses and SLA"),
        (SupportDisputes, "support", "حل النزاعات", "Resolve disputes", "Resolve fare disputes"),
        (HelpManage, "support", "إدارة مركز المساعدة", "Manage help center", "Help articles"),
        (CorporateManage, "corporate", "إدارة الشركات", "Manage corporate", "Corporate accounts and invoices"),
        (ReportsView, "reports", "عرض التقارير", "View reports", "KPIs and statistics"),
        (ReportsExport, "reports", "تصدير التقارير", "Export reports", "CSV exports and snapshot rebuilds"),
        (AdminUsersManage, "admin", "إدارة مستخدمي الإدارة", "Manage admin users", "Admin users"),
        (AdminRolesManage, "admin", "إدارة الأدوار", "Manage roles", "Roles and their permissions"),
        (AuditView, "admin", "سجل التدقيق", "Audit log", "The audit log"),
    ]);

    public static readonly IReadOnlySet<string> Codes = Definitions.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Roles seeded when missing. Doc 12 names only <c>super_admin</c> (<c>*</c>); the other system roles are starting points that admins may re-shape
    /// (their permissions are editable, their code is not, and none can be deleted).
    /// </summary>
    public static readonly IReadOnlyList<SystemRoleDefinition> SystemRoles =
    [
        new(SuperAdminRole, "مدير النظام", "Super admin", "All permissions (*)", [All]),
        new("operations_manager", "مدير العمليات", "Operations manager", "Drivers, trips, live map, safety, cancellation, scheduling and airports",
        [
            DashboardView, DriversView, DriversReview, PassengersView, UsersSuspend, TripsView, TripsCancel, LiveView, PricingView, SafetyManage, CancellationManage,
            CancellationReview, ReliabilityManage, RatingsManage, SchedulingManage, AirportManage, SupportView, NotificationsView, ReportsView,
        ]),
        new("finance", "المالية", "Finance", "Payments, refunds, payouts, settlements, wallets, corporate billing and reports",
        [
            DashboardView, TripsView, PaymentsView, PaymentsRefund, PaymentsRefundApprove, PayoutsApprove, SettlementsManage, WalletsAdjust, CorporateManage, ReportsView, ReportsExport,
        ]),
        new("support_agent", "موظف الدعم", "Support agent", "Tickets, disputes, help center and trip lookups",
        [
            DashboardView, TripsView, PassengersView, DriversView, SupportView, SupportManage, SupportDisputes, HelpManage, CancellationReview, NotificationsView,
        ]),
        new("analyst", "محلل", "Analyst", "Read-only KPIs and exports",
        [
            DashboardView, TripsView, PricingView, ReportsView, ReportsExport,
        ]),
    ];

    private static IReadOnlyList<PermissionDefinition> Build((string Code, string Module, string Ar, string En, string Description)[] items) =>
        items.Select((x, i) => new PermissionDefinition(x.Code, x.Module, x.Ar, x.En, x.Description, (i + 1) * 10)).ToArray();
}
