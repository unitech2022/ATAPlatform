using ATA.Domain.Catalog;
using ATA.Domain.Drivers;
using ATA.Domain.Passengers;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> b)
    {
        b.ToTable("trips");
        b.HasKey(x => x.Id);
        b.Property(x => x.TripNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.TripNumber).IsUnique();
        b.Property(x => x.PickupName).HasMaxLength(120).IsRequired();
        b.Property(x => x.PickupAddress).HasMaxLength(500).IsRequired();
        b.Property(x => x.PickupLat).HasPrecision(10, 7);
        b.Property(x => x.PickupLng).HasPrecision(10, 7);
        b.Property(x => x.DropoffName).HasMaxLength(120).IsRequired();
        b.Property(x => x.DropoffAddress).HasMaxLength(500).IsRequired();
        b.Property(x => x.DropoffLat).HasPrecision(10, 7);
        b.Property(x => x.DropoffLng).HasPrecision(10, 7);
        b.Property(x => x.PinCodeHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.PinCodeProtected).HasMaxLength(255).IsRequired();
        b.Property(x => x.CancellationReason).HasMaxLength(500);
        b.Property(x => x.RiderNote).HasMaxLength(500);
        b.Property(x => x.FareBreakdown).HasColumnType("json");
        b.Property(x => x.PlannedRoute).HasColumnType("json");
        b.Property(x => x.TierCommissionDiscountPercent).HasPrecision(5, 2);
        b.Property(x => x.PlannedRouteSource).HasDefaultValue(ATA.Domain.Safety.PlannedRouteSource.Straight).HasSentinel((ATA.Domain.Safety.PlannedRouteSource)(-1));
        b.HasIndex(x => new { x.PassengerId, x.CreatedAt });
        b.HasIndex(x => new { x.DriverId, x.CreatedAt });
        b.HasIndex(x => x.Status);
        b.Ignore(x => x.IsTerminal);
        b.Ignore(x => x.HasDriver);
        b.Ignore(x => x.CanBeCancelled);
        b.HasOne<PassengerProfile>().WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Stops).WithOne().HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(e => e.TripId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripStopConfiguration : IEntityTypeConfiguration<TripStop>
{
    public void Configure(EntityTypeBuilder<TripStop> b)
    {
        b.ToTable("trip_stops");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500).IsRequired();
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.HasIndex(x => new { x.TripId, x.Sequence }).IsUnique();
    }
}

public sealed class TripOfferConfiguration : IEntityTypeConfiguration<TripOffer>
{
    public void Configure(EntityTypeBuilder<TripOffer> b)
    {
        b.ToTable("trip_offers");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.DriverId, x.Status });
        b.HasIndex(x => new { x.TripId, x.Status });
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripEventConfiguration : IEntityTypeConfiguration<TripEvent>
{
    public void Configure(EntityTypeBuilder<TripEvent> b)
    {
        b.ToTable("trip_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(40).IsRequired();
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.Property(x => x.Data).HasColumnType("json");
        b.HasIndex(x => new { x.TripId, x.CreatedAt });
    }
}

public sealed class DriverLocationConfiguration : IEntityTypeConfiguration<DriverLocation>
{
    public void Configure(EntityTypeBuilder<DriverLocation> b)
    {
        b.ToTable("driver_locations");
        b.HasKey(x => x.DriverId);
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.Property(x => x.Heading).HasPrecision(6, 2);
        b.Property(x => x.Speed).HasPrecision(8, 2);
        b.Property(x => x.Accuracy).HasPrecision(8, 2);
        b.HasIndex(x => new { x.IsOnline, x.UpdatedAt });
        b.HasOne<DriverProfile>().WithOne().HasForeignKey<DriverLocation>(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DriverLocationHistoryConfiguration : IEntityTypeConfiguration<DriverLocationHistory>
{
    public void Configure(EntityTypeBuilder<DriverLocationHistory> b)
    {
        b.ToTable("driver_location_history");
        b.HasKey(x => x.Id);
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.HasIndex(x => new { x.TripId, x.RecordedAt });
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.SetNull);
    }
}
