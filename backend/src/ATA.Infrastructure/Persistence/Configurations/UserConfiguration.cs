using ATA.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.PhoneNumber).IsUnique();
        b.Property(x => x.FullName).HasMaxLength(120);
        b.HasMany(x => x.Roles).WithOne().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Roles).AutoInclude();
    }
}
