import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Confirmation before a driver leaves the favourites; resolves to `true`
/// when confirmed.
abstract final class RemoveFavoriteDialog {
  static Future<bool> show(BuildContext context, {required String name}) async {
    final AppLocalizations l10n = context.l10n;
    final bool? ok = await showDialog<bool>(
      context: context,
      builder: (BuildContext dialog) => AlertDialog(
        title: Text(l10n.favoriteRemoveTitle(name)),
        content: Text(l10n.favoriteRemoveCopy),
        actions: <Widget>[
          TextButton(
            onPressed: () => Navigator.of(dialog).pop(false),
            child: Text(l10n.cancel),
          ),
          TextButton(
            key: const ValueKey<String>('confirm-remove-favorite'),
            onPressed: () => Navigator.of(dialog).pop(true),
            child: Text(l10n.favoriteRemoveConfirm),
          ),
        ],
      ),
    );
    return ok ?? false;
  }
}
