using ATA.Domain.Identity;
using ATA.Domain.Wallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> b)
    {
        b.ToTable("wallets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.HasIndex(x => new { x.UserId, x.Kind }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Ignore(x => x.LedgerAccount);
    }
}
