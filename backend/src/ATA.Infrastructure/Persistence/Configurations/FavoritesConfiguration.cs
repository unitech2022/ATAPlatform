using ATA.Domain.Drivers;
using ATA.Domain.Favorites;
using ATA.Domain.Identity;
using ATA.Domain.Passengers;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

// ----- F16 favourite drivers (doc 10 §F16.1) -----

public sealed class FavoriteDriverConfiguration : IEntityTypeConfiguration<FavoriteDriver>
{
    public void Configure(EntityTypeBuilder<FavoriteDriver> b)
    {
        b.ToTable("favorite_drivers");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PassengerId, x.DriverId }).IsUnique();
        b.HasIndex(x => x.DriverId);
        b.HasOne<PassengerProfile>().WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.SourceTripId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FavoriteDriverDiscountRuleConfiguration : IEntityTypeConfiguration<FavoriteDriverDiscountRule>
{
    public void Configure(EntityTypeBuilder<FavoriteDriverDiscountRule> b)
    {
        b.ToTable("favorite_driver_discount_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(FavoriteDriverDiscountRule.NameMaxLength).IsRequired();
        b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.Property(x => x.RideCategoryIds).HasColumnType("json");
        b.Property(x => x.ZoneIds).HasColumnType("json");
        b.Property(x => x.BookingTypes).HasColumnType("json");
        b.HasIndex(x => new { x.IsActive, x.Priority });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
    }
}
