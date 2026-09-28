import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/data/models/cancellation_models.dart';
import 'package:ata_app/features/trip/data/models/reliability_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/get_reliability.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGet extends Mock implements GetReliability {}

const Map<String, dynamic> _driverJson = <String, dynamic>{
  'role': 'driver',
  'level': 'matching_deprioritized',
  'restrictedUntil': null,
  'windowDays': 30,
  'tripsAccepted': 40,
  'tripsCompleted': 33,
  'cancellationsAtFault': 7,
  'cancellationRate': 0.175,
  'reliabilityRate': 0.825,
  'noShowCount': 0,
  'penaltyPoints': 9,
  'nextLevel': <String, dynamic>{
    'level': 'incentives_reduced',
    'minPenaltyPoints': 12,
    'minCancellationRate': 0.2,
  },
  'recentEvents': <Map<String, dynamic>>[
    <String, dynamic>{
      'tripId': 't9',
      'tripNumber': 'T-20260927-00009',
      'stage': 'en_route',
      'reasonName': 'الراكب لا يرد',
      'feeCharged': 0,
      'penaltyPoints': 3,
      'excuseStatus': 'not_applicable',
      'createdAt': '2026-09-27T10:00:00Z',
    },
  ],
  'offersReceived': 50,
  'offersAccepted': 40,
  'acceptanceRate': 0.8,
  'effects': <String, dynamic>{'matchingFactor': 0.7, 'incentiveMultiplier': 1},
};

void main() {
  late _MockGet get;

  setUp(() => get = _MockGet());

  test('parses the driver ReliabilitySummary with effects', () {
    final ReliabilitySummary s = ReliabilityModel.fromJson(_driverJson);
    expect(s.level, RestrictionLevel.matchingDeprioritized);
    expect(s.level.isDeprioritized, isTrue);
    expect(s.nextLevel?.minPenaltyPoints, 12);
    expect(s.recentEvents.single.penaltyPoints, 3);
    expect(s.acceptanceRate, 0.8);
    expect(s.matchingFactor, 0.7);
    expect(s.isDriver, isTrue);
  });

  test('parses the cancel preview and the Trip.cancellation object', () {
    final CancelPreview p = CancelPreviewModel.fromJson(<String, dynamic>{
      'stage': 'scheduled',
      'bookingType': 'scheduled',
      'fee': 12.5,
      'isFree': false,
      'freeUntil': null,
      'requiresReview': true,
      'message': 'رسوم محتملة',
    });
    expect(p.isScheduled, isTrue);
    expect(p.requiresReview, isTrue);
    final TripModel trip = TripModel.fromJson(const <String, dynamic>{
      'id': 't1',
      'status': 'cancelled',
      'cancellation': <String, dynamic>{
        'stage': 'no_show',
        'reasonCode': 'passenger_no_show',
        'reasonName': 'لم يحضر الراكب',
        'atFault': 'passenger',
        'compensation': 8,
        'feeStatus': 'charged',
        'excuseStatus': 'not_applicable',
      },
    });
    expect(trip.cancellation?.isNoShow, isTrue);
    expect(trip.cancellation?.compensation, 8);
    expect(TripModel.fromJson(trip.toJson()).cancellation, trip.cancellation);
  });

  blocTest<ReliabilityCubit, ReliabilityState>(
    'loads the summary for the role',
    build: () => ReliabilityCubit(getReliability: get, role: TripActor.driver),
    setUp: () => when(() => get(TripActor.driver)).thenAnswer(
      (_) async => Right<Failure, ReliabilitySummary>(
        ReliabilityModel.fromJson(_driverJson),
      ),
    ),
    act: (ReliabilityCubit cubit) => cubit.load(),
    expect: () => <dynamic>[
      const ReliabilityState(loading: true),
      isA<ReliabilityState>()
          .having((ReliabilityState s) => s.loading, 'loading', false)
          .having(
            (ReliabilityState s) => s.summary?.penaltyPoints,
            'points',
            9,
          ),
    ],
  );

  blocTest<ReliabilityCubit, ReliabilityState>(
    'keeps the failure when the summary cannot be loaded',
    build: () =>
        ReliabilityCubit(getReliability: get, role: TripActor.passenger),
    setUp: () => when(() => get(TripActor.passenger)).thenAnswer(
      (_) async => const Left<Failure, ReliabilitySummary>(
        NetworkFailure(message: 'offline'),
      ),
    ),
    act: (ReliabilityCubit cubit) => cubit.load(),
    expect: () => const <ReliabilityState>[
      ReliabilityState(loading: true),
      ReliabilityState(failure: NetworkFailure(message: 'offline')),
    ],
  );
}
