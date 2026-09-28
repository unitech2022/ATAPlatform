using ATA.Domain.Admin;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Files;
using ATA.Domain.Identity;
using ATA.Domain.Notifications;
using ATA.Domain.Passengers;
using ATA.Domain.Wallet;
using Microsoft.EntityFrameworkCore;

namespace ATA.Infrastructure.Persistence;

public class AtaDbContext(DbContextOptions<AtaDbContext> options, IClock clock) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();

    public DbSet<PassengerProfile> Passengers => Set<PassengerProfile>();
    public DbSet<SavedPlace> SavedPlaces => Set<SavedPlace>();

    public DbSet<DriverProfile> Drivers => Set<DriverProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<DriverDocument> DriverDocuments => Set<DriverDocument>();
    public DbSet<DriverStatusLog> DriverStatusLogs => Set<DriverStatusLog>();

    public DbSet<RideCategory> RideCategories => Set<RideCategory>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<City> Cities => Set<City>();

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtaDbContext).Assembly);
        NamingConventions.Apply(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    private void StampTimestamps()
    {
        var now = clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case AuditableEntity auditable when entry.State == EntityState.Added:
                    if (auditable.CreatedAt == default) auditable.CreatedAt = now;
                    auditable.UpdatedAt = now;
                    break;
                case AuditableEntity auditable when entry.State == EntityState.Modified:
                    auditable.UpdatedAt = now;
                    break;
                case Entity entity when entry.State == EntityState.Added && entity.CreatedAt == default:
                    entity.CreatedAt = now;
                    break;
                case NotificationPreference pref when entry.State is EntityState.Added or EntityState.Modified:
                    pref.UpdatedAt = now;
                    break;
                case UserRole role when entry.State == EntityState.Added && role.GrantedAt == default:
                    role.GrantedAt = now;
                    break;
                case DriverStatusLog log when entry.State == EntityState.Added && log.ChangedAt == default:
                    log.ChangedAt = now;
                    break;
            }
        }
    }
}
