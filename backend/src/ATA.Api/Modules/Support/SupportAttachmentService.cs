using System.Security.Cryptography;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Files;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Storage;

namespace ATA.Api.Modules.Support;

/// <summary>
/// <c>POST /support/attachments</c> (doc 11 §F18.2): the pre-upload of a message attachment with the same limits as driver documents (F2): jpg / png / pdf up to 10 MB.
/// <c>stored_files.owner_user_id</c> is the uploader; the file is attached to a message by its <c>fileId</c> afterwards.
/// </summary>
public sealed class SupportAttachmentService(AtaDbContext db, ICurrentUser currentUser, IFileStorage storage)
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
    };

    public async Task<AttachmentUploadDto> UploadAsync(IFormFile? file, CancellationToken ct)
    {
        new Validator().Rule("file", file is not null && file.Length > 0, "required").ThrowIfInvalid();
        if (file!.Length > MaxFileBytes)
        {
            throw new DomainException(ErrorCodes.FileTooLarge, new { maxBytes = MaxFileBytes, sizeBytes = file.Length });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.TryGetValue(extension, out var contentType))
        {
            throw new DomainException(ErrorCodes.UnsupportedFileType, new { extension, allowed = AllowedExtensions.Keys });
        }

        var userId = currentUser.UserId;
        var stored = new StoredFile
        {
            OwnerUserId = userId,
            StorageKey = $"{SupportTicketWriter.StoragePrefix}{userId}/{Guid.CreateVersion7()}{extension.ToLowerInvariant()}",
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
        await db.SaveChangesAsync(ct);
        return new AttachmentUploadDto(stored.Id, stored.OriginalName, stored.ContentType, stored.SizeBytes);
    }
}
