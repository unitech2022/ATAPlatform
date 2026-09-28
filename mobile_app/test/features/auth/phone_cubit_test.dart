import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/usecases/request_otp.dart';
import 'package:ata_app/features/auth/presentation/cubit/phone_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/phone_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fakes.dart';

class _MockRequestOtp extends Mock implements RequestOtp {}

void main() {
  late _MockRequestOtp requestOtp;

  setUpAll(() {
    registerFallbackValue(
      const RequestOtpParams(
        phoneNumber: '',
        role: UserRole.passenger,
        language: 'ar',
      ),
    );
  });

  setUp(() => requestOtp = _MockRequestOtp());

  PhoneCubit build() => PhoneCubit(
    requestOtp: requestOtp,
    role: UserRole.passenger,
    language: 'ar',
  );

  blocTest<PhoneCubit, PhoneState>(
    'keypad input is capped at nine digits and delete removes the last one',
    build: build,
    act: (PhoneCubit cubit) {
      for (final String d in '5123456789'.split('')) {
        cubit.addDigit(d);
      }
      cubit.deleteDigit();
    },
    verify: (PhoneCubit cubit) {
      expect(cubit.state.digits, '51234567');
      expect(cubit.state.isValid, isFalse);
    },
  );

  blocTest<PhoneCubit, PhoneState>(
    'submit is ignored while the number is invalid',
    build: build,
    act: (PhoneCubit cubit) => cubit
      ..addDigit('4')
      ..submit(),
    verify: (_) => verifyNever(() => requestOtp(any())),
  );

  blocTest<PhoneCubit, PhoneState>(
    'submit normalizes the number and exposes the OTP request',
    build: build,
    setUp: () => when(
      () => requestOtp(any()),
    ).thenAnswer((_) async => const Right<Failure, OtpRequest>(testOtpRequest)),
    seed: () => const PhoneState(digits: '512345678'),
    act: (PhoneCubit cubit) => cubit.submit(),
    expect: () => <PhoneState>[
      const PhoneState(digits: '512345678', submitting: true),
      const PhoneState(digits: '512345678', result: testOtpRequest),
    ],
    verify: (_) {
      final RequestOtpParams params =
          verify(() => requestOtp(captureAny())).captured.single
              as RequestOtpParams;
      expect(params.phoneNumber, '+966512345678');
      expect(params.role, UserRole.passenger);
    },
  );

  blocTest<PhoneCubit, PhoneState>(
    'failures are surfaced and cleared on the next key press',
    build: build,
    setUp: () => when(() => requestOtp(any())).thenAnswer(
      (_) async => const Left<Failure, OtpRequest>(
        ServerFailure(
          code: 'rate_limited',
          message: '',
          details: <String, dynamic>{'retryAfterSeconds': 30},
        ),
      ),
    ),
    seed: () => const PhoneState(digits: '512345678'),
    act: (PhoneCubit cubit) async {
      await cubit.submit();
      cubit.deleteDigit();
    },
    verify: (PhoneCubit cubit) {
      expect(cubit.state.failure, isNull);
      expect(cubit.state.digits, '51234567');
    },
  );
}
