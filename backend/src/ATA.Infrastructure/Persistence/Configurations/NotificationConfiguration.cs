using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(60).IsRequired();
        b.Property(x => x.Category).HasDefaultValue(NotificationCategory.System).HasSentinel((NotificationCategory)(-1));
        b.Property(x => x.TitleAr).HasMaxLength(200).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(200).IsRequired();
        b.Property(x => x.BodyAr).HasMaxLength(1000).IsRequired();
        b.Property(x => x.BodyEn).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Data).HasColumnType("json");
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => x.CampaignId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<NotificationCampaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> b)
    {
        b.ToTable("notification_templates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.Property(x => x.TitleAr).HasMaxLength(120);
        b.Property(x => x.TitleEn).HasMaxLength(120);
        b.Property(x => x.BodyAr).HasMaxLength(1000).IsRequired();
        b.Property(x => x.BodyEn).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => new { x.Code, x.Channel }).IsUnique();
    }
}

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> b)
    {
        b.ToTable("notification_deliveries");
        b.HasKey(x => x.Id);
        b.Property(x => x.PhoneNumber).HasMaxLength(20);
        b.Property(x => x.EventCode).HasMaxLength(60).IsRequired();
        b.Property(x => x.SkippedReason).HasMaxLength(30);
        b.Property(x => x.Provider).HasMaxLength(20).IsRequired();
        b.Property(x => x.ProviderMessageId).HasMaxLength(100);
        b.Property(x => x.ErrorCode).HasMaxLength(60);
        b.Property(x => x.ErrorMessage).HasMaxLength(500);
        b.Property(x => x.Payload).HasColumnType("json").IsRequired();
        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => x.CampaignId);
        b.HasIndex(x => x.NotificationId);
        b.HasOne<Notification>().WithMany().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<NotificationCampaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class NotificationCampaignConfiguration : IEntityTypeConfiguration<NotificationCampaign>
{
    public void Configure(EntityTypeBuilder<NotificationCampaign> b)
    {
        b.ToTable("notification_campaigns");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Channels).HasColumnType("json").IsRequired();
        b.Property(x => x.Audience).HasColumnType("json").IsRequired();
        b.Property(x => x.TitleAr).HasMaxLength(120).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.BodyAr).HasMaxLength(1000).IsRequired();
        b.Property(x => x.BodyEn).HasMaxLength(1000).IsRequired();
        b.Property(x => x.DeepLink).HasMaxLength(255);
        b.HasIndex(x => new { x.Status, x.ScheduledAt });
        b.Ignore(x => x.IsEditable);
    }
}

public sealed class DocumentExpiryNoticeConfiguration : IEntityTypeConfiguration<DocumentExpiryNotice>
{
    public void Configure(EntityTypeBuilder<DocumentExpiryNotice> b)
    {
        b.ToTable("document_expiry_notices");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.DriverDocumentId, x.OffsetDays }).IsUnique();
        b.HasOne<DriverDocument>().WithMany().HasForeignKey(x => x.DriverDocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}
