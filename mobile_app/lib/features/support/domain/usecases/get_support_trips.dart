import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_trips.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/domain/usecases/get_passenger_trips.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// Recent finished trips of the current role (completed or cancelled), the
/// candidates for a ticket's related trip.
class GetSupportTrips implements UseCase<List<TripSummary>, TripActor> {
  const GetSupportTrips(this._passengerTrips, this._driverTrips);

  final GetPassengerTrips _passengerTrips;
  final GetDriverTrips _driverTrips;

  @override
  Future<Either<Failure, List<TripSummary>>> call(TripActor params) async {
    final result = params == TripActor.driver
        ? await _driverTrips(const NoParams())
        : await _passengerTrips(const NoParams());
    return result.map(
      (page) => page.items
          .where(
            (TripSummary t) =>
                t.status == TripStatus.completed ||
                t.status == TripStatus.cancelled,
          )
          .toList(growable: false),
    );
  }
}
