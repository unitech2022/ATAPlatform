using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Identity;

/// <summary>The authenticated user's own account: profile, preferences, devices and deletion.</summary>
public sealed class MeService(AtaDbContext db, ICurrentUser currentUser, AuthService auth, IClock clock)
{
    public async Task<MeResponse> GetAsync(CancellationToken ct)
    {
        var user = await LoadUserAsync(ct);
        var passenger = await db.Passengers.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == user.Id, ct);

        return new MeResponse(
            UserDto.From(user),
            passenger is null ? null : new MePassengerDto(passenger.RatingAvg, passenger.RatingCount, passenger.PreferFemaleDriver, passenger.DefaultPaymentMethod, user.CreatedAt.Year.ToString()),
            driver is null ? null : new MeDriverDto(driver.ApplicationNumber, driver.ApplicationStatus, driver.IsOnline, driver.Tier));
    }

    public async Task<UserDto> UpdateAsync(UpdateMeRequest request, CancellationToken ct)
    {
        new Validator()
            .Rule(nameof(request.FullName), request.FullName is null || request.FullName.Trim().Length is >= 2 and <= 120, "length must be 2-120")
            .Rule(nameof(request.Gender), request.Gender is null or Gender.Male or Gender.Female, "must be male or female")
            .ThrowIfInvalid();

        var user = await LoadUserAsync(ct);
        if (request.FullName is not null) user.FullName = request.FullName.Trim();
        if (request.Language is not null) user.Language = request.Language.Value;
        if (request.Gender is not null) user.Gender = request.Gender.Value;
        if (request.AcceptTerms == true) user.TermsAcceptedAt ??= clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return UserDto.From(user);
    }

    public async Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(CancellationToken ct)
    {
        var pref = await GetOrCreatePreferencesAsync(ct);
        await db.SaveChangesAsync(ct);
        return new NotificationPreferencesDto(pref.Trips, pref.Wallet, pref.Safety, pref.Offers);
    }

    public async Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(NotificationPreferencesDto request, CancellationToken ct)
    {
        var pref = await GetOrCreatePreferencesAsync(ct);
        pref.Trips = request.Trips;
        pref.Wallet = request.Wallet;
        pref.Safety = request.Safety;
        pref.Offers = request.Offers;
        await db.SaveChangesAsync(ct);
        return new NotificationPreferencesDto(pref.Trips, pref.Wallet, pref.Safety, pref.Offers);
    }

    public async Task RegisterDeviceAsync(RegisterDeviceRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.DeviceId), request.DeviceId, 128)
            .Require(nameof(request.Platform), request.Platform)
            .ThrowIfInvalid();

        var userId = currentUser.UserId;
        var device = await db.UserDevices.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == request.DeviceId, ct);
        if (device is null)
        {
            device = new UserDevice { UserId = userId, DeviceId = request.DeviceId! };
            db.UserDevices.Add(device);
        }

        device.Platform = request.Platform!.Value;
        device.DeviceName = request.DeviceName;
        device.PushToken = request.PushToken ?? device.PushToken;
        device.AppVersion = request.AppVersion ?? device.AppVersion;
        device.LastSeenAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Deactivates the account, schedules deletion and revokes every refresh token.</summary>
    public async Task DeleteAccountAsync(CancellationToken ct)
    {
        var user = await LoadUserAsync(ct);
        var now = clock.UtcNow;
        user.Status = UserStatus.Deleted;
        user.DeletedAt = now;
        await db.Drivers.Where(d => d.UserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(d => d.IsOnline, false), ct);
        await auth.RevokeAllAsync(user.Id, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<User> LoadUserAsync(CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct) ?? throw new DomainException(ErrorCodes.Unauthorized);
        user.EnsureActive();
        return user;
    }

    private async Task<NotificationPreference> GetOrCreatePreferencesAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var pref = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (pref is null)
        {
            pref = new NotificationPreference { UserId = userId };
            db.NotificationPreferences.Add(pref);
        }

        return pref;
    }
}
