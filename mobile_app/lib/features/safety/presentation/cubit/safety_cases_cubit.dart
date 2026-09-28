import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/usecases/get_my_safety_cases.dart';
import 'package:ata_app/features/safety/domain/usecases/get_safety_case.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_list_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "My reports" (`/safety/cases`) or one case (`/safety/cases/:id`, from
/// `ata://safety/cases/{id}`): status and the public notes.
class SafetyCasesCubit extends Cubit<SafetyListState<SafetyCaseSummary>> {
  SafetyCasesCubit({required this._getCases, required this._getCase})
    : super(const SafetyListState<SafetyCaseSummary>());

  final GetMySafetyCases _getCases;
  final GetSafetyCase _getCase;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getCases(1);
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(loading: false, failure: f)),
      (PageResult<SafetyCaseSummary> page) =>
          emit(state.copyWith(loading: false, items: page.items)),
    );
  }

  Future<void> loadOne(String caseId) async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getCase(caseId);
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(loading: false, failure: f)),
      (SafetyCaseSummary item) => emit(
        state.copyWith(loading: false, items: <SafetyCaseSummary>[item]),
      ),
    );
  }
}
