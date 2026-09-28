using ATA.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class RideCategoryConfiguration : IEntityTypeConfiguration<RideCategory>
{
    public void Configure(EntityTypeBuilder<RideCategory> b)
    {
        b.ToTable("ride_categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameAr).HasMaxLength(80).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(80).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(255);
        b.Property(x => x.DescriptionEn).HasMaxLength(255);
        b.Property(x => x.Icon).HasMaxLength(40);
        b.Property(x => x.DriverSharePercent).HasPrecision(5, 2);
    }
}
