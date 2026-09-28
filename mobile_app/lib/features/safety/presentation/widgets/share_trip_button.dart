import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/safety/presentation/cubit/trip_share_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/trip_share_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/trip_share_sheet.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:share_plus/share_plus.dart';

/// "Share trip": creates a tracking link through the API and hands it to
/// share_plus; the secondary button manages links and trusted contacts.
class ShareTripButton extends StatelessWidget {
  const ShareTripButton({super.key, required this.tripId});

  final String tripId;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<TripShareCubit>(
      create: (_) => TripShareCubit(
        createShare: getIt(),
        getShares: getIt(),
        revokeShare: getIt(),
        getContacts: getIt(),
        tripId: tripId,
      ),
      child: BlocConsumer<TripShareCubit, TripShareState>(
        listenWhen: (TripShareState p, TripShareState c) =>
            p.shareRequest != c.shareRequest ||
            (c.failure != null && p.failure != c.failure),
        listener: _onChange,
        builder: (BuildContext context, TripShareState state) {
          final AppLocalizations l10n = context.l10n;
          final TripShareCubit cubit = context.read<TripShareCubit>();
          return Row(
            children: <Widget>[
              Expanded(
                child: AtaButton(
                  label: l10n.shareTrip,
                  icon: AtaIcons.pin,
                  variant: AtaButtonVariant.outline,
                  height: AtaSizes.buttonCompact,
                  loading: state.creating,
                  onPressed: cubit.shareLink,
                ),
              ),
              const SizedBox(width: AtaSpacing.sm),
              AtaButton(
                label: l10n.manageSharing,
                variant: AtaButtonVariant.soft,
                height: AtaSizes.buttonCompact,
                expanded: false,
                onPressed: () => TripShareSheet.show(context, cubit),
              ),
            ],
          );
        },
      ),
    );
  }

  void _onChange(BuildContext context, TripShareState state) {
    final AppLocalizations l10n = context.l10n;
    if (state.failure != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(failureText(state.failure!, l10n))),
      );
      return;
    }
    final String? url = state.linkToShare;
    if (url == null) return;
    SharePlus.instance.share(ShareParams(text: l10n.shareTripLinkText(url)));
  }
}
