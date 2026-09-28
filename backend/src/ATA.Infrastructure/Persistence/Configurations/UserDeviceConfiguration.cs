using ATA.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> b)
    {
        b.ToTable("user_devices");
        b.HasKey(x => x.Id);
        b.Property(x => x.DeviceId).HasMaxLength(128).IsRequired();
        b.Property(x => x.DeviceName).HasMaxLength(120);
        b.Property(x => x.PushToken).HasMaxLength(512);
        b.Property(x => x.AppVersion).HasMaxLength(32);
        b.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
