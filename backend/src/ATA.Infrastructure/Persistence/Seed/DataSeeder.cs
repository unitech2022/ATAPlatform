using ATA.Domain.Cancellation;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Incentives;
using ATA.Domain.Matching;
using ATA.Domain.Notifications;
using ATA.Domain.Pricing;
using ATA.Domain.Promotions;
using ATA.Domain.Ratings;
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
        await SeedZonesAsync(cancellationToken);
        await SeedDemandLevelsAsync(cancellationToken);
        await SeedPricingRulesAsync(cancellationToken);
        await SeedDemandRulesAsync(cancellationToken);
        await SeedMatchingSettingsAsync(cancellationToken);
        await SeedNotificationTemplatesAsync(cancellationToken);
        await SeedCancellationReasonsAsync(cancellationToken);
        await SeedCancellationRulesAsync(cancellationToken);
        await SeedReliabilityThresholdsAsync(cancellationToken);
        await SeedRatingTagsAsync(cancellationToken);
        await SeedDriverTierRulesAsync(cancellationToken);
        await SeedPromotionsAsync(cancellationToken);
        await SeedIncentivesAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>F15 rating tags (doc 10 "البيانات الأولية"), added by (target role, code) when missing.</summary>
    private async Task SeedRatingTagsAsync(CancellationToken ct)
    {
        (RatingRole Target, string Code, string Ar, string En)[] tags =
        [
            (RatingRole.Driver, "driving", "القيادة", "Driving"),
            (RatingRole.Driver, "cleanliness", "النظافة", "Cleanliness"),
            (RatingRole.Driver, "behaviour", "التعامل", "Behaviour"),
            (RatingRole.Driver, "navigation", "معرفة الطريق", "Navigation"),
            (RatingRole.Driver, "vehicle_condition", "حالة المركبة", "Vehicle condition"),
            (RatingRole.Passenger, "punctuality", "الالتزام بالوقت", "Punctuality"),
            (RatingRole.Passenger, "behaviour", "التعامل", "Behaviour"),
            (RatingRole.Passenger, "cleanliness", "النظافة", "Cleanliness"),
        ];
        var existing = (await db.RatingTags.Select(t => new { t.TargetRole, t.Code }).ToListAsync(ct)).Select(t => (t.TargetRole, t.Code)).ToHashSet();
        var order = 0;
        foreach (var t in tags)
        {
            order++;
            if (!existing.Contains((t.Target, t.Code)))
            {
                db.RatingTags.Add(new RatingTag { Code = t.Code, TargetRole = t.Target, NameAr = t.Ar, NameEn = t.En, SortOrder = order, IsActive = true });
            }
        }
    }

    /// <summary>The four tiers of doc 10 §F15.7, added by tier when missing (admin edits are kept).</summary>
    private async Task SeedDriverTierRulesAsync(CancellationToken ct)
    {
        (DriverTier Tier, int Trips, decimal Rating, decimal Acceptance, decimal Cancellation, decimal Discount, decimal Norm, string Ar, string En)[] rules =
        [
            (DriverTier.Bronze, 0, 0m, 0m, 1m, 0m, 0.25m, "المستوى الأساسي", "Base tier"),
            (DriverTier.Silver, 60, 4.70m, 0.80m, 0.08m, 5m, 0.50m, "خصم 5% من العمولة وأولوية أعلى في الطلبات", "5% off the commission and higher matching priority"),
            (DriverTier.Gold, 150, 4.80m, 0.85m, 0.05m, 10m, 0.75m, "خصم 10% من العمولة وأولوية عالية في الطلبات", "10% off the commission and high matching priority"),
            (DriverTier.Platinum, 250, 4.90m, 0.90m, 0.03m, 15m, 1.00m, "خصم 15% من العمولة وأعلى أولوية في الطلبات", "15% off the commission and top matching priority"),
        ];
        var existing = (await db.DriverTierRules.Select(r => r.Tier).ToListAsync(ct)).ToHashSet();
        var order = 0;
        foreach (var r in rules)
        {
            order++;
            if (!existing.Contains(r.Tier))
            {
                db.DriverTierRules.Add(new DriverTierRule
                {
                    Tier = r.Tier, MinCompletedTrips = r.Trips, MinRatingAvg = r.Rating, MinAcceptanceRate = r.Acceptance, MaxCancellationRate = r.Cancellation,
                    CommissionDiscountPercent = r.Discount, MatchingNorm = r.Norm, BenefitsAr = r.Ar, BenefitsEn = r.En, SortOrder = order,
                });
            }
        }
    }

    /// <summary>Sample codes <c>WELCOME</c> and <c>ATA10</c>, added by code when missing (a deactivated code is never re-created).</summary>
    private async Task SeedPromotionsAsync(CancellationToken ct)
    {
        var existing = (await db.Promotions.Select(p => p.Code).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2027, 12, 31, 20, 59, 59, DateTimeKind.Utc);
        if (!existing.Contains("WELCOME"))
        {
            db.Promotions.Add(new Promotion
            {
                Code = "WELCOME", NameAr = "خصم الترحيب", NameEn = "Welcome discount", DescriptionAr = "20% على رحلتك الأولى حتى 15 ر.س",
                DescriptionEn = "20% off your first trip, up to SAR 15", Type = PromotionType.Percent, Value = 20m, MaxDiscount = 15m, ValidFrom = from, ValidTo = to,
                PerUserLimit = 1, FirstTripOnly = true, IsPublic = true, IsActive = true,
            });
        }

        if (!existing.Contains("ATA10"))
        {
            db.Promotions.Add(new Promotion
            {
                Code = "ATA10", NameAr = "خصم 10 ريال", NameEn = "SAR 10 off", DescriptionAr = "10 ر.س على الرحلات من 30 ر.س", DescriptionEn = "SAR 10 off trips from SAR 30",
                Type = PromotionType.Fixed, Value = 10m, MinFare = 30m, ValidFrom = from, ValidTo = to, PerUserLimit = 3, IsStackable = false, IsPublic = true, IsActive = true,
            });
        }
    }

    /// <summary>The sample weekly incentive (Thursday/Friday evenings, 10 trips → 75 SAR), seeded once while the table is empty.</summary>
    private async Task SeedIncentivesAsync(CancellationToken ct)
    {
        if (await db.DriverIncentives.AnyAsync(ct) || !await db.Cities.AnyAsync(c => c.Id == SeedIds.CityRiyadh, ct))
        {
            return;
        }

        db.DriverIncentives.Add(new DriverIncentive
        {
            NameAr = "10 رحلات مساء الخميس والجمعة", NameEn = "10 trips on Thursday and Friday evenings",
            DescriptionAr = "أكمل 10 رحلات بين 16:00 و23:59 يومي الخميس والجمعة واربح 75 ر.س", DescriptionEn = "Complete 10 trips between 16:00 and 23:59 on Thursday and Friday to earn SAR 75",
            Type = IncentiveType.Weekly, CityId = SeedIds.CityRiyadh, TargetTrips = 10, RewardAmount = 75m, DaysOfWeek = "[4,5]",
            DailyFrom = new TimeOnly(16, 0), DailyTo = new TimeOnly(23, 59),
            StartsAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndsAt = new DateTime(2027, 12, 31, 21, 0, 0, DateTimeKind.Utc),
            IsActive = true, NotifyOnPublish = false,
        });
    }

    /// <summary>
    /// F14 reasons (doc 09 §F14.6), added by (actor, code) when missing; edited rows are never touched. The four codes the F8 app sends
    /// (<c>changed_mind</c>, <c>driver_late</c>, <c>wrong_pickup</c>, <c>other</c>) are valid in every passenger stage.
    /// </summary>
    private async Task SeedCancellationReasonsAsync(CancellationToken ct)
    {
        const string accepted = "[\"after_accept\",\"en_route\"]";
        const string atPickup = "[\"arrived\",\"waiting\"]";
        const string afterAccept = "[\"after_accept\",\"en_route\",\"arrived\",\"waiting\"]";
        (CancellationActor Actor, string Code, string Ar, string En, string? Stages, bool Excusable, bool Emergency, bool Note, bool Selectable)[] reasons =
        [
            (CancellationActor.Passenger, "changed_mind", "غيرت رأيي", "I changed my mind", null, false, false, false, true),
            (CancellationActor.Passenger, "driver_late", "الكابتن تأخر", "The driver is late", null, false, false, false, true),
            (CancellationActor.Passenger, "driver_too_far", "الكابتن بعيد", "The driver is too far", accepted, false, false, false, true),
            (CancellationActor.Passenger, "wrong_pickup", "موقع الالتقاط خاطئ", "Wrong pickup location", null, false, false, false, true),
            (CancellationActor.Passenger, "found_other_ride", "وجدت وسيلة أخرى", "I found another ride", null, false, false, false, true),
            (CancellationActor.Passenger, "driver_asked_to_cancel", "الكابتن طلب الإلغاء", "The driver asked me to cancel", afterAccept, true, false, false, true),
            (CancellationActor.Passenger, "driver_not_moving", "الكابتن لا يتحرك", "The driver is not moving", accepted, true, false, false, true),
            (CancellationActor.Passenger, "safety_concern", "قلق على السلامة", "Safety concern", null, false, true, false, true),
            (CancellationActor.Passenger, "other", "سبب آخر", "Other", null, false, false, true, true),
            (CancellationActor.Driver, "passenger_not_responding", "الراكب لا يرد", "The passenger is not responding", atPickup, false, false, false, true),
            (CancellationActor.Driver, "passenger_asked_to_cancel", "الراكب طلب الإلغاء", "The passenger asked me to cancel", null, true, false, false, true),
            (CancellationActor.Driver, "wrong_pickup_location", "موقع الالتقاط خاطئ", "Wrong pickup location", null, false, false, false, true),
            (CancellationActor.Driver, "pickup_too_far", "موقع الالتقاط بعيد", "The pickup is too far", accepted, false, false, false, true),
            (CancellationActor.Driver, "vehicle_issue", "عطل في المركبة", "Vehicle issue", null, true, false, false, true),
            (CancellationActor.Driver, "safety_concern", "قلق على السلامة", "Safety concern", null, false, true, false, true),
            (CancellationActor.Driver, "other", "سبب آخر", "Other", null, false, false, true, true),
            (CancellationActor.Driver, "passenger_no_show", "الراكب لم يحضر", "The passenger did not show up", "[\"no_show\"]", false, false, false, false),
            (CancellationActor.System, "no_drivers", "لا يوجد كباتن", "No drivers available", null, false, false, false, false),
            (CancellationActor.System, "payment_failed", "فشل الدفع", "Payment failed", null, false, false, false, false),
            (CancellationActor.System, "admin_cancelled", "ألغتها الإدارة", "Cancelled by support", null, false, false, false, false),
            (CancellationActor.System, "scheduled_driver_unavailable", "الكابتن المحجوز غير متاح", "The reserved driver is unavailable", null, false, false, false, false),
        ];
        var existing = (await db.CancellationReasons.Select(r => new { r.Actor, r.Code }).ToListAsync(ct)).Select(r => (r.Actor, r.Code)).ToHashSet();
        var order = 0;
        foreach (var r in reasons)
        {
            order++;
            if (existing.Contains((r.Actor, r.Code)))
            {
                continue;
            }

            db.CancellationReasons.Add(new CancellationReason
            {
                Code = r.Code, Actor = r.Actor, NameAr = r.Ar, NameEn = r.En, Stages = r.Stages, IsExcusable = r.Excusable, IsEmergency = r.Emergency,
                RequiresNote = r.Note, IsSelectable = r.Selectable, SortOrder = order, IsActive = true,
            });
        }
    }

    /// <summary>City-wide rules of §F14.6, seeded once while the table is empty (admin deletions are not resurrected).</summary>
    private async Task SeedCancellationRulesAsync(CancellationToken ct)
    {
        if (await db.CancellationRules.AnyAsync(ct))
        {
            return;
        }

        CancellationRule Rule(string name, CancellationActor actor, CancellationStage stage, int window, CancellationFeeType type, decimal? amount, decimal? min, decimal compensation, int points) => new()
        {
            Name = name, Actor = actor, Stage = stage, FreeWindowSeconds = window, FeeType = type, FeeAmount = amount, MinFee = min,
            DriverCompensationPercent = compensation, PenaltyPoints = points, Priority = 0, IsActive = true,
        };

        db.CancellationRules.AddRange(
            Rule("Passenger — before accept", CancellationActor.Passenger, CancellationStage.BeforeAccept, 0, CancellationFeeType.None, null, null, 0m, 0),
            Rule("Passenger — after accept", CancellationActor.Passenger, CancellationStage.AfterAccept, 120, CancellationFeeType.Fixed, 5m, null, 50m, 1),
            Rule("Passenger — driver en route", CancellationActor.Passenger, CancellationStage.EnRoute, 120, CancellationFeeType.Fixed, 10m, null, 70m, 2),
            Rule("Passenger — driver arrived", CancellationActor.Passenger, CancellationStage.Arrived, 0, CancellationFeeType.PricingRule, null, null, 80m, 2),
            Rule("Passenger — waiting", CancellationActor.Passenger, CancellationStage.Waiting, 0, CancellationFeeType.PricingRule, null, 10m, 80m, 3),
            Rule("Passenger — no-show", CancellationActor.Passenger, CancellationStage.NoShow, 0, CancellationFeeType.PricingRule, null, 10m, 80m, 4),
            Rule("Driver — after accept", CancellationActor.Driver, CancellationStage.AfterAccept, 60, CancellationFeeType.None, null, null, 0m, 2),
            Rule("Driver — en route", CancellationActor.Driver, CancellationStage.EnRoute, 0, CancellationFeeType.None, null, null, 0m, 3),
            Rule("Driver — arrived", CancellationActor.Driver, CancellationStage.Arrived, 0, CancellationFeeType.None, null, null, 0m, 4),
            Rule("Driver — waiting", CancellationActor.Driver, CancellationStage.Waiting, 0, CancellationFeeType.None, null, null, 0m, 2));
    }

    /// <summary>The restriction ladder of §F14.6, added by (role, level) when missing.</summary>
    private async Task SeedReliabilityThresholdsAsync(CancellationToken ct)
    {
        (Role Role, RestrictionLevel Level, int Points, decimal Rate, int? Hours, decimal? Factor, decimal? Reduction, int Order)[] ladder =
        [
            (Role.Driver, RestrictionLevel.Warning, 4, 0.10m, null, null, null, 1),
            (Role.Driver, RestrictionLevel.MatchingDeprioritized, 8, 0.15m, null, 0.70m, null, 2),
            (Role.Driver, RestrictionLevel.IncentivesReduced, 12, 0.20m, null, 0.60m, 50m, 3),
            (Role.Driver, RestrictionLevel.TemporarilyRestricted, 18, 0.30m, 24, null, null, 4),
            (Role.Driver, RestrictionLevel.Suspended, 30, 0.45m, null, null, null, 5),
            (Role.Passenger, RestrictionLevel.Warning, 4, 0.15m, null, null, null, 1),
            (Role.Passenger, RestrictionLevel.TemporarilyRestricted, 12, 0.35m, 24, null, null, 4),
            (Role.Passenger, RestrictionLevel.Suspended, 25, 0.50m, null, null, null, 5),
        ];
        var existing = (await db.ReliabilityThresholds.Select(t => new { t.Role, t.Level }).ToListAsync(ct)).Select(t => (t.Role, t.Level)).ToHashSet();
        foreach (var t in ladder.Where(t => !existing.Contains((t.Role, t.Level))))
        {
            db.ReliabilityThresholds.Add(new ReliabilityThreshold
            {
                Role = t.Role, Level = t.Level, MinPenaltyPoints = t.Points, MinCancellationRate = t.Rate, MinTripsForRate = 10, RestrictionHours = t.Hours,
                DeprioritizeFactor = t.Factor, IncentiveReductionPercent = t.Reduction, SortOrder = t.Order, IsActive = true,
            });
        }
    }

    /// <summary>One template per catalogue event and default channel (F13), from the code defaults; existing rows are never touched.</summary>
    private async Task SeedNotificationTemplatesAsync(CancellationToken ct)
    {
        var existing = (await db.NotificationTemplates.Select(t => new { t.Code, t.Channel }).ToListAsync(ct))
            .Select(t => (t.Code, t.Channel)).ToHashSet();
        foreach (var definition in NotificationEvents.All)
        {
            foreach (var channel in definition.DefaultChannels)
            {
                if (existing.Contains((definition.Code, channel)))
                {
                    continue;
                }

                var text = definition.Text;
                var hasTitle = channel != NotificationChannel.Sms;
                db.NotificationTemplates.Add(new NotificationTemplate
                {
                    Code = definition.Code,
                    Channel = channel,
                    TitleAr = hasTitle ? text.TitleAr : null,
                    TitleEn = hasTitle ? text.TitleEn : null,
                    BodyAr = text.BodyAr,
                    BodyEn = text.BodyEn,
                    IsActive = true,
                });
            }
        }
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
        var existing = await db.RideCategories.ToListAsync(ct);
        RideCategory[] categories =
        [
            new() { Id = SeedIds.RideCategories.Saver, Code = "saver", NameAr = "توفير", NameEn = "Saver", DescriptionAr = "أقل سعر", DescriptionEn = "Lowest fare", Icon = "car-small", Seats = 4, MaxStops = 1, SortOrder = 1, BaseFare = 6m, PerKm = 1.5m, PerMinute = 0.3m, BookingFee = 2m, MinFare = 10m },
            new() { Id = SeedIds.RideCategories.Economy, Code = "economy", NameAr = "اقتصادي", NameEn = "Economy", DescriptionAr = "سيارة مريحة", DescriptionEn = "Comfortable car", Icon = "car", Seats = 4, MaxStops = 2, SortOrder = 2, BaseFare = 8m, PerKm = 1.8m, PerMinute = 0.35m, BookingFee = 2m, MinFare = 12m },
            new() { Id = SeedIds.RideCategories.Comfort, Code = "comfort", NameAr = "مريح", NameEn = "Comfort", DescriptionAr = "سيارات أحدث ومساحة أكبر", DescriptionEn = "Newer cars with more room", Icon = "car-comfort", Seats = 4, MaxStops = 2, SortOrder = 3, BaseFare = 12m, PerKm = 2.4m, PerMinute = 0.45m, BookingFee = 3m, MinFare = 18m },
            new() { Id = SeedIds.RideCategories.Family, Code = "family", NameAr = "عائلي", NameEn = "Family (XL)", DescriptionAr = "حتى 6 مقاعد", DescriptionEn = "Up to 6 seats", Icon = "van", Seats = 6, MaxStops = 2, SortOrder = 4, BaseFare = 15m, PerKm = 3m, PerMinute = 0.55m, BookingFee = 3m, MinFare = 22m },
            new() { Id = SeedIds.RideCategories.Premium, Code = "premium", NameAr = "ATA بلس", NameEn = "ATA Plus", DescriptionAr = "سيارات فاخرة", DescriptionEn = "Premium cars", Icon = "car-premium", Seats = 4, MaxStops = 2, SortOrder = 5, BaseFare = 20m, PerKm = 4m, PerMinute = 0.8m, BookingFee = 5m, MinFare = 35m },
            new() { Id = SeedIds.RideCategories.Airport, Code = "airport", NameAr = "المطار", NameEn = "Airport", DescriptionAr = "رحلات المطار بسعر ثابت", DescriptionEn = "Fixed-fare airport rides", Icon = "plane", Seats = 4, MaxStops = 1, SortOrder = 6, BaseFare = 30m, PerKm = 2.5m, PerMinute = 0.4m, BookingFee = 5m, MinFare = 60m },
        ];
        foreach (var category in categories)
        {
            var current = existing.FirstOrDefault(c => c.Code == category.Code);
            if (current is null)
            {
                db.RideCategories.Add(category);
            }
            else if (current.BaseFare == 0m && current.PerKm == 0m && current.MinFare == 0m)
            {
                // Rows created before F8 have no pricing yet: back-fill the defaults once, without touching edited values.
                current.BaseFare = category.BaseFare;
                current.PerKm = category.PerKm;
                current.PerMinute = category.PerMinute;
                current.BookingFee = category.BookingFee;
                current.MinFare = category.MinFare;
                current.DriverSharePercent = category.DriverSharePercent;
            }
        }
    }

    /// <summary>The Riyadh <c>city_default</c> zone: a generous rectangle around the city that catches every pickup with no dedicated zone.</summary>
    private async Task SeedZonesAsync(CancellationToken ct)
    {
        if (await db.Zones.AnyAsync(z => z.Id == SeedIds.ZoneRiyadhDefault || (z.CityId == SeedIds.CityRiyadh && z.Code == Zone.CityDefaultCode), ct))
        {
            return;
        }

        db.Zones.Add(new Zone
        {
            Id = SeedIds.ZoneRiyadhDefault, CityId = SeedIds.CityRiyadh, Code = Zone.CityDefaultCode, NameAr = "الرياض (افتراضي)", NameEn = "Riyadh (default)",
            Polygon = "[[24.20,46.00],[24.20,47.30],[25.60,47.30],[25.60,46.00],[24.20,46.00]]",
            CenterLat = 24.7135517m, CenterLng = 46.6752957m, Priority = 0, IsActive = true,
        });
    }

    private async Task SeedDemandLevelsAsync(CancellationToken ct)
    {
        var existing = await db.DemandLevels.Select(l => l.Code).ToListAsync(ct);
        DemandLevel[] levels =
        [
            new() { Id = SeedIds.DemandLevels.Normal, Code = DemandLevel.Normal, NameAr = "طبيعي", NameEn = "Normal", Multiplier = 1.00m, Color = "#19B7A5", SortOrder = 1 },
            new() { Id = SeedIds.DemandLevels.Moderate, Code = DemandLevel.Moderate, NameAr = "متوسط", NameEn = "Moderate", Multiplier = 1.20m, Color = "#123650", SortOrder = 2 },
            new() { Id = SeedIds.DemandLevels.High, Code = DemandLevel.High, NameAr = "مرتفع", NameEn = "High", Multiplier = 1.50m, Color = "#E0A100", SortOrder = 3 },
            new() { Id = SeedIds.DemandLevels.VeryHigh, Code = DemandLevel.VeryHigh, NameAr = "مرتفع جداً", NameEn = "Very high", Multiplier = 1.90m, Color = "#C23B4A", SortOrder = 4 },
        ];
        db.DemandLevels.AddRange(levels.Where(l => !existing.Contains(l.Code)));
    }

    /// <summary>
    /// One city-wide pricing rule per ride category, migrated from the F8 <c>ride_categories</c> pricing columns (which stay as the
    /// <c>FlatPricing</c> fallback), each with the example <c>night</c> multiplier 00:00–05:00 ×1.15.
    /// </summary>
    private async Task SeedPricingRulesAsync(CancellationToken ct)
    {
        var categories = await db.RideCategories.ToListAsync(ct);
        var withRules = await db.PricingRules.Where(r => r.ZoneId == null).Select(r => r.RideCategoryId).Distinct().ToListAsync(ct);
        var ruleIds = new Dictionary<Guid, Guid>
        {
            [SeedIds.RideCategories.Saver] = SeedIds.PricingRules.Saver,
            [SeedIds.RideCategories.Economy] = SeedIds.PricingRules.Economy,
            [SeedIds.RideCategories.Comfort] = SeedIds.PricingRules.Comfort,
            [SeedIds.RideCategories.Family] = SeedIds.PricingRules.Family,
            [SeedIds.RideCategories.Premium] = SeedIds.PricingRules.Premium,
            [SeedIds.RideCategories.Airport] = SeedIds.PricingRules.Airport,
        };
        foreach (var category in categories.Where(c => !withRules.Contains(c.Id)))
        {
            var rule = new PricingRule
            {
                Id = ruleIds.GetValueOrDefault(category.Id, Guid.CreateVersion7()),
                RideCategoryId = category.Id,
                ZoneId = null,
                Name = $"{category.NameEn} — city default",
                BaseFare = category.BaseFare,
                PerKm = category.PerKm,
                PerMinute = category.PerMinute,
                BookingFee = category.BookingFee,
                ServiceFeePercent = 0m,
                MinFare = category.MinFare,
                WaitingPerMinute = category.PerMinute,
                FreeWaitingMinutes = 3,
                CancellationFee = 0m,
                DriverSharePercent = category.DriverSharePercent,
                EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Priority = 0,
                IsActive = true,
            };
            rule.TimeMultipliers.Add(new PricingTimeMultiplier { PricingRuleId = rule.Id, DayOfWeek = null, FromTime = new TimeOnly(0, 0), ToTime = new TimeOnly(5, 0), Multiplier = 1.15m, Label = "night" });
            db.PricingRules.Add(rule);
        }
    }

    private async Task SeedDemandRulesAsync(CancellationToken ct)
    {
        if (await db.DemandRules.AnyAsync(ct))
        {
            return;
        }

        db.DemandRules.Add(new DemandRule
        {
            Id = SeedIds.DemandRuleDefault, ZoneId = null, RideCategoryId = null, Metric = DemandMetrics.RequestsPerDriver, WindowMinutes = 10,
            ThresholdModerate = 0.8m, ThresholdHigh = 1.5m, ThresholdVeryHigh = 2.5m, IsActive = true,
        });
    }

    private async Task SeedMatchingSettingsAsync(CancellationToken ct)
    {
        if (await db.MatchingSettings.AnyAsync(m => m.ZoneId == null && m.RideCategoryId == null, ct))
        {
            return;
        }

        db.MatchingSettings.Add(new MatchingSettings
        {
            Id = SeedIds.MatchingSettingsDefault, ZoneId = null, RideCategoryId = null,
            RadiusMeters = 5000, MaxRadiusMeters = 12000, RadiusStepMeters = 2500, OfferTimeoutSeconds = 20, SearchTimeoutSeconds = 120, MaxCandidates = 8,
            Weights = MatchingWeights.Default.ToJson(), AllowCategoryUpgrade = false, PreferFavoriteDriver = true, IsActive = true,
        });
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
