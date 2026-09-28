import 'package:ata_app/features/auth/domain/usecases/complete_rider_profile.dart';
import 'package:ata_app/features/auth/presentation/cubit/terms_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Name entry and terms acceptance for new riders.
class TermsCubit extends Cubit<TermsState> {
  TermsCubit({required this._completeProfile}) : super(const TermsState());

  final CompleteRiderProfile _completeProfile;

  void nameChanged(String value) =>
      emit(state.copyWith(fullName: value, clearFailure: true));

  void acceptedChanged(bool value) =>
      emit(state.copyWith(accepted: value, clearFailure: true));

  Future<void> submit() async {
    if (!state.canSubmit) return;
    emit(state.copyWith(submitting: true, clearFailure: true));
    final result = await _completeProfile(state.fullName.trim());
    emit(
      result.fold(
        (failure) => state.copyWith(submitting: false, failure: failure),
        (user) => state.copyWith(submitting: false, user: user),
      ),
    );
  }
}
