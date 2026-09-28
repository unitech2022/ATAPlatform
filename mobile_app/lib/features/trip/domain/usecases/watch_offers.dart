import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';

/// Live feed of dispatch offers; `null` when the offer expired or none.
class WatchOffers {
  const WatchOffers(this._repository);

  final TripRepository _repository;

  Stream<Offer?> call() => _repository.watchOffers();
}
