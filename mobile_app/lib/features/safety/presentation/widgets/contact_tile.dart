import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// One trusted contact: name, phone, auto-share toggle, edit / delete.
class ContactTile extends StatelessWidget {
  const ContactTile({super.key, required this.contact, required this.busy});

  final TrustedContact contact;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TrustedContactsCubit cubit = context.read<TrustedContactsCubit>();
    final String? relationship = contact.relationship;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.sm),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      relationship == null || relationship.isEmpty
                          ? contact.name
                          : '${contact.name} · $relationship',
                      style: AtaText.bodyStrong,
                    ),
                    Text(
                      contact.phoneNumber,
                      style: AtaText.caption,
                      textDirection: TextDirection.ltr,
                    ),
                  ],
                ),
              ),
              TextButton(
                onPressed: busy ? null : () => cubit.startEdit(contact),
                child: Text(l10n.edit, style: AtaText.label),
              ),
              TextButton(
                onPressed: busy ? null : () => cubit.delete(contact.id),
                child: Text(
                  l10n.delete,
                  style: AtaText.label.copyWith(color: AtaColors.danger),
                ),
              ),
            ],
          ),
          Row(
            children: <Widget>[
              Expanded(child: Text(l10n.autoShareLabel, style: AtaText.small)),
              AtaToggle(
                value: contact.autoShare,
                onChanged: busy ? null : (_) => cubit.toggleAutoShare(contact),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
