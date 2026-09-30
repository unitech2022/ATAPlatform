using ATA.Domain.Admin;
using ATA.Domain.Airports;
using ATA.Domain.Cancellation;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Drivers;
using ATA.Domain.Favorites;
using ATA.Domain.Files;
using ATA.Domain.Identity;
using ATA.Domain.Incentives;
using ATA.Domain.Matching;
using ATA.Domain.Notifications;
using ATA.Domain.Passengers;
using ATA.Domain.Payments;
using ATA.Domain.Pricing;
using ATA.Domain.Promotions;
using ATA.Domain.Ratings;
using ATA.Domain.Safety;
using ATA.Domain.Scheduling;
using ATA.Domain.Support;
using ATA.Domain.Trips;
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

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripStop> TripStops => Set<TripStop>();
    public DbSet<TripOffer> TripOffers => Set<TripOffer>();
    public DbSet<TripEvent> TripEvents => Set<TripEvent>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<DriverLocationHistory> DriverLocationHistory => Set<DriverLocationHistory>();

    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<ZoneCategorySetting> ZoneCategorySettings => Set<ZoneCategorySetting>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<PricingTimeMultiplier> PricingTimeMultipliers => Set<PricingTimeMultiplier>();
    public DbSet<DemandLevel> DemandLevels => Set<DemandLevel>();
    public DbSet<DemandRule> DemandRules => Set<DemandRule>();
    public DbSet<DemandOverride> DemandOverrides => Set<DemandOverride>();
    public DbSet<DemandSnapshot> DemandSnapshots => Set<DemandSnapshot>();
    public DbSet<FareQuote> FareQuotes => Set<FareQuote>();

    public DbSet<MatchingSettings> MatchingSettings => Set<MatchingSettings>();
    public DbSet<MatchingAttempt> MatchingAttempts => Set<MatchingAttempt>();
    public DbSet<MatchingCandidate> MatchingCandidates => Set<MatchingCandidate>();

    public DbSet<RideCategory> RideCategories => Set<RideCategory>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<City> Cities => Set<City>();

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<LedgerJournal> LedgerJournals => Set<LedgerJournal>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<PayoutBatch> PayoutBatches => Set<PayoutBatch>();
    public DbSet<SettlementBatch> SettlementBatches => Set<SettlementBatch>();
    public DbSet<Settlement> Settlements => Set<Settlement>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<NotificationCampaign> NotificationCampaigns => Set<NotificationCampaign>();
    public DbSet<DocumentExpiryNotice> DocumentExpiryNotices => Set<DocumentExpiryNotice>();

    public DbSet<TripShare> TripShares => Set<TripShare>();
    public DbSet<TrustedContact> TrustedContacts => Set<TrustedContact>();
    public DbSet<SafetyCase> SafetyCases => Set<SafetyCase>();
    public DbSet<SafetyCaseNote> SafetyCaseNotes => Set<SafetyCaseNote>();
    public DbSet<SafetyCaseAttachment> SafetyCaseAttachments => Set<SafetyCaseAttachment>();
    public DbSet<SafetyAlert> SafetyAlerts => Set<SafetyAlert>();
    public DbSet<TripMessage> TripMessages => Set<TripMessage>();
    public DbSet<LostItemReport> LostItemReports => Set<LostItemReport>();

    public DbSet<CancellationReason> CancellationReasons => Set<CancellationReason>();
    public DbSet<CancellationRule> CancellationRules => Set<CancellationRule>();
    public DbSet<CancellationEvent> CancellationEvents => Set<CancellationEvent>();
    public DbSet<ReliabilityProfile> ReliabilityProfiles => Set<ReliabilityProfile>();
    public DbSet<ReliabilityThreshold> ReliabilityThresholds => Set<ReliabilityThreshold>();
    public DbSet<ReliabilityAdjustment> ReliabilityAdjustments => Set<ReliabilityAdjustment>();

    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<RatingTag> RatingTags => Set<RatingTag>();
    public DbSet<RatingFlag> RatingFlags => Set<RatingFlag>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();
    public DbSet<DriverTierRule> DriverTierRules => Set<DriverTierRule>();
    public DbSet<DriverTierHistory> DriverTierHistory => Set<DriverTierHistory>();
    public DbSet<DriverIncentive> DriverIncentives => Set<DriverIncentive>();
    public DbSet<DriverIncentiveProgress> DriverIncentiveProgress => Set<DriverIncentiveProgress>();
    public DbSet<DriverIncentiveTrip> DriverIncentiveTrips => Set<DriverIncentiveTrip>();
    public DbSet<FavoriteDriver> FavoriteDrivers => Set<FavoriteDriver>();
    public DbSet<FavoriteDriverDiscountRule> FavoriteDriverDiscountRules => Set<FavoriteDriverDiscountRule>();

    public DbSet<ScheduledRideRule> ScheduledRideRules => Set<ScheduledRideRule>();
    public DbSet<ScheduledRideReservation> ScheduledRideReservations => Set<ScheduledRideReservation>();
    public DbSet<ScheduledRideReminder> ScheduledRideReminders => Set<ScheduledRideReminder>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<AirportZone> AirportZones => Set<AirportZone>();
    public DbSet<AirportQueueEntry> AirportQueueEntries => Set<AirportQueueEntry>();

    public DbSet<HelpCategory> HelpCategories => Set<HelpCategory>();
    public DbSet<HelpArticle> HelpArticles => Set<HelpArticle>();
    public DbSet<SupportSlaPolicy> SupportSlaPolicies => Set<SupportSlaPolicy>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<SupportMessageAttachment> SupportMessageAttachments => Set<SupportMessageAttachment>();
    public DbSet<CannedResponse> CannedResponses => Set<CannedResponse>();
    public DbSet<FareDispute> FareDisputes => Set<FareDispute>();

    public DbSet<CorporateAccount> CorporateAccounts => Set<CorporateAccount>();
    public DbSet<CorporateUser> CorporateUsers => Set<CorporateUser>();
    public DbSet<CorporateInvitation> CorporateInvitations => Set<CorporateInvitation>();
    public DbSet<CorporateCostCenter> CorporateCostCenters => Set<CorporateCostCenter>();
    public DbSet<CorporatePolicy> CorporatePolicies => Set<CorporatePolicy>();
    public DbSet<CorporateAdjustment> CorporateAdjustments => Set<CorporateAdjustment>();
    public DbSet<CorporateInvoice> CorporateInvoices => Set<CorporateInvoice>();
    public DbSet<CorporateInvoiceLine> CorporateInvoiceLines => Set<CorporateInvoiceLine>();
    public DbSet<CorporateApiKey> CorporateApiKeys => Set<CorporateApiKey>();

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
                case TripOffer offer when entry.State == EntityState.Added && offer.SentAt == default:
                    offer.SentAt = now;
                    break;
                case DriverLocation location when entry.State is EntityState.Added or EntityState.Modified:
                    location.UpdatedAt = now;
                    break;
                case DriverLocationHistory history when entry.State == EntityState.Added && history.RecordedAt == default:
                    history.RecordedAt = now;
                    break;
                case MatchingAttempt attempt when entry.State == EntityState.Added && attempt.StartedAt == default:
                    attempt.StartedAt = now;
                    break;
                case DemandSnapshot snapshot when entry.State == EntityState.Added && snapshot.ComputedAt == default:
                    snapshot.ComputedAt = now;
                    break;
                case PaymentWebhookEvent webhook when entry.State == EntityState.Added && webhook.ReceivedAt == default:
                    webhook.ReceivedAt = now;
                    break;
                case SupportSlaPolicy policy when entry.State is EntityState.Added or EntityState.Modified:
                    policy.UpdatedAt = now;
                    break;
                case DocumentExpiryNotice notice when entry.State == EntityState.Added && notice.SentAt == default:
                    notice.SentAt = now;
                    break;
            }
        }
    }
}
