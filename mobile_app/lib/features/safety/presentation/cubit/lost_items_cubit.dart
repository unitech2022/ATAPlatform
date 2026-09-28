import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/usecases/get_driver_lost_items.dart';
import 'package:ata_app/features/safety/domain/usecases/get_my_lost_items.dart';
import 'package:ata_app/features/safety/domain/usecases/respond_to_lost_item.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_list_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Passenger lost item reports and their status (`/safety/lost-items`).
class LostItemsCubit extends Cubit<SafetyListState<LostItemReport>> {
  LostItemsCubit({required this._getMine})
    : super(const SafetyListState<LostItemReport>());

  final GetMyLostItems _getMine;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getMine(1);
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(loading: false, failure: f)),
      (PageResult<LostItemReport> page) =>
          emit(state.copyWith(loading: false, items: page.items)),
    );
  }
}

/// Driver lost item reports (`/driver/lost-items`) with found / not found.
class DriverLostItemsCubit extends Cubit<SafetyListState<LostItemReport>> {
  DriverLostItemsCubit({required this._getItems, required this._respond})
    : super(const SafetyListState<LostItemReport>());

  final GetDriverLostItems _getItems;
  final RespondToLostItem _respond;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getItems(1);
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(loading: false, failure: f)),
      (PageResult<LostItemReport> page) =>
          emit(state.copyWith(loading: false, items: page.items)),
    );
  }

  Future<void> respond(String id, {required bool found}) async {
    if (state.busyId != null) return;
    emit(state.copyWith(busyId: id, clearFailure: true));
    final result = await _respond(LostItemAnswer(id: id, found: found));
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(clearBusy: true, failure: f)),
      (_) => emit(
        state.copyWith(
          clearBusy: true,
          items: state.items
              .map(
                (LostItemReport r) => r.id == id ? r.answered(found: found) : r,
              )
              .toList(growable: false),
        ),
      ),
    );
  }
}
