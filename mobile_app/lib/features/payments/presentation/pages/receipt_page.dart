import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/add_favorite_button.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/presentation/cubit/receipt_cubit.dart';
import 'package:ata_app/features/payments/presentation/cubit/receipt_state.dart';
import 'package:ata_app/features/payments/presentation/widgets/receipt_lines_card.dart';
import 'package:ata_app/features/payments/presentation/widgets/receipt_payment_card.dart';
import 'package:ata_app/features/safety/presentation/widgets/trip_help_actions.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/rides/:tripId/receipt`: itemised receipt of a completed trip.
class ReceiptPage extends StatelessWidget {
  const ReceiptPage({super.key, required this.tripId});

  final String tripId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<ReceiptCubit>(
      create: (_) =>
          ReceiptCubit(getTripReceipt: getIt(), tripId: tripId)..load(),
      child: PageWrap(
        children: <Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: PillButton.back(
              label: l10n.back,
              onTap: () => context.canPop()
                  ? context.pop()
                  : context.go(AppRoutes.rides),
            ),
          ),
          const SizedBox(height: AtaSpacing.xl),
          BlocBuilder<ReceiptCubit, ReceiptState>(
            builder: (BuildContext context, ReceiptState state) {
              final Receipt? receipt = state.receipt;
              if (receipt == null && state.failure == null) {
                return const CenteredLoader();
              }
              if (state.unavailable) {
                return AtaCard(
                  child: Text(
                    l10n.receiptUnavailable,
                    style: AtaText.bodyMuted,
                    textAlign: TextAlign.center,
                  ),
                );
              }
              if (receipt == null) {
                return FailureView(
                  failure: state.failure!,
                  onRetry: context.read<ReceiptCubit>().load,
                );
              }
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  _ReceiptBody(receipt: receipt),
                  if (receipt.driverName != null) ...<Widget>[
                    const SizedBox(height: AtaSpacing.md),
                    AddFavoriteButton(tripId: tripId),
                  ],
                  const SizedBox(height: AtaSpacing.md),
                  TripHelpActions(tripId: tripId),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}

class _ReceiptBody extends StatelessWidget {
  const _ReceiptBody({required this.receipt});

  final Receipt receipt;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? issued = receipt.issuedAt;
    final List<String> meta = <String>[
      ?receipt.rideCategory,
      ?receipt.driverName,
      if (receipt.distanceMeters > 0)
        TripText.distance(l10n, receipt.distanceMeters),
      if (receipt.durationSeconds > 0)
        TripText.duration(l10n, receipt.durationSeconds),
    ];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        ScreenTitle(
          eyebrow: issued == null
              ? l10n.receiptEyebrow
              : DateText.longDate(issued, context.localeCode),
          title: receipt.dropoffName == null
              ? l10n.receiptTitle
              : l10n.tripRoute(receipt.pickupName ?? '', receipt.dropoffName!),
          copy: meta.join(' · '),
        ),
        const SizedBox(height: AtaSpacing.xl),
        ReceiptLinesCard(receipt: receipt),
        const SizedBox(height: AtaSpacing.md),
        ReceiptPaymentCard(receipt: receipt),
      ],
    );
  }
}
