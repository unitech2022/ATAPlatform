using ATA.Domain.Catalog;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
{
    public void Configure(EntityTypeBuilder<DriverProfile> b)
    {
        b.ToTable("drivers");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.ApplicationNumber).HasMaxLength(16).IsRequired();
        b.HasIndex(x => x.ApplicationNumber).IsUnique();
        b.HasIndex(x => x.ApplicationStatus);
        b.Property(x => x.RejectionReason).HasMaxLength(1000);
        b.Property(x => x.NationalId).HasMaxLength(20);
        b.Property(x => x.Iban).HasMaxLength(34);
        b.Property(x => x.RatingAvg).HasPrecision(3, 2);
        b.HasOne<User>().WithOne().HasForeignKey<DriverProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Vehicles).WithOne().HasForeignKey(v => v.DriverId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Documents).WithOne().HasForeignKey(d => d.DriverId).OnDelete(DeleteBehavior.Cascade);
    }
}
