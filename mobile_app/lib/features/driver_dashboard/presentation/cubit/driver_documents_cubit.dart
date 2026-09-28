import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_documents_state.dart';
import 'package:ata_app/features/driver_onboarding/domain/usecases/get_driver_application.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads documents and the vehicle from the driver application.
class DriverDocumentsCubit extends Cubit<DriverDocumentsState> {
  DriverDocumentsCubit({required this._getApplication})
    : super(const DriverDocumentsState());

  final GetDriverApplication _getApplication;

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
}
