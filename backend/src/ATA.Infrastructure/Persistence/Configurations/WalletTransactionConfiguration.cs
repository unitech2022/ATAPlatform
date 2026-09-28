using ATA.Domain.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> b)
    {
        b.ToTable("wallet_transactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReferenceType).HasMaxLength(40);
        b.Property(x => x.IdempotencyKey).HasMaxLength(128);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Property(x => x.Description).HasMaxLength(255);
        b.HasIndex(x => new { x.WalletId, x.CreatedAt });
        b.HasOne<Wallet>().WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
    }
}
