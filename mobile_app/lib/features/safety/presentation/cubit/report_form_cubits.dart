import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/utils/phone_number.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/usecases/report_lost_item.dart';
import 'package:ata_app/features/safety/domain/usecases/submit_safety_report.dart';
import 'package:ata_app/features/safety/presentation/cubit/report_form_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

typedef LostItemFormState = ReportFormState<LostItemCategory, LostItemReport>;
typedef SafetyReportFormState =
    ReportFormState<SafetyReportCategory, SafetyCaseSummary>;

/// Lost item form of a completed trip (`/rides/:tripId/lost-item`).
class LostItemCubit extends Cubit<LostItemFormState> {
  LostItemCubit({required this._report, required this.tripId})
    : super(const LostItemFormState());

  final ReportLostItem _report;
  final String tripId;

  void categoryChanged(LostItemCategory c) =>
      emit(state.copyWith(category: c, clearFailure: true));
  void descriptionChanged(String v) =>
      emit(state.copyWith(description: v, clearFailure: true));
  void contactPhoneChanged(String v) =>
      emit(state.copyWith(contactPhone: v, clearFailure: true));

  Future<void> submit() async {
    if (state.submitting || state.result != null) return;
    if (!state.isValid) {
      emit(state.copyWith(showErrors: true));
      return;
    }
    emit(state.copyWith(submitting: true, clearFailure: true));
    final String phone = state.contactPhone.trim();
    final result = await _report(
      LostItemDraft(
        tripId: tripId,
        category: state.category!,
        description: state.description,
        contactPhone: phone.isEmpty ? null : PhoneNumber.normalize(phone),
      ),
    );
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(submitting: false, failure: f)),
      (LostItemReport r) => emit(state.copyWith(submitting: false, result: r)),
    );
  }
}

/// Non-emergency safety report (`/safety/report?tripId=`).
class SafetyReportCubit extends Cubit<SafetyReportFormState> {
  SafetyReportCubit({required this._submit, required this.tripId})
    : super(const SafetyReportFormState());

  final SubmitSafetyReport _submit;
  final String tripId;

  void categoryChanged(SafetyReportCategory c) =>
      emit(state.copyWith(category: c, clearFailure: true));
  void descriptionChanged(String v) =>
      emit(state.copyWith(description: v, clearFailure: true));

  Future<void> submit() async {
    if (state.submitting || state.result != null) return;
    if (!state.isValid) {
      emit(state.copyWith(showErrors: true));
      return;
    }
    emit(state.copyWith(submitting: true, clearFailure: true));
    final result = await _submit(
      SafetyReportDraft(
        tripId: tripId,
        category: state.category!,
        description: state.description,
      ),
    );
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(submitting: false, failure: f)),
      (SafetyCaseSummary c) =>
          emit(state.copyWith(submitting: false, result: c)),
    );
  }
}
