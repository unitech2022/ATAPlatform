import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized copy for the corporate account (F19): roles, statuses and the
/// policy violations with the options the company allows.
abstract final class CorporateText {
  /// Sunday (`0`) of the API's day numbering, as a date: 2024-01-07.
  static final DateTime _sunday = DateTime(2024, 1, 7);

  static String role(AppLocalizations l10n, CorporateRole role) =>
      role == CorporateRole.admin ? l10n.corpRoleAdmin : l10n.corpRoleEmployee;

  static String status(AppLocalizations l10n, CorporateMemberStatus status) =>
      switch (status) {
        CorporateMemberStatus.active => l10n.corpStatusActive,
        CorporateMemberStatus.invited => l10n.corpStatusInvited,
        CorporateMemberStatus.disabled => l10n.corpStatusDisabled,
      };

  /// `1,079.5 ر.س`.
  static String money(AppLocalizations l10n, num amount) =>
      l10n.priceWithCurrency(Money.compact(amount));

  /// The sentence explaining why a rule blocks the trip.
  static String violation(AppLocalizations l10n, PolicyViolation v) =>
      switch (v.rule) {
        PolicyRule.category => l10n.corpViolationCategory,
        PolicyRule.day => l10n.corpViolationDay,
        PolicyRule.timeWindow => l10n.corpViolationTime,
        PolicyRule.zone => l10n.corpViolationZone,
        PolicyRule.maxFare => l10n.corpViolationMaxFare(
          money(l10n, v.limit ?? 0),
        ),
        PolicyRule.scheduled => l10n.corpViolationScheduled,
        PolicyRule.purposeRequired => l10n.corpPurposeRequired,
        PolicyRule.costCenterRequired => l10n.corpCostCenterRequired,
        PolicyRule.budget =>
          v.limit == null
              ? l10n.corpViolationBudget
              : l10n.corpViolationBudgetLeft(money(l10n, v.limit!)),
        PolicyRule.creditLimit => l10n.corpViolationCredit,
        PolicyRule.unknown => l10n.corpViolationUnknown,
      };

  /// The options the policy allows for the rule ("الفئات المسموحة: …"), or
  /// `null` when the rule lists none. [categoryName] turns a category code
  /// into its display name.
  static String? allowedOptions(
    AppLocalizations l10n,
    PolicyViolation v, {
    String Function(String code)? categoryName,
  }) {
    if (v.allowed.isEmpty) return null;
    final String separator = l10n.corpListSeparator;
    switch (v.rule) {
      case PolicyRule.category:
        return l10n.corpViolationCategoryAllowed(
          v.allowed.map(categoryName ?? _same).join(separator),
        );
      case PolicyRule.day:
        return l10n.corpViolationDayAllowed(days(l10n, v.allowed));
      case PolicyRule.timeWindow:
        return l10n.corpViolationTimeAllowed(
          v.allowed.map((String w) => window(l10n, w)).join(separator),
        );
      case PolicyRule.zone:
        return l10n.corpViolationZoneAllowed(v.allowed.join(separator));
      default:
        return null;
    }
  }

  /// `الأحد، الثلاثاء` for day numbers `0..6` (Sunday = 0).
  static String days(AppLocalizations l10n, List<String> days) => days
      .map((String d) {
        final int? index = int.tryParse(d);
        return index == null
            ? d
            : DateText.weekday(
                _sunday.add(Duration(days: index % 7)),
                l10n.localeName,
              );
      })
      .join(l10n.corpListSeparator);

  /// `07:00 – 22:00` from `07:00-22:00`.
  static String window(AppLocalizations l10n, String window) {
    final List<String> parts = window.split(RegExp('[-–]'));
    return parts.length == 2
        ? l10n.corpTimeWindow(parts[0].trim(), parts[1].trim())
        : window;
  }

  /// Resolver from a category code to its catalog name (the code itself
  /// when the catalog does not list it).
  static String Function(String code) categoryNames(
    List<RideCategory> categories,
  ) =>
      (String code) =>
          categories
              .where((RideCategory c) => c.code == code)
              .map((RideCategory c) => c.name)
              .firstOrNull ??
          code;

  static String _same(String code) => code;
}
