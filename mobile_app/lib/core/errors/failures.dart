import 'package:ata_app/core/errors/app_exception.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

/// Domain-level failure returned on the left side of `Either`.
sealed class Failure extends Equatable {
  const Failure({required this.code, required this.message, this.details});

  final String code;
  final String message;
  final Map<String, dynamic>? details;

  /// Reads an integer detail such as `attemptsLeft` or `retryAfterSeconds`.
  int? intDetail(String key) {
    final Object? value = details?[key];
    return value is num ? value.toInt() : null;
  }

  @override
  List<Object?> get props => <Object?>[code, message, details];
}

/// The API answered with an error envelope.
final class ServerFailure extends Failure {
  const ServerFailure({
    required super.code,
    required super.message,
    super.details,
    this.statusCode,
  });

  final int? statusCode;

  @override
  List<Object?> get props => <Object?>[...super.props, statusCode];
}

/// The device could not reach the API.
final class NetworkFailure extends Failure {
  const NetworkFailure({required super.message})
    : super(code: AppException.networkCode);
}

/// The session is no longer valid.
final class UnauthorizedFailure extends Failure {
  const UnauthorizedFailure({required super.message})
    : super(code: AppException.unauthorizedCode);
}

/// Anything else (parsing errors, bugs).
final class UnexpectedFailure extends Failure {
  const UnexpectedFailure({required super.message})
    : super(code: AppException.unexpectedCode);
}

/// Converts any thrown object into a [Failure].
Failure failureFromError(Object error) {
  if (error is AppException) {
    return switch (error.code) {
      AppException.networkCode => NetworkFailure(message: error.message),
      AppException.unauthorizedCode => UnauthorizedFailure(
        message: error.message,
      ),
      _ => ServerFailure(
        code: error.code,
        message: error.message,
        details: error.details,
        statusCode: error.statusCode,
      ),
    };
  }
  return UnexpectedFailure(message: error.toString());
}

/// Runs [action] and folds any thrown error into `Left(Failure)`.
Future<Either<Failure, T>> guard<T>(Future<T> Function() action) async {
  try {
    return Right<Failure, T>(await action());
  } on Object catch (error) {
    return Left<Failure, T>(failureFromError(error));
  }
}
