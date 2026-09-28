import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_cases_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_list_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_subpage.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_text.dart';
import 'package:ata_app/features/safety/presentation/widgets/status_badge.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/safety/cases` (my reports) and `/safety/cases/:caseId` (one case with
/// its public notes, from `ata://safety/cases/{id}`).
class SafetyCasesPage extends StatelessWidget {
  const SafetyCasesPage({super.key, this.caseId});

  final String? caseId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<SafetyCasesCubit>(
      create: (_) {
        final SafetyCasesCubit cubit = SafetyCasesCubit(
          getCases: getIt(),
          getCase: getIt(),
        );
        caseId == null ? cubit.load() : cubit.loadOne(caseId!);
        return cubit;
      },
      child: BlocBuilder<SafetyCasesCubit, SafetyListState<SafetyCaseSummary>>(
        builder: (BuildContext context, SafetyListState<SafetyCaseSummary> s) {
          final SafetyCasesCubit cubit = context.read<SafetyCasesCubit>();
          return SafetySubpage(
            title: l10n.myReportsTitle,
            copy: caseId == null ? l10n.myReportsCopy : null,
            backTo: caseId == null ? AppRoutes.safety : AppRoutes.safetyCases,
            children: <Widget>[
              if (s.loading && s.items.isEmpty)
                const CenteredLoader()
              else if (s.failure != null && s.items.isEmpty)
                FailureView(
                  failure: s.failure!,
                  onRetry: () =>
                      caseId == null ? cubit.load() : cubit.loadOne(caseId!),
                )
              else if (s.isEmpty)
                Text(l10n.noReports, style: AtaText.bodyMuted)
              else
                for (final SafetyCaseSummary c in s.items)
                  _CaseCard(item: c, detailed: caseId != null),
            ],
          );
        },
      ),
    );
  }
}

class _CaseCard extends StatelessWidget {
  const _CaseCard({required this.item, required this.detailed});

  final SafetyCaseSummary item;
  final bool detailed;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? opened = item.openedAt;
    return Padding(
      padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
      child: AtaCard(
        onTap: detailed
            ? null
            : () => context.push(AppRoutes.safetyCase(item.id)),
        padding: const EdgeInsets.all(AtaSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    SafetyText.caseType(l10n, item.type),
                    style: AtaText.bodyStrong,
                  ),
                ),
                StatusBadge(
                  status: item.status,
                  label: SafetyText.caseStatus(l10n, item.status),
                ),
              ],
            ),
            Text(
              <String>[
                item.caseNumber,
                ?item.tripNumber,
                if (opened != null)
                  DateText.dayAndTime(opened, context.localeCode),
              ].join(' · '),
              style: AtaText.caption,
            ),
            if (detailed) ...<Widget>[
              const SizedBox(height: AtaSpacing.sm),
              if (item.publicNotes.isEmpty)
                Text(l10n.caseNoUpdates, style: AtaText.small)
              else
                for (final SafetyCaseNote note in item.publicNotes)
                  Padding(
                    padding: const EdgeInsets.only(top: AtaSpacing.xs),
                    child: Text(note.body, style: AtaText.body),
                  ),
            ],
          ],
        ),
      ),
    );
  }
}
