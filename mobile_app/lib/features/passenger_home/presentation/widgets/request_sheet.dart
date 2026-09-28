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
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/female_driver_option.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/payment_row.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/ride_category_list.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/route_fields.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/searching_view.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/time_pills.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The white bottom sheet: request form or the "searching" state.
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
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            const SheetHandle(),
            BlocSelector<HomeCubit, HomeState, bool>(
              selector: (HomeState state) => state.isSearching,
              builder: (BuildContext context, bool searching) =>
                  searching ? const SearchingView() : const _RequestForm(),
            ),
          ],
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
        const SizedBox(height: AtaSpacing.xl),
        const RouteFields(),
        const SizedBox(height: AtaSpacing.xl),
        const TimePills(),
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
        const SizedBox(height: AtaSpacing.lg),
        const PaymentRow(),
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
          Money.compact(state.estimate.price),
        );
        return AtaButton(
          label: l10n.requestRide(name, price),
          trailingIcon: AtaIcons.arrow,
          onPressed: state.canRequest
              ? context.read<HomeCubit>().requestRide
              : null,
        );
      },
    );
  }
}
