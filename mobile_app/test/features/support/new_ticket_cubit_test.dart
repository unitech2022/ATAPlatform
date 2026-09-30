import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_trips.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:ata_app/features/rides/domain/usecases/get_passenger_trips.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:ata_app/features/support/domain/usecases/create_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/get_support_trips.dart';
import 'package:ata_app/features/support/presentation/cubit/fare_dispute_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/new_ticket_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/support_fakes.dart';
import '../../helpers/test_app.dart';

void main() {
  late FakeSupportRepository repo;
  late FakeRidesRepository rides;
  late FakeDriverTripsRepository driverTrips;

  setUp(() async {
    await registerTestDependencies();
    repo = getIt<SupportRepository>() as FakeSupportRepository;
    rides = getIt<RidesRepository>() as FakeRidesRepository;
    driverTrips =
        getIt<DriverDashboardRepository>() as FakeDriverTripsRepository;
  });
  tearDown(getIt.reset);

  NewTicketCubit cubit({
    TripActor actor = TripActor.passenger,
    TicketType? type,
    String? tripId,
    String subject = '',
  }) => NewTicketCubit(
    getTrips: GetSupportTrips(
      getIt<GetPassengerTrips>(),
      getIt<GetDriverTrips>(),
    ),
    create: CreateTicket(repo),
    actor: actor,
    initialType: type,
    tripId: tripId,
    initialSubject: subject,
  );

  group('NewTicketCubit', () {
    test('loads the recent finished trips only', () async {
      rides.trips = <TripSummary>[
        fakeSupportTrip('a'),
        fakeSupportTrip('b', status: TripStatus.cancelled),
        fakeSupportTrip('c', status: TripStatus.active),
        fakeSupportTrip('d', status: TripStatus.scheduled),
      ];
      final NewTicketCubit c = cubit();
      await c.start();
      expect(c.state.trips.map((TripSummary t) => t.id), <String>['a', 'b']);
      expect(c.state.tripsLoading, isFalse);
      await c.close();
    });

    test('drivers pick from their own trips', () async {
      rides.trips = <TripSummary>[fakeSupportTrip('rider-trip')];
      driverTrips.trips = <TripSummary>[fakeSupportTrip('driver-trip')];
      final NewTicketCubit c = cubit(actor: TripActor.driver);
      await c.start();
      expect(c.state.trips.single.id, 'driver-trip');
      await c.close();
    });

    test('a trip failure can be retried', () async {
      rides.failure = apiFailure('boom');
      final NewTicketCubit c = cubit();
      await c.start();
      expect(c.state.tripsFailure?.code, 'boom');
      rides.failure = null;
      rides.trips = <TripSummary>[fakeSupportTrip('a')];
      await c.retryTrips();
      expect(c.state.tripsFailure, isNull);
      expect(c.state.trips, hasLength(1));
      await c.close();
    });

    test('trip, payment and lost item tickets need a trip', () async {
      final NewTicketCubit c = cubit();
      c
        ..subjectChanged('مشكلة')
        ..messageChanged('تفاصيل')
        ..selectType(TicketType.tripIssue);
      expect(c.state.needsTrip, isTrue);
      expect(c.state.valid, isFalse);

      c.selectTrip('trip1');
      expect(c.state.valid, isTrue);

      c.selectType(TicketType.account);
      expect(c.state.needsTrip, isFalse);
      c.selectTrip(null);
      expect(c.state.valid, isTrue);
      await c.close();
    });

    test('subject and message are required and bounded', () async {
      final NewTicketCubit c = cubit(type: TicketType.other);
      expect(c.state.valid, isFalse);
      c.subjectChanged('موضوع');
      expect(c.state.valid, isFalse);
      c.messageChanged('   ');
      expect(c.state.valid, isFalse);
      c.messageChanged('نص');
      expect(c.state.valid, isTrue);
      c.subjectChanged('x' * 161);
      expect(c.state.valid, isFalse);
      await c.close();
    });

    test('an invalid form is not sent', () async {
      final NewTicketCubit c = cubit(type: TicketType.tripIssue);
      await c.submit();
      expect(repo.created, isEmpty);
      await c.close();
    });

    test('submits the ticket with the uploaded file ids', () async {
      final NewTicketCubit c = cubit(
        type: TicketType.tripIssue,
        tripId: 'trip1',
        subject: 'مشكلة',
      );
      c.messageChanged('  تفاصيل  ');
      await c.submit(fileIds: <String>['f1', 'f2']);
      final request = repo.created.single;
      expect(request.type, TicketType.tripIssue);
      expect(request.tripId, 'trip1');
      expect(request.message, 'تفاصيل');
      expect(request.fileIds, <String>['f1', 'f2']);
      expect(request.dispute, isNull);
      expect(c.state.created?.id, startsWith('new'));
      expect(c.state.submitting, isFalse);
      await c.close();
    });

    test('a ticket without a trip sends no tripId', () async {
      final NewTicketCubit c = cubit(type: TicketType.account, subject: 's');
      c.messageChanged('m');
      await c.submit();
      expect(repo.created.single.tripId, isNull);
      await c.close();
    });

    test('the dispute travels only with a payment issue of a trip', () async {
      const DisputeDraft draft = DisputeDraft(
        reason: DisputeReason.routeLonger,
        requestedRefundAmount: 12,
      );
      final NewTicketCubit pay = cubit(
        type: TicketType.paymentIssue,
        tripId: 'trip1',
        subject: 'أجرة',
      );
      pay.messageChanged('المسار أطول');
      expect(pay.state.canDispute, isTrue);
      await pay.submit(dispute: draft);
      expect(repo.created.last.dispute, draft);
      await pay.close();

      final NewTicketCubit other = cubit(
        type: TicketType.tripIssue,
        tripId: 'trip1',
        subject: 'رحلة',
      );
      other.messageChanged('م');
      expect(other.state.canDispute, isFalse);
      await other.submit(dispute: draft);
      expect(repo.created.last.dispute, isNull);
      await other.close();
    });

    for (final String code in <String>[
      'dispute_window_closed',
      'dispute_exists',
      'attachment_limit',
    ]) {
      test('$code keeps the form and shows the API error', () async {
        repo.createFailure = apiFailure(code);
        final NewTicketCubit c = cubit(
          type: TicketType.paymentIssue,
          tripId: 'trip1',
          subject: 'أجرة',
        );
        c.messageChanged('م');
        await c.submit(
          dispute: const DisputeDraft(reason: DisputeReason.overcharged),
        );
        expect(c.state.failure?.code, code);
        expect(c.state.created, isNull);
        expect(c.state.submitting, isFalse);
        expect(c.state.subject, 'أجرة');

        // Editing clears the error.
        c.messageChanged('م2');
        expect(c.state.failure, isNull);
        await c.close();
      });
    }

    test('a second tap while sending is ignored', () async {
      final NewTicketCubit c = cubit(type: TicketType.other, subject: 's');
      c.messageChanged('m');
      final Future<void> first = c.submit();
      final Future<void> second = c.submit();
      await Future.wait(<Future<void>>[first, second]);
      expect(repo.created, hasLength(1));
      await c.close();
    });
  });

  group('FareDisputeCubit', () {
    test('starts off with the first reason and no refund', () {
      final FareDisputeCubit c = FareDisputeCubit();
      expect(c.state.enabled, isFalse);
      expect(c.state.draft, isNull);
      expect(c.state.valid, isTrue);
      expect(FareDisputeCubit(enabled: true).state.enabled, isTrue);
    });

    test('builds the dispute draft with the optional refund', () {
      final FareDisputeCubit c = FareDisputeCubit(enabled: true);
      expect(c.state.draft?.reason, DisputeReason.overcharged);
      expect(c.state.draft?.requestedRefundAmount, isNull);

      c
        ..selectReason(DisputeReason.waitingCharged)
        ..refundChanged('12,5');
      expect(c.state.draft?.reason, DisputeReason.waitingCharged);
      expect(c.state.draft?.requestedRefundAmount, 12.5);
      expect(c.state.valid, isTrue);
    });

    test('a refund that is not a positive number blocks the form', () {
      final FareDisputeCubit c = FareDisputeCubit(enabled: true)
        ..refundChanged('abc');
      expect(c.state.refundInvalid, isTrue);
      expect(c.state.valid, isFalse);
      expect(c.state.draft?.requestedRefundAmount, isNull);

      c.refundChanged('0');
      expect(c.state.refundInvalid, isTrue);
      c.refundChanged('');
      expect(c.state.refundInvalid, isFalse);
      expect(c.state.valid, isTrue);
    });

    test('an invalid refund does not matter while the dispute is off', () {
      final FareDisputeCubit c = FareDisputeCubit()..refundChanged('abc');
      expect(c.state.valid, isTrue);
      c.toggle(enabled: true);
      expect(c.state.valid, isFalse);
      c.toggle(enabled: false);
      expect(c.state.draft, isNull);
    });
  });
}
