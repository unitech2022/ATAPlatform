import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_cancellation_reasons.dart';
import 'package:ata_app/features/trip/domain/usecases/preview_cancellation.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/trip_fakes.dart';

class _MockReasons extends Mock implements GetCancellationReasons {}

class _MockPreview extends Mock implements PreviewCancellation {}

class _MockCancel extends Mock implements CancelTrip {}

void main() {
  late _MockReasons reasons;
  late _MockPreview preview;
  late _MockCancel cancel;

  const CancellationReason changedMind = CancellationReason(
    code: 'changed_mind',
    name: 'غيرت رأيي',
  );
  const CancellationReason other = CancellationReason(
    code: 'other',
    name: 'أخرى',
    requiresNote: true,
  );
  const CancelPreview tenRiyals = CancelPreview(
    stage: 'en_route',
    fee: 10,
    isFree: false,
    message: 'سيتم خصم 10.00 ر.س رسوم إلغاء لأن الكابتن في الطريق إليك',
  );

  setUpAll(() {
    registerFallbackValue(
      const CancellationReasonsParams(actor: TripActor.passenger),
    );
    registerFallbackValue(
      const PreviewCancellationParams(tripId: '', actor: TripActor.passenger),
    );
    registerFallbackValue(
      const CancelTripParams(
        tripId: '',
        actor: TripActor.passenger,
        reasonCode: '',
      ),
    );
  });

  setUp(() {
    reasons = _MockReasons();
    preview = _MockPreview();
    cancel = _MockCancel();
    when(() => reasons(any())).thenAnswer(
      (_) async => const Right<Failure, List<CancellationReason>>(
        <CancellationReason>[changedMind, other],
      ),
    );
    when(
      () => preview(any()),
    ).thenAnswer((_) async => const Right<Failure, CancelPreview>(tenRiyals));
  });

  CancelFlowCubit build({TripActor actor = TripActor.passenger}) =>
      CancelFlowCubit(
        getReasons: reasons,
        preview: preview,
        cancelTrip: cancel,
        tripId: 't1',
        actor: actor,
        stage: 'en_route',
      );

  blocTest<CancelFlowCubit, CancelFlowState>(
    'loads the reasons for the stage, previews the fee, confirms with it',
    build: build,
    setUp: () => when(() => cancel(any())).thenAnswer(
      (_) async => Right<Failure, Trip>(tripAt(TripStage.cancelled)),
    ),
    act: (CancelFlowCubit cubit) async {
      await cubit.load();
      await cubit.select(changedMind);
      expect(cubit.state.preview, tenRiyals);
      expect(cubit.state.canConfirm, isTrue);
      await cubit.confirm();
    },
    verify: (CancelFlowCubit cubit) {
      final CancellationReasonsParams r =
          verify(() => reasons(captureAny())).captured.single
              as CancellationReasonsParams;
      expect(r.stage, 'en_route');
      final CancelTripParams p =
          verify(() => cancel(captureAny())).captured.single
              as CancelTripParams;
      expect(p.reasonCode, 'changed_mind');
      expect(p.expectedFee, 10);
      expect(p.expectedPenaltyPoints, isNull);
      expect(cubit.state.status, CancelFlowStatus.done);
      expect(cubit.state.cancelledTrip?.status, TripStage.cancelled);
    },
  );

  blocTest<CancelFlowCubit, CancelFlowState>(
    'a reason requiring a note cannot be confirmed without one',
    build: build,
    setUp: () => when(() => cancel(any())).thenAnswer(
      (_) async => Right<Failure, Trip>(tripAt(TripStage.cancelled)),
    ),
    act: (CancelFlowCubit cubit) async {
      await cubit.load();
      await cubit.select(other);
      expect(cubit.state.canConfirm, isFalse);
      await cubit.confirm();
      expect(cubit.state.noteMissing, isTrue);
      cubit.noteChanged('الطريق مغلق');
      await cubit.confirm();
    },
    verify: (_) {
      final CancelTripParams p =
          verify(() => cancel(captureAny())).captured.single
              as CancelTripParams;
      expect(p.note, 'الطريق مغلق');
    },
  );

  blocTest<CancelFlowCubit, CancelFlowState>(
    'cancellation_fee_changed re-runs the preview and keeps the sheet open',
    build: build,
    setUp: () => when(() => cancel(any())).thenAnswer(
      (_) async => const Left<Failure, Trip>(
        ServerFailure(
          code: 'cancellation_fee_changed',
          message: '',
          details: <String, dynamic>{'fee': 15},
          statusCode: 409,
        ),
      ),
    ),
    act: (CancelFlowCubit cubit) async {
      await cubit.load();
      await cubit.select(changedMind);
      await cubit.confirm();
    },
    verify: (CancelFlowCubit cubit) {
      verify(() => preview(any())).called(2);
      expect(cubit.state.feeChanged, isTrue);
      expect(cubit.state.status, CancelFlowStatus.ready);
      expect(cubit.state.preview, tenRiyals);
      expect(cubit.state.cancelledTrip, isNull);
    },
  );

  blocTest<CancelFlowCubit, CancelFlowState>(
    'drivers confirm with the previewed penalty points',
    build: () => build(actor: TripActor.driver),
    setUp: () {
      when(() => preview(any())).thenAnswer(
        (_) async => const Right<Failure, CancelPreview>(
          CancelPreview(stage: 'en_route', penaltyPoints: 3, isFree: false),
        ),
      );
      when(() => cancel(any())).thenAnswer(
        (_) async => Right<Failure, Trip>(tripAt(TripStage.cancelled)),
      );
    },
    act: (CancelFlowCubit cubit) async {
      await cubit.load();
      await cubit.select(changedMind);
      await cubit.confirm();
    },
    verify: (_) {
      final CancelTripParams p =
          verify(() => cancel(captureAny())).captured.single
              as CancelTripParams;
      expect(p.expectedPenaltyPoints, 3);
      expect(p.expectedFee, isNull);
    },
  );
}
