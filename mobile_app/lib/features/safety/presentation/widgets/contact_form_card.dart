import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Add / edit form of a trusted contact; values live in the cubit.
class ContactFormCard extends StatelessWidget {
  const ContactFormCard({super.key, required this.form});

  final ContactForm form;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TrustedContactsCubit cubit = context.read<TrustedContactsCubit>();
    final String key = form.editingId ?? 'new';
    final String? phoneError = form.errors.contains(ContactFieldError.phoneSelf)
        ? l10n.contactPhoneSelf
        : form.errors.contains(ContactFieldError.phoneInvalid)
        ? l10n.phoneInvalid
        : null;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            form.isEditing ? l10n.editTrustedContact : l10n.addTrustedContact,
            style: AtaText.section,
          ),
          const SizedBox(height: AtaSpacing.sm),
          TextFormField(
            key: ValueKey<String>('name-$key'),
            initialValue: form.draft.name,
            onChanged: cubit.nameChanged,
            maxLength: TrustedContactDraft.maxNameLength,
            decoration: InputDecoration(
              labelText: l10n.contactNameLabel,
              counterText: '',
              errorText: form.errors.contains(ContactFieldError.nameRequired)
                  ? l10n.contactNameRequired
                  : null,
            ),
          ),
          TextFormField(
            key: ValueKey<String>('phone-$key'),
            initialValue: form.draft.phoneNumber,
            onChanged: cubit.phoneChanged,
            keyboardType: TextInputType.phone,
            textDirection: TextDirection.ltr,
            decoration: InputDecoration(
              labelText: l10n.contactPhoneLabel,
              hintText: l10n.contactPhoneHint,
              errorText: phoneError,
            ),
          ),
          TextFormField(
            key: ValueKey<String>('rel-$key'),
            initialValue: form.draft.relationship ?? '',
            onChanged: cubit.relationshipChanged,
            maxLength: TrustedContactDraft.maxRelationshipLength,
            decoration: InputDecoration(
              labelText: l10n.contactRelationshipLabel,
              counterText: '',
            ),
          ),
          const SizedBox(height: AtaSpacing.sm),
          _ToggleRow(
            label: l10n.autoShareLabel,
            value: form.draft.autoShare,
            onChanged: cubit.autoShareChanged,
          ),
          _ToggleRow(
            label: l10n.notifyOnSosLabel,
            value: form.draft.notifyOnSos,
            onChanged: cubit.notifyOnSosChanged,
          ),
          if (form.failure != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            InlineError(message: failureText(form.failure!, l10n)),
          ],
          const SizedBox(height: AtaSpacing.md),
          Row(
            children: <Widget>[
              Expanded(
                child: AtaButton(
                  label: l10n.save,
                  height: AtaSizes.buttonCompact,
                  loading: form.saving,
                  onPressed: cubit.save,
                ),
              ),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: AtaButton(
                  label: l10n.cancel,
                  variant: AtaButtonVariant.soft,
                  height: AtaSizes.buttonCompact,
                  onPressed: form.saving ? null : cubit.cancelForm,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ToggleRow extends StatelessWidget {
  const _ToggleRow({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final String label;
  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        children: <Widget>[
          Expanded(child: Text(label, style: AtaText.small)),
          AtaToggle(value: value, onChanged: onChanged),
        ],
      ),
    );
  }
}
