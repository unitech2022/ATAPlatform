import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_row.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/corporate_block.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/fare_details_link.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/favorite_drivers_row.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/female_driver_option.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/offered_price_row.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/payment_row.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/promo_code_row.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/ride_category_list.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/route_fields.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/scheduled_summary_card.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/time_pills.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/presentation/widgets/demand_badge.dart';
import 'package:ata_app/features/rating/presentation/widgets/pending_rating_card.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/schedule_picker_sheet.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:ata_app/features/wallet/presentation/widgets/outstanding_balance_banner.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// The white bottom sheet with the request form. Once a trip is created the
/// router moves the rider to `/trip`.
class RequestSheet extends StatelessWidget {
  const RequestSheet({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AtaColors.white,
        borderRadius: AtaRadii.sheetTopRadius,
        boxShadow: AtaShadows.panel,
      ),
      child: SingleChildScrollView(
        padding: EdgeInsets.fromLTRB(
          AtaSpacing.gutter,
          AtaSpacing.md,
          AtaSpacing.gutter,
          AtaSpacing.gutter + MediaQuery.paddingOf(context).bottom,
        ),
        child: const Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[SheetHandle(), _RequestForm()],
        ),
      ),
    );
  }
}

class _RequestForm extends StatelessWidget {
  const _RequestForm();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(
          l10n.homeEyebrow,
          style: AtaText.label.copyWith(color: AtaColors.brand),
        ),
        const SizedBox(height: AtaSpacing.xxs),
        Text(l10n.homeTitle, style: AtaText.title),
        const SizedBox(height: AtaSpacing.xs),
        BlocSelector<HomeCubit, HomeState, bool>(
          selector: (HomeState state) => state.isScheduled,
          // Scheduled rides are priced without surge (F17).
          builder: (BuildContext context, bool scheduled) =>
              scheduled ? const SizedBox.shrink() : const DemandBadge(),
        ),
        const SizedBox(height: AtaSpacing.md),
        const PendingRatingCard(),
        const RouteFields(),
        const SizedBox(height: AtaSpacing.sm),
        const AirportRow(),
        const SizedBox(height: AtaSpacing.xl),
        const TimePills(),
        const ScheduledSummaryCard(),
        const SizedBox(height: AtaSpacing.xl),
        const FemaleDriverOption(),
        const SizedBox(height: AtaSpacing.xl),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: <Widget>[
            Text(l10n.chooseRide, style: AtaText.section),
            Text(l10n.pricesEstimated, style: AtaText.caption),
          ],
        ),
        const SizedBox(height: AtaSpacing.md),
        const RideCategoryList(),
        const FareDetailsLink(),
        const SizedBox(height: AtaSpacing.sm),
        const PaymentRow(),
        const CorporateBlock(),
        const SizedBox(height: AtaSpacing.sm),
        const FavoriteDriversRow(),
        const SizedBox(height: AtaSpacing.sm),
        const PromoCodeRow(),
        const SizedBox(height: AtaSpacing.sm),
        const OfferedPriceRow(),
        const SizedBox(height: AtaSpacing.lg),
        const _RequestButton(),
        const SizedBox(height: AtaSpacing.md),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: <Widget>[
            const AtaIcon(
              AtaIcons.shield,
              size: AtaSizes.iconSmall,
              color: AtaColors.brand,
            ),
            const SizedBox(width: AtaSpacing.xs),
            Text(l10n.safeRide, style: AtaText.caption),
          ],
        ),
      ],
    );
  }
}

class _RequestButton extends StatelessWidget {
  const _RequestButton();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<HomeCubit, HomeState>(
      builder: (BuildContext context, HomeState state) {
        if (state.categoriesFailure != null) {
          return InlineError(
            message: failureText(state.categoriesFailure!, l10n),
          );
        }
        final String name = state.selectedCategory?.name ?? '';
        final String price = l10n.priceWithCurrency(
          Money.compact(state.offeredPrice ?? state.displayPrice),
        );
        return BlocBuilder<TripRequestCubit, TripRequestState>(
          builder: (BuildContext context, TripRequestState request) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                if (request.isOutstandingBalance) ...<Widget>[
                  OutstandingBalanceBanner(
                    title: l10n.outstandingBalanceTitle,
                    message: failureText(request.failure!, l10n),
                    actionLabel: l10n.topUpToContinue,
                    amount: request.outstandingAmount,
                    onAction: () => _topUp(context),
                  ),
                  const SizedBox(height: AtaSpacing.sm),
                ] else if (request.failure != null) ...<Widget>[
                  InlineError(message: failureText(request.failure!, l10n)),
                  const SizedBox(height: AtaSpacing.sm),
                ],
                AtaButton(
                  label: state.isScheduled
                      ? l10n.scheduleRide(name, price)
                      : l10n.requestRide(name, price),
                  trailingIcon: AtaIcons.arrow,
                  loading: request.isBusy || request.isSearching,
                  onPressed:
                      state.canRequest &&
                          !request.isBusy &&
                          !request.isOutstandingBalance
                      ? () => _submit(context, state)
                      : null,
                ),
              ],
            );
          },
        );
      },
    );
  }

  /// Books the ride; a scheduled time is re-checked against the clock first
  /// (the window moves), and the picker reopens when it no longer fits.
  void _submit(BuildContext context, HomeState state) {
    if (state.isScheduled) {
      final ScheduleTimeCubit schedule = context.read<ScheduleTimeCubit>();
      if (!schedule.recheck()) {
        SchedulePickerSheet.show(
          context,
          rideCategoryId: state.selectedCategory?.id,
        );
        return;
      }
    }
    context.read<TripRequestCubit>().request(
      buildTripRequest(state, context.l10n),
    );
  }

  /// Opens the top-up and unblocks the request once the rider is back.
  Future<void> _topUp(BuildContext context) async {
    final TripRequestCubit cubit = context.read<TripRequestCubit>();
    await context.push<double>(AppRoutes.walletTopUp);
    cubit.reset();
  }
}
