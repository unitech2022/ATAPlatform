import 'dart:async';

import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/usecases/request_otp.dart';
import 'package:ata_app/features/auth/domain/usecases/verify_otp.dart';
import 'package:ata_app/features/auth/presentation/cubit/otp_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Code entry, verification and resend countdown for the OTP screen.
class OtpCubit extends Cubit<OtpState> {
  OtpCubit({
    required this._verifyOtp,
    required this._requestOtp,
    required OtpRequest request,
    required this.role,
    required this.language,
    this._countdown = secondsCountdown,
  }) : super(
         OtpState(
           requestId: request.requestId,
           phoneNumber: request.phoneNumber,
           devCode: request.devCode,
           secondsLeft: request.resendAfterSeconds,
         ),
       ) {
    _startCountdown(request.resendAfterSeconds);
  }

  final VerifyOtp _verifyOtp;
  final RequestOtp _requestOtp;
  final Countdown _countdown;
  final UserRole role;
  final String language;
  StreamSubscription<int>? _ticker;

  void addDigit(String digit) {
    if (state.verifying || state.isComplete) return;
    emit(state.copyWith(code: state.code + digit, clearFailure: true));
  }

  void deleteDigit() {
    if (state.verifying || state.code.isEmpty) return;
    emit(
      state.copyWith(
        code: state.code.substring(0, state.code.length - 1),
        clearFailure: true,
      ),
    );
  }

  Future<void> verify() async {
    if (!state.canVerify) return;
    emit(state.copyWith(verifying: true, clearFailure: true));
    final result = await _verifyOtp(
      VerifyOtpParams(
        requestId: state.requestId,
        phoneNumber: state.phoneNumber,
        code: state.code,
        role: role,
      ),
    );
    emit(
      result.fold(
        (failure) =>
            state.copyWith(verifying: false, failure: failure, code: ''),
        (session) => state.copyWith(verifying: false, session: session),
      ),
    );
  }

  Future<void> resend() async {
    if (!state.canResend) return;
    emit(state.copyWith(resending: true, clearFailure: true));
    final result = await _requestOtp(
      RequestOtpParams(
        phoneNumber: state.phoneNumber,
        role: role,
        language: language,
      ),
    );
    result.fold(
      (failure) => emit(state.copyWith(resending: false, failure: failure)),
      (OtpRequest request) {
        emit(
          state.copyWith(
            resending: false,
            requestId: request.requestId,
            devCode: request.devCode,
            code: '',
            secondsLeft: request.resendAfterSeconds,
          ),
        );
        _startCountdown(request.resendAfterSeconds);
      },
    );
  }

  void _startCountdown(int seconds) {
    _ticker?.cancel();
    if (seconds <= 0) return;
    _ticker = _countdown(seconds).listen((int left) {
      if (!isClosed) emit(state.copyWith(secondsLeft: left));
    });
  }

  @override
  Future<void> close() async {
    await _ticker?.cancel();
    return super.close();
  }
}
