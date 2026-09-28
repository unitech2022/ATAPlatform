using ATA.Domain.Drivers;
using ATA.Domain.Files;
using ATA.Domain.Identity;
using ATA.Domain.Payments;
using ATA.Domain.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> b)
    {
        b.ToTable("payment_methods");
        b.HasKey(x => x.Id);
        b.Property(x => x.Provider).HasMaxLength(20).IsRequired();
        b.Property(x => x.Type).HasMaxLength(20).IsRequired();
        b.Property(x => x.GatewayToken).HasMaxLength(255).IsRequired();
        b.Property(x => x.Brand).HasMaxLength(20).IsRequired();
        b.Property(x => x.Last4).HasMaxLength(4).IsFixedLength().IsRequired();
        b.Property(x => x.HolderName).HasMaxLength(100);
        b.Property(x => x.Fingerprint).HasMaxLength(64);
        b.HasIndex(x => new { x.UserId, x.Status });
        b.HasIndex(x => new { x.UserId, x.Fingerprint }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Provider).HasMaxLength(20).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.GatewayPaymentId).HasMaxLength(100);
        b.HasIndex(x => x.GatewayPaymentId).IsUnique();
        b.Property(x => x.GatewayStatus).HasMaxLength(40);
        b.Property(x => x.ActionUrl).HasMaxLength(1000);
        b.Property(x => x.ReturnUrl).HasMaxLength(500);
        b.Property(x => x.FailureCode).HasMaxLength(60);
        b.Property(x => x.FailureMessage).HasMaxLength(500);
        b.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Property(x => x.Metadata).HasColumnType("json");
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => x.TripId);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.Ignore(x => x.IsTerminal);
        b.Ignore(x => x.Refundable);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Wallet>().WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> b)
    {
        b.ToTable("payment_webhook_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Provider).HasMaxLength(20).IsRequired();
        b.Property(x => x.EventId).HasMaxLength(100).IsRequired();
        b.Property(x => x.EventType).HasMaxLength(60).IsRequired();
        b.Property(x => x.GatewayPaymentId).HasMaxLength(100);
        b.Property(x => x.Payload).HasColumnType("json").IsRequired();
        b.Property(x => x.Error).HasMaxLength(500);
        b.HasIndex(x => new { x.Provider, x.EventId }).IsUnique();
        b.HasIndex(x => new { x.ProcessingStatus, x.ReceivedAt });
        b.HasIndex(x => x.GatewayPaymentId);
    }
}

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> b)
    {
        b.ToTable("refunds");
        b.HasKey(x => x.Id);
        b.Property(x => x.RefundNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.RefundNumber).IsUnique();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.RejectedReason).HasMaxLength(500);
        b.Property(x => x.GatewayRefundId).HasMaxLength(100);
        b.Property(x => x.FailureMessage).HasMaxLength(500);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => x.TripId);
        b.HasIndex(x => x.PaymentId);
        b.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> b)
    {
        b.ToTable("payouts");
        b.HasKey(x => x.Id);
        b.Property(x => x.PayoutNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.PayoutNumber).IsUnique();
        b.Property(x => x.IbanMasked).HasMaxLength(34).IsRequired();
        b.Property(x => x.IbanEncrypted).HasMaxLength(500).IsRequired();
        b.Property(x => x.AccountHolderName).HasMaxLength(150).IsRequired();
        b.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Property(x => x.BankReference).HasMaxLength(100);
        b.Property(x => x.RejectedReason).HasMaxLength(500);
        b.HasIndex(x => new { x.DriverId, x.RequestedAt });
        b.HasIndex(x => x.Status);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Wallet>().WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PayoutBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class PayoutBatchConfiguration : IEntityTypeConfiguration<PayoutBatch>
{
    public void Configure(EntityTypeBuilder<PayoutBatch> b)
    {
        b.ToTable("payout_batches");
        b.HasKey(x => x.Id);
        b.Property(x => x.BatchNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.BatchNumber).IsUnique();
        b.Property(x => x.BankReference).HasMaxLength(100);
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.ExportFileId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SettlementBatchConfiguration : IEntityTypeConfiguration<SettlementBatch>
{
    public void Configure(EntityTypeBuilder<SettlementBatch> b)
    {
        b.ToTable("settlement_batches");
        b.HasKey(x => x.Id);
        b.Property(x => x.BatchNumber).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.BatchNumber).IsUnique();
        b.Property(x => x.Error).HasMaxLength(500);
        b.HasIndex(x => new { x.CityId, x.PeriodStart });
        b.HasOne<ATA.Domain.Catalog.City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> b)
    {
        b.ToTable("settlements");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BatchId, x.DriverId }).IsUnique();
        b.HasIndex(x => x.DriverId);
        b.HasOne<SettlementBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Payout>().WithMany().HasForeignKey(x => x.PayoutId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class LedgerJournalConfiguration : IEntityTypeConfiguration<LedgerJournal>
{
    public void Configure(EntityTypeBuilder<LedgerJournal> b)
    {
        b.ToTable("ledger_journals");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReferenceType).HasMaxLength(40).IsRequired();
        b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
        b.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
        b.HasIndex(x => x.CreatedAt);
    }
}
