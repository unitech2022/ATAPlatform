import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/usecases/request_otp.dart';
import 'package:ata_app/features/auth/domain/usecases/verify_otp.dart';
import 'package:ata_app/features/auth/presentation/cubit/otp_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/otp_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fakes.dart';

class _MockVerifyOtp extends Mock implements VerifyOtp {}

class _MockRequestOtp extends Mock implements RequestOtp {}

/// Emits the whole countdown synchronously.
Stream<int> instantCountdown(int seconds) => Stream<int>.fromIterable(
  List<int>.generate(seconds, (int i) => seconds - i - 1),
);

void main() {
  late _MockVerifyOtp verifyOtp;
  late _MockRequestOtp requestOtp;

  setUpAll(() {
    registerFallbackValue(
      const VerifyOtpParams(
        requestId: '',
        phoneNumber: '',
        code: '',
        role: UserRole.passenger,
      ),
    );
    registerFallbackValue(
      const RequestOtpParams(
        phoneNumber: '',
        role: UserRole.passenger,
        language: 'ar',
      ),
    );
  });

  setUp(() {
    verifyOtp = _MockVerifyOtp();
    requestOtp = _MockRequestOtp();
  });

  OtpCubit build({OtpRequest request = testOtpRequest}) => OtpCubit(
    verifyOtp: verifyOtp,
    requestOtp: requestOtp,
    request: request,
    role: UserRole.passenger,
    language: 'ar',
    countdown: instantCountdown,
  );

  test('starts with the resend countdown and the dev code', () async {
    final OtpCubit cubit = build();
    expect(cubit.state.secondsLeft, 60);
    expect(cubit.state.devCode, '1234');
    expect(cubit.state.canResend, isFalse);
    await Future<void>.delayed(Duration.zero);
    expect(cubit.state.secondsLeft, 0);
    expect(cubit.state.canResend, isTrue);
    await cubit.close();
  });

  blocTest<OtpCubit, OtpState>(
    'accepts at most four digits',
    build: build,
    act: (OtpCubit cubit) {
      for (final String d in '12345'.split('')) {
        cubit.addDigit(d);
      }
    },
    verify: (OtpCubit cubit) {
      expect(cubit.state.code, '1234');
      expect(cubit.state.isComplete, isTrue);
    },
  );

  blocTest<OtpCubit, OtpState>(
    'verify emits the session on success',
    build: build,
    setUp: () => when(
      () => verifyOtp(any()),
    ).thenAnswer((_) async => const Right<Failure, AuthSession>(testSession)),
    act: (OtpCubit cubit) async {
      '1234'.split('').forEach(cubit.addDigit);
      await cubit.verify();
    },
    verify: (OtpCubit cubit) {
      expect(cubit.state.session, testSession);
      expect(cubit.state.verifying, isFalse);
      final VerifyOtpParams params =
          verify(() => verifyOtp(captureAny())).captured.single
              as VerifyOtpParams;
      expect(params.code, '1234');
      expect(params.requestId, 'req-1');
    },
  );

  blocTest<OtpCubit, OtpState>(
    'an invalid code clears the boxes and keeps the failure',
    build: build,
    setUp: () => when(() => verifyOtp(any())).thenAnswer(
      (_) async => const Left<Failure, AuthSession>(
        ServerFailure(
          code: 'otp_invalid',
          message: '',
          details: <String, dynamic>{'attemptsLeft': 2},
        ),
      ),
    ),
    act: (OtpCubit cubit) async {
      '9999'.split('').forEach(cubit.addDigit);
      await cubit.verify();
    },
    verify: (OtpCubit cubit) {
      expect(cubit.state.code, isEmpty);
      expect(cubit.state.failure?.intDetail('attemptsLeft'), 2);
      expect(cubit.state.session, isNull);
    },
  );

  blocTest<OtpCubit, OtpState>(
    'resend requests a new code and restarts the countdown',
    build: () => build(
      request: const OtpRequest(
        requestId: 'req-1',
        phoneNumber: '+966512345678',
        expiresInSeconds: 300,
        resendAfterSeconds: 0,
      ),
    ),
    setUp: () => when(() => requestOtp(any())).thenAnswer(
      (_) async => const Right<Failure, OtpRequest>(
        OtpRequest(
          requestId: 'req-2',
          phoneNumber: '+966512345678',
          expiresInSeconds: 300,
          resendAfterSeconds: 3,
          devCode: '5678',
        ),
      ),
    ),
    act: (OtpCubit cubit) => cubit.resend(),
    wait: Duration.zero,
    verify: (OtpCubit cubit) {
      expect(cubit.state.requestId, 'req-2');
      expect(cubit.state.devCode, '5678');
      expect(cubit.state.secondsLeft, 0);
    },
  );
}
