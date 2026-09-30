import 'package:ata_app/features/corporate/data/models/corporate_check_model.dart';
import 'package:ata_app/features/corporate/data/models/corporate_invitation_model.dart';
import 'package:ata_app/features/corporate/data/models/corporate_profile_model.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/features/corporate/domain/entities/trip_corporate.dart';
import 'package:ata_app/features/payments/data/models/receipt_model.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:flutter_test/flutter_test.dart';

/// `GET /passenger/corporate` exactly as in `docs/12` §F19.4.
const Map<String, dynamic> profileJson = <String, dynamic>{
  'membership': <String, dynamic>{
    'corporateUserId': 'cu1',
    'accountId': 'ca1',
    'companyName': 'شركة المثال',
    'role': 'employee',
    'employeeNumber': 'E-17',
    'department': 'IT',
    'status': 'active',
  },
  'policy': <String, dynamic>{
    'name': 'Default',
    'allowedRideCategoryCodes': <String>['economy', 'comfort'],
    'timeWindows': <Map<String, dynamic>>[
      <String, dynamic>{'from': '07:00', 'to': '22:00'},
    ],
    'allowedDays': null,
    'maxFarePerTrip': 150.00,
    'requirePurpose': true,
    'requireCostCenter': false,
    'allowScheduled': true,
  },
  'budget': <String, dynamic>{
    'monthly': 1500.00,
    'spent': 420.50,
    'remaining': 1079.50,
  },
  'costCenters': <Map<String, dynamic>>[
    <String, dynamic>{'id': 'cc1', 'code': 'IT-01', 'name': 'IT'},
  ],
};

