import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_budget.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_eligibility.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
import 'package:ata_app/features/corporate/domain/entities/eligibility_draft.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/features/corporate/domain/usecases/check_corporate_eligibility.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:ata_app/l10n/generated/app_localizations_en.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/corporate_fakes.dart';

CorporateProfile _profile(
  CorporatePolicySummary policy, {
  CorporateBudget? budget,
  CorporateMemberStatus status = CorporateMemberStatus.active,
}) => CorporateProfile(
  membership: CorporateMembership(
    corporateUserId: 'cu1',
    accountId: 'ca1',
    companyName: 'X',
    status: status,
  ),
  policy: policy,
  budget: budget,
);

/// Sunday 2026-10-04 12:00 in Riyadh (09:00 UTC).
final DateTime _sundayNoon = DateTime.utc(2026, 10, 4, 9);

CheckCorporateEligibility _check([DateTime? now]) =>
    CheckCorporateEligibility(() => now ?? _sundayNoon);

Set<PolicyRule> _rules(CorporateEligibility e) =>
    e.violations.map((PolicyViolation v) => v.rule).toSet();

void main() {
  setUpAll(() => initializeDateFormatting('ar'));

  group('CheckCorporateEligibility', () {
    test('no restriction means eligible', () {
      final CorporateEligibility e = _check()(
        EligibilityDraft(
          profile: _profile(const CorporatePolicySummary()),
          categoryCode: 'economy',
        ),
      );
      expect(e.eligible, isTrue);
      expect(e.remainingBudget, isNull);
    });

    test('not a member or not active is never eligible', () {
      expect(_check()(const EligibilityDraft(profile: null)).eligible, isFalse);
      expect(
        _check()(
          EligibilityDraft(
            profile: _profile(
              const CorporatePolicySummary(),
              status: CorporateMemberStatus.disabled,
            ),
          ),
        ).eligible,
        isFalse,
      );
    });

    test('a category outside the policy lists the allowed codes', () {
      final CorporateEligibility e = _check()(
        EligibilityDraft(
          profile: _profile(
            const CorporatePolicySummary(
              allowedRideCategoryCodes: <String>['economy', 'comfort'],
            ),
          ),
          categoryCode: 'premium',
        ),
      );
      expect(_rules(e), <PolicyRule>{PolicyRule.category});
      expect(e.violations.single.allowed, <String>['economy', 'comfort']);
    });

    test('the day uses Riyadh time with Sunday = 0', () {
      const CorporatePolicySummary policy = CorporatePolicySummary(
        allowedDays: <int>[1, 2, 3, 4],
      );
      const CorporatePolicySummary sundayOnly = CorporatePolicySummary(
        allowedDays: <int>[0],
      );
      // Sunday 2026-10-04 12:00 Riyadh.
      expect(
        _rules(_check()(EligibilityDraft(profile: _profile(policy)))),
        <PolicyRule>{PolicyRule.day},
      );
      expect(
        _check()(EligibilityDraft(profile: _profile(sundayOnly))).eligible,
        isTrue,
      );
      // Saturday 23:30 UTC is already Sunday 02:30 in Riyadh.
      expect(
        _check()(
          EligibilityDraft(
            profile: _profile(sundayOnly),
            scheduledAt: DateTime.utc(2026, 10, 3, 23, 30),
          ),
        ).eligible,
        isTrue,
      );
    });

    test('time windows are checked on the pickup time in Riyadh', () {
      const CorporatePolicySummary policy = CorporatePolicySummary(
        timeWindows: <TimeWindow>[TimeWindow(from: '07:00', to: '22:00')],
      );
      expect(
        _check()(EligibilityDraft(profile: _profile(policy))).eligible,
        isTrue,
      );
      final CorporateEligibility late = _check()(
        EligibilityDraft(
          profile: _profile(policy),
          // 23:30 Riyadh.
          scheduledAt: DateTime.utc(2026, 10, 4, 20, 30),
        ),
      );
      expect(_rules(late), <PolicyRule>{PolicyRule.timeWindow});
      expect(late.violations.single.allowed, <String>['07:00-22:00']);
    });

    test('a window crossing midnight wraps', () {
      const TimeWindow night = TimeWindow(from: '22:00', to: '06:00');
      expect(night.contains(23 * 60), isTrue);
      expect(night.contains(3 * 60), isTrue);
      expect(night.contains(12 * 60), isFalse);
      expect(const TimeWindow(from: 'x', to: 'y').contains(1), isTrue);
    });

    test('scheduled rides, per-trip limit and budget', () {
      final CorporateProfile profile = _profile(
        const CorporatePolicySummary(maxFarePerTrip: 50, allowScheduled: false),
        budget: const CorporateBudget(monthly: 100, spent: 70, remaining: 30),
      );
      final CorporateEligibility e = _check()(
        EligibilityDraft(
          profile: profile,
          scheduledAt: DateTime.utc(2026, 10, 5, 9),
          estimatedFare: 60,
        ),
      );
      expect(_rules(e), <PolicyRule>{
        PolicyRule.scheduled,
        PolicyRule.maxFare,
        PolicyRule.budget,
      });
      expect(
        e.violations.firstWhere((v) => v.rule == PolicyRule.maxFare).limit,
        50,
      );
      expect(
        e.violations.firstWhere((v) => v.rule == PolicyRule.budget).limit,
        30,
      );
      expect(e.remainingBudget, 30);
    });

    test('a fare within the limits passes; an exhausted budget never does', () {
      final CorporateProfile ok = _profile(
        const CorporatePolicySummary(maxFarePerTrip: 50),
        budget: const CorporateBudget(monthly: 100, spent: 10, remaining: 90),
      );
      expect(
        _check()(EligibilityDraft(profile: ok, estimatedFare: 50)).eligible,
        isTrue,
      );
      final CorporateProfile spent = _profile(
        const CorporatePolicySummary(),
        budget: const CorporateBudget(monthly: 100, spent: 100, remaining: 0),
      );
      expect(_rules(_check()(EligibilityDraft(profile: spent))), <PolicyRule>{
        PolicyRule.budget,
      });
    });

    test('the API evaluation is merged; it wins on the same rule', () {
      final CorporateProfile profile = _profile(
        const CorporatePolicySummary(maxFarePerTrip: 50),
      );
      final CorporateEligibility e = _check()(
        EligibilityDraft(
          profile: profile,
          estimatedFare: 60,
          serverCheck: const CorporateQuoteCheck(
            allowed: false,
            violations: <PolicyViolation>[
              PolicyViolation(rule: PolicyRule.maxFare, limit: 55),
              PolicyViolation(
                rule: PolicyRule.zone,
                allowed: <String>['الشمال'],
              ),
              PolicyViolation(rule: PolicyRule.purposeRequired),
            ],
            remainingBudget: 79.5,
          ),
        ),
      );
      expect(_rules(e), <PolicyRule>{PolicyRule.maxFare, PolicyRule.zone});
      expect(
        e.violations.firstWhere((v) => v.rule == PolicyRule.maxFare).limit,
        55,
      );
      // The form enforces the purpose / cost center, not this list.
      expect(_rules(e), isNot(contains(PolicyRule.purposeRequired)));
      expect(e.remainingBudget, 79.5);
    });

    test('allowed = false without a reason still blocks', () {
      final CorporateEligibility e = _check()(
        EligibilityDraft(
          profile: _profile(const CorporatePolicySummary()),
          serverCheck: const CorporateQuoteCheck(allowed: false),
        ),
      );
      expect(e.eligible, isFalse);
      expect(_rules(e), <PolicyRule>{PolicyRule.unknown});
    });

    test('an allowed server answer keeps the profile budget', () {
      final CorporateEligibility e = _check()(
        const EligibilityDraft(
          profile: testCorporateProfile,
          categoryCode: 'economy',
          serverCheck: CorporateQuoteCheck(allowed: true),
        ),
      );
      expect(e.eligible, isTrue);
      expect(e.remainingBudget, 1079.5);
    });
  });

  group('policy error mapping', () {
    final AppLocalizationsAr ar = AppLocalizationsAr();
    final AppLocalizationsEn en = AppLocalizationsEn();

    ServerFailure failure(String code, {Map<String, dynamic>? details}) =>
        ServerFailure(code: code, message: 'server', details: details);

    test('every F19 code has its localized text', () {
      const Map<String, String> expected = <String, String>{
        'corporate_not_member': 'لست عضواً في حساب شركة',
        'corporate_account_inactive': 'حساب الشركة غير نشط',
        'corporate_member_elsewhere': 'الرقم مرتبط بحساب شركة آخر',
        'corporate_policy_violation': 'الرحلة تخالف سياسة شركتك',
        'corporate_budget_exceeded': 'تجاوزت الميزانية الشهرية المتاحة',
        'corporate_credit_limit_exceeded':
            'تجاوز حساب الشركة الحد الائتماني، تواصل مع مسؤول الشركة',
        'invitation_expired':
            'انتهت صلاحية الدعوة، اطلب من شركتك إعادة إرسالها',
      };
      expected.forEach((String code, String text) {
        expect(failureText(failure(code), ar), text, reason: code);
      });
      expect(failureText(failure('corporate_not_member'), en), isNot(ar));
    });

    test('a policy violation lists the broken rules and allowed options', () {
      final String text = failureText(
        failure(
          'corporate_policy_violation',
          details: <String, dynamic>{
            'violations': <dynamic>[
              <String, dynamic>{
                'rule': 'category',
                'allowed': <String>['economy', 'comfort'],
              },
              <String, dynamic>{'rule': 'max_fare', 'limit': 150},
              <String, dynamic>{
                'rule': 'day',
                'allowed': <int>[0, 1],
              },
              <String, dynamic>{
                'rule': 'time_window',
                'allowed': <String>['07:00-22:00'],
              },
            ],
          },
        ),
        ar,
      );
      expect(text, contains('هذه الفئة غير مسموحة في سياسة شركتك'));
      expect(text, contains('الفئات المسموحة: economy، comfort'));
      expect(text, contains('الأجرة تتجاوز الحد الأقصى للرحلة (150 ر.س)'));
      expect(text, contains('الأيام المسموحة: الأحد، الاثنين'));
      expect(text, contains('الساعات المسموحة: 07:00 – 22:00'));
    });

    test('a violation without details falls back to the generic text', () {
      expect(
        failureText(failure('corporate_policy_violation'), ar),
        'الرحلة تخالف سياسة شركتك',
      );
    });

    test('the budget error names what is left when the API says so', () {
      expect(
        failureText(
          failure(
            'corporate_budget_exceeded',
            details: <String, dynamic>{'remaining': 79.5},
          ),
          ar,
        ),
        'تجاوزت الميزانية الشهرية المتاحة، المتبقي 79.5 ر.س',
      );
    });

    test('rule texts and allowed lines', () {
      expect(
        CorporateText.violation(
          ar,
          const PolicyViolation(rule: PolicyRule.scheduled),
        ),
        'الحجز المجدول غير مسموح في سياسة شركتك',
      );
      expect(
        CorporateText.violation(
          ar,
          const PolicyViolation(rule: PolicyRule.budget, limit: 12),
        ),
        'تجاوزت الميزانية الشهرية المتاحة، المتبقي 12 ر.س',
      );
      expect(
        CorporateText.allowedOptions(
          ar,
          const PolicyViolation(
            rule: PolicyRule.zone,
            allowed: <String>['الشمال', 'الغرب'],
          ),
        ),
        'المناطق المسموحة: الشمال، الغرب',
      );
      expect(
        CorporateText.allowedOptions(
          ar,
          const PolicyViolation(rule: PolicyRule.scheduled),
        ),
        isNull,
      );
    });
  });
}
