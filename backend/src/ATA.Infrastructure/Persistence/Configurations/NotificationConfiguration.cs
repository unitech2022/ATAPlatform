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
        b.Property(x => x.TitleAr).HasMaxLength(200).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(200).IsRequired();
        b.Property(x => x.BodyAr).HasMaxLength(1000).IsRequired();
        b.Property(x => x.BodyEn).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Data).HasColumnType("json");
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
