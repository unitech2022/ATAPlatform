using ATA.Domain.Identity;
using ATA.Domain.Passengers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

public sealed class PassengerProfileConfiguration : IEntityTypeConfiguration<PassengerProfile>
{
    public void Configure(EntityTypeBuilder<PassengerProfile> b)
    {
        b.ToTable("passengers");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.RatingAvg).HasPrecision(3, 2);
        b.HasOne<User>().WithOne().HasForeignKey<PassengerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.SavedPlaces).WithOne().HasForeignKey(p => p.PassengerId).OnDelete(DeleteBehavior.Cascade);
    }
}
