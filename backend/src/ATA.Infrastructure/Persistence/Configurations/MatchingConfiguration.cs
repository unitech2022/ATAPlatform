using ATA.Domain.Catalog;
using ATA.Domain.Drivers;
using ATA.Domain.Matching;
using ATA.Domain.Pricing;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class MatchingSettingsConfiguration : IEntityTypeConfiguration<MatchingSettings>
{
    public void Configure(EntityTypeBuilder<MatchingSettings> b)
    {
        b.ToTable("matching_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Weights).HasColumnType("json").IsRequired();
        b.HasIndex(x => new { x.ZoneId, x.RideCategoryId }).IsUnique();
        b.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MatchingAttemptConfiguration : IEntityTypeConfiguration<MatchingAttempt>
{
    public void Configure(EntityTypeBuilder<MatchingAttempt> b)
    {
        b.ToTable("matching_attempts");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.TripId, x.Round });
        b.Property(x => x.Mode).HasDefaultValue(MatchingMode.Normal);
        b.Ignore(x => x.IsOpen);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Candidates).WithOne().HasForeignKey(c => c.AttemptId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MatchingCandidateConfiguration : IEntityTypeConfiguration<MatchingCandidate>
{
    public void Configure(EntityTypeBuilder<MatchingCandidate> b)
    {
        b.ToTable("matching_candidates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Score).HasPrecision(6, 4);
        b.HasIndex(x => new { x.AttemptId, x.Rank });
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
    }
}
