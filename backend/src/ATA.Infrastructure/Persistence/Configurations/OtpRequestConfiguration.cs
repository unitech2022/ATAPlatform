using ATA.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class OtpRequestConfiguration : IEntityTypeConfiguration<OtpRequest>
{
    public void Configure(EntityTypeBuilder<OtpRequest> b)
    {
        b.ToTable("otp_requests");
        b.HasKey(x => x.Id);
        b.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.HasIndex(x => new { x.PhoneNumber, x.CreatedAt });
    }
}
