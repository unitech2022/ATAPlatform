import 'package:ata_app/features/corporate/domain/entities/corporate_eligibility.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/eligibility_draft.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';

/// Decides whether the corporate payment fits the current draft
/// (`docs/12` §F19.2): a device pre-check of the parts of the policy the
/// profile exposes (category, day, time window, scheduled, per-trip limit,
/// remaining budget), merged with the API's evaluation of the quote, which
/// also knows the zones and the credit limit. The API wins on a rule both
/// report. `purpose_required` / `cost_center_required` are not reported
/// here: the form enforces them next to the fields.
class CheckCorporateEligibility {
  const CheckCorporateEligibility([this._now]);

  /// Riyadh has no daylight saving: UTC+3 all year.
  static const Duration riyadhOffset = Duration(hours: 3);
  static const int _minutesPerHour = 60;
  static const int _daysPerWeek = 7;

  final DateTime Function()? _now;

  CorporateEligibility call(EligibilityDraft draft) {
    final CorporateProfile? profile = draft.profile;
    if (profile == null || !profile.isActive) {
      return const CorporateEligibility(
        violations: <PolicyViolation>[
          PolicyViolation(rule: PolicyRule.unknown),
        ],
      );
    }
    final Map<PolicyRule, PolicyViolation> found =
        <PolicyRule, PolicyViolation>{
          for (final PolicyViolation v in _preCheck(profile, draft)) v.rule: v,
        };
    final server = draft.serverCheck;
    if (server != null) {
      for (final PolicyViolation v in server.violations) {
        if (v.rule == PolicyRule.purposeRequired ||
            v.rule == PolicyRule.costCenterRequired) {
          continue;
        }
        found[v.rule] = v;
      }
      if (!server.allowed && found.isEmpty) {
        found[PolicyRule.unknown] = const PolicyViolation(
          rule: PolicyRule.unknown,
        );
      }
    }
    return CorporateEligibility(
      violations: found.values.toList(growable: false),
      remainingBudget:
          draft.serverCheck?.remainingBudget ?? profile.budget?.remaining,
    );
  }

  List<PolicyViolation> _preCheck(
    CorporateProfile profile,
    EligibilityDraft draft,
  ) {
    final CorporatePolicySummary policy = profile.policy;
    final DateTime riyadh = (draft.scheduledAt ?? (_now ?? DateTime.now)())
        .toUtc()
        .add(riyadhOffset);
    final int day = riyadh.weekday % _daysPerWeek;
    final int minute = riyadh.hour * _minutesPerHour + riyadh.minute;
    final List<int>? days = policy.allowedDays;
    final List<TimeWindow>? windows = policy.timeWindows;
    final double? fare = draft.estimatedFare;
    final double? maxFare = policy.maxFarePerTrip;
    final double? remaining = profile.budget?.remaining;
    return <PolicyViolation>[
      if (!policy.allowsCategory(draft.categoryCode))
        PolicyViolation(
          rule: PolicyRule.category,
          allowed: policy.allowedRideCategoryCodes ?? const <String>[],
        ),
      if (days != null && !days.contains(day))
        PolicyViolation(
          rule: PolicyRule.day,
          allowed: days.map((int d) => '$d').toList(growable: false),
        ),
      if (windows != null &&
          windows.isNotEmpty &&
          !windows.any((TimeWindow w) => w.contains(minute)))
        PolicyViolation(
          rule: PolicyRule.timeWindow,
          allowed: windows
              .map((TimeWindow w) => '${w.from}-${w.to}')
              .toList(growable: false),
        ),
      if (draft.isScheduled && !policy.allowScheduled)
        const PolicyViolation(rule: PolicyRule.scheduled),
      if (fare != null && maxFare != null && fare > maxFare)
        PolicyViolation(rule: PolicyRule.maxFare, limit: maxFare),
      if (remaining != null &&
          (remaining <= 0 || (fare != null && fare > remaining)))
        PolicyViolation(rule: PolicyRule.budget, limit: remaining),
    ];
  }
}
