using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATA.Infrastructure.Persistence.Seed;

/// <summary>Idempotent seed of catalog data and the development admin account.</summary>
public sealed class DataSeeder(AtaDbContext db, IPasswordHasher passwordHasher, IConfiguration configuration, ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCitiesAsync(cancellationToken);
        await SeedRideCategoriesAsync(cancellationToken);
        await SeedDocumentTypesAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCitiesAsync(CancellationToken ct)
    {
        if (!await db.Cities.AnyAsync(c => c.Id == SeedIds.CityRiyadh, ct))
        {
            db.Cities.Add(new City
            {
                Id = SeedIds.CityRiyadh, Code = "riyadh", NameAr = "الرياض", NameEn = "Riyadh", CountryCode = "SA",
                CenterLat = 24.7135517m, CenterLng = 46.6752957m,
            });
        }
    }

    private async Task SeedRideCategoriesAsync(CancellationToken ct)
    {
        var existing = await db.RideCategories.Select(c => c.Code).ToListAsync(ct);
        RideCategory[] categories =
        [
            new() { Id = SeedIds.RideCategories.Saver, Code = "saver", NameAr = "توفير", NameEn = "Saver", DescriptionAr = "أقل سعر", DescriptionEn = "Lowest fare", Icon = "car-small", Seats = 4, MaxStops = 1, SortOrder = 1 },
            new() { Id = SeedIds.RideCategories.Economy, Code = "economy", NameAr = "اقتصادي", NameEn = "Economy", DescriptionAr = "سيارة مريحة", DescriptionEn = "Comfortable car", Icon = "car", Seats = 4, MaxStops = 2, SortOrder = 2 },
            new() { Id = SeedIds.RideCategories.Comfort, Code = "comfort", NameAr = "مريح", NameEn = "Comfort", DescriptionAr = "سيارات أحدث ومساحة أكبر", DescriptionEn = "Newer cars with more room", Icon = "car-comfort", Seats = 4, MaxStops = 2, SortOrder = 3 },
            new() { Id = SeedIds.RideCategories.Family, Code = "family", NameAr = "عائلي", NameEn = "Family (XL)", DescriptionAr = "حتى 6 مقاعد", DescriptionEn = "Up to 6 seats", Icon = "van", Seats = 6, MaxStops = 2, SortOrder = 4 },
            new() { Id = SeedIds.RideCategories.Premium, Code = "premium", NameAr = "ATA بلس", NameEn = "ATA Plus", DescriptionAr = "سيارات فاخرة", DescriptionEn = "Premium cars", Icon = "car-premium", Seats = 4, MaxStops = 2, SortOrder = 5 },
            new() { Id = SeedIds.RideCategories.Airport, Code = "airport", NameAr = "المطار", NameEn = "Airport", DescriptionAr = "رحلات المطار بسعر ثابت", DescriptionEn = "Fixed-fare airport rides", Icon = "plane", Seats = 4, MaxStops = 1, SortOrder = 6 },
        ];
        db.RideCategories.AddRange(categories.Where(c => !existing.Contains(c.Code)));
    }

    private async Task SeedDocumentTypesAsync(CancellationToken ct)
    {
        var existing = await db.DocumentTypes.Select(d => d.Code).ToListAsync(ct);
        DocumentType[] types =
        [
            new() { Id = SeedIds.DocumentTypes.NationalId, Code = "national_id", NameAr = "الهوية الوطنية / الإقامة", NameEn = "National ID / Iqama", AppliesTo = DocumentAppliesTo.Driver, IsRequired = true, RequiresExpiry = true, SortOrder = 1 },
            new() { Id = SeedIds.DocumentTypes.DrivingLicense, Code = "driving_license", NameAr = "رخصة القيادة", NameEn = "Driving license", AppliesTo = DocumentAppliesTo.Driver, IsRequired = true, RequiresExpiry = true, SortOrder = 2 },
            new() { Id = SeedIds.DocumentTypes.VehicleRegistration, Code = "vehicle_registration", NameAr = "استمارة المركبة", NameEn = "Vehicle registration", AppliesTo = DocumentAppliesTo.Vehicle, IsRequired = true, RequiresExpiry = true, SortOrder = 3 },
            new() { Id = SeedIds.DocumentTypes.Insurance, Code = "insurance", NameAr = "التأمين", NameEn = "Insurance", AppliesTo = DocumentAppliesTo.Vehicle, IsRequired = true, RequiresExpiry = true, SortOrder = 4 },
            new() { Id = SeedIds.DocumentTypes.ProfilePhoto, Code = "profile_photo", NameAr = "الصورة الشخصية", NameEn = "Profile photo", AppliesTo = DocumentAppliesTo.Driver, IsRequired = true, RequiresExpiry = false, SortOrder = 5 },
        ];
        db.DocumentTypes.AddRange(types.Where(t => !existing.Contains(t.Code)));
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var username = configuration["Admin:Username"] ?? "admin";
        var password = configuration["Admin:Password"] ?? "Admin@12345";
        var phone = configuration["Admin:PhoneNumber"] ?? "+966500000000";

        if (await db.AdminAccounts.AnyAsync(a => a.Username == username, ct))
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        if (user is null)
        {
            user = new User { PhoneNumber = phone, FullName = "ATA Admin", Language = Language.Ar, PhoneVerifiedAt = DateTime.UtcNow };
            db.Users.Add(user);
        }

        if (!user.HasRole(Role.Admin))
        {
            user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.Admin });
        }

        db.AdminAccounts.Add(new AdminAccount
        {
            UserId = user.Id,
            Username = username,
            PasswordHash = passwordHasher.Hash(password),
            Permissions = "[\"*\"]",
            IsActive = true,
        });
        logger.LogInformation("Seeded admin account '{Username}'", username);
    }
}
