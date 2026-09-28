import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/presentation/cubit/trip_share_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/trip_share_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Links of the trip (views, revoke) and SMS sharing to trusted contacts.
class TripShareSheet extends StatelessWidget {
  const TripShareSheet({super.key});

  static Future<void> show(BuildContext context, TripShareCubit cubit) {
    cubit.load();
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (_) => BlocProvider<TripShareCubit>.value(
        value: cubit,
        child: const TripShareSheet(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<TripShareCubit, TripShareState>(
      builder: (BuildContext context, TripShareState state) {
        final TripShareCubit cubit = context.read<TripShareCubit>();
        return SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg + MediaQuery.paddingOf(context).bottom,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              const SheetHandle(),
              Text(l10n.shareTripTitle, style: AtaText.section),
              Text(l10n.shareSheetCopy, style: AtaText.small),
              const SizedBox(height: AtaSpacing.md),
              if (state.loading) const CenteredLoader(),
              Text(l10n.shareToContacts, style: AtaText.label),
              const SizedBox(height: AtaSpacing.xs),
              if (state.contacts.isEmpty)
                AtaButton(
                  label: l10n.addTrustedContact,
                  variant: AtaButtonVariant.soft,
                  height: AtaSizes.buttonCompact,
                  onPressed: () {
                    Navigator.of(context).pop();
                    context.push(AppRoutes.safetyContacts);
                  },
                )
              else ...<Widget>[
                for (final TrustedContact c in state.contacts) ...<Widget>[
                  SelectableTile(
                    selected: state.selectedContactIds.contains(c.id),
                    onTap: () => cubit.toggleContact(c.id),
                    child: Row(
                      children: <Widget>[
                        RadioDot(
                          selected: state.selectedContactIds.contains(c.id),
                        ),
                        const SizedBox(width: AtaSpacing.sm),
                        Expanded(child: Text(c.name, style: AtaText.label)),
                      ],
                    ),
                  ),
                  const SizedBox(height: AtaSpacing.xs),
                ],
                AtaButton(
                  label: l10n.sendBySms,
                  height: AtaSizes.buttonCompact,
                  loading: state.creating,
                  onPressed: state.selectedContactIds.isEmpty
                      ? null
                      : cubit.sendToContacts,
                ),
                if (state.smsSent > 0)
                  Text(
                    l10n.smsSentTo(state.smsSent),
                    style: AtaText.caption.copyWith(color: AtaColors.brand),
                  ),
              ],
              const SizedBox(height: AtaSpacing.md),
              Text(l10n.activeLinks, style: AtaText.label),
              if (state.activeShares.isEmpty)
                Text(l10n.noActiveLinks, style: AtaText.small),
              for (final TripShare share in state.activeShares)
                _ShareRow(share: share, busy: state.busyId == share.id),
            ],
          ),
        );
      },
    );
  }
}

class _ShareRow extends StatelessWidget {
  const _ShareRow({required this.share, required this.busy});

  final TripShare share;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        children: <Widget>[
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(
                  share.trustedContactName ?? share.url,
                  style: AtaText.label,
                  overflow: TextOverflow.ellipsis,
                  textDirection: share.trustedContactName == null
                      ? TextDirection.ltr
                      : null,
                ),
                Text(l10n.shareViews(share.viewCount), style: AtaText.caption),
              ],
            ),
          ),
          TextButton(
            onPressed: busy
                ? null
                : () => context.read<TripShareCubit>().revoke(share.id),
            child: Text(
              l10n.revokeLink,
              style: AtaText.label.copyWith(color: AtaColors.danger),
            ),
          ),
        ],
      ),
    );
  }
}
