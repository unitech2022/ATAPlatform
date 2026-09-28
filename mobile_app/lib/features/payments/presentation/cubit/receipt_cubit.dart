import 'package:ata_app/features/payments/domain/usecases/get_trip_receipt.dart';
import 'package:ata_app/features/payments/presentation/cubit/receipt_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads the itemised receipt of one trip.
class ReceiptCubit extends Cubit<ReceiptState> {
  ReceiptCubit({required this._getTripReceipt, required this.tripId})
    : super(const ReceiptState());

  final GetTripReceipt _getTripReceipt;
  final String tripId;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getTripReceipt(tripId);
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (receipt) => state.copyWith(loading: false, receipt: receipt),
      ),
    );
  }
}
