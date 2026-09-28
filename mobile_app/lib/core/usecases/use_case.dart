import 'package:ata_app/core/errors/failures.dart';
import 'package:fpdart/fpdart.dart';

/// A single application action. Cubits call use cases and nothing else.
abstract interface class UseCase<T, P> {
  Future<Either<Failure, T>> call(P params);
}

/// Marker for use cases that take no input.
final class NoParams {
  const NoParams();
}
