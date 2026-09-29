import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized labels of tiers and incentives.
abstract final class RewardsText {
  static const int _percent = 100;

  static String tier(AppLocalizations l10n, DriverTier tier) => switch (tier) {
    DriverTier.bronze => l10n.tierBronze,
    DriverTier.silver => l10n.tierSilver,
    DriverTier.gold => l10n.tierGold,
    DriverTier.platinum => l10n.tierPlatinum,
  };

  static String criterion(AppLocalizations l10n, TierCriterion c) =>
      switch (c) {
        TierCriterion.trips => l10n.tierCriterionTrips,
        TierCriterion.rating => l10n.tierCriterionRating,
        TierCriterion.acceptance => l10n.tierCriterionAcceptance,
        TierCriterion.cancellation => l10n.tierCriterionCancellation,
      };

  /// `96 / 150`, `4.82 / 4.85`, `88% / 90%`, `4% / ≤ 3%`.
  static String checkValue(TierCheck c) => switch (c.criterion) {
    TierCriterion.trips =>
      '${Money.integer(c.current)} / ${Money.integer(c.target)}',
    TierCriterion.rating =>
      '${Money.compact(c.current)} / ${Money.compact(c.target)}',
    TierCriterion.acceptance => '${pct(c.current)} / ${pct(c.target)}',
    TierCriterion.cancellation => '${pct(c.current)} / ≤ ${pct(c.target)}',
  };

  static String pct(double rate) => '${Money.compact(rate * _percent)}%';

  static String type(AppLocalizations l10n, String type) => switch (type) {
    'daily' => l10n.incentiveTypeDaily,
    'weekly' => l10n.incentiveTypeWeekly,
    'zone_quest' => l10n.incentiveTypeZone,
    'one_time' => l10n.incentiveTypeOneTime,
    _ => type,
  };

  static String tab(AppLocalizations l10n, IncentiveTab tab) => switch (tab) {
    IncentiveTab.active => l10n.incentiveTabActive,
    IncentiveTab.upcoming => l10n.incentiveTabUpcoming,
    IncentiveTab.completed => l10n.incentiveTabCompleted,
  };

  static String status(AppLocalizations l10n, IncentiveProgressStatus s) =>
      switch (s) {
        IncentiveProgressStatus.inProgress => l10n.incentiveInProgress,
        IncentiveProgressStatus.achieved => l10n.incentiveAchieved,
        IncentiveProgressStatus.paid => l10n.incentivePaid,
        IncentiveProgressStatus.expired => l10n.incentiveExpired,
        IncentiveProgressStatus.voided => l10n.incentiveVoided,
      };

  /// 0 = Sunday … 6 = Saturday.
  static String day(AppLocalizations l10n, int day) => switch (day) {
    0 => l10n.daySun,
    1 => l10n.dayMon,
    2 => l10n.dayTue,
    3 => l10n.dayWed,
    4 => l10n.dayThu,
    5 => l10n.dayFri,
    _ => l10n.daySat,
  };

  /// "الخميس، الجمعة · 16:00–22:00"; `null` without a window.
  static String? window(AppLocalizations l10n, IncentiveWindow? w) {
    if (w == null) return null;
    final String days = w.daysOfWeek.map((int d) => day(l10n, d)).join('، ');
    final String hours = w.from == null || w.to == null
        ? ''
        : '${w.from}–${w.to}';
    final String text = <String>[
      days,
      hours,
    ].where((String s) => s.isNotEmpty).join(' · ');
    return text.isEmpty ? null : text;
  }

  static String? zones(Incentive i) {
    final List<IncentiveZone>? zones = i.zones;
    if (zones == null || zones.isEmpty) return null;
    return zones.map((IncentiveZone z) => z.name).join('، ');
  }

  static String? period(AppLocalizations l10n, Incentive i, String locale) {
    final DateTime? end = i.periodEnd;
    return end == null
        ? null
        : l10n.incentiveEndsAt(DateText.longDate(end, locale));
  }
}
