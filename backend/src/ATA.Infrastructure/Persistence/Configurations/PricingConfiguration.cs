using ATA.Domain.Catalog;
using ATA.Domain.Passengers;
using ATA.Domain.Pricing;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> b)
    {
        b.ToTable("zones");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameAr).HasMaxLength(80).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(80).IsRequired();
        b.Property(x => x.Polygon).HasColumnType("json").IsRequired();
        b.Property(x => x.OperatingHours).HasColumnType("json");
        b.Property(x => x.CenterLat).HasPrecision(10, 7);
        b.Property(x => x.CenterLng).HasPrecision(10, 7);
        b.HasIndex(x => new { x.CityId, x.IsActive });
        b.Ignore(x => x.IsCityDefault);
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.CategorySettings).WithOne().HasForeignKey(s => s.ZoneId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ZoneCategorySettingConfiguration : IEntityTypeConfiguration<ZoneCategorySetting>
{
    public void Configure(EntityTypeBuilder<ZoneCategorySetting> b)
    {
        b.ToTable("zone_category_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.SurgeCap).HasPrecision(4, 2);
        b.HasIndex(x => new { x.ZoneId, x.RideCategoryId }).IsUnique();
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> b)
    {
        b.ToTable("pricing_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.ServiceFeePercent).HasPrecision(5, 2);
        b.Property(x => x.DriverSharePercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.RideCategoryId, x.ZoneId, x.IsActive });
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.TimeMultipliers).WithOne().HasForeignKey(m => m.PricingRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PricingTimeMultiplierConfiguration : IEntityTypeConfiguration<PricingTimeMultiplier>
{
    public void Configure(EntityTypeBuilder<PricingTimeMultiplier> b)
    {
        b.ToTable("pricing_time_multipliers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Multiplier).HasPrecision(4, 2);
        b.Property(x => x.Label).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.PricingRuleId);
    }
}

public sealed class DemandLevelConfiguration : IEntityTypeConfiguration<DemandLevel>
{
    public void Configure(EntityTypeBuilder<DemandLevel> b)
    {
        b.ToTable("demand_levels");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameAr).HasMaxLength(80).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(80).IsRequired();
        b.Property(x => x.Multiplier).HasPrecision(4, 2);
        b.Property(x => x.Color).HasMaxLength(16).IsRequired();
    }
}

public sealed class DemandRuleConfiguration : IEntityTypeConfiguration<DemandRule>
{
    public void Configure(EntityTypeBuilder<DemandRule> b)
    {
        b.ToTable("demand_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Metric).HasMaxLength(40).IsRequired();
        b.Property(x => x.ThresholdModerate).HasPrecision(6, 2);
        b.Property(x => x.ThresholdHigh).HasPrecision(6, 2);
        b.Property(x => x.ThresholdVeryHigh).HasPrecision(6, 2);
        b.HasIndex(x => new { x.ZoneId, x.RideCategoryId, x.IsActive });
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DemandOverrideConfiguration : IEntityTypeConfiguration<DemandOverride>
{
    public void Configure(EntityTypeBuilder<DemandOverride> b)
    {
        b.ToTable("demand_overrides");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(255).IsRequired();
        b.HasIndex(x => new { x.ZoneId, x.StartsAt, x.EndsAt });
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DemandLevel>().WithMany().HasForeignKey(x => x.DemandLevelId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DemandSnapshotConfiguration : IEntityTypeConfiguration<DemandSnapshot>
{
    public void Configure(EntityTypeBuilder<DemandSnapshot> b)
    {
        b.ToTable("demand_snapshots");
        b.HasKey(x => x.Id);
        b.Property(x => x.Ratio).HasPrecision(8, 3);
        b.Property(x => x.DemandLevelCode).HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.ZoneId, x.ComputedAt });
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FareQuoteConfiguration : IEntityTypeConfiguration<FareQuote>
{
    public void Configure(EntityTypeBuilder<FareQuote> b)
    {
        b.ToTable("fare_quotes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Breakdown).HasColumnType("json").IsRequired();
        b.Property(x => x.DemandLevelCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.DriverSharePercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.PassengerId, x.CreatedAt });
        b.HasIndex(x => x.GroupId);
        b.HasOne<PassengerProfile>().WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.PickupZoneId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.DropoffZoneId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.UsedTripId).OnDelete(DeleteBehavior.SetNull);
    }
}
