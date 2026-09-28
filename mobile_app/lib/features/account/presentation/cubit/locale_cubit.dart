import 'package:ata_app/features/account/domain/usecases/change_language.dart';
import 'package:ata_app/features/account/domain/usecases/get_saved_locale.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Current app locale; persisted locally and synced to the account when
/// the user is signed in.
class LocaleCubit extends Cubit<Locale> {
  LocaleCubit({
    required GetSavedLocale getSavedLocale,
    required this._changeLanguage,
  }) : super(Locale(getSavedLocale()));

  final ChangeLanguage _changeLanguage;

  static const Locale arabic = Locale('ar');
  static const Locale english = Locale('en');
  static const List<Locale> supported = <Locale>[arabic, english];

  bool get isArabic => state.languageCode == arabic.languageCode;

  Future<void> change(String languageCode, {bool syncRemote = false}) async {
    if (languageCode == state.languageCode) return;
    emit(Locale(languageCode));
    await _changeLanguage(
      ChangeLanguageParams(languageCode: languageCode, syncRemote: syncRemote),
    );
  }
}
