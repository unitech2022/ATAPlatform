import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_trips.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_earnings_summary.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_overview_state.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:fpdart/fpdart.dart';

/// Loads earnings and recent trips for the overview tab.
class DriverOverviewCubit extends Cubit<DriverOverviewState> {
  DriverOverviewCubit({required this._getEarnings, required this._getTrips})
    : super(const DriverOverviewState());

  final GetEarningsSummary _getEarnings;
  final GetDriverTrips _getTrips;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final (
      Either<Failure, EarningsSummary> earnings,
      Either<Failure, PageResult<TripSummary>> trips,
    ) = await (
      _getEarnings(const NoParams()),
      _getTrips(const NoParams()),
    ).wait;

    DriverOverviewState next = state.copyWith(loading: false);
    next = earnings.fold(
      (Failure failure) => next.copyWith(failure: failure),
      (EarningsSummary summary) => next.copyWith(earnings: summary),
    );
    next = trips.fold(
      (Failure failure) => next.copyWith(failure: next.failure ?? failure),
      (PageResult<TripSummary> page) => next.copyWith(trips: page.items),
    );
    emit(next);
  }
}
