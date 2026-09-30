import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// The help search box (the cubit debounces what is typed).
class HelpSearchField extends StatelessWidget {
  const HelpSearchField({super.key, required this.onChanged, this.version = 0});

  final ValueChanged<String> onChanged;

  /// Changing it rebuilds the field empty (after "all topics").
  final int version;

  @override
  Widget build(BuildContext context) {
    return TextField(
      key: ValueKey<int>(version),
      onChanged: onChanged,
      textInputAction: TextInputAction.search,
      style: AtaText.body,
      decoration: InputDecoration(
        hintText: context.l10n.helpSearchHint,
        prefixIcon: const Padding(
          padding: EdgeInsetsDirectional.only(start: 16, end: 8),
          child: AtaIcon(AtaIcons.search, color: AtaColors.muted),
        ),
        prefixIconConstraints: const BoxConstraints(),
      ),
    );
  }
}
