import 'package:ata_app/core/utils/phone_number.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/usecases/request_otp.dart';
import 'package:ata_app/features/auth/presentation/cubit/phone_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Keypad input and OTP request for the phone screen.
class PhoneCubit extends Cubit<PhoneState> {
  PhoneCubit({
    required this._requestOtp,
    required this.role,
    required this.language,
  }) : super(const PhoneState());

  final RequestOtp _requestOtp;
  final UserRole role;
  final String language;

  void addDigit(String digit) {
    if (state.submitting || state.digits.length >= PhoneNumber.localLength) {
      return;
    }
    emit(state.copyWith(digits: state.digits + digit, clearFailure: true));
  }

  void deleteDigit() {
    if (state.submitting || state.digits.isEmpty) return;
    emit(
      state.copyWith(
        digits: state.digits.substring(0, state.digits.length - 1),
        clearFailure: true,
      ),
    );
  }

  Future<void> submit() async {
    if (!state.canSubmit) return;
    emit(
      state.copyWith(submitting: true, clearFailure: true, clearResult: true),
    );
    final result = await _requestOtp(
      RequestOtpParams(
        phoneNumber: PhoneNumber.normalize(state.digits)!,
        role: role,
        language: language,
      ),
    );
    emit(
      result.fold(
        (failure) => state.copyWith(submitting: false, failure: failure),
        (request) => state.copyWith(submitting: false, result: request),
      ),
    );
  }

  /// Clears the navigation trigger after the OTP page was pushed.
  void consumeResult() => emit(state.copyWith(clearResult: true));
}
