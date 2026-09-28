import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/usecases/get_profile.dart';
import 'package:ata_app/features/account/presentation/cubit/account_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads `GET /me` for the account page.
class AccountCubit extends Cubit<AccountState> {
  AccountCubit({required this._getProfile}) : super(const AccountState());

  final GetProfile _getProfile;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getProfile(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (profile) => state.copyWith(loading: false, profile: profile),
      ),
    );
  }
}
