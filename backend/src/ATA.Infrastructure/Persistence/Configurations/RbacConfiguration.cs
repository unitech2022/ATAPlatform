using ATA.Domain.Identity;
using ATA.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

// ----- F20 roles & permissions (doc 12 §F20.1) -----

public sealed class AdminRoleConfiguration : IEntityTypeConfiguration<AdminRole>
{
    public void Configure(EntityTypeBuilder<AdminRole> b)
    {
        b.ToTable("roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Module).HasMaxLength(30).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        b.HasOne<AdminRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AdminAccountRoleConfiguration : IEntityTypeConfiguration<AdminAccountRole>
{
    public void Configure(EntityTypeBuilder<AdminAccountRole> b)
    {
        b.ToTable("admin_account_roles");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.AdminAccountId, x.RoleId }).IsUnique();
        b.HasIndex(x => x.RoleId);
        b.HasOne<AdminAccount>().WithMany().HasForeignKey(x => x.AdminAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<AdminRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdminRecoveryCodeConfiguration : IEntityTypeConfiguration<AdminRecoveryCode>
{
    public void Configure(EntityTypeBuilder<AdminRecoveryCode> b)
    {
        b.ToTable("admin_recovery_codes");
        b.HasKey(x => x.Id);
        b.Property(x => x.CodeHash).HasMaxLength(255).IsRequired();
        b.HasIndex(x => x.AdminAccountId);
        b.HasOne<AdminAccount>().WithMany().HasForeignKey(x => x.AdminAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
