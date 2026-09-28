import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/contact_form_card.dart';
import 'package:ata_app/features/safety/presentation/widgets/contact_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/safety/contacts`: up to 5 trusted contacts with auto-share and SOS
/// notification toggles (F12).
class TrustedContactsPage extends StatelessWidget {
  const TrustedContactsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<TrustedContactsCubit>(
      create: (_) => TrustedContactsCubit(
        getContacts: getIt(),
        addContact: getIt(),
        updateContact: getIt(),
        deleteContact: getIt(),
      )..load(),
      child: BlocBuilder<TrustedContactsCubit, TrustedContactsState>(
        builder: (BuildContext context, TrustedContactsState state) {
          final TrustedContactsCubit cubit = context
              .read<TrustedContactsCubit>();
          return PageWrap(
            children: <Widget>[
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: PillButton.back(
                  label: l10n.back,
                  onTap: () => context.pop(),
                ),
              ),
              const SizedBox(height: AtaSpacing.xl),
              ScreenTitle(
                eyebrow: l10n.safetyEyebrow,
                title: l10n.trustedContactsTitle,
                copy: l10n.trustedContactsPageCopy,
              ),
              const SizedBox(height: AtaSpacing.xl),
              if (state.form != null) ...<Widget>[
                ContactFormCard(form: state.form!),
                const SizedBox(height: AtaSpacing.md),
              ],
              if (state.loading && state.contacts.isEmpty)
                const CenteredLoader()
              else if (state.contacts.isEmpty && state.failure != null)
                FailureView(failure: state.failure!, onRetry: cubit.load)
              else
                _List(state: state),
              if (state.failure != null &&
                  state.contacts.isNotEmpty) ...<Widget>[
                const SizedBox(height: AtaSpacing.sm),
                InlineError(message: failureText(state.failure!, l10n)),
              ],
              const SizedBox(height: AtaSpacing.md),
              if (state.form == null)
                AtaButton(
                  label: l10n.addTrustedContact,
                  icon: AtaIcons.plus,
                  onPressed: state.canAdd ? cubit.startAdd : null,
                ),
              if (!state.canAdd)
                Padding(
                  padding: const EdgeInsets.only(top: AtaSpacing.xs),
                  child: Text(
                    l10n.trustedContactsLimitError,
                    style: AtaText.caption,
                    textAlign: TextAlign.center,
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}

class _List extends StatelessWidget {
  const _List({required this.state});

  final TrustedContactsState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    if (state.contacts.isEmpty) {
      return AtaCard(
        child: Text(
          l10n.noTrustedContacts,
          style: AtaText.bodyMuted,
          textAlign: TextAlign.center,
        ),
      );
    }
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            l10n.trustedContactsCount(
              state.contacts.length,
              TrustedContact.maxPerUser,
            ),
            style: AtaText.caption,
          ),
          for (final TrustedContact contact in state.contacts)
            ContactTile(contact: contact, busy: state.busyId == contact.id),
        ],
      ),
    );
  }
}
