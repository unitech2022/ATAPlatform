import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Persists the locale locally and, when [ChangeLanguageParams.syncRemote],
/// on the account (`PATCH /me { language }`).
class ChangeLanguageParams {
  const ChangeLanguageParams({
    required this.languageCode,
    required this.syncRemote,
  });

  final String languageCode;
  final bool syncRemote;
}

class ChangeLanguage implements UseCase<Unit, ChangeLanguageParams> {
  const ChangeLanguage(this._repository);

  final AccountRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(ChangeLanguageParams params) async {
    final Either<Failure, Unit> saved = await _repository.saveLocale(
      params.languageCode,
    );
    if (saved.isLeft() || !params.syncRemote) return saved;
    // Remote sync is best effort: the local choice wins.
    await _repository.updateLanguage(params.languageCode);
    return saved;
  }
}
