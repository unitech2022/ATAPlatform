namespace ATA.Api.Modules.Reporting;

/// <param name="Dimensional">False for metrics with no pickup city / zone / category (online hours, incentives, support): computed for the <c>all</c> scope only.</param>
/// <param name="Group"><c>core</c> or <c>v1.1</c> (the dashboard shows the v1.1 metrics in their own section).</param>
public sealed record KpiDefinition(string Code, string NameAr, string NameEn, KpiUnit Unit, KpiAggregation Aggregation, bool Dimensional = true, string Group = KpiCodes.CoreGroup);

/// <summary>The unified KPI definitions of doc 12 §F20.6 (codes, units, aggregation).</summary>
public static class KpiCodes
{
    public const string CoreGroup = "core";
    public const string V11Group = "v1.1";

    public const string CompletedTrips = "completed_trips";
    public const string RequestedTrips = "requested_trips";
    public const string CompletionRate = "completion_rate";
    public const string TripsPerActiveRider = "trips_per_active_rider";
    public const string AverageEta = "average_eta";
    public const string AverageTimeToAssign = "average_time_to_assign";
    public const string NoDriversRate = "no_drivers_rate";
    public const string DriverAcceptanceRate = "driver_acceptance_rate";
    public const string DriverCancellationRate = "driver_cancellation_rate";
    public const string PassengerCancellationRate = "passenger_cancellation_rate";
    public const string AverageFare = "average_fare";
    public const string DriverEarningsPerOnlineHour = "driver_earnings_per_online_hour";
    public const string OnlineHours = "online_hours";
    public const string Gmv = "gmv";
    public const string PlatformRevenue = "platform_revenue";
    public const string TakeRate = "take_rate";
    public const string IncentivesPaid = "incentives_paid";
    public const string RefundsAmount = "refunds_amount";
    public const string ActiveRiders = "active_riders";
    public const string ActiveDrivers = "active_drivers";
    public const string NewRiders = "new_riders";
    public const string RepeatRate = "repeat_rate";
    public const string CustomerRating = "customer_rating";
    public const string SupportResolutionTime = "support_resolution_time";
    public const string CancellationFeeRevenue = "cancellation_fee_revenue";
    public const string RepeatCancellationRate = "repeat_cancellation_rate";
    public const string DriverReliabilityRate = "driver_reliability_rate";
    public const string PassengerReliabilityRate = "passenger_reliability_rate";
    public const string FavoriteDriverBookingRate = "favorite_driver_booking_rate";
    public const string FavoriteDriverDiscountUsage = "favorite_driver_discount_usage";
    public const string ScheduledRideCompletionRate = "scheduled_ride_completion_rate";
    public const string ScheduledRideCancellationRate = "scheduled_ride_cancellation_rate";

