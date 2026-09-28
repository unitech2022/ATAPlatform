import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_onboarding/domain/usecases/get_driver_application.dart';
import 'package:ata_app/features/driver_onboarding/presentation/cubit/driver_pending_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Opens an external URL; returns `false` when it could not be launched.
typedef UrlOpener = Future<bool> Function(String url);

/// Loads the application status and opens the upload portal.
class DriverPendingCubit extends Cubit<DriverPendingState> {
  DriverPendingCubit({
    required this._getApplication,
    required this._openUrl,
    required this.portalUrl,
  }) : super(const DriverPendingState());

  final GetDriverApplication _getApplication;
  final UrlOpener _openUrl;
  final String portalUrl;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getApplication(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (application) =>
            state.copyWith(loading: false, application: application),
      ),
    );
  }

  Future<void> openPortal() async {
    emit(state.copyWith(portalOpenFailed: false));
    final bool opened = await _openUrl(portalUrl);
    if (!opened) emit(state.copyWith(portalOpenFailed: true));
  }

  void portalFailureShown() => emit(state.copyWith(portalOpenFailed: false));
}
