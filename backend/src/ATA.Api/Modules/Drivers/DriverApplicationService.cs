using System.Security.Cryptography;
using ATA.Api.Common;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Files;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Drivers;

/// <summary>Driver onboarding: profile, vehicle, document uploads, submission and the read model shared with the admin module.</summary>
public sealed class DriverApplicationService(AtaDbContext db, ICurrentUser currentUser, IFileStorage storage, IClock clock)
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
    };

    public async Task<DriverProfile> LoadOwnAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.Include(d => d.Vehicles).Include(d => d.Documents).FirstOrDefaultAsync(d => d.UserId == userId, ct)
            ?? throw new DomainException(ErrorCodes.Forbidden);
    }

    public async Task<DriverApplicationDto> BuildApplicationAsync(Guid driverId, Language lang, CancellationToken ct)
    {
        var driver = Guard.NotFound(await db.Drivers.AsNoTracking().Include(d => d.Vehicles).Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.Id == driverId, ct));
        var fullName = await db.Users.Where(u => u.Id == driver.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        var types = await db.DocumentTypes.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var fileIds = driver.Documents.Select(d => d.FileId).ToList();
        var files = await db.StoredFiles.AsNoTracking().Where(f => fileIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);

        var documents = driver.Documents
            .OrderBy(d => types.FindIndex(t => t.Id == d.DocumentTypeId))
            .Select(d => ToDocumentDto(d, types.First(t => t.Id == d.DocumentTypeId), files[d.FileId], lang))
            .ToList();

        var required = types.Select(t => new RequiredDocumentDto(
            t.Id, t.Code, lang.Pick(t.NameAr, t.NameEn), t.AppliesTo, t.IsRequired, t.RequiresExpiry,
            driver.Documents.Any(d => d.DocumentTypeId == t.Id && d.Status != DocumentStatus.Rejected))).ToList();

        var vehicle = driver.Vehicles.FirstOrDefault(v => v.IsActive);
        var steps = ComputeSteps(driver, fullName, vehicle, types);

        return new DriverApplicationDto(
            driver.ApplicationNumber,
            driver.ApplicationStatus,
            driver.RejectionReason,
            driver.SubmittedAt,
            driver.ApprovedAt,
            new DriverProfileDto(fullName, driver.NationalId, driver.DateOfBirth, driver.CityId, driver.Gender, driver.Iban),
            vehicle is null ? null : ToVehicleDto(vehicle),
            documents,
            required,
            steps);
    }

    public async Task<DriverDocumentDto> GetDocumentDtoAsync(Guid documentId, Language lang, CancellationToken ct)
    {
        var document = Guard.NotFound(await db.DriverDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, ct));
        var type = await db.DocumentTypes.AsNoTracking().FirstAsync(t => t.Id == document.DocumentTypeId, ct);
        var file = await db.StoredFiles.AsNoTracking().FirstAsync(f => f.Id == document.FileId, ct);
        return ToDocumentDto(document, type, file, lang);
    }

    /// <summary>Codes of required document types that are not yet <c>verified</c> for the driver.</summary>
    public async Task<List<string>> MissingVerifiedDocumentsAsync(Guid driverId, CancellationToken ct)
    {
        var required = await db.DocumentTypes.AsNoTracking().Where(t => t.IsActive && t.IsRequired).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var verified = await db.DriverDocuments.AsNoTracking()
            .Where(d => d.DriverId == driverId && d.Status == DocumentStatus.Verified)
            .Select(d => d.DocumentTypeId).ToListAsync(ct);
        return required.Where(t => !verified.Contains(t.Id)).Select(t => t.Code).ToList();
    }

    public async Task<DriverApplicationDto> UpdateProfileAsync(UpdateDriverProfileRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.FullName), request.FullName, 120)
            .Require(nameof(request.NationalId), request.NationalId, 10)
            .Rule(nameof(request.NationalId), request.NationalId is null || (request.NationalId.Length == 10 && request.NationalId.All(char.IsAsciiDigit)), "must be 10 digits")
            .Require(nameof(request.DateOfBirth), request.DateOfBirth)
            .Rule(nameof(request.DateOfBirth), request.DateOfBirth is null || IsAdult(request.DateOfBirth.Value), "driver must be at least 18 years old")
            .Require(nameof(request.CityId), request.CityId)
            .Require(nameof(request.Gender), request.Gender)
            .Rule(nameof(request.Gender), request.Gender is null or Gender.Male or Gender.Female, "must be male or female")
            .Rule(nameof(request.Iban), string.IsNullOrWhiteSpace(request.Iban) || IsSaudiIban(request.Iban), "must be a valid Saudi IBAN (SA + 22 characters)")
            .ThrowIfInvalid();

        var driver = await LoadOwnAsync(ct);
        driver.EnsureEditable();
        if (!await db.Cities.AnyAsync(c => c.Id == request.CityId && c.IsActive, ct))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { cityId = "unknown city" });
        }

        var user = await db.Users.FirstAsync(u => u.Id == driver.UserId, ct);
        user.FullName = request.FullName!.Trim();
        user.Gender = request.Gender!.Value;
        driver.NationalId = request.NationalId;
        driver.DateOfBirth = request.DateOfBirth;
        driver.CityId = request.CityId;
        driver.Gender = request.Gender!.Value;
        driver.Iban = string.IsNullOrWhiteSpace(request.Iban) ? null : request.Iban.Replace(" ", string.Empty).ToUpperInvariant();
        await db.SaveChangesAsync(ct);
        return await BuildApplicationAsync(driver.Id, lang, ct);
    }

    public async Task<DriverApplicationDto> UpsertVehicleAsync(UpsertVehicleRequest request, Language lang, CancellationToken ct)
    {
        var currentYear = clock.UtcNow.Year;
        new Validator()
            .Require(nameof(request.Make), request.Make, 60)
            .Require(nameof(request.Model), request.Model, 60)
            .Require(nameof(request.Year), request.Year)
            .Rule(nameof(request.Year), request.Year is null || (request.Year >= currentYear - 15 && request.Year <= currentYear + 1), $"must be between {currentYear - 15} and {currentYear + 1}")
            .Require(nameof(request.Color), request.Color, 40)
            .Require(nameof(request.PlateNumber), request.PlateNumber, 20)
            .Require(nameof(request.Seats), request.Seats)
            .Rule(nameof(request.Seats), request.Seats is null or (>= 2 and <= 8), "must be between 2 and 8")
            .Require(nameof(request.RideCategoryId), request.RideCategoryId)
            .ThrowIfInvalid();

        var driver = await LoadOwnAsync(ct);
        driver.EnsureEditable();
        if (!await db.RideCategories.AnyAsync(c => c.Id == request.RideCategoryId && c.IsActive, ct))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { rideCategoryId = "unknown ride category" });
        }

        var plate = NormalizePlate(request.PlateNumber!);
        var vehicle = driver.Vehicles.FirstOrDefault(v => v.IsActive);
        var currentVehicleId = vehicle?.Id;
        if (await db.Vehicles.AnyAsync(v => v.PlateNumber == plate && v.Id != currentVehicleId, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { plateNumber = "already registered" });
        }

        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                DriverId = driver.Id, RideCategoryId = request.RideCategoryId!.Value, Make = request.Make!.Trim(), Model = request.Model!.Trim(),
                Color = request.Color!.Trim(), PlateNumber = plate,
            };
            driver.Vehicles.Add(vehicle);
        }

        vehicle.RideCategoryId = request.RideCategoryId!.Value;
        vehicle.Make = request.Make!.Trim();
        vehicle.Model = request.Model!.Trim();
        vehicle.Year = request.Year!.Value;
        vehicle.Color = request.Color!.Trim();
        vehicle.PlateNumber = plate;
        vehicle.Seats = request.Seats!.Value;
        foreach (var doc in driver.Documents.Where(d => d.VehicleId is null))
        {
            doc.VehicleId = await IsVehicleTypeAsync(doc.DocumentTypeId, ct) ? vehicle.Id : null;
        }

        await db.SaveChangesAsync(ct);
        return await BuildApplicationAsync(driver.Id, lang, ct);
    }

    public async Task<DriverDocumentDto> UploadDocumentAsync(Guid? documentTypeId, IFormFile? file, DateOnly? expiresAt, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require("documentTypeId", documentTypeId)
            .Rule("file", file is not null && file.Length > 0, "required")
            .ThrowIfInvalid();

        var driver = await LoadOwnAsync(ct);
        driver.EnsureEditable();
        var type = await db.DocumentTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == documentTypeId && t.IsActive, ct)
            ?? throw new DomainException(ErrorCodes.ValidationFailed, new { documentTypeId = "unknown document type" });

        if (type.RequiresExpiry && expiresAt is null)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { expiresAt = "required for this document type" });
        }

        if (expiresAt is not null && expiresAt <= DateOnly.FromDateTime(clock.UtcNow))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { expiresAt = "must be in the future" });
        }

        if (file!.Length > MaxFileBytes)
        {
            throw new DomainException(ErrorCodes.FileTooLarge, new { maxBytes = MaxFileBytes, sizeBytes = file.Length });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.TryGetValue(extension, out var contentType))
        {
            throw new DomainException(ErrorCodes.UnsupportedFileType, new { extension, allowed = AllowedExtensions.Keys });
        }

        var existing = driver.Documents.FirstOrDefault(d => d.DocumentTypeId == type.Id);
        if (existing is not null && existing.Status is DocumentStatus.Verified)
        {
            throw new DomainException(ErrorCodes.Conflict, new { documentTypeId = "a verified document of this type already exists" });
        }

        var stored = new StoredFile
        {
            OwnerUserId = driver.UserId,
            StorageKey = $"drivers/{driver.Id}/{Guid.CreateVersion7()}{extension.ToLowerInvariant()}",
            OriginalName = Path.GetFileName(file.FileName),
            ContentType = contentType,
            SizeBytes = file.Length,
            Sha256 = string.Empty,
        };

        await using (var upload = file.OpenReadStream())
        {
            stored.Sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(upload, ct));
            upload.Position = 0;
            await storage.SaveAsync(stored.StorageKey, upload, ct);
        }

        db.StoredFiles.Add(stored);

        string? replacedKey = null;
        if (existing is not null)
        {
            replacedKey = await db.StoredFiles.Where(f => f.Id == existing.FileId).Select(f => f.StorageKey).FirstOrDefaultAsync(ct);
            db.DriverDocuments.Remove(existing);
            driver.Documents.Remove(existing);
        }

        var document = new DriverDocument
        {
            DriverId = driver.Id,
            VehicleId = type.AppliesTo == DocumentAppliesTo.Vehicle ? driver.Vehicles.FirstOrDefault(v => v.IsActive)?.Id : null,
            DocumentTypeId = type.Id,
            FileId = stored.Id,
            ExpiresAt = expiresAt,
        };
        driver.Documents.Add(document);
        await db.SaveChangesAsync(ct);

        if (replacedKey is not null)
        {
            await storage.DeleteAsync(replacedKey, ct);
        }

        return ToDocumentDto(document, type, stored, lang);
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken ct)
    {
        var driver = await LoadOwnAsync(ct);
        var document = Guard.NotFound(driver.Documents.FirstOrDefault(d => d.Id == documentId));
        if (document.Status != DocumentStatus.Pending)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = document.Status });
        }

        var key = await db.StoredFiles.Where(f => f.Id == document.FileId).Select(f => f.StorageKey).FirstAsync(ct);
        db.DriverDocuments.Remove(document);
        await db.SaveChangesAsync(ct);
        await storage.DeleteAsync(key, ct);
    }

    public async Task<SubmitResponse> SubmitAsync(CancellationToken ct)
    {
        var driver = await LoadOwnAsync(ct);
        driver.EnsureEditable();
        var fullName = await db.Users.Where(u => u.Id == driver.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        var types = await db.DocumentTypes.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var vehicle = driver.Vehicles.FirstOrDefault(v => v.IsActive);

        var missing = new List<string>();
        if (!driver.IsProfileComplete(fullName)) missing.Add("profile");
        if (vehicle is null) missing.Add("vehicle");
        missing.AddRange(MissingDocumentCodes(driver, types).Select(code => $"documents:{code}"));
        if (missing.Count > 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { missing });
        }

        driver.Submit(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return new SubmitResponse(driver.ApplicationStatus);
    }

    public static DriverDocumentDto ToDocumentDto(DriverDocument d, DocumentType type, StoredFile file, Language lang) => new(
        d.Id, type.Id, type.Code, lang.Pick(type.NameAr, type.NameEn), d.Status, d.ExpiresAt, d.ReviewNote, file.Id, file.OriginalName, d.CreatedAt);

    public static VehicleDto ToVehicleDto(Vehicle v) => new(v.Id, v.Make, v.Model, v.Year, v.Color, v.PlateNumber, v.Seats, v.RideCategoryId);

    private static ApplicationStepsDto ComputeSteps(DriverProfile driver, string? fullName, Vehicle? vehicle, List<DocumentType> types)
    {
        var profile = driver.IsProfileComplete(fullName);
        var vehicleComplete = vehicle is not null;
        var documents = MissingDocumentCodes(driver, types).Count == 0;
        return new ApplicationStepsDto(profile, vehicleComplete, documents, profile && vehicleComplete && documents && driver.CanEditApplication);
    }

    private static List<string> MissingDocumentCodes(DriverProfile driver, List<DocumentType> types) =>
        types.Where(t => t.IsRequired && !driver.Documents.Any(d => d.DocumentTypeId == t.Id && d.Status != DocumentStatus.Rejected))
            .Select(t => t.Code).ToList();

    private Task<bool> IsVehicleTypeAsync(Guid documentTypeId, CancellationToken ct) =>
        db.DocumentTypes.AnyAsync(t => t.Id == documentTypeId && t.AppliesTo == DocumentAppliesTo.Vehicle, ct);

    private bool IsAdult(DateOnly dateOfBirth) => dateOfBirth <= DateOnly.FromDateTime(clock.UtcNow).AddYears(-18);

    private static bool IsSaudiIban(string iban)
    {
        var compact = iban.Replace(" ", string.Empty).ToUpperInvariant();
        return compact.Length == 24 && compact.StartsWith("SA", StringComparison.Ordinal) && compact[2..].All(char.IsAsciiLetterOrDigit);
    }

    private static string NormalizePlate(string plate) => string.Join(' ', plate.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
