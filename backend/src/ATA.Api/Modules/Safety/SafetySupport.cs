using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Safety;

/// <summary>SignalR events of F12 on <c>/hubs/trips</c> (doc 09 §F12.8).</summary>
public interface ISafetyNotifier
{
    Task TripMessageAsync(Guid userId, TripMessageDto message, CancellationToken ct);

    Task TripMessagesReadAsync(Guid userId, TripMessagesReadEvent read, CancellationToken ct);

    Task SafetyCheckAsync(Guid userId, SafetyCheckEvent check, CancellationToken ct);

    Task SafetyCaseOpenedAsync(AdminSafetyCaseListItemDto safetyCase, CancellationToken ct);

    Task SafetyCaseUpdatedAsync(AdminSafetyCaseListItemDto safetyCase, CancellationToken ct);

    Task SafetyAlertRaisedAsync(AdminSafetyAlertDto alert, CancellationToken ct);
}

public static class SafetyHubEvents
{
    public const string TripMessage = "TripMessage";
    public const string TripMessagesRead = "TripMessagesRead";
    public const string SafetyCheck = "SafetyCheck";
    public const string SafetyCaseOpened = "SafetyCaseOpened";
    public const string SafetyCaseUpdated = "SafetyCaseUpdated";
    public const string SafetyAlertRaised = "SafetyAlertRaised";
}

public sealed class SignalRSafetyNotifier(IHubContext<TripsHub> hub, ILogger<SignalRSafetyNotifier> logger) : ISafetyNotifier
{
    public Task TripMessageAsync(Guid userId, TripMessageDto message, CancellationToken ct) => SendAsync(TripsHub.UserGroup(userId), SafetyHubEvents.TripMessage, message, ct);

    public Task TripMessagesReadAsync(Guid userId, TripMessagesReadEvent read, CancellationToken ct) => SendAsync(TripsHub.UserGroup(userId), SafetyHubEvents.TripMessagesRead, read, ct);

    public Task SafetyCheckAsync(Guid userId, SafetyCheckEvent check, CancellationToken ct) => SendAsync(TripsHub.UserGroup(userId), SafetyHubEvents.SafetyCheck, check, ct);

    public Task SafetyCaseOpenedAsync(AdminSafetyCaseListItemDto safetyCase, CancellationToken ct) => SendAsync(TripsHub.AdminsGroup, SafetyHubEvents.SafetyCaseOpened, safetyCase, ct);

    public Task SafetyCaseUpdatedAsync(AdminSafetyCaseListItemDto safetyCase, CancellationToken ct) => SendAsync(TripsHub.AdminsGroup, SafetyHubEvents.SafetyCaseUpdated, safetyCase, ct);

    public Task SafetyAlertRaisedAsync(AdminSafetyAlertDto alert, CancellationToken ct) => SendAsync(TripsHub.AdminsGroup, SafetyHubEvents.SafetyAlertRaised, alert, ct);

    private async Task SendAsync(string group, string method, object payload, CancellationToken ct)
    {
        try
        {
            await hub.Clients.Group(group).SendAsync(method, payload, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "SignalR {Method} to {Group} failed", method, group);
        }
    }
}

/// <summary>Arabic/English labels used in notification texts.</summary>
public static class SafetyLabels
{
    public static (string Ar, string En) Type(SafetyCaseType type) => type switch
    {
        SafetyCaseType.Sos => ("طوارئ SOS", "SOS"),
        SafetyCaseType.UnexpectedStop => ("توقف غير متوقع", "unexpected stop"),
        SafetyCaseType.RouteDeviation => ("انحراف عن المسار", "route deviation"),
        SafetyCaseType.TripOverrun => ("تجاوز مدة الرحلة", "trip overrun"),
        _ => ("بلاغ سلامة", "safety report"),
    };

    public static (string Ar, string En) AlertType(SafetyAlertType type) => type switch
    {
        SafetyAlertType.UnexpectedStop => ("توقفاً غير متوقع", "an unexpected stop"),
        SafetyAlertType.RouteDeviation => ("انحرافاً عن المسار", "a route deviation"),
        _ => ("تأخراً كبيراً في الوصول", "a much longer trip than expected"),
    };

