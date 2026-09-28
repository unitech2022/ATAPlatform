using ATA.Domain.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        b.ToTable("ledger_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Account).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.JournalId);
        b.HasIndex(x => x.Account);
        b.HasOne<WalletTransaction>().WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LedgerJournal>().WithMany().HasForeignKey(x => x.JournalId).OnDelete(DeleteBehavior.Restrict);
    }
}
