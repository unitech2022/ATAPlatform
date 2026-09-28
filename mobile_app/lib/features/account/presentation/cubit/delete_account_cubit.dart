import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/usecases/delete_account.dart';
import 'package:ata_app/features/account/presentation/cubit/delete_account_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Runs `DELETE /me` after the user confirms.
class DeleteAccountCubit extends Cubit<DeleteAccountState> {
  DeleteAccountCubit({required this._deleteAccount})
    : super(const DeleteAccountState());

  final DeleteAccount _deleteAccount;

  Future<void> confirm() async {
    if (state.submitting) return;
    emit(state.copyWith(submitting: true, clearFailure: true));
    final result = await _deleteAccount(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(submitting: false, failure: failure),
        (_) => state.copyWith(submitting: false, deleted: true),
      ),
    );
  }
}
