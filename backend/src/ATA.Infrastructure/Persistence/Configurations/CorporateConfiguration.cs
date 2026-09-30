using ATA.Domain.Catalog;
using ATA.Domain.Corporate;
using ATA.Domain.Files;
using ATA.Domain.Identity;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

// ----- F19 corporate accounts (doc 12 §F19.1) -----

public sealed class CorporateAccountConfiguration : IEntityTypeConfiguration<CorporateAccount>
{
    public void Configure(EntityTypeBuilder<CorporateAccount> b)
    {
        b.ToTable("corporate_accounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.AccountNumber).HasMaxLength(12).IsRequired();
        b.HasIndex(x => x.AccountNumber).IsUnique();
        b.Property(x => x.LegalNameAr).HasMaxLength(200).IsRequired();
        b.Property(x => x.LegalNameEn).HasMaxLength(200).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        b.Property(x => x.CrNumber).HasMaxLength(10).IsRequired();
        b.HasIndex(x => x.CrNumber).IsUnique();
        b.Property(x => x.VatNumber).HasMaxLength(15);
        b.Property(x => x.BillingEmail).HasMaxLength(254).IsRequired();
        b.Property(x => x.BillingAddress).HasColumnType("json");
        b.Property(x => x.ContactName).HasMaxLength(120).IsRequired();
        b.Property(x => x.ContactPhone).HasMaxLength(20).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasIndex(x => x.Status);
        b.Ignore(x => x.IsActive);
        b.HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CorporateUserConfiguration : IEntityTypeConfiguration<CorporateUser>
{
    public void Configure(EntityTypeBuilder<CorporateUser> b)
    {
        b.ToTable("corporate_users");
        b.HasKey(x => x.Id);
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(120);
        b.Property(x => x.EmployeeNumber).HasMaxLength(40);
        b.Property(x => x.Department).HasMaxLength(80);
        b.HasIndex(x => new { x.CorporateAccountId, x.PhoneNumber }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Status });
        b.HasIndex(x => new { x.PhoneNumber, x.Status });
        b.Ignore(x => x.IsAdmin);
        b.HasOne<CorporateAccount>().WithMany().HasForeignKey(x => x.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<CorporateCostCenter>().WithMany().HasForeignKey(x => x.CostCenterId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<CorporatePolicy>().WithMany().HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CorporateInvitationConfiguration : IEntityTypeConfiguration<CorporateInvitation>
{
    public void Configure(EntityTypeBuilder<CorporateInvitation> b)
    {
        b.ToTable("corporate_invitations");
        b.HasKey(x => x.Id);
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.PhoneNumber, x.ExpiresAt });
        b.HasIndex(x => x.CorporateUserId);
        b.Ignore(x => x.IsOpen);
        b.HasOne<CorporateAccount>().WithMany().HasForeignKey(x => x.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<CorporateUser>().WithMany().HasForeignKey(x => x.CorporateUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CorporateCostCenterConfiguration : IEntityTypeConfiguration<CorporateCostCenter>
{
    public void Configure(EntityTypeBuilder<CorporateCostCenter> b)
    {
        b.ToTable("corporate_cost_centers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(x => new { x.CorporateAccountId, x.Code }).IsUnique();
        b.HasOne<CorporateAccount>().WithMany().HasForeignKey(x => x.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CorporatePolicyConfiguration : IEntityTypeConfiguration<CorporatePolicy>
{
    public void Configure(EntityTypeBuilder<CorporatePolicy> b)
    {
        b.ToTable("corporate_policies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.AllowedRideCategoryIds).HasColumnType("json");
        b.Property(x => x.AllowedDays).HasColumnType("json");
        b.Property(x => x.TimeWindows).HasColumnType("json");
        b.Property(x => x.AllowedZoneIds).HasColumnType("json");
        b.HasIndex(x => x.CorporateAccountId);
        b.HasOne<CorporateAccount>().WithMany().HasForeignKey(x => x.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CorporateAdjustmentConfiguration : IEntityTypeConfiguration<CorporateAdjustment>
{
    public void Configure(EntityTypeBuilder<CorporateAdjustment> b)
    {
        b.ToTable("corporate_adjustments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
        b.HasIndex(x => new { x.CorporateAccountId, x.InvoiceId });
        b.HasOne<CorporateAccount>().WithMany().HasForeignKey(x => x.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<CorporateInvoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CorporateInvoiceConfiguration : IEntityTypeConfiguration<CorporateInvoice>
{
    public void Configure(EntityTypeBuilder<CorporateInvoice> b)
    {
        b.ToTable("corporate_invoices");
        b.HasKey(x => x.Id);
        b.Property(x => x.InvoiceNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.InvoiceNumber).IsUnique();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.VatRate).HasPrecision(5, 2);
        b.Property(x => x.SellerSnapshot).HasColumnType("json");
        b.Property(x => x.BuyerSnapshot).HasColumnType("json");
        b.Property(x => x.PaymentReference).HasMaxLength(100);
        b.Property(x => x.VoidReason).HasMaxLength(500);
        // UNIQUE(corporate_account_id, period_start) except void: voiding sets period_active to NULL, and NULLs never collide.
        b.HasIndex(x => new { x.CorporateAccountId, x.PeriodStart, x.PeriodActive }).IsUnique();
        b.HasIndex(x => new { x.Status, x.DueDate });
        b.Ignore(x => x.Outstanding);
        b.Ignore(x => x.IsOpen);
        b.HasOne<CorporateAccount>().WithMany().HasForeignKey(x => x.CorporateAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.PdfFileId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.IssuedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CorporateInvoiceLineConfiguration : IEntityTypeConfiguration<CorporateInvoiceLine>
{
    public void Configure(EntityTypeBuilder<CorporateInvoiceLine> b)
    {
        b.ToTable("corporate_invoice_lines");
        b.HasKey(x => x.Id);
        b.Property(x => x.TripNumber).HasMaxLength(20);
        b.Property(x => x.EmployeeName).HasMaxLength(120);
        b.Property(x => x.EmployeeNumber).HasMaxLength(40);
        b.Property(x => x.Department).HasMaxLength(80);
        b.Property(x => x.CostCenterCode).HasMaxLength(30);
        b.Property(x => x.GuestName).HasMaxLength(80);
        b.Property(x => x.Purpose).HasMaxLength(200);
        b.Property(x => x.PickupName).HasMaxLength(120);
        b.Property(x => x.DropoffName).HasMaxLength(120);
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
        b.HasIndex(x => x.InvoiceId);
        b.HasIndex(x => x.TripId);
        b.HasOne<CorporateInvoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CorporateApiKeyConfiguration : IEntityTypeConfiguration<CorporateApiKey>
{
    public void Configure(EntityTypeBuilder<CorporateApiKey> b)
    {
        b.ToTable("corporate_api_keys");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.KeyPrefix).HasMaxLength(8).IsFixedLength().IsRequired();
        b.Property(x => x.KeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.Scopes).HasColumnType("json");
        b.HasIndex(x => x.KeyPrefix);
        b.HasIndex(x => x.CorporateAccountId);
        b.HasOne<CorporateAccount>().WithMany().HasForeignKey(x => x.CorporateAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
