using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Notifications;

/// <summary>Admin console for F13: event catalogue, templates (validated against the catalogue), delivery log with retry, on-duty flag.</summary>
public sealed class NotificationAdminService(
    AtaDbContext db,
    NotificationTemplateCache cache,
    INotificationDispatcher dispatcher,
    DeliveryQueue queue,
    AuditService audit,
    ICurrentUser currentUser)
{
    public IReadOnlyList<NotificationEventDto> Events() =>
        NotificationEvents.All.Select(e => new NotificationEventDto(e.Code, e.Category, e.IsCritical, e.Recipients, e.DefaultChannels, e.AllowedChannels, e.Placeholders, e.DeepLink)).ToList();

    public async Task<IReadOnlyList<NotificationTemplateDto>> ListTemplatesAsync(string? code, NotificationChannel? channel, bool? isActive, CancellationToken ct)
    {
        var query = db.NotificationTemplates.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(t => t.Code == code);
        if (channel is not null) query = query.Where(t => t.Channel == channel);
        if (isActive is not null) query = query.Where(t => t.IsActive == isActive);
        var rows = await query.OrderBy(t => t.Code).ThenBy(t => t.Channel).ToListAsync(ct);
        return await ToDtosAsync(rows, ct);
    }

    public async Task<NotificationTemplateDto> CreateTemplateAsync(CreateTemplateRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Code), request.Code, 60)
            .Require(nameof(request.Channel), request.Channel)
            .Require(nameof(request.BodyAr), request.BodyAr, 1000)
            .Require(nameof(request.BodyEn), request.BodyEn, 1000)
            .ThrowIfInvalid();
        var definition = NotificationEvents.Find(request.Code!.Trim()) ?? throw new DomainException(ErrorCodes.UnknownEventCode, new { code = request.Code });
        new Validator().Rule(nameof(request.Channel), definition.AllowedChannels.Contains(request.Channel!.Value),
            "must be one of: " + string.Join('|', definition.AllowedChannels.Select(c => c.ToString().ToLowerInvariant()))).ThrowIfInvalid();
        ValidateTexts(definition, request.Channel.Value, request.TitleAr, request.TitleEn, request.BodyAr!, request.BodyEn!);
        if (await db.NotificationTemplates.AnyAsync(t => t.Code == definition.Code && t.Channel == request.Channel, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { code = definition.Code, channel = request.Channel });
        }

        var template = new NotificationTemplate
        {
            Code = definition.Code, Channel = request.Channel.Value, TitleAr = Clean(request.TitleAr), TitleEn = Clean(request.TitleEn),
            BodyAr = request.BodyAr!.Trim(), BodyEn = request.BodyEn!.Trim(), IsActive = request.IsActive ?? true, UpdatedBy = currentUser.UserId,
        };
        db.NotificationTemplates.Add(template);
        audit.Log("notification_template.create", "notification_template", template.Id, null, Snapshot(template));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return (await ToDtosAsync([template], ct))[0];
    }

    public async Task<NotificationTemplateDto> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.BodyAr), request.BodyAr, 1000)
            .Require(nameof(request.BodyEn), request.BodyEn, 1000)
            .ThrowIfInvalid();
        var template = Guard.NotFound(await db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, ct));
        var definition = NotificationEvents.Find(template.Code) ?? throw new DomainException(ErrorCodes.UnknownEventCode, new { code = template.Code });
        ValidateTexts(definition, template.Channel, request.TitleAr, request.TitleEn, request.BodyAr!, request.BodyEn!);
        var before = Snapshot(template);
        template.TitleAr = Clean(request.TitleAr);
        template.TitleEn = Clean(request.TitleEn);
        template.BodyAr = request.BodyAr!.Trim();
        template.BodyEn = request.BodyEn!.Trim();
        template.IsActive = request.IsActive ?? template.IsActive;
        template.UpdatedBy = currentUser.UserId;
        audit.Log("notification_template.update", "notification_template", template.Id, before, Snapshot(template));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return (await ToDtosAsync([template], ct))[0];
    }

    public async Task<TemplatePreviewDto> PreviewAsync(Guid id, PreviewTemplateRequest request, CancellationToken ct)
    {
        var template = Guard.NotFound(await db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct));
        var language = request.Language == "en" ? "en" : "ar";
        var values = new Dictionary<string, string?>(request.Placeholders ?? [], StringComparer.Ordinal);
        var title = template.Channel == NotificationChannel.Sms ? null : TemplateRenderer.Render(language == "en" ? template.TitleEn : template.TitleAr, values, language);
        var body = TemplateRenderer.Render(language == "en" ? template.BodyEn : template.BodyAr, values, language);
        return new TemplatePreviewDto(title, body, body.Length, TemplateRenderer.SmsSegments(body));
    }

    /// <summary>Sends the event to one user for real (with <c>data.test = true</c>), with sample placeholder values.</summary>
    public async Task SendTestAsync(Guid id, TestTemplateRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.UserId), request.UserId).ThrowIfInvalid();
        var template = Guard.NotFound(await db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct));
        var user = Guard.NotFound(await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, ct));
        var definition = NotificationEvents.Find(template.Code)!;
        var values = definition.Placeholders.ToDictionary(p => p, p => (string?)$"[{p}]", StringComparer.Ordinal);
        await dispatcher.DispatchAsync(new NotificationRequest(definition.Code, user.Id, values, IsTest: true,
            ExtraChannels: [template.Channel]), ct);
        audit.Log("notification_template.test", "notification_template", template.Id, null, new { userId = user.Id });
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<DeliveryDto>> ListDeliveriesAsync(Guid? userId, string? eventCode, NotificationChannel? channel, DeliveryStatus? status, Guid? campaignId,
        DateOnly? from, DateOnly? to, Paging paging, CancellationToken ct)
    {
        var query = db.NotificationDeliveries.AsNoTracking().AsQueryable();
        if (userId is not null) query = query.Where(d => d.UserId == userId);
        if (!string.IsNullOrWhiteSpace(eventCode)) query = query.Where(d => d.EventCode == eventCode);
        if (channel is not null) query = query.Where(d => d.Channel == channel);
        if (status is not null) query = query.Where(d => d.Status == status);
        if (campaignId is not null) query = query.Where(d => d.CampaignId == campaignId);
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(d => d.CreatedAt >= fromAt);
        }

        if (to is { } t)
        {
            var toAt = t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(d => d.CreatedAt < toAt);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(d => d.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var userIds = rows.Where(d => d.UserId != null).Select(d => d.UserId!.Value).Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.PhoneNumber }, ct);
        var items = rows.Select(d =>
        {
            var user = d.UserId is { } uid ? users.GetValueOrDefault(uid) : null;
            var phone = d.PhoneNumber ?? user?.PhoneNumber;
            return new DeliveryDto(d.Id, d.EventCode, d.Channel, d.Status, d.SkippedReason, d.UserId, d.CampaignId, user?.FullName, phone is null ? null : PhoneMasking.Mask(phone),
                d.Provider, d.ProviderMessageId, d.ErrorCode, d.ErrorMessage, d.Attempts, d.SentAt, d.OpenedAt, d.CreatedAt, JsonSerializer.Deserialize<JsonElement>(d.Payload));
        }).ToList();
        return paging.Result(items, total);
    }

    public async Task RetryDeliveryAsync(Guid id, CancellationToken ct)
    {
        var delivery = Guard.NotFound(await db.NotificationDeliveries.FirstOrDefaultAsync(d => d.Id == id, ct));
        if (delivery.Status is not (DeliveryStatus.Failed or DeliveryStatus.Skipped))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = delivery.Status });
        }

        var before = new { delivery.Status, delivery.Attempts };
        delivery.Status = DeliveryStatus.Queued;
        delivery.SkippedReason = null;
        delivery.NextAttemptAt = null;
        audit.Log("notification_delivery.retry", "notification_delivery", delivery.Id, before, new { delivery.Status });
        await db.SaveChangesAsync(ct);
        queue.Enqueue(delivery.Id);
    }

    public async Task<DutyDto> GetDutyAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var account = await db.AdminAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        return new DutyDto(account.OnDuty);
    }

    public async Task<DutyDto> SetDutyAsync(DutyDto request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var account = await db.AdminAccounts.FirstOrDefaultAsync(a => a.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var before = new { account.OnDuty };
        account.OnDuty = request.OnDuty;
        audit.Log("admin.duty", "admin_account", account.Id, before, new { account.OnDuty });
        await db.SaveChangesAsync(ct);
        return new DutyDto(account.OnDuty);
    }

    /// <summary>Titles are required except for SMS; every <c>{placeholder}</c> must belong to the event (<c>422 template_placeholder_invalid</c>).</summary>
    private static void ValidateTexts(NotificationEventDefinition definition, NotificationChannel channel, string? titleAr, string? titleEn, string bodyAr, string bodyEn)
    {
        var v = new Validator()
            .Rule(nameof(titleAr), channel == NotificationChannel.Sms || !string.IsNullOrWhiteSpace(titleAr), "required")
            .Rule(nameof(titleEn), channel == NotificationChannel.Sms || !string.IsNullOrWhiteSpace(titleEn), "required")
            .Rule(nameof(titleAr), titleAr is null || titleAr.Length <= 120, "max_length:120")
            .Rule(nameof(titleEn), titleEn is null || titleEn.Length <= 120, "max_length:120");
        v.ThrowIfInvalid();
        var used = new[] { titleAr, titleEn, bodyAr, bodyEn }.SelectMany(TemplateRenderer.PlaceholdersIn).Distinct();
        var unknown = used.Where(p => !definition.Placeholders.Contains(p)).ToList();
        if (unknown.Count > 0)
        {
            throw new DomainException(ErrorCodes.TemplatePlaceholderInvalid, new { unknownPlaceholders = unknown, allowed = definition.Placeholders });
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<IReadOnlyList<NotificationTemplateDto>> ToDtosAsync(IReadOnlyList<NotificationTemplate> rows, CancellationToken ct)
    {
        var ids = rows.Where(t => t.UpdatedBy != null).Select(t => t.UpdatedBy!.Value).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        return rows.Select(t => new NotificationTemplateDto(t.Id, t.Code, t.Channel, t.TitleAr, t.TitleEn, t.BodyAr, t.BodyEn, t.IsActive, t.UpdatedAt,
            t.UpdatedBy is { } by ? names.GetValueOrDefault(by) : null)).ToList();
    }

    private static object Snapshot(NotificationTemplate t) => new { t.Code, t.Channel, t.TitleAr, t.TitleEn, t.BodyAr, t.BodyEn, t.IsActive };
}