    public static (string Ar, string En) Priority(SafetyPriority priority) => priority switch
    {
        SafetyPriority.Critical => ("حرج", "critical"),
        SafetyPriority.High => ("عالٍ", "high"),
        SafetyPriority.Medium => ("متوسط", "medium"),
        _ => ("منخفض", "low"),
    };

    public static (string Ar, string En) Status(SafetyCaseStatus status) => status switch
    {
        SafetyCaseStatus.Open => ("مفتوح", "open"),
        SafetyCaseStatus.InProgress => ("قيد المعالجة", "in progress"),
        SafetyCaseStatus.Escalated => ("تم التصعيد", "escalated"),
        _ => ("تم الحل", "resolved"),
    };

    public static (string Ar, string En) LostItemStatus(Domain.Safety.LostItemStatus status) => status switch
    {
        Domain.Safety.LostItemStatus.Open => ("مفتوح", "open"),
        Domain.Safety.LostItemStatus.DriverContacted => ("تم التواصل مع الكابتن", "driver contacted"),
        Domain.Safety.LostItemStatus.Found => ("عثر الكابتن على الغرض", "found by the driver"),
        Domain.Safety.LostItemStatus.Returned => ("تمت الإعادة", "returned"),
        Domain.Safety.LostItemStatus.NotFound => ("لم يُعثر عليه", "not found"),
        _ => ("مغلق", "closed"),
    };

    public static (string Ar, string En) ItemCategory(LostItemCategory category) => category switch
    {
        LostItemCategory.Phone => ("جوال", "phone"),
        LostItemCategory.Wallet => ("محفظة", "wallet"),
        LostItemCategory.Bag => ("حقيبة", "bag"),
        LostItemCategory.Keys => ("مفاتيح", "keys"),
        LostItemCategory.Documents => ("مستندات", "documents"),
        _ => ("غرض", "item"),
    };

    public static string Snake<TEnum>(TEnum value) where TEnum : struct, Enum => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    public static string? FirstName(string? fullName) => fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
}

/// <summary>Planned trip routes (<c>trips.planned_route</c>) and the geometry used by route-deviation detection.</summary>
public static class PlannedRoutes
{
    public static List<decimal[]> Straight(decimal pickupLat, decimal pickupLng, IEnumerable<(decimal Lat, decimal Lng)> stops, decimal dropoffLat, decimal dropoffLng)
    {
        var points = new List<decimal[]> { new[] { pickupLat, pickupLng } };
        points.AddRange(stops.Select(s => new[] { s.Lat, s.Lng }));
        points.Add([dropoffLat, dropoffLng]);
        return points;
    }

    public static string Serialize(IReadOnlyList<decimal[]> points) => JsonSerializer.Serialize(points);

    /// <summary>The stored route, or straight segments pickup → stops → dropoff when none was stored.</summary>
    public static List<decimal[]> Of(Trip trip, IEnumerable<TripStop> stops)
    {
        if (!string.IsNullOrWhiteSpace(trip.PlannedRoute))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<decimal[]>>(trip.PlannedRoute);
                if (parsed is { Count: >= 2 } && parsed.All(p => p.Length == 2))
                {
                    return parsed;
                }
            }
            catch (JsonException)
            {
            }
        }

        return Straight(trip.PickupLat, trip.PickupLng, stops.OrderBy(s => s.Sequence).Select(s => (s.Lat, s.Lng)), trip.DropoffLat, trip.DropoffLng);
    }

    /// <summary>Shortest distance in meters from a point to a polyline (equirectangular projection around the point).</summary>
    public static double DistanceToRouteMeters(double lat, double lng, IReadOnlyList<decimal[]> route)
    {
        if (route.Count == 0)
        {
            return 0;
        }

        const double metersPerDegree = 111_320d;
        var cos = Math.Cos(lat * Math.PI / 180d);
        (double X, double Y) Project(decimal[] p) => (((double)p[1] - lng) * metersPerDegree * cos, ((double)p[0] - lat) * metersPerDegree);

        if (route.Count == 1)
        {
            var (x, y) = Project(route[0]);
            return Math.Sqrt(x * x + y * y);
        }

        var best = double.MaxValue;
        for (var i = 0; i < route.Count - 1; i++)
        {
            var a = Project(route[i]);
            var b = Project(route[i + 1]);
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lengthSquared = dx * dx + dy * dy;
            var t = lengthSquared == 0 ? 0 : Math.Clamp(-(a.X * dx + a.Y * dy) / lengthSquared, 0, 1);
            var px = a.X + t * dx;
            var py = a.Y + t * dy;
            best = Math.Min(best, Math.Sqrt(px * px + py * py));
        }

        return best;
    }

    /// <summary>Keeps at most <paramref name="max"/> points (evenly spaced, always including the last one).</summary>
    public static List<decimal[]> Downsample(IReadOnlyList<decimal[]> points, int max)
    {
        if (points.Count <= max)
        {
            return points.ToList();
        }

        var step = (double)(points.Count - 1) / (max - 1);
        var result = new List<decimal[]>(max);
        for (var i = 0; i < max; i++)
        {
            result.Add(points[(int)Math.Round(i * step)]);
        }

        return result;
    }
}

