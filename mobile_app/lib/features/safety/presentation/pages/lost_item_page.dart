import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/presentation/cubit/report_form_cubits.dart';
import 'package:ata_app/features/safety/presentation/widgets/choice_wrap.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_subpage.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/rides/:tripId/lost-item`: report an item left in a completed trip.
class LostItemPage extends StatelessWidget {
  const LostItemPage({super.key, required this.tripId});

  final String tripId;

  static const int _maxDescription = 1000;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<LostItemCubit>(
      create: (_) => LostItemCubit(report: getIt(), tripId: tripId),
      child: BlocBuilder<LostItemCubit, LostItemFormState>(
        builder: (BuildContext context, LostItemFormState state) {
          final LostItemCubit cubit = context.read<LostItemCubit>();
          final LostItemReport? result = state.result;
          return SafetySubpage(
            title: l10n.lostItemTitle,
            copy: l10n.lostItemCopy,
            backTo: AppRoutes.rides,
            children: <Widget>[
              if (result != null)
                AtaCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: <Widget>[
                      Text(l10n.lostItemSubmitted, style: AtaText.section),
                      Text(
                        l10n.reportNumberLine(result.reportNumber),
                        style: AtaText.small,
                      ),
                      Text(
                        SafetyText.lostStatus(l10n, result.status),
                        style: AtaText.caption,
                      ),
                      const SizedBox(height: AtaSpacing.md),
                      AtaButton(
                        label: l10n.lostItemsTitle,
                        height: AtaSizes.buttonCompact,
                        onPressed: () => context.go(AppRoutes.safetyLostItems),
                      ),
                    ],
                  ),
                )
              else ...<Widget>[
                ChoiceWrap<LostItemCategory>(
                  values: LostItemCategory.values,
                  selected: state.category,
                  label: (LostItemCategory c) =>
                      SafetyText.lostCategory(l10n, c),
                  onSelected: state.submitting ? null : cubit.categoryChanged,
                  error: state.showErrors && state.categoryMissing
                      ? l10n.chooseCategory
                      : null,
                ),
                const SizedBox(height: AtaSpacing.md),
                TextField(
                  onChanged: cubit.descriptionChanged,
                  maxLength: _maxDescription,
                  minLines: 2,
                  maxLines: 5,
                  decoration: InputDecoration(
                    hintText: l10n.lostItemDescribe,
                    errorText: state.showErrors && state.descriptionMissing
                        ? l10n.descriptionRequired
                        : null,
                  ),
                ),
                TextField(
                  onChanged: cubit.contactPhoneChanged,
                  keyboardType: TextInputType.phone,
                  textDirection: TextDirection.ltr,
                  decoration: InputDecoration(
                    labelText: l10n.lostItemContactPhone,
                    hintText: l10n.contactPhoneHint,
                  ),
                ),
                if (state.failure != null) ...<Widget>[
                  const SizedBox(height: AtaSpacing.sm),
                  InlineError(message: failureText(state.failure!, l10n)),
                ],
                const SizedBox(height: AtaSpacing.md),
                AtaButton(
                  label: l10n.submitReport,
                  loading: state.submitting,
                  onPressed: cubit.submit,
                ),
              ],
            ],
          );
        },
      ),
    );
  }
}
