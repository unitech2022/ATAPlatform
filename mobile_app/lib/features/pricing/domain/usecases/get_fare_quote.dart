import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /pricing/quote`.
class GetFareQuote implements UseCase<FareQuote, QuoteRequest> {
  const GetFareQuote(this._repository);

  final PricingRepository _repository;

  @override
  Future<Either<Failure, FareQuote>> call(QuoteRequest params) =>
      _repository.quote(params);
}
