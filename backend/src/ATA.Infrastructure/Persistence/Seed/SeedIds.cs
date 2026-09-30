namespace ATA.Infrastructure.Persistence.Seed;

/// <summary>Stable identifiers for seeded catalog rows so that clients and tests can reference them.</summary>
public static class SeedIds
{
    public static readonly Guid CityRiyadh = Guid.Parse("019985f0-0000-7000-8000-000000000001");

    public static class RideCategories
    {
        public static readonly Guid Saver = Guid.Parse("019985f0-0000-7000-8000-000000000101");
        public static readonly Guid Economy = Guid.Parse("019985f0-0000-7000-8000-000000000102");
        public static readonly Guid Comfort = Guid.Parse("019985f0-0000-7000-8000-000000000103");
        public static readonly Guid Family = Guid.Parse("019985f0-0000-7000-8000-000000000104");
        public static readonly Guid Premium = Guid.Parse("019985f0-0000-7000-8000-000000000105");
        public static readonly Guid Airport = Guid.Parse("019985f0-0000-7000-8000-000000000106");
    }

    public static readonly Guid ZoneRiyadhDefault = Guid.Parse("019985f0-0000-7000-8000-000000000301");
    public static readonly Guid DemandRuleDefault = Guid.Parse("019985f0-0000-7000-8000-000000000501");
    public static readonly Guid MatchingSettingsDefault = Guid.Parse("019985f0-0000-7000-8000-000000000601");

    public static class DemandLevels
    {
        public static readonly Guid Normal = Guid.Parse("019985f0-0000-7000-8000-000000000401");
        public static readonly Guid Moderate = Guid.Parse("019985f0-0000-7000-8000-000000000402");
        public static readonly Guid High = Guid.Parse("019985f0-0000-7000-8000-000000000403");
        public static readonly Guid VeryHigh = Guid.Parse("019985f0-0000-7000-8000-000000000404");
    }

    /// <summary>City-wide pricing rule per seeded ride category (same suffix as the category, in the 07xx range).</summary>
    public static class PricingRules
    {
        public static readonly Guid Saver = Guid.Parse("019985f0-0000-7000-8000-000000000701");
        public static readonly Guid Economy = Guid.Parse("019985f0-0000-7000-8000-000000000702");
        public static readonly Guid Comfort = Guid.Parse("019985f0-0000-7000-8000-000000000703");
        public static readonly Guid Family = Guid.Parse("019985f0-0000-7000-8000-000000000704");
        public static readonly Guid Premium = Guid.Parse("019985f0-0000-7000-8000-000000000705");
        public static readonly Guid Airport = Guid.Parse("019985f0-0000-7000-8000-000000000706");
    }

    public static readonly Guid ScheduledRideRuleDefault = Guid.Parse("019985f0-0000-7000-8000-000000000801");
    public static readonly Guid AirportRuh = Guid.Parse("019985f0-0000-7000-8000-000000000901");

    public static class DocumentTypes
    {
        public static readonly Guid NationalId = Guid.Parse("019985f0-0000-7000-8000-000000000201");
        public static readonly Guid DrivingLicense = Guid.Parse("019985f0-0000-7000-8000-000000000202");
        public static readonly Guid VehicleRegistration = Guid.Parse("019985f0-0000-7000-8000-000000000203");
        public static readonly Guid Insurance = Guid.Parse("019985f0-0000-7000-8000-000000000204");
        public static readonly Guid ProfilePhoto = Guid.Parse("019985f0-0000-7000-8000-000000000205");
    }
}
