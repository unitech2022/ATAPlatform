using ATA.Domain.Drivers;
using ATA.Domain.Files;
using ATA.Domain.Identity;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class TripShareConfiguration : IEntityTypeConfiguration<TripShare>
{
    public void Configure(EntityTypeBuilder<TripShare> b)
    {
        b.ToTable("trip_shares");
        b.HasKey(x => x.Id);
        b.Property(x => x.Token).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.Token).IsUnique();
        b.HasIndex(x => x.TripId);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TrustedContact>().WithMany().HasForeignKey(x => x.TrustedContactId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TrustedContactConfiguration : IEntityTypeConfiguration<TrustedContact>
{
    public void Configure(EntityTypeBuilder<TrustedContact> b)
    {
        b.ToTable("trusted_contacts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.Relationship).HasMaxLength(40);
        b.Property(x => x.NotifyOnSos).HasDefaultValue(true).HasSentinel(true);
        b.HasIndex(x => new { x.UserId, x.PhoneNumber }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SafetyCaseConfiguration : IEntityTypeConfiguration<SafetyCase>
{
    public void Configure(EntityTypeBuilder<SafetyCase> b)
    {
        b.ToTable("safety_cases");
        b.HasKey(x => x.Id);
        b.Property(x => x.CaseNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.CaseNumber).IsUnique();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Resolution).HasMaxLength(2000);
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.Property(x => x.LastLat).HasPrecision(10, 7);
        b.Property(x => x.LastLng).HasPrecision(10, 7);
        b.Ignore(x => x.IsOpen);
        b.HasIndex(x => new { x.Status, x.Priority, x.OpenedAt });
        b.HasIndex(x => x.TripId);
        b.HasIndex(x => new { x.ReporterUserId, x.OpenedAt });
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReporterUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SubjectUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SafetyCaseNoteConfiguration : IEntityTypeConfiguration<SafetyCaseNote>
{
    public void Configure(EntityTypeBuilder<SafetyCaseNote> b)
    {
        b.ToTable("safety_case_notes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.IsInternal).HasDefaultValue(true).HasSentinel(true);
        b.HasIndex(x => new { x.CaseId, x.CreatedAt });
        b.HasOne<SafetyCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SafetyCaseAttachmentConfiguration : IEntityTypeConfiguration<SafetyCaseAttachment>
{
    public void Configure(EntityTypeBuilder<SafetyCaseAttachment> b)
    {
        b.ToTable("safety_case_attachments");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CaseId);
        b.HasOne<SafetyCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SafetyAlertConfiguration : IEntityTypeConfiguration<SafetyAlert>
{
    public void Configure(EntityTypeBuilder<SafetyAlert> b)
    {
        b.ToTable("safety_alerts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Lat).HasPrecision(10, 7);
        b.Property(x => x.Lng).HasPrecision(10, 7);
        b.Property(x => x.Metrics).HasColumnType("json");
        b.Ignore(x => x.IsPending);
        b.HasIndex(x => new { x.TripId, x.Type, x.Status });
        b.HasIndex(x => new { x.Status, x.RespondBy });
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<SafetyCase>().WithMany().HasForeignKey(x => x.SafetyCaseId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.DismissedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TripMessageConfiguration : IEntityTypeConfiguration<TripMessage>
{
    public void Configure(EntityTypeBuilder<TripMessage> b)
    {
        b.ToTable("trip_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).HasMaxLength(TripMessage.MaxBodyLength).IsRequired();
        b.Property(x => x.QuickReplyCode).HasMaxLength(40);
        b.HasIndex(x => new { x.TripId, x.CreatedAt });
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class LostItemReportConfiguration : IEntityTypeConfiguration<LostItemReport>
{
    public void Configure(EntityTypeBuilder<LostItemReport> b)
    {
        b.ToTable("lost_item_reports");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReportNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.ReportNumber).IsUnique();
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.ContactPhone).HasMaxLength(20);
        b.Property(x => x.DriverNote).HasMaxLength(500);
        b.HasIndex(x => new { x.DriverId, x.Status });
        b.HasIndex(x => new { x.ReporterUserId, x.CreatedAt });
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReporterUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ClosedBy).OnDelete(DeleteBehavior.SetNull);
    }
}
