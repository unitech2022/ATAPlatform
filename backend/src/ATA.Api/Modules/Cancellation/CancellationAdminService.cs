using System.Text.Json;
using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Cancellation;

/// <summary>Admin console of F14 (doc 09 §F14.4 "الإدارة"): reasons, rules, thresholds, events, excuse reviews, reliability profiles and KPIs. Every write is audited.</summary>
public sealed partial class CancellationAdminService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    AuditService audit,
    CancellationEngine engine,
    ReliabilityService reliability,
    TripCancellationService tripCancellations,
    TripReadService reads,
    RefundService refunds,
    ZoneResolver zones,
    Corporate.CorporateCreditService corporateCredit,
    IOptions<CancellationOptions> options)
{
    [GeneratedRegex("^[a-z0-9_]{2,60}$")]
    private static partial Regex CodePattern();

    // ----- reasons -----

    public async Task<IReadOnlyList<AdminCancellationReasonDto>> ReasonsAsync(CancellationActor? actor, CancellationToken ct)
    {
        var query = db.CancellationReasons.AsNoTracking().AsQueryable();
        if (actor is not null) query = query.Where(r => r.Actor == actor);
        return (await query.OrderBy(r => r.Actor).ThenBy(r => r.SortOrder).ThenBy(r => r.Code).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<AdminCancellationReasonDto> CreateReasonAsync(CancellationReasonRequest request, CancellationToken ct)
    {
        ValidateReason(request);
        var code = request.Code!.Trim();
        if (await db.CancellationReasons.AnyAsync(r => r.Actor == request.Actor && r.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { code });
        }

        var reason = new CancellationReason { Code = code, NameAr = string.Empty, NameEn = string.Empty };
        Apply(reason, request);
        db.CancellationReasons.Add(reason);
        audit.Log("cancellation_reason.create", "cancellation_reason", reason.Id, null, ToDto(reason));
        await db.SaveChangesAsync(ct);
        return ToDto(reason);
    }

    public async Task<AdminCancellationReasonDto> UpdateReasonAsync(Guid id, CancellationReasonRequest request, CancellationToken ct)
    {
        ValidateReason(request);
        var reason = Guard.NotFound(await db.CancellationReasons.FirstOrDefaultAsync(r => r.Id == id, ct));
        var code = request.Code!.Trim();
        if (await db.CancellationReasons.AnyAsync(r => r.Id != id && r.Actor == request.Actor && r.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { code });
        }

        var before = ToDto(reason);
        reason.Code = code;
        Apply(reason, request);
        audit.Log("cancellation_reason.update", "cancellation_reason", reason.Id, before, ToDto(reason));
        await db.SaveChangesAsync(ct);
        return ToDto(reason);
    }

    /// <summary>Deletes an unused reason; a reason already referenced by events is deactivated instead.</summary>
    public async Task DeleteReasonAsync(Guid id, CancellationToken ct)
    {
        var reason = Guard.NotFound(await db.CancellationReasons.FirstOrDefaultAsync(r => r.Id == id, ct));
        var used = await db.CancellationEvents.AnyAsync(e => e.ReasonId == id, ct);
        audit.Log("cancellation_reason.delete", "cancellation_reason", reason.Id, ToDto(reason), new { deactivated = used });
        if (used)
        {
            reason.IsActive = false;
        }
        else
        {
            db.CancellationReasons.Remove(reason);
        }

        await db.SaveChangesAsync(ct);
    }

    // ----- rules -----

    public async Task<IReadOnlyList<CancellationRuleDto>> RulesAsync(CancellationActor? actor, CancellationStage? stage, BookingType? bookingType, CancellationToken ct)
    {
        var query = db.CancellationRules.AsNoTracking().AsQueryable();
        if (actor is not null) query = query.Where(r => r.Actor == actor);
        if (stage is not null) query = query.Where(r => r.Stage == stage);
        if (bookingType is not null) query = query.Where(r => r.BookingType == bookingType);
        return (await query.ToListAsync(ct)).OrderBy(r => r.Actor).ThenBy(r => r.Stage).ThenByDescending(r => r.Priority).ThenBy(r => r.CreatedAt).Select(ToDto).ToList();
    }

    public async Task<CancellationRuleDto> CreateRuleAsync(CancellationRuleRequest request, CancellationToken ct)
    {
        await ValidateRuleAsync(request, ct);
        var rule = new CancellationRule { Name = string.Empty };
        Apply(rule, request);
        db.CancellationRules.Add(rule);
        audit.Log("cancellation_rule.create", "cancellation_rule", rule.Id, null, ToDto(rule));
        await db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task<CancellationRuleDto> UpdateRuleAsync(Guid id, CancellationRuleRequest request, CancellationToken ct)
    {
        await ValidateRuleAsync(request, ct);
        var rule = Guard.NotFound(await db.CancellationRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        var before = ToDto(rule);
        Apply(rule, request);
        audit.Log("cancellation_rule.update", "cancellation_rule", rule.Id, before, ToDto(rule));
        await db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.CancellationRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        audit.Log("cancellation_rule.delete", "cancellation_rule", rule.Id, ToDto(rule), null);
        db.CancellationRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }

    public async Task<SimulateCancellationResult> SimulateAsync(SimulateCancellationRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Actor), request.Actor)
            .Rule(nameof(request.Actor), request.Actor is null or CancellationActor.Passenger or CancellationActor.Driver, "must be passenger|driver")
            .Require(nameof(request.Stage), request.Stage)
            .Require(nameof(request.SecondsSinceAnchor), request.SecondsSinceAnchor)
            .Rule(nameof(request.SecondsSinceAnchor), request.SecondsSinceAnchor is null or >= 0, "must be positive")
            .Require(nameof(request.EstimatedFare), request.EstimatedFare)
            .Rule(nameof(request.EstimatedFare), request.EstimatedFare is null or >= 0, "must be positive")
            .ThrowIfInvalid();
        var actor = request.Actor!.Value;
        var stage = request.Stage!.Value;
        var now = clock.UtcNow;
        var rules = await db.CancellationRules.AsNoTracking().Where(r => r.IsActive && r.Actor == actor && r.Stage == stage).ToListAsync(ct);
        var rule = CancellationMath.SelectRule(rules, actor, stage, request.BookingType ?? BookingType.Now, request.RideCategoryId ?? Guid.Empty, request.ZoneId);
        var pricingFee = 0m;
        if (rule?.FeeType == CancellationFeeType.PricingRule && request.RideCategoryId is { } categoryId)
        {
            var zoneId = request.ZoneId;
            pricingFee = (await db.PricingRules.AsNoTracking()
                    .Where(r => r.RideCategoryId == categoryId && r.IsActive && (r.ZoneId == null || r.ZoneId == zoneId) && r.EffectiveFrom <= now && (r.EffectiveTo == null || r.EffectiveTo > now))
                    .ToListAsync(ct))
                .OrderByDescending(r => r.ZoneId != null).ThenByDescending(r => r.Priority).Select(r => r.CancellationFee).FirstOrDefault();
        }

        var outcome = CancellationMath.Calculate(rule, now.AddSeconds(-request.SecondsSinceAnchor!.Value), now, request.EstimatedFare!.Value, pricingFee);
        var compensation = actor == CancellationActor.Passenger ? CancellationMath.Compensation(outcome.Fee, rule) : 0m;
        return new SimulateCancellationResult(rule?.Id, rule?.Name, outcome.Fee, compensation, outcome.PenaltyPoints, outcome.IsFree);
    }

    // ----- thresholds -----

    public async Task<IReadOnlyList<ReliabilityThresholdDto>> ThresholdsAsync(Role? role, CancellationToken ct)
    {
        var query = db.ReliabilityThresholds.AsNoTracking().AsQueryable();
        if (role is not null) query = query.Where(t => t.Role == role);
        return (await query.ToListAsync(ct)).OrderBy(t => t.Role).ThenBy(t => t.SortOrder).Select(ToDto).ToList();
    }

    public async Task<ReliabilityThresholdDto> UpdateThresholdAsync(Guid id, ReliabilityThresholdRequest request, CancellationToken ct)
    {
        new Validator()
            .Rule(nameof(request.MinPenaltyPoints), request.MinPenaltyPoints is null or >= 0, "must be positive")
            .Rule(nameof(request.MinCancellationRate), request.MinCancellationRate is null or (>= 0 and <= 1), "must be between 0 and 1")
            .Rule(nameof(request.MinTripsForRate), request.MinTripsForRate is null or >= 0, "must be positive")
            .Rule(nameof(request.RestrictionHours), request.RestrictionHours is null or > 0, "must be positive")
            .Rule(nameof(request.DeprioritizeFactor), request.DeprioritizeFactor is null or (> 0 and <= 1), "must be in (0, 1]")
            .Rule(nameof(request.IncentiveReductionPercent), request.IncentiveReductionPercent is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(request.MinPenaltyPoints), request.MinPenaltyPoints is not null || request.MinCancellationRate is not null, "a points or rate trigger is required")
            .ThrowIfInvalid();
        var threshold = Guard.NotFound(await db.ReliabilityThresholds.FirstOrDefaultAsync(t => t.Id == id, ct));
        new Validator().Rule(nameof(request.RestrictionHours), threshold.Level != RestrictionLevel.TemporarilyRestricted || (request.RestrictionHours ?? threshold.RestrictionHours) is > 0, "required for temporarily_restricted").ThrowIfInvalid();
        var before = ToDto(threshold);
        threshold.MinPenaltyPoints = request.MinPenaltyPoints;
        threshold.MinCancellationRate = request.MinCancellationRate;
        threshold.MinTripsForRate = request.MinTripsForRate ?? threshold.MinTripsForRate;
        threshold.RestrictionHours = request.RestrictionHours ?? (threshold.Level == RestrictionLevel.TemporarilyRestricted ? threshold.RestrictionHours : null);
        threshold.DeprioritizeFactor = request.DeprioritizeFactor;
        threshold.IncentiveReductionPercent = request.IncentiveReductionPercent;
        threshold.SortOrder = request.SortOrder ?? threshold.SortOrder;
        threshold.IsActive = request.IsActive ?? threshold.IsActive;
        audit.Log("reliability_threshold.update", "reliability_threshold", threshold.Id, before, ToDto(threshold));
        await db.SaveChangesAsync(ct);
        return ToDto(threshold);
    }

    // ----- events & excuses -----

    public async Task<PagedResult<AdminCancellationEventDto>> EventsAsync(TripActor? actor, CancellationStage? stage, AtFault? atFault, CancellationFeeStatus? feeStatus, ExcuseStatus? excuseStatus,
        DateOnly? from, DateOnly? to, string? search, Paging paging, Language lang, CancellationToken ct)
    {
        var query = FilterEvents(actor, stage, atFault, feeStatus, excuseStatus, from, to, search);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(e => e.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await EventDtosAsync(rows, lang, ct), total);
    }

    public async Task<PagedResult<ExcuseQueueItemDto>> ExcusesAsync(ExcuseStatus? status, Paging paging, Language lang, CancellationToken ct)
    {
        var filter = status ?? ExcuseStatus.Pending;
        var query = db.CancellationEvents.AsNoTracking().Where(e => e.ExcuseStatus == filter);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(e => e.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var dtos = await EventDtosAsync(rows, lang, ct);
        var ruleIds = rows.Where(r => r.RuleId != null).Select(r => r.RuleId!.Value).Distinct().ToList();
        var rules = await db.CancellationRules.AsNoTracking().Where(r => ruleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);
        var now = clock.UtcNow;
        var sla = options.Value.ExcuseReviewSlaHours;
        var items = dtos.Zip(rows, (d, e) =>
        {
            var age = Math.Round((now - e.CreatedAt).TotalHours, 1);
            var pending = CancellationEngine.PendingPoints(e, e.RuleId is { } rid ? rules.GetValueOrDefault(rid) : null);
            return new ExcuseQueueItemDto(d.Id, d.TripId, d.TripNumber, d.Actor, d.UserId, d.UserName, d.AtFault, d.Stage, d.ReasonCode, d.ReasonName, d.Note, d.EstimatedFare,
                d.FeeAmount, d.FeeCharged, d.FeeStatus, d.FeeMethod, d.CompensationAmount, d.PenaltyPoints, d.CountsTowardRate, d.ExcuseStatus, d.ReviewedByName, d.ReviewedAt,
                d.ReviewNote, d.CreatedAt, age, e.ExcuseStatus == ExcuseStatus.Pending && age > sla, pending);
        }).ToList();
        return paging.Result(items, total);
    }

    /// <summary>
    /// Excuse review: approve → waived, nobody at fault, not counted (a fee already charged is refunded through F11 → <c>refunded</c>); reject → the fee
    /// is charged now and the points apply. Only <c>pending</c> excuses can be reviewed, except the approval (waiver) of an already charged fee.
    /// </summary>
    public async Task<AdminCancellationEventDto> ReviewAsync(Guid eventId, ReviewExcuseRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Rule(nameof(request.Decision), request.Decision is "approve" or "reject", "must be approve|reject")
            .Require(nameof(request.Note), request.Note, 500)
            .ThrowIfInvalid();
        var cancellation = Guard.NotFound(await db.CancellationEvents.FirstOrDefaultAsync(e => e.Id == eventId, ct));
        var approve = request.Decision == "approve";
        var appeal = approve && cancellation.ExcuseStatus == ExcuseStatus.NotApplicable && cancellation.FeeStatus == CancellationFeeStatus.Charged && cancellation.FeeCharged > 0;
        if (cancellation.ExcuseStatus != ExcuseStatus.Pending && !appeal)
        {
            throw new DomainException(ErrorCodes.Conflict, new { excuseStatus = cancellation.ExcuseStatus, cancellation.FeeStatus });
        }

        var trip = Guard.NotFound(await reads.FindAsync(cancellation.TripId, ct));
        var participants = await reads.ParticipantsAsync(trip, ct);
        var before = Snapshot(cancellation);
        cancellation.ReviewedBy = currentUser.UserId;
        cancellation.ReviewedAt = clock.UtcNow;
        cancellation.ReviewNote = request.Note!.Trim();
        if (approve)
        {
            cancellation.ExcuseStatus = ExcuseStatus.Approved;
            cancellation.AtFault = AtFault.None;
            cancellation.CountsTowardRate = false;
            cancellation.PenaltyPoints = 0;
            var charged = cancellation.FeeCharged;
            cancellation.FeeStatus = charged > 0 ? CancellationFeeStatus.Charged : CancellationFeeStatus.Waived;
            audit.Log("cancellation.review", "cancellation_event", cancellation.Id, before, Snapshot(cancellation));
            await db.SaveChangesAsync(ct);
            if (charged > 0 && cancellation.FeeMethod == CancellationFeeMethod.Corporate)
            {
                // F19: a fee billed to a company is reversed on its account, never refunded to the rider's wallet.
                await corporateCredit.WaiveCancellationFeeAsync(cancellation, trip, cancellation.ReviewNote ?? string.Empty, currentUser.UserId, ct);
                await db.SaveChangesAsync(ct);
            }
            else if (charged > 0)
            {
                var refund = await refunds.RefundCancellationFeeAsync(trip.Id, participants.PassengerUserId, charged, cancellation.FeeMethod == CancellationFeeMethod.Card,
                    $"Cancellation fee waived: {cancellation.ReviewNote}", ct);
                if (refund.Status == RefundStatus.Succeeded)
                {
                    cancellation.FeeStatus = CancellationFeeStatus.Refunded;
                    await db.SaveChangesAsync(ct);
                }
            }
        }
        else
        {
            await db.InTransactionAsync(async () =>
            {
                cancellation.ExcuseStatus = ExcuseStatus.Rejected;
                await engine.ApplyRejectionAsync(cancellation, trip, ct);
                audit.Log("cancellation.review", "cancellation_event", cancellation.Id, before, Snapshot(cancellation));
                await db.SaveChangesAsync(ct);
            }, ct);
        }

        await engine.RefreshReliabilityAsync(participants, ct);
        return (await EventDtosAsync([cancellation], lang, ct))[0];
    }

    // ----- reliability profiles -----

    public async Task<PagedResult<ReliabilityProfileListItemDto>> ProfilesAsync(Role? role, RestrictionLevel? level, string? search, Paging paging, CancellationToken ct)
    {
        var query = from p in db.ReliabilityProfiles.AsNoTracking()
                    join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                    select new { p, u.FullName, u.PhoneNumber };
        if (role is not null) query = query.Where(x => x.p.Role == role);
        if (level is not null) query = query.Where(x => x.p.RestrictionLevel == level);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.PhoneNumber.Contains(phone) || (x.FullName != null && x.FullName.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.p.PenaltyPoints).ThenByDescending(x => x.p.CancellationsAtFault).ThenBy(x => x.p.UserId).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var now = clock.UtcNow;
        return paging.Result(rows.Select(x => ToListItem(x.p, x.FullName, x.PhoneNumber, now)).ToList(), total);
    }

    public async Task<ReliabilityProfileDetailDto> ProfileAsync(Guid userId, Role? role, Language lang, CancellationToken ct)
    {
        var profileRole = role ?? await DefaultRoleAsync(userId, ct);
        var profile = await reliability.RefreshAsync(userId, profileRole, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        return await DetailAsync(profile, lang, ct);
    }

    public async Task<ReliabilityProfileDetailDto> AdjustAsync(Guid userId, ReliabilityAdjustRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Role), request.Role)
            .Rule(nameof(request.Role), request.Role is null or Role.Passenger or Role.Driver, "must be passenger|driver")
            .Require(nameof(request.Action), request.Action)
            .Rule(nameof(request.Points), request.Action is not (ReliabilityAction.AddPoints or ReliabilityAction.RemovePoints) || request.Points is > 0, "required and positive")
            .Rule(nameof(request.Level), request.Action != ReliabilityAction.SetLevel || request.Level is not null, "required for set_level")
            .Rule(nameof(request.Until), request.Until is null || request.Until.Value.ToUniversalTime() > clock.UtcNow, "must be in the future")
            .Require(nameof(request.Reason), request.Reason, 500)
            .ThrowIfInvalid();
        var role = request.Role!.Value;
        var exists = role == Role.Driver ? await db.Drivers.AsNoTracking().AnyAsync(d => d.UserId == userId, ct) : await db.Passengers.AsNoTracking().AnyAsync(p => p.UserId == userId, ct);
        if (!exists)
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        var adjustment = new ReliabilityAdjustment
        {
            UserId = userId,
            Role = role,
            Action = request.Action!.Value,
            Points = request.Action switch
            {
                ReliabilityAction.AddPoints => request.Points,
                ReliabilityAction.RemovePoints => -request.Points,
                _ => null,
            },
            Level = request.Action == ReliabilityAction.SetLevel ? request.Level : null,
            Until = request.Action == ReliabilityAction.SetLevel ? request.Until?.ToUniversalTime() : null,
            Reason = request.Reason!.Trim(),
            CreatedBy = currentUser.UserId,
            CreatedAt = clock.UtcNow,
        };
        db.ReliabilityAdjustments.Add(adjustment);
        audit.Log("reliability.adjust", "reliability_profile", userId, null, new { role, adjustment.Action, adjustment.Points, adjustment.Level, adjustment.Until, adjustment.Reason });
        await db.SaveChangesAsync(ct);
        var profile = await reliability.RefreshAsync(userId, role, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        return await DetailAsync(profile, lang, ct);
    }

    // ----- KPIs -----

    public async Task<CancellationStatsDto> StatsAsync(DateOnly? from, DateOnly? to, Guid? cityId, Guid? zoneId, Guid? rideCategoryId, CancellationToken ct)
    {
        var tripQuery = db.Trips.AsNoTracking().AsQueryable();
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            tripQuery = tripQuery.Where(t => t.RequestedAt >= fromAt);
        }

        if (to is { } t0)
        {
            var toAt = t0.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            tripQuery = tripQuery.Where(t => t.RequestedAt < toAt);
        }

        if (rideCategoryId is { } categoryId) tripQuery = tripQuery.Where(t => t.RideCategoryId == categoryId);
        var trips = await tripQuery.Select(t => new { t.Id, t.PassengerId, t.DriverId, t.AssignedAt, t.ArrivedAt, t.Status, t.PickupLat, t.PickupLng, t.RequestedAt }).ToListAsync(ct);
        if (cityId is not null || zoneId is not null)
        {
            var all = await zones.AllAsync(ct);
            trips = trips.Where(t =>
            {
                var zone = zones.Resolve(all, t.PickupLat, t.PickupLng, t.RequestedAt);
                return zone is not null && (zoneId is null || zone.Id == zoneId) && (cityId is null || zone.CityId == cityId);
            }).ToList();
        }

        var tripIds = trips.Select(t => t.Id).ToHashSet();
        var byTrip = trips.ToDictionary(t => t.Id);
        var allEvents = await db.CancellationEvents.AsNoTracking()
            .Select(e => new { e.TripId, e.AtFault, e.CountsTowardRate, e.Stage, e.FeeCharged, e.FeeStatus, e.ExcuseStatus }).ToListAsync(ct);
        var events = allEvents.Where(e => tripIds.Contains(e.TripId)).ToList();
        var assigned = trips.Count(t => t.AssignedAt != null);
        var completed = trips.Count(t => t.AssignedAt != null && t.Status == TripStatus.Completed);
        var arrived = trips.Count(t => t.ArrivedAt != null);
        var faults = events.Where(e => e.CountsTowardRate && e.AtFault != AtFault.None)
            .Select(e => e.AtFault == AtFault.Passenger ? $"p:{byTrip[e.TripId].PassengerId}" : $"d:{byTrip[e.TripId].DriverId}").ToList();
        var perUser = faults.GroupBy(x => x).Select(g => g.Count()).ToList();
        var approved = events.Count(e => e.ExcuseStatus == ExcuseStatus.Approved);
        var rejected = events.Count(e => e.ExcuseStatus == ExcuseStatus.Rejected);
        var reliabilityRate = assigned == 0 ? 1m : Rate(completed, assigned);
        return new CancellationStatsDto(
            from, to, events.Count, assigned,
            Rate(events.Count(e => e.CountsTowardRate && e.AtFault == AtFault.Passenger), assigned),
            Rate(events.Count(e => e.CountsTowardRate && e.AtFault == AtFault.Driver), assigned),
            events.Where(e => e.FeeStatus == CancellationFeeStatus.Charged).Sum(e => e.FeeCharged),
            Rate(perUser.Count(c => c >= 2), perUser.Count),
            reliabilityRate,
            reliabilityRate,
            Rate(approved, approved + rejected),
            Rate(events.Count(e => e.Stage == CancellationStage.NoShow), arrived));
    }

    // ----- helpers -----

    private IQueryable<CancellationEvent> FilterEvents(TripActor? actor, CancellationStage? stage, AtFault? atFault, CancellationFeeStatus? feeStatus, ExcuseStatus? excuseStatus,
        DateOnly? from, DateOnly? to, string? search)
    {
        var query = db.CancellationEvents.AsNoTracking().AsQueryable();
        if (actor is not null) query = query.Where(e => e.Actor == actor);
        if (stage is not null) query = query.Where(e => e.Stage == stage);
        if (atFault is not null) query = query.Where(e => e.AtFault == atFault);
        if (feeStatus is not null) query = query.Where(e => e.FeeStatus == feeStatus);
        if (excuseStatus is not null) query = query.Where(e => e.ExcuseStatus == excuseStatus);
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAt >= fromAt);
        }

        if (to is { } t)
        {
            var toAt = t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAt < toAt);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            var tripIds = db.Trips.Where(tr => tr.TripNumber.Contains(term)).Select(tr => tr.Id);
            var userIds = db.Users.Where(u => u.PhoneNumber.Contains(phone) || (u.FullName != null && u.FullName.Contains(term))).Select(u => u.Id);
            query = query.Where(e => tripIds.Contains(e.TripId) || (e.UserId != null && userIds.Contains(e.UserId.Value)));
        }

        return query;
    }

    private async Task<IReadOnlyList<AdminCancellationEventDto>> EventDtosAsync(IReadOnlyList<CancellationEvent> rows, Language lang, CancellationToken ct)
    {
        var tripIds = rows.Select(e => e.TripId).Distinct().ToList();
        var userIds = rows.SelectMany(e => new[] { e.UserId, e.ReviewedBy }).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var reasonIds = rows.Where(e => e.ReasonId != null).Select(e => e.ReasonId!.Value).Distinct().ToList();
        var numbers = await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TripNumber, ct);
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.PhoneNumber, ct);
        var reasons = await db.CancellationReasons.AsNoTracking().Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => lang.Pick(r.NameAr, r.NameEn), ct);
        return rows.Select(e => new AdminCancellationEventDto(e.Id, e.TripId, numbers.GetValueOrDefault(e.TripId) ?? string.Empty, e.Actor, e.UserId,
            e.UserId is { } u ? names.GetValueOrDefault(u) : null, e.AtFault, e.Stage, e.ReasonCode, e.ReasonId is { } r ? reasons.GetValueOrDefault(r) : null, e.Note, e.EstimatedFare,
            e.FeeAmount, e.FeeCharged, e.FeeStatus, e.FeeMethod, e.CompensationAmount, e.PenaltyPoints, e.CountsTowardRate, e.ExcuseStatus,
            e.ReviewedBy is { } rb ? names.GetValueOrDefault(rb) : null, e.ReviewedAt, e.ReviewNote, e.CreatedAt)).ToList();
    }

    private async Task<ReliabilityProfileDetailDto> DetailAsync(ReliabilityProfile p, Language lang, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Where(u => u.Id == p.UserId).Select(u => new { u.FullName, u.PhoneNumber }).FirstAsync(ct);
        var thresholds = await reliability.ThresholdsAsync(p.Role, ct);
        var now = clock.UtcNow;
        var snapshot = reliability.Snapshot(p, thresholds, now);
        var events = await tripCancellations.RecentEventsAsync(p.UserId, p.Role, now.AddDays(-60), 200, lang, ct);
        var adjustmentRows = await db.ReliabilityAdjustments.AsNoTracking().Where(a => a.UserId == p.UserId && a.Role == p.Role).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        var creators = adjustmentRows.Select(a => a.CreatedBy).Distinct().ToList();
        var creatorNames = await db.Users.AsNoTracking().Where(u => creators.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.PhoneNumber, ct);
        var item = ToListItem(p, user.FullName, user.PhoneNumber, now);
        return new ReliabilityProfileDetailDto(item.UserId, item.Name, item.Phone, item.Role, item.Level, item.RestrictedUntil, item.CancellationRate, item.ReliabilityRate,
            item.PenaltyPoints, item.NoShowCount, item.TripsAccepted, item.LastComputedAt, p.WindowDays, p.TripsRequested, p.TripsCompleted, p.CancellationsAtFault,
            p.Role == Role.Driver ? p.OffersReceived : null, p.Role == Role.Driver ? p.OffersAccepted : null, p.AcceptanceRate, p.LevelChangedAt, reliability.NextLevel(p, thresholds),
            new ReliabilityEffectsDto(reliability.MatchingFactor(snapshot), reliability.IncentiveMultiplier(snapshot)), events,
            adjustmentRows.Select(a => new ReliabilityAdjustmentDto(a.Id, a.Action, a.Points, a.Level, a.Until, a.Reason, creatorNames.GetValueOrDefault(a.CreatedBy), a.CreatedAt)).ToList());
    }

    private static ReliabilityProfileListItemDto ToListItem(ReliabilityProfile p, string? name, string? phone, DateTime now) =>
        new(p.UserId, name, phone, p.Role, p.RestrictionLevel,
            RestrictionLevels.IsRestricting(p.RestrictionLevel) && (p.RestrictedUntil is null || p.RestrictedUntil > now) ? p.RestrictedUntil : null,
            p.CancellationRate, p.ReliabilityRate, p.PenaltyPoints, p.NoShowCount, p.TripsAccepted, p.LastComputedAt == default ? null : p.LastComputedAt);

    private async Task<Role> DefaultRoleAsync(Guid userId, CancellationToken ct) =>
        await db.Drivers.AsNoTracking().AnyAsync(d => d.UserId == userId, ct) ? Role.Driver : Role.Passenger;

    private static void ValidateReason(CancellationReasonRequest r) => new Validator()
        .Require(nameof(r.Code), r.Code, 60)
        .Rule(nameof(r.Code), r.Code is null || CodePattern().IsMatch(r.Code.Trim()), "lowercase letters, digits and _ only")
        .Require(nameof(r.Actor), r.Actor)
        .Require(nameof(r.NameAr), r.NameAr, 120)
        .Require(nameof(r.NameEn), r.NameEn, 120)
        .Rule(nameof(r.Stages), r.Stages is null || r.Stages.Count > 0, "use null for every stage")
        .ThrowIfInvalid();

    private static void Apply(CancellationReason reason, CancellationReasonRequest r)
    {
        reason.Actor = r.Actor!.Value;
        reason.NameAr = r.NameAr!.Trim();
        reason.NameEn = r.NameEn!.Trim();
        reason.Stages = r.Stages is null ? null : JsonSerializer.Serialize(r.Stages.Distinct().ToList(), JsonDefaults.Options);
        reason.IsExcusable = r.IsExcusable ?? false;
        reason.IsEmergency = r.IsEmergency ?? false;
        reason.RequiresNote = r.RequiresNote ?? false;
        reason.IsSelectable = r.IsSelectable ?? true;
        reason.SortOrder = r.SortOrder ?? 0;
        reason.IsActive = r.IsActive ?? true;
    }

    private async Task ValidateRuleAsync(CancellationRuleRequest r, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(r.Name), r.Name, 120)
            .Require(nameof(r.Actor), r.Actor)
            .Rule(nameof(r.Actor), r.Actor is null or CancellationActor.Passenger or CancellationActor.Driver, "must be passenger|driver")
            .Require(nameof(r.Stage), r.Stage)
            .Rule(nameof(r.Stage), r.Stage != CancellationStage.Scheduled, "scheduled trips use scheduled_ride_rules")
            .Rule(nameof(r.Stage), r.Stage != CancellationStage.NoShow || r.Actor is null or CancellationActor.Passenger, "no_show is for passengers only")
            .Require(nameof(r.FeeType), r.FeeType)
            .Rule(nameof(r.FeeType), !(r.Actor == CancellationActor.Passenger && r.Stage == CancellationStage.BeforeAccept) || r.FeeType is null or CancellationFeeType.None,
                "no passenger fee before a driver accepts")
            .Rule(nameof(r.FeeAmount), r.FeeType != CancellationFeeType.Fixed || r.FeeAmount is >= 0, "required for fixed")
            .Rule(nameof(r.FeePercent), r.FeeType != CancellationFeeType.Percent || r.FeePercent is >= 0 and <= 100, "required for percent (0–100)")
            .Rule(nameof(r.FeePercent), r.FeePercent is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.FeeAmount), r.FeeAmount is null or >= 0, "must be positive")
            .Rule(nameof(r.MinFee), r.MinFee is null or >= 0, "must be positive")
            .Rule(nameof(r.MaxFee), r.MaxFee is null or >= 0, "must be positive")
            .Rule(nameof(r.MaxFee), r.MinFee is null || r.MaxFee is null || r.MaxFee >= r.MinFee, "must be ≥ minFee")
            .Rule(nameof(r.FreeWindowSeconds), r.FreeWindowSeconds is null or >= 0, "must be positive")
            .Rule(nameof(r.DriverCompensationPercent), r.DriverCompensationPercent is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.PenaltyPoints), r.PenaltyPoints is null or >= 0, "must be positive");
        if (r.RideCategoryId is { } categoryId)
        {
            v.Rule(nameof(r.RideCategoryId), await db.RideCategories.AsNoTracking().AnyAsync(c => c.Id == categoryId, ct), "unknown ride category");
        }

        if (r.ZoneId is { } zoneId)
        {
            v.Rule(nameof(r.ZoneId), await db.Zones.AsNoTracking().AnyAsync(z => z.Id == zoneId, ct), "unknown zone");
        }

        v.ThrowIfInvalid();
    }

    private static void Apply(CancellationRule rule, CancellationRuleRequest r)
    {
        rule.Name = r.Name!.Trim();
        rule.Actor = r.Actor!.Value;
        rule.Stage = r.Stage!.Value;
        rule.BookingType = r.BookingType;
        rule.RideCategoryId = r.RideCategoryId;
        rule.ZoneId = r.ZoneId;
        rule.FreeWindowSeconds = r.FreeWindowSeconds ?? 0;
        rule.FeeType = r.FeeType!.Value;
        rule.FeeAmount = rule.FeeType == CancellationFeeType.Fixed ? r.FeeAmount : null;
        rule.FeePercent = rule.FeeType == CancellationFeeType.Percent ? r.FeePercent : null;
        rule.MinFee = rule.FeeType == CancellationFeeType.None ? null : r.MinFee;
        rule.MaxFee = rule.FeeType == CancellationFeeType.None ? null : r.MaxFee;
        rule.DriverCompensationPercent = r.DriverCompensationPercent ?? 0m;
        rule.PenaltyPoints = r.PenaltyPoints ?? 0;
        rule.Priority = r.Priority ?? 0;
        rule.IsActive = r.IsActive ?? true;
    }

    private static decimal Rate(int part, int whole) => whole == 0 ? 0m : decimal.Round((decimal)part / whole, 4, MidpointRounding.AwayFromZero);

    private static object Snapshot(CancellationEvent e) => new { e.ExcuseStatus, e.AtFault, e.FeeStatus, e.FeeCharged, e.PenaltyPoints, e.CountsTowardRate, e.ReviewNote };

    public static AdminCancellationReasonDto ToDto(CancellationReason r) =>
        new(r.Id, r.Code, r.Actor, r.NameAr, r.NameEn, CancellationEngine.StagesOf(r), r.IsExcusable, r.IsEmergency, r.RequiresNote, r.IsSelectable, r.SortOrder, r.IsActive, r.CreatedAt, r.UpdatedAt);

    public static CancellationRuleDto ToDto(CancellationRule r) =>
        new(r.Id, r.Name, r.Actor, r.Stage, r.BookingType, r.RideCategoryId, r.ZoneId, r.FreeWindowSeconds, r.FeeType, r.FeeAmount, r.FeePercent, r.MinFee, r.MaxFee,
            r.DriverCompensationPercent, r.PenaltyPoints, r.Priority, r.IsActive, r.CreatedAt, r.UpdatedAt);

    public static ReliabilityThresholdDto ToDto(ReliabilityThreshold t) =>
        new(t.Id, t.Role, t.Level, t.MinPenaltyPoints, t.MinCancellationRate, t.MinTripsForRate, t.RestrictionHours, t.DeprioritizeFactor, t.IncentiveReductionPercent, t.SortOrder, t.IsActive);
}
