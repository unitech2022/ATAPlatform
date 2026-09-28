using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Safety;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Safety;

/// <summary>Trusted contacts of the signed-in user (at most <see cref="TrustedContact.MaxPerUser"/>).</summary>
public sealed class TrustedContactService(AtaDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<TrustedContactDto>> ListAsync(CancellationToken ct) => await ListForUserAsync(currentUser.UserId, ct);

    public async Task<IReadOnlyList<TrustedContactDto>> ListForUserAsync(Guid userId, CancellationToken ct) =>
        (await db.TrustedContacts.AsNoTracking().Where(c => c.UserId == userId).OrderBy(c => c.CreatedAt).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<TrustedContactDto> CreateAsync(TrustedContactRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var phone = await ValidateAsync(request, userId, ct);
        if (await db.TrustedContacts.CountAsync(c => c.UserId == userId, ct) >= TrustedContact.MaxPerUser)
        {
            throw new DomainException(ErrorCodes.TrustedContactsLimit, new { limit = TrustedContact.MaxPerUser });
        }

        if (await db.TrustedContacts.AnyAsync(c => c.UserId == userId && c.PhoneNumber == phone, ct))
        {
            throw new DomainException(ErrorCodes.TrustedContactExists);
        }

        var contact = new TrustedContact
        {
            UserId = userId, Name = request.Name!.Trim(), PhoneNumber = phone, Relationship = Clean(request.Relationship),
            AutoShare = request.AutoShare ?? false, NotifyOnSos = request.NotifyOnSos ?? true,
        };
        db.TrustedContacts.Add(contact);
        await db.SaveChangesAsync(ct);
        return ToDto(contact);
    }

    public async Task<TrustedContactDto> UpdateAsync(Guid id, TrustedContactRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var contact = Guard.NotFound(await db.TrustedContacts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct));
        var phone = await ValidateAsync(request, userId, ct);
        if (phone != contact.PhoneNumber && await db.TrustedContacts.AnyAsync(c => c.UserId == userId && c.PhoneNumber == phone && c.Id != id, ct))
        {
            throw new DomainException(ErrorCodes.TrustedContactExists);
        }

        contact.Name = request.Name!.Trim();
        contact.PhoneNumber = phone;
        contact.Relationship = Clean(request.Relationship);
        contact.AutoShare = request.AutoShare ?? contact.AutoShare;
        contact.NotifyOnSos = request.NotifyOnSos ?? contact.NotifyOnSos;
        await db.SaveChangesAsync(ct);
        return ToDto(contact);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var contact = Guard.NotFound(await db.TrustedContacts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct));
        db.TrustedContacts.Remove(contact);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> ValidateAsync(TrustedContactRequest request, Guid userId, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Name), request.Name, 80)
            .Require(nameof(request.PhoneNumber), request.PhoneNumber, 20)
            .Rule(nameof(request.Relationship), request.Relationship is null || request.Relationship.Length <= 40, "max_length:40")
            .ThrowIfInvalid();
        var phone = PhoneNumber.Normalize(request.PhoneNumber);
        var own = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.PhoneNumber).FirstOrDefaultAsync(ct);
        new Validator().Rule(nameof(request.PhoneNumber), phone != own, "self").ThrowIfInvalid();
        return phone;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static TrustedContactDto ToDto(TrustedContact c) => new(c.Id, c.Name, c.PhoneNumber, c.Relationship, c.AutoShare, c.NotifyOnSos, c.CreatedAt);
}
