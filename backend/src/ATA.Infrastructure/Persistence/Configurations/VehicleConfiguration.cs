using ATA.Domain.Catalog;
using ATA.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("vehicles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Make).HasMaxLength(60).IsRequired();
        b.Property(x => x.Model).HasMaxLength(60).IsRequired();
        b.Property(x => x.Color).HasMaxLength(40).IsRequired();
        b.Property(x => x.PlateNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.PlateNumber).IsUnique();
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
