import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/data/datasources/pricing_remote_data_source.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// [PricingRepository] backed by the REST API.
class PricingRepositoryImpl implements PricingRepository {
  const PricingRepositoryImpl(this._remote);

  final PricingRemoteDataSource _remote;

  @override
  Future<Either<Failure, FareQuote>> quote(QuoteRequest request) =>
      guard(() => _remote.quote(QuoteRequestMapper.body(request)));

  @override
  Future<Either<Failure, DemandLevel>> demandAt(GeoPoint point) =>
      guard(() => _remote.demand(lat: point.lat, lng: point.lng));
}
