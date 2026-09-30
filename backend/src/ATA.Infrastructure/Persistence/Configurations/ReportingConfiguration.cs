using ATA.Domain.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

// ----- F20 KPI snapshots (doc 12 §F20.1 / §F20.6) -----

public sealed class ReportSnapshotConfiguration : IEntityTypeConfiguration<ReportSnapshot>
{
    public void Configure(EntityTypeBuilder<ReportSnapshot> b)
    {
        b.ToTable("report_snapshots");
        b.HasKey(x => x.Id);
        b.Property(x => x.ScopeKey).HasMaxLength(160).IsRequired();
        b.Property(x => x.MetricCode).HasMaxLength(60).IsRequired();
        b.Property(x => x.Value).HasPrecision(18, 4);
        b.Property(x => x.Numerator).HasPrecision(18, 4);
        b.Property(x => x.Denominator).HasPrecision(18, 4);
        b.HasIndex(x => new { x.SnapshotDate, x.ScopeKey, x.MetricCode }).IsUnique();
        b.HasIndex(x => new { x.MetricCode, x.SnapshotDate });
    }
}
