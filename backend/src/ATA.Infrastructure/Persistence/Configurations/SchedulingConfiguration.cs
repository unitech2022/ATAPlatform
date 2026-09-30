using ATA.Domain.Airports;
using ATA.Domain.Catalog;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

// ----- F17 scheduled rides (doc 11 §F17.2) -----

public sealed class ScheduledRideRuleConfiguration : IEntityTypeConfiguration<ScheduledRideRule>
{
    public void Configure(EntityTypeBuilder<ScheduledRideRule> b)
    {
        b.ToTable("scheduled_ride_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.RiderReminderOffsets).HasColumnType("json").IsRequired();
        b.Property(x => x.DriverReminderOffsets).HasColumnType("json").IsRequired();
        b.Property(x => x.LateCancelFeePercent).HasPrecision(5, 2);
        b.Property(x => x.LateCancelDriverCompensationPercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.CityId, x.RideCategoryId }).IsUnique();
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ScheduledRideReservationConfiguration : IEntityTypeConfiguration<ScheduledRideReservation>
{
    public void Configure(EntityTypeBuilder<ScheduledRideReservation> b)
    {
        b.ToTable("scheduled_ride_reservations");
        b.HasKey(x => x.Id);
        b.Ignore(x => x.IsActive);
        b.Ignore(x => x.IsPreAssignment);
        b.Ignore(x => x.IsDriverFault);
        b.HasIndex(x => new { x.DriverId, x.Status });
        b.HasIndex(x => new { x.TripId, x.Status });
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ScheduledRideReminderConfiguration : IEntityTypeConfiguration<ScheduledRideReminder>
{
    public void Configure(EntityTypeBuilder<ScheduledRideReminder> b)
    {
        b.ToTable("scheduled_ride_reminders");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.Status, x.SendAt });
        b.HasIndex(x => x.TripId);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<ScheduledRideReservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

// ----- F17 airports (doc 11 §F17.6) -----

public sealed class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> b)
    {
        b.ToTable("airports");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(3).IsFixedLength().IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.Property(x => x.Geofence).HasColumnType("json").IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AirportZoneConfiguration : IEntityTypeConfiguration<AirportZone>
{
    public void Configure(EntityTypeBuilder<AirportZone> b)
    {
        b.ToTable("airport_zones");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.TerminalCode).HasMaxLength(10);
        b.Property(x => x.NameAr).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.Polygon).HasColumnType("json");
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.Property(x => x.InstructionsAr).HasMaxLength(500);
        b.Property(x => x.InstructionsEn).HasMaxLength(500);
        b.HasIndex(x => new { x.AirportId, x.Code }).IsUnique();
        b.HasOne<Airport>().WithMany().HasForeignKey(x => x.AirportId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AirportQueueEntryConfiguration : IEntityTypeConfiguration<AirportQueueEntry>
{
    public void Configure(EntityTypeBuilder<AirportQueueEntry> b)
    {
        b.ToTable("airport_queue_entries");
        b.HasKey(x => x.Id);
        b.Ignore(x => x.IsActive);
        b.HasIndex(x => new { x.AirportId, x.Status, x.EnteredAt });
        b.HasIndex(x => new { x.DriverId, x.Status });
        b.HasOne<Airport>().WithMany().HasForeignKey(x => x.AirportId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<RideCategory>().WithMany().HasForeignKey(x => x.RideCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.OfferedTripId).OnDelete(DeleteBehavior.SetNull);
    }
}
