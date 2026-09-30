using ATA.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class AdminAccountConfiguration : IEntityTypeConfiguration<AdminAccount>
{
    public void Configure(EntityTypeBuilder<AdminAccount> b)
    {
        b.ToTable("admin_accounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Username).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(x => x.MfaSecret).HasMaxLength(512);
        b.Property(x => x.Permissions).HasColumnType("json").IsRequired();
        b.HasOne(x => x.User).WithOne().HasForeignKey<AdminAccount>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
