using ATA.Domain.Cancellation;
using ATA.Domain.Catalog;
using ATA.Domain.Identity;
using ATA.Domain.Pricing;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class CancellationReasonConfiguration : IEntityTypeConfiguration<CancellationReason>
{
    public void Configure(EntityTypeBuilder<CancellationReason> b)
    {
        b.ToTable("cancellation_reasons");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.Stages).HasColumnType("json");
        b.Ignore(x => x.NeedsReview);
        // The same code (e.g. `other`, `safety_concern`) exists for passengers and drivers.
        b.HasIndex(x => new { x.Actor, x.Code }).IsUnique();
    }
}

public sealed class CancellationRuleConfiguration : IEntityTypeConfiguration<CancellationRule>
{
    public void Configure(EntityTypeBuilder<CancellationRule> b)
    {
        b.ToTable("cancellation_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.FeePercent).HasPrecision(5, 2);
        b.Property(x => x.DriverCompensationPercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.Actor, x.Stage, x.IsActive });
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CancellationEventConfiguration : IEntityTypeConfiguration<CancellationEvent>
{
    public void Configure(EntityTypeBuilder<CancellationEvent> b)
    {
        b.ToTable("cancellation_events");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TripId).IsUnique();
        b.Property(x => x.ReasonCode).HasMaxLength(60).IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.ReviewNote).HasMaxLength(500);
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => x.ExcuseStatus);
        b.HasIndex(x => x.CreatedAt);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<CancellationReason>().WithMany().HasForeignKey(x => x.ReasonId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<CancellationRule>().WithMany().HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ReliabilityProfileConfiguration : IEntityTypeConfiguration<ReliabilityProfile>
{
    public void Configure(EntityTypeBuilder<ReliabilityProfile> b)
    {
        b.ToTable("reliability_profiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.CancellationRate).HasPrecision(5, 4);
        b.Property(x => x.AcceptanceRate).HasPrecision(5, 4);
        b.Property(x => x.ReliabilityRate).HasPrecision(5, 4);
        b.HasIndex(x => new { x.UserId, x.Role }).IsUnique();
        b.HasIndex(x => new { x.Role, x.RestrictionLevel });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReliabilityThresholdConfiguration : IEntityTypeConfiguration<ReliabilityThreshold>
{
    public void Configure(EntityTypeBuilder<ReliabilityThreshold> b)
    {
        b.ToTable("reliability_thresholds");
        b.HasKey(x => x.Id);
        b.Property(x => x.MinCancellationRate).HasPrecision(5, 4);
        b.Property(x => x.DeprioritizeFactor).HasPrecision(4, 2);
        b.Property(x => x.IncentiveReductionPercent).HasPrecision(5, 2);
        b.Property(x => x.MinTripsForRate).HasDefaultValue(10).HasSentinel(-1);
        b.HasIndex(x => new { x.Role, x.Level }).IsUnique();
    }
}

public sealed class ReliabilityAdjustmentConfiguration : IEntityTypeConfiguration<ReliabilityAdjustment>
{
    public void Configure(EntityTypeBuilder<ReliabilityAdjustment> b)
    {
        b.ToTable("reliability_adjustments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.HasIndex(x => new { x.UserId, x.Role, x.CreatedAt });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
