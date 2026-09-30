import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/presentation/cubit/report_form_cubits.dart';
import 'package:ata_app/features/safety/presentation/widgets/choice_wrap.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_subpage.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_text.dart';
import 'package:ata_app/features/support/presentation/widgets/ticket_link_button.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/safety/report?tripId=`: non-emergency safety report (F12.6).
class SafetyReportPage extends StatelessWidget {
  const SafetyReportPage({super.key, required this.tripId});

  final String tripId;

  static const int _maxDescription = 2000;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<SafetyReportCubit>(
      create: (_) => SafetyReportCubit(submit: getIt(), tripId: tripId),
      child: BlocBuilder<SafetyReportCubit, SafetyReportFormState>(
        builder: (BuildContext context, SafetyReportFormState state) {
          final SafetyReportCubit cubit = context.read<SafetyReportCubit>();
          final SafetyCaseSummary? result = state.result;
          return SafetySubpage(
            title: l10n.safetyReportTitle,
            copy: l10n.safetyReportCopy,
            children: <Widget>[
              if (result != null)
                AtaCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: <Widget>[
                      Text(l10n.reportSubmitted, style: AtaText.section),
                      Text(
                        l10n.reportNumberLine(result.caseNumber),
                        style: AtaText.small,
                      ),
                      const SizedBox(height: AtaSpacing.md),
                      if (result.supportTicketId != null) ...<Widget>[
                        TicketLinkButton(ticketId: result.supportTicketId!),
                        const SizedBox(height: AtaSpacing.sm),
                      ],
                      AtaButton(
                        label: l10n.myReportsTitle,
                        height: AtaSizes.buttonCompact,
                        onPressed: () =>
                            context.go(AppRoutes.safetyCase(result.id)),
                      ),
                    ],
                  ),
                )
              else ...<Widget>[
                ChoiceWrap<SafetyReportCategory>(
                  values: SafetyReportCategory.values,
                  selected: state.category,
                  label: (SafetyReportCategory c) =>
                      SafetyText.reportCategory(l10n, c),
                  onSelected: state.submitting ? null : cubit.categoryChanged,
                  error: state.showErrors && state.categoryMissing
                      ? l10n.chooseCategory
                      : null,
                ),
                const SizedBox(height: AtaSpacing.md),
                TextField(
                  onChanged: cubit.descriptionChanged,
                  maxLength: _maxDescription,
                  minLines: 3,
                  maxLines: 6,
                  decoration: InputDecoration(
                    hintText: l10n.describeWhatHappened,
                    errorText: state.showErrors && state.descriptionMissing
                        ? l10n.descriptionRequired
                        : null,
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
