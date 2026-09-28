import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/widgets.dart';

/// Shorthand for `AppLocalizations.of(context)`.
extension L10nContext on BuildContext {
  AppLocalizations get l10n => AppLocalizations.of(this);

  /// `ar` or `en`.
  String get localeCode => Localizations.localeOf(this).languageCode;
}
