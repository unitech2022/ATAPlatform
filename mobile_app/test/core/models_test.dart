import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/auth/data/models/auth_session_model.dart';
import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/catalog/data/models/ride_category_model.dart';
import 'package:ata_app/features/driver_dashboard/data/models/earnings_summary_model.dart';
import 'package:ata_app/features/driver_onboarding/data/models/driver_application_model.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:ata_app/features/rides/data/models/trip_summary_model.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/wallet/data/models/wallet_summary_model.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('AuthSessionModel parses the AuthResponse contract', () {
    final AuthSessionModel session = AuthSessionModel.fromJson(
      const <String, dynamic>{
        'accessToken': 'jwt',
        'accessTokenExpiresIn': 3600,
        'refreshToken': 'opaque',
        'isNewUser': true,
        'user': <String, dynamic>{
          'id': 'uuid',
          'phoneNumber': '+966512345678',
          'fullName': null,
          'language': 'ar',
          'gender': 'unknown',
          'roles': <String>['driver'],
          'termsAcceptedAt': null,
          'createdAt': '2026-09-28T12:00:00Z',
        },
        'driver': <String, dynamic>{
          'applicationNumber': 'ATA-28419',
          'applicationStatus': 'draft',
        },
      },
      activeRole: UserRole.driver,
    );

    expect(session.accessToken, 'jwt');
    expect(session.isNewUser, isTrue);
    expect(session.user.hasAcceptedTerms, isFalse);
    expect(session.user.createdAt, isNotNull);
    expect(session.driver?.applicationNumber, 'ATA-28419');
    expect(session.driver?.applicationStatus, DriverApplicationStatus.draft);
    expect(session.isDriverApproved, isFalse);
  });

  test('RideCategoryModel parses estimate and defaults', () {
    final RideCategoryModel category = RideCategoryModel.fromJson(
      const <String, dynamic>{
        'id': 'c1',
        'code': 'economy',
        'name': 'اقتصادي',
        'description': 'سيارة مريحة',
        'icon': 'car',
        'seats': 4,
        'maxStops': 2,
        'sortOrder': 2,
        'estimate': <String, dynamic>{'etaMinutes': 2, 'price': 38},
      },
    );
    expect(category.estimate?.price, 38);
    expect(category.maxStops, 2);

    final RideCategoryModel bare = RideCategoryModel.fromJson(
      const <String, dynamic>{
        'id': 'c2',
        'code': 'airport',
        'name': 'المطار',
        'estimate': null,
      },
    );
    expect(bare.estimate, isNull);
    expect(bare.icon, 'car');
  });

  test('WalletSummaryModel parses payment methods', () {
    final WalletSummaryModel wallet = WalletSummaryModel.fromJson(
      const <String, dynamic>{
        'id': 'w',
        'kind': 'passenger',
        'currency': 'SAR',
        'balance': 125.0,
        'paymentMethods': <Map<String, dynamic>>[
          <String, dynamic>{
            'type': 'wallet',
            'label': 'محفظة ATA',
            'isDefault': true,
          },
          <String, dynamic>{'type': 'cash', 'label': 'الدفع نقداً'},
        ],
      },
    );
    expect(wallet.balance, 125);
    expect(wallet.paymentMethods.first.isDefault, isTrue);
    expect(wallet.paymentMethods.last.type, 'cash');
  });

  test('DriverApplicationModel parses documents, required list and steps', () {
    final DriverApplicationModel application = DriverApplicationModel.fromJson(
      const <String, dynamic>{
        'applicationNumber': 'ATA-1',
        'status': 'under_review',
        'rejectionReason': null,
        'profile': <String, dynamic>{'fullName': 'خالد أحمد'},
        'vehicle': <String, dynamic>{
          'id': 'v1',
          'make': 'تويوتا',
          'model': 'كامري',
          'year': 2023,
          'color': 'أبيض',
          'plateNumber': 'أ ب ج 2841',
          'seats': 4,
        },
        'documents': <Map<String, dynamic>>[
          <String, dynamic>{
            'id': 'd1',
            'documentTypeId': 't1',
            'documentTypeCode': 'national_id',
            'documentTypeName': 'الهوية الوطنية',
            'status': 'verified',
            'expiresAt': '2027-06-18T00:00:00Z',
          },
        ],
        'requiredDocuments': <Map<String, dynamic>>[
          <String, dynamic>{
            'documentTypeId': 't1',
            'code': 'national_id',
            'name': 'الهوية الوطنية',
            'appliesTo': 'driver',
            'isRequired': true,
            'requiresExpiry': true,
            'uploaded': true,
          },
        ],
        'steps': <String, dynamic>{
          'profileComplete': true,
          'vehicleComplete': true,
          'documentsComplete': false,
          'canSubmit': false,
        },
      },
    );
    expect(application.status, DriverApplicationStatus.underReview);
    expect(application.fullName, 'خالد أحمد');
    expect(application.vehicle?.plateNumber, 'أ ب ج 2841');
    expect(application.documentFor('t1')?.status, DocumentStatus.verified);
    expect(application.requiredDocuments.single.uploaded, isTrue);
    expect(application.steps.canSubmit, isFalse);
  });

  test('EarningsSummaryModel maps nested today/week objects', () {
    final EarningsSummaryModel earnings = EarningsSummaryModel.fromJson(
      const <String, dynamic>{
        'today': <String, dynamic>{
          'earnings': 286,
          'trips': 12,
          'onlineHours': 6.5,
        },
        'week': <String, dynamic>{'earnings': 1840, 'target': 2500},
        'ratingAvg': 4.9,
      },
    );
    expect(earnings.todayTrips, 12);
    expect(earnings.weekProgress, closeTo(0.736, 0.001));
  });

  test('PageResult parses items with the given parser', () {
    final PageResult<TripSummaryModel> page =
        PageResult<TripSummaryModel>.fromJson(const <String, dynamic>{
          'items': <Map<String, dynamic>>[
            <String, dynamic>{
              'id': 't1',
              'destinationName': 'واجهة الرياض',
              'pickupName': 'حي النرجس',
              'status': 'completed',
              'fare': 38,
              'categoryName': 'اقتصادي',
              'completedAt': '2026-09-28T07:35:00Z',
            },
          ],
          'page': 1,
          'pageSize': 20,
          'total': 1,
        }, TripSummaryModel.fromJson);
    expect(page.items.single.status, TripStatus.completed);
    expect(page.hasMore, isFalse);
    expect(const PageResult<int>.empty().isEmpty, isTrue);
  });
}
