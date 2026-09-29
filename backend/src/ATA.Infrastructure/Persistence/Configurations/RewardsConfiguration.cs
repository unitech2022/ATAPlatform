using ATA.Domain.Catalog;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Incentives;
using ATA.Domain.Passengers;
using ATA.Domain.Promotions;
using ATA.Domain.Ratings;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

// ----- F15 ratings (doc 10 §F15.1) -----

public sealed class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.ToTable("ratings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tags).HasColumnType("json").IsRequired();
        b.Property(x => x.Comment).HasMaxLength(500);
        b.Property(x => x.HiddenReason).HasMaxLength(500);
        b.HasIndex(x => new { x.TripId, x.RaterRole }).IsUnique();
        b.HasIndex(x => new { x.RateeUserId, x.RateeRole, x.CreatedAt });
        b.HasIndex(x => new { x.RaterUserId, x.CreatedAt });
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RaterUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RateeUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.HiddenBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RatingTagConfiguration : IEntityTypeConfiguration<RatingTag>
{
    public void Configure(EntityTypeBuilder<RatingTag> b)
    {
        b.ToTable("rating_tags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(80).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(80).IsRequired();
        // `behaviour` and `cleanliness` exist for both targets, so codes are unique per target role.
        b.HasIndex(x => new { x.TargetRole, x.Code }).IsUnique();
    }
}

public sealed class RatingFlagConfiguration : IEntityTypeConfiguration<RatingFlag>
{
    public void Configure(EntityTypeBuilder<RatingFlag> b)
    {
        b.ToTable("rating_flags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Value).HasPrecision(4, 2);
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => new { x.UserId, x.Type, x.Status });
        b.HasIndex(x => x.RatingId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Rating>().WithMany().HasForeignKey(x => x.RatingId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

// ----- F15 promotions (doc 10 §F15.4) -----

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> b)
    {
        b.ToTable("promotions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Promotion.CodeMaxLength).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameAr).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(500);
        b.Property(x => x.DescriptionEn).HasMaxLength(500);
        b.Property(x => x.PerUserLimit).HasDefaultValue(1).HasSentinel(-1);
        b.Property(x => x.NewUserDays).HasDefaultValue(30).HasSentinel(-1);
        b.Property(x => x.RideCategoryIds).HasColumnType("json");
        b.Property(x => x.ZoneIds).HasColumnType("json");
        b.Property(x => x.PaymentMethods).HasColumnType("json");
        b.Property(x => x.BookingTypes).HasColumnType("json");
        b.HasIndex(x => new { x.IsActive, x.ValidFrom, x.ValidTo });
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class PromotionRedemptionConfiguration : IEntityTypeConfiguration<PromotionRedemption>
{
    public void Configure(EntityTypeBuilder<PromotionRedemption> b)
    {
        b.ToTable("promotion_redemptions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TripId).IsUnique();
        b.HasIndex(x => new { x.PromotionId, x.Status });
        b.HasIndex(x => new { x.PassengerId, x.PromotionId });
        b.HasOne<Promotion>().WithMany().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PassengerProfile>().WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
    }
}

// ----- F15 driver tiers and incentives (doc 10 §F15.7, §F15.8) -----

public sealed class DriverTierRuleConfiguration : IEntityTypeConfiguration<DriverTierRule>
{
    public void Configure(EntityTypeBuilder<DriverTierRule> b)
    {
        b.ToTable("driver_tier_rules");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Tier).IsUnique();
        b.Property(x => x.MinRatingAvg).HasPrecision(3, 2);
        b.Property(x => x.MinAcceptanceRate).HasPrecision(5, 4);
        b.Property(x => x.MaxCancellationRate).HasPrecision(5, 4);
        b.Property(x => x.CommissionDiscountPercent).HasPrecision(5, 2);
        b.Property(x => x.MatchingNorm).HasPrecision(3, 2);
        b.Property(x => x.BenefitsAr).HasMaxLength(500);
        b.Property(x => x.BenefitsEn).HasMaxLength(500);
    }
}

public sealed class DriverTierHistoryConfiguration : IEntityTypeConfiguration<DriverTierHistory>
{
    public void Configure(EntityTypeBuilder<DriverTierHistory> b)
    {
        b.ToTable("driver_tier_history");
        b.HasKey(x => x.Id);
        b.Property(x => x.Metrics).HasColumnType("json");
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => new { x.DriverId, x.ComputedAt });
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class DriverIncentiveConfiguration : IEntityTypeConfiguration<DriverIncentive>
{
    public void Configure(EntityTypeBuilder<DriverIncentive> b)
    {
        b.ToTable("driver_incentives");
        b.HasKey(x => x.Id);
        b.Property(x => x.NameAr).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(500);
        b.Property(x => x.DescriptionEn).HasMaxLength(500);
        b.Property(x => x.ZoneIds).HasColumnType("json");
        b.Property(x => x.RideCategoryIds).HasColumnType("json");
        b.Property(x => x.DaysOfWeek).HasColumnType("json");
        b.Property(x => x.MinRating).HasPrecision(3, 2);
        b.HasIndex(x => new { x.CityId, x.IsActive, x.StartsAt, x.EndsAt });
        b.Ignore(x => x.IsRecurring);
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class DriverIncentiveProgressConfiguration : IEntityTypeConfiguration<DriverIncentiveProgress>
{
    public void Configure(EntityTypeBuilder<DriverIncentiveProgress> b)
    {
        b.ToTable("driver_incentive_progress");
        b.HasKey(x => x.Id);
        b.Property(x => x.IncentiveMultiplier).HasPrecision(4, 2);
        b.Property(x => x.VoidedReason).HasMaxLength(500);
        b.HasIndex(x => new { x.IncentiveId, x.DriverId, x.PeriodStart }).IsUnique();
        b.HasIndex(x => new { x.Status, x.PeriodEnd });
        b.HasIndex(x => new { x.DriverId, x.Status });
        b.Ignore(x => x.IsOpen);
        b.HasOne<DriverIncentive>().WithMany().HasForeignKey(x => x.IncentiveId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DriverIncentiveTripConfiguration : IEntityTypeConfiguration<DriverIncentiveTrip>
{
    public void Configure(EntityTypeBuilder<DriverIncentiveTrip> b)
    {
        b.ToTable("driver_incentive_trips");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ProgressId, x.TripId }).IsUnique();
        b.HasIndex(x => x.TripId);
        b.HasOne<DriverIncentiveProgress>().WithMany().HasForeignKey(x => x.ProgressId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
    }
}