    public static readonly IReadOnlyList<KpiDefinition> All =
    [
        new(CompletedTrips, "الرحلات المكتملة", "Completed Trips", KpiUnit.Count, KpiAggregation.Sum),
        new(RequestedTrips, "الرحلات المطلوبة", "Requested Trips", KpiUnit.Count, KpiAggregation.Sum),
        new(CompletionRate, "نسبة الإكمال", "Completion Rate", KpiUnit.Percent, KpiAggregation.Ratio),
        new(TripsPerActiveRider, "الرحلات لكل راكب نشط", "Trips per Active Rider", KpiUnit.Ratio, KpiAggregation.Distinct),
        new(AverageEta, "متوسط وقت الوصول", "Average ETA", KpiUnit.Seconds, KpiAggregation.Avg),
        new(AverageTimeToAssign, "متوسط وقت الإسناد", "Time to Assign", KpiUnit.Seconds, KpiAggregation.Avg),
        new(NoDriversRate, "نسبة عدم توفر سائقين", "No Drivers Rate", KpiUnit.Percent, KpiAggregation.Ratio),
        new(DriverAcceptanceRate, "نسبة قبول السائقين", "Driver Acceptance Rate", KpiUnit.Percent, KpiAggregation.Ratio),
        new(DriverCancellationRate, "نسبة إلغاء السائقين", "Driver Cancellation Rate", KpiUnit.Percent, KpiAggregation.Ratio),
        new(PassengerCancellationRate, "نسبة إلغاء الركاب", "Passenger Cancellation Rate", KpiUnit.Percent, KpiAggregation.Ratio),
        new(AverageFare, "متوسط الأجرة", "Average Fare", KpiUnit.Sar, KpiAggregation.Ratio),
        new(DriverEarningsPerOnlineHour, "أرباح السائق لكل ساعة اتصال", "Driver Earnings per Online Hour", KpiUnit.Sar, KpiAggregation.Ratio, Dimensional: false),
        new(OnlineHours, "ساعات الاتصال", "Online Hours", KpiUnit.Hours, KpiAggregation.Sum, Dimensional: false),
        new(Gmv, "إجمالي قيمة الرحلات", "GMV", KpiUnit.Sar, KpiAggregation.Sum),
        new(PlatformRevenue, "إيرادات المنصة", "Platform Revenue", KpiUnit.Sar, KpiAggregation.Sum),
        new(TakeRate, "نسبة الاستقطاع", "Take Rate", KpiUnit.Percent, KpiAggregation.Ratio),
        new(IncentivesPaid, "الحوافز المصروفة", "Incentives Paid", KpiUnit.Sar, KpiAggregation.Sum, Dimensional: false),
        new(RefundsAmount, "الاستردادات", "Refunds", KpiUnit.Sar, KpiAggregation.Sum),
        new(ActiveRiders, "الركاب النشطون", "Active Riders", KpiUnit.Count, KpiAggregation.Distinct),
        new(ActiveDrivers, "السائقون النشطون", "Active Drivers", KpiUnit.Count, KpiAggregation.Distinct),
        new(NewRiders, "الركاب الجدد", "New Riders", KpiUnit.Count, KpiAggregation.Sum),
        new(RepeatRate, "نسبة التكرار", "Repeat Rate", KpiUnit.Percent, KpiAggregation.Distinct),
        new(CustomerRating, "تقييم العملاء", "Customer Rating", KpiUnit.Rating, KpiAggregation.Avg),
        new(SupportResolutionTime, "زمن حل تذاكر الدعم", "Support Resolution Time", KpiUnit.Hours, KpiAggregation.Avg, Dimensional: false),
        new(CancellationFeeRevenue, "إيرادات رسوم الإلغاء", "Cancellation Fee Revenue", KpiUnit.Sar, KpiAggregation.Sum, Group: V11Group),
        new(RepeatCancellationRate, "نسبة الإلغاء المتكرر", "Repeat Cancellation Rate", KpiUnit.Percent, KpiAggregation.Distinct, Group: V11Group),
        new(DriverReliabilityRate, "نسبة موثوقية السائقين", "Driver Reliability Rate", KpiUnit.Percent, KpiAggregation.Ratio, Group: V11Group),
        new(PassengerReliabilityRate, "نسبة موثوقية الركاب", "Passenger Reliability Rate", KpiUnit.Percent, KpiAggregation.Ratio, Group: V11Group),
        new(FavoriteDriverBookingRate, "نسبة حجوزات السائق المفضل", "Favorite Driver Booking Rate", KpiUnit.Percent, KpiAggregation.Ratio, Group: V11Group),
        new(FavoriteDriverDiscountUsage, "استخدام خصم السائق المفضل", "Favorite Driver Discount Usage", KpiUnit.Sar, KpiAggregation.Sum, Group: V11Group),
        new(ScheduledRideCompletionRate, "نسبة إكمال الرحلات المجدولة", "Scheduled Ride Completion Rate", KpiUnit.Percent, KpiAggregation.Ratio, Group: V11Group),
        new(ScheduledRideCancellationRate, "نسبة إلغاء الرحلات المجدولة", "Scheduled Ride Cancellation Rate", KpiUnit.Percent, KpiAggregation.Ratio, Group: V11Group),
    ];

    public static readonly IReadOnlyDictionary<string, KpiDefinition> ByCode = All.ToDictionary(d => d.Code, StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> AllCodes = All.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> DistinctCodes = All.Where(d => d.Aggregation == KpiAggregation.Distinct).Select(d => d.Code).ToHashSet(StringComparer.Ordinal);
}

/// <summary>
/// Value of a metric from its accumulated parts: <c>sum</c> → the value; <c>ratio</c>/<c>avg</c>/distinct ratios → numerator ÷ denominator (a
/// <c>percent</c> is a share 0..1, e.g. 0.875); null when the denominator is 0 (no data).
/// </summary>
public static class KpiMath
{
    public static decimal? Ratio(KpiDefinition definition, decimal numerator, decimal denominator)
    {
        if (denominator == 0)
        {
            return null;
        }

        return Round(numerator / denominator);
    }

    public static decimal Round(decimal value) => decimal.Round(value, 4, MidpointRounding.AwayFromZero);

    /// <summary>Rounded for display: counts as integers, percent shares to 4 decimals, everything else to 2 decimals.</summary>
    public static decimal? Display(KpiDefinition definition, decimal? value) =>
        value is null ? null : decimal.Round(value.Value, definition.Unit switch { KpiUnit.Count => 0, KpiUnit.Percent => 4, _ => 2 }, MidpointRounding.AwayFromZero);

    /// <summary>(value − previous) ÷ previous × 100, one decimal; null without a previous value (or when it is 0).</summary>
    public static decimal? ChangePercent(decimal? value, decimal? previous) =>
        value is null || previous is null || previous == 0 ? null : decimal.Round((value.Value - previous.Value) / Math.Abs(previous.Value) * 100m, 1, MidpointRounding.AwayFromZero);
}
