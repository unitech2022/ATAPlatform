import 'package:ata_app/features/account/domain/repositories/account_repository.dart';

/// Reads the persisted locale synchronously at app start.
class GetSavedLocale {
  const GetSavedLocale(this._repository);

  final AccountRepository _repository;

  String call() => _repository.getSavedLocale();
}