/// <summary>Creates safety cases (numbering, system notes, ops fan-out) and builds the admin list rows used by the hub events.</summary>
public sealed class SafetyCaseFactory(
    AtaDbContext db,
    INotificationDispatcher notifications,
    ISafetyNotifier realtime,
    IClock clock,
    IOptions<SafetyOptions> options)
{
    private const int NumberRetries = 3;
    private readonly List<SafetyCase> _opened = [];
    private readonly List<Guid> _updated = [];

    /// <summary>Adds the case and its first system note to the unit of work, notifies operations when asked (push + hotline SMS), and saves.</summary>
    public async Task<SafetyCase> OpenAsync(SafetyCase safetyCase, string systemNote, bool notifyOps, string? userName, CancellationToken ct)
    {
        var now = clock.UtcNow;
        safetyCase.OpenedAt = safetyCase.OpenedAt == default ? now : safetyCase.OpenedAt;
        safetyCase.CaseNumber = await NextNumberAsync(now, 0, ct);
        db.SafetyCases.Add(safetyCase);
        AddNote(safetyCase.Id, null, SafetyNoteKind.System, systemNote);
        if (notifyOps)
        {
            await NotifyOpsAsync(safetyCase, userName, ct);
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException) when (attempt < NumberRetries)
            {
                safetyCase.CaseNumber = await NextNumberAsync(now, attempt + 1, ct);
            }
        }

        _opened.Add(safetyCase);
        return safetyCase;
    }

    /// <summary>Adds the case without saving (the caller saves in its own unit of work, then calls <see cref="PublishAsync"/>).</summary>
    public async Task<SafetyCase> AddAsync(SafetyCase safetyCase, string systemNote, bool notifyOps, string? userName, CancellationToken ct)
    {
        var now = clock.UtcNow;
        safetyCase.OpenedAt = safetyCase.OpenedAt == default ? now : safetyCase.OpenedAt;
        safetyCase.CaseNumber = await NextNumberAsync(now, db.SafetyCases.Local.Count(c => c.CaseNumber.StartsWith($"SC-{now:yyyyMMdd}-")), ct);
        db.SafetyCases.Add(safetyCase);
        AddNote(safetyCase.Id, null, SafetyNoteKind.System, systemNote);
        if (notifyOps)
        {
            await NotifyOpsAsync(safetyCase, userName, ct);
        }

        _opened.Add(safetyCase);
        return safetyCase;
    }

    public SafetyCaseNote AddNote(Guid caseId, Guid? authorUserId, SafetyNoteKind kind, string body, bool isInternal = true)
    {
        var note = new SafetyCaseNote
        {
            CaseId = caseId, AuthorUserId = authorUserId, Kind = kind, Body = body.Length > 2000 ? body[..2000] : body, IsInternal = isInternal,
            CreatedAt = NoteTimestamp(),
        };
        db.SafetyCaseNotes.Add(note);
        return note;
    }

    /// <summary>Marks a case as changed so <see cref="PublishAsync"/> pushes <c>SafetyCaseUpdated</c>.</summary>
    public void Touch(Guid caseId) => _updated.Add(caseId);

    /// <summary>Pushes <c>SafetyCaseOpened</c> / <c>SafetyCaseUpdated</c> for what changed since the last call; call after saving.</summary>
    public async Task PublishAsync(CancellationToken ct)
    {
        var opened = _opened.ToList();
        var updated = _updated.Distinct().Except(opened.Select(c => c.Id)).ToList();
        _opened.Clear();
        _updated.Clear();
        if (opened.Count > 0)
        {
            foreach (var row in await ListItemsAsync(opened.Select(c => c.Id).ToList(), ct))
            {
                await realtime.SafetyCaseOpenedAsync(row, ct);
            }
        }

        if (updated.Count > 0)
        {
            foreach (var row in await ListItemsAsync(updated, ct))
            {
                await realtime.SafetyCaseUpdatedAsync(row, ct);
            }
        }
    }

    /// <summary>
    /// <c>safety.alert</c> push to every on-duty, active admin holding <c>safety.manage</c>, plus an SMS to each <c>Safety:OpsHotlinePhones</c> number.
    /// </summary>
    public async Task NotifyOpsAsync(SafetyCase safetyCase, string? userName, CancellationToken ct)
    {
        var (typeAr, typeEn) = SafetyLabels.Type(safetyCase.Type);
        var (priorityAr, priorityEn) = SafetyLabels.Priority(safetyCase.Priority);
        Dictionary<string, string?> Placeholders() => NotificationPlaceholders.Of(("caseNumber", safetyCase.CaseNumber), ("userName", userName ?? string.Empty))
            .Localized("type", typeAr, typeEn).Localized("priority", priorityAr, priorityEn);
        var extra = new Dictionary<string, object?> { ["caseId"] = safetyCase.Id, ["tripId"] = safetyCase.TripId, ["priority"] = SafetyLabels.Snake(safetyCase.Priority) };

        var agents = await db.AdminAccounts.AsNoTracking().Where(a => a.OnDuty && a.IsActive).Select(a => new { a.UserId, a.Permissions }).ToListAsync(ct);
        foreach (var agent in agents.Where(a => Permissions.Grants(a.Permissions, Permissions.SafetyManage)))
        {
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SafetyAlert, agent.UserId, Placeholders(), "case", safetyCase.Id, extra), ct);
        }

        foreach (var phone in options.Value.OpsHotlinePhones.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
        {
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SafetyAlert, Guid.Empty, Placeholders(), "case", safetyCase.Id, extra,
                RecipientPhoneOverride: phone.Trim(), ExtraChannels: [NotificationChannel.Sms]), ct);
        }
    }

    public async Task<IReadOnlyList<AdminSafetyCaseListItemDto>> ListItemsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var cases = await db.SafetyCases.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        return await ToListItemsAsync(cases, ct);
    }

    public async Task<IReadOnlyList<AdminSafetyCaseListItemDto>> ToListItemsAsync(IReadOnlyList<SafetyCase> cases, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var tripIds = cases.Where(c => c.TripId != null).Select(c => c.TripId!.Value).Distinct().ToList();
        var userIds = cases.SelectMany(c => new[] { c.ReporterUserId, c.AssignedToUserId }).Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
        var numbers = await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TripNumber, ct);
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.PhoneNumber, ct);
        return cases.Select(c => new AdminSafetyCaseListItemDto(
            c.Id, c.CaseNumber, c.Type, c.Source, c.Priority, c.Status, c.TripId, c.TripId is { } t ? numbers.GetValueOrDefault(t) : null,
            c.ReporterUserId is { } r ? names.GetValueOrDefault(r) : null, c.ReporterRole, c.AssignedToUserId is { } a ? names.GetValueOrDefault(a) : null,
            c.OpenedAt, c.FirstResponseAt, (int)Math.Max(0, ((c.ResolvedAt ?? now) - c.OpenedAt).TotalSeconds), c.LastLat, c.LastLng)).ToList();
    }

    private async Task<string> NextNumberAsync(DateTime now, int offset, CancellationToken ct) =>
        await SequenceNumbers.NextAsync(db.SafetyCases.Select(c => c.CaseNumber), $"SC-{now:yyyyMMdd}-", 4, offset, ct);

    private static long _lastNoteTicks;

    /// <summary>Monotonic note timestamps so the timeline keeps insertion order within one clock tick.</summary>
    private DateTime NoteTimestamp()
    {
        var now = clock.UtcNow.Ticks;
        while (true)
        {
            var last = Interlocked.Read(ref _lastNoteTicks);
            var next = Math.Max(now, last + TimeSpan.TicksPerMicrosecond);
            if (Interlocked.CompareExchange(ref _lastNoteTicks, next, last) == last)
            {
                return new DateTime(next, DateTimeKind.Utc);
            }
        }
    }
}
