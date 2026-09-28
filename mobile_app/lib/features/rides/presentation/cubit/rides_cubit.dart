import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/rides/domain/usecases/get_passenger_trips.dart';
import 'package:ata_app/features/rides/presentation/cubit/rides_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads the rider's trip history.
class RidesCubit extends Cubit<RidesState> {
  RidesCubit({required this._getTrips}) : super(const RidesState());

  final GetPassengerTrips _getTrips;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getTrips(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (page) => state.copyWith(loading: false, trips: page.items),
      ),
    );
  }
}