void main() {
  group('CorporateProfileModel', () {
    test('parses the documented response', () {
      final CorporateProfile profile = CorporateProfileModel.fromBody(
        profileJson,
      )!;
      expect(profile.isActive, isTrue);
      expect(profile.membership.companyName, 'شركة المثال');
      expect(profile.membership.role, CorporateRole.employee);
      expect(profile.membership.employeeNumber, 'E-17');
      expect(profile.policy.allowedRideCategoryCodes, <String>[
        'economy',
        'comfort',
      ]);
      expect(profile.policy.timeWindows!.single.from, '07:00');
      expect(profile.policy.allowedDays, isNull);
      expect(profile.perTripLimit, 150);
      expect(profile.policy.requirePurpose, isTrue);
      expect(profile.policy.requireCostCenter, isFalse);
      expect(profile.budget!.remaining, 1079.5);
      expect(profile.budget!.usedFraction, closeTo(0.2803, 0.001));
      expect(profile.costCenterById('cc1')!.code, 'IT-01');
    });

    test('a null, empty or membership-less body means "not a member"', () {
      expect(CorporateProfileModel.fromBody(null), isNull);
      expect(CorporateProfileModel.fromBody(const <String, dynamic>{}), isNull);
      expect(CorporateProfileModel.fromBody('nope'), isNull);
      expect(CorporateProfileModel.fromBody(<dynamic>[]), isNull);
    });

    test('is tolerant: missing policy / budget, empty lists, odd values', () {
      final CorporateProfile profile = CorporateProfileModel.fromBody(
        <String, dynamic>{
          'membership': <String, dynamic>{
            'companyName': 'X',
            'role': 'corporate_admin',
            'status': 'something-new',
          },
          'policy': <String, dynamic>{
            'allowedRideCategoryCodes': <String>[],
            'timeWindows': <dynamic>[],
            'allowedDays': <dynamic>[1, 2.0, 'x'],
          },
          'budget': <String, dynamic>{'monthly': 100, 'spent': 30},
        },
      )!;
      // An unknown status is never treated as active.
      expect(profile.membership.status, CorporateMemberStatus.disabled);
      expect(profile.isActive, isFalse);
      expect(profile.membership.isAdmin, isTrue);
      expect(profile.policy.allowedRideCategoryCodes, isNull);
      expect(profile.policy.timeWindows, isNull);
      expect(profile.policy.allowedDays, <int>[1, 2]);
      expect(profile.policy.allowScheduled, isTrue);
      // `remaining` is derived when the API omits it.
      expect(profile.budget!.remaining, 70);
      expect(profile.costCenters, isEmpty);
    });

    test('no budget means no monthly limit', () {
      final CorporateProfile profile = CorporateProfileModel.fromBody(
        <String, dynamic>{
          'membership': profileJson['membership'],
          'budget': null,
        },
      )!;
      expect(profile.budget, isNull);
    });
  });

  group('CorporateInvitationModel', () {
    const Map<String, dynamic> row = <String, dynamic>{
      'id': 'i1',
      'companyName': 'شركة المثال',
      'role': 'employee',
      'expiresAt': '2026-10-07T10:00:00Z',
    };

    test('parses a bare array', () {
      final List<CorporateInvitation> list = CorporateInvitationModel.listFrom(
        <dynamic>[row, 'garbage', 4],
      );
      expect(list, hasLength(1));
      expect(list.single.id, 'i1');
      expect(list.single.expiresAt, DateTime.utc(2026, 10, 7, 10));
      expect(list.single.isExpiredAt(DateTime.utc(2026, 10, 8)), isTrue);
      expect(list.single.isExpiredAt(DateTime.utc(2026, 10, 1)), isFalse);
    });

    test('parses a paged envelope and ignores anything else', () {
      final List<CorporateInvitation> paged = CorporateInvitationModel.listFrom(
        <String, dynamic>{
          'items': <dynamic>[row],
          'page': 1,
          'pageSize': 20,
          'total': 1,
        },
      );
      expect(paged.single.companyName, 'شركة المثال');
      expect(CorporateInvitationModel.listFrom(null), isEmpty);
      expect(CorporateInvitationModel.listFrom('x'), isEmpty);
      expect(
        CorporateInvitationModel.listFrom(const <String, dynamic>{}),
        isEmpty,
      );
    });

    test('an invitation without an expiry never expires', () {
      final CorporateInvitation i = CorporateInvitationModel.fromJson(
        const <String, dynamic>{'id': 'i2', 'companyName': 'Y'},
      );
      expect(i.expiresAt, isNull);
      expect(i.isExpiredAt(DateTime.utc(2100)), isFalse);
      expect(i.role, CorporateRole.employee);
    });
  });

  group('PolicyViolation', () {
    test('parses rule, limit and allowed options', () {
      final List<PolicyViolation> list = PolicyViolation.listFrom(<dynamic>[
        <String, dynamic>{
          'rule': 'category',
          'allowed': <String>['economy'],
        },
        <String, dynamic>{'rule': 'max_fare', 'limit': 150},
        <String, dynamic>{
          'rule': 'time_window',
          'allowed': <dynamic>[
            <String, dynamic>{'from': '07:00', 'to': '22:00'},
          ],
        },
        'purpose_required',
        <String, dynamic>{'rule': 'from_the_future'},
        42,
      ]);
      expect(list.map((PolicyViolation v) => v.rule), <PolicyRule>[
        PolicyRule.category,
        PolicyRule.maxFare,
        PolicyRule.timeWindow,
        PolicyRule.purposeRequired,
        PolicyRule.unknown,
        PolicyRule.unknown,
      ]);
      expect(list[0].allowed, <String>['economy']);
      expect(list[1].limit, 150);
      expect(list[2].allowed, <String>['07:00-22:00']);
    });

    test('anything but a list is empty', () {
      expect(PolicyViolation.listFrom(null), isEmpty);
      expect(PolicyViolation.listFrom('x'), isEmpty);
    });
  });

  group('corporate fields of the quote, the trip and the receipt', () {
    test('the quote carries the policy evaluation', () {
      final CorporateQuoteCheck? check = CorporateCheckModel.quoteCheck(
        <String, dynamic>{
          'corporate': <String, dynamic>{
            'allowed': false,
            'violations': <dynamic>[
              <String, dynamic>{'rule': 'zone'},
            ],
            'remainingBudget': 79.5,
          },
        },
      );
      expect(check!.allowed, isFalse);
      expect(check.violations.single.rule, PolicyRule.zone);
      expect(check.remainingBudget, 79.5);
      expect(CorporateCheckModel.quoteCheck(const <String, dynamic>{}), isNull);

      final QuoteModel quote = QuoteModel.fromJson(const <String, dynamic>{
        'quoteId': 'q1',
        'corporate': <String, dynamic>{'allowed': true},
      });
      expect(quote.corporate!.allowed, isTrue);
      expect(quote.toJson()['corporate'], isNotNull);
      expect(QuoteModel.fromJson(const <String, dynamic>{}).corporate, isNull);
    });

    test('the trip carries company, purpose and cost center', () {
      final TripModel trip = TripModel.fromJson(const <String, dynamic>{
        'id': 't1',
        'tripNumber': 'T-1',
        'status': 'completed',
        'paymentMethod': 'corporate',
        'corporate': <String, dynamic>{
          'companyName': 'شركة المثال',
          'purpose': 'اجتماع عميل',
          'costCenter': 'IT-01',
          'isGuest': false,
          'guestName': null,
        },
      });
      expect(trip.isCorporate, isTrue);
      expect(trip.corporate!.purpose, 'اجتماع عميل');
      expect(trip.corporate!.costCenter, 'IT-01');
      expect(trip.corporate!.isGuest, isFalse);
      final TripCorporate round = TripModel.fromJson(trip.toJson()).corporate!;
      expect(round, trip.corporate);
      // A trip without the object stays a plain trip.
      expect(
        TripModel.fromJson(const <String, dynamic>{
          'id': 't2',
          'paymentMethod': 'cash',
        }).isCorporate,
        isFalse,
      );
    });

    test('the receipt reads the same object', () {
      final receipt = ReceiptModel.fromJson(<String, dynamic>{
        'tripId': 't1',
        'payment': <String, dynamic>{'method': 'corporate'},
        'corporate': <String, dynamic>{
          'companyName': 'شركة المثال',
          'purpose': 'زيارة',
        },
      });
      expect(receipt.isCorporate, isTrue);
      expect(receipt.corporate!.purpose, 'زيارة');
      expect(receipt.corporate!.hasCostCenter, isFalse);
      // The method alone is enough to know the company paid.
      expect(
        ReceiptModel.fromJson(<String, dynamic>{
          'payment': <String, dynamic>{'method': 'corporate'},
        }).isCorporate,
        isTrue,
      );
    });
  });
}
