using ATA.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class DriverStatusLogConfiguration : IEntityTypeConfiguration<DriverStatusLog>
{
    public void Configure(EntityTypeBuilder<DriverStatusLog> b)
    {
        b.ToTable("driver_status_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Latitude).HasPrecision(10, 7);
        b.Property(x => x.Longitude).HasPrecision(10, 7);
        b.HasIndex(x => new { x.DriverId, x.ChangedAt });
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
    }
}
