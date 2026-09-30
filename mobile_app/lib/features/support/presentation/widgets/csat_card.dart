import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/star_rating.dart';
import 'package:ata_app/features/support/presentation/cubit/csat_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/ticket_detail_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The one-time satisfaction prompt of a resolved / closed ticket: 1–5
/// stars and an optional comment. Once sent (or when the ticket already has
/// a score) it becomes a thank-you.
class CsatCard extends StatelessWidget {
  const CsatCard({super.key, this.ratedScore});

  /// The score already stored on the ticket, if any.
  final int? ratedScore;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocConsumer<CsatCubit, CsatState>(
      listenWhen: (CsatState p, CsatState c) => !p.submitted && c.submitted,
      listener: (BuildContext context, CsatState state) =>
          context.read<TicketDetailCubit>().rated(state.score),
      builder: (BuildContext context, CsatState state) {
        final CsatCubit cubit = context.read<CsatCubit>();
        final bool done = state.submitted || ratedScore != null;
        return AtaCard(
          padding: const EdgeInsets.all(AtaSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Text(
                done ? l10n.csatThanks : l10n.csatTitle,
                style: AtaText.section,
              ),
              const SizedBox(height: AtaSpacing.sm),
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: StarRating(
                  value: done ? (ratedScore ?? state.score) : state.score,
                  onChanged: done ? null : cubit.selectScore,
                  semanticLabel: l10n.csatStar,
                ),
              ),
              if (!done) ...<Widget>[
                const SizedBox(height: AtaSpacing.sm),
                TextField(
                  key: const ValueKey<String>('csat-comment'),
                  onChanged: cubit.commentChanged,
                  maxLength: CsatState.commentMax,
                  minLines: 2,
                  maxLines: 4,
                  decoration: InputDecoration(
                    hintText: l10n.csatCommentHint,
                    counterText: '',
                  ),
                ),
                if (state.failure != null) ...<Widget>[
                  const SizedBox(height: AtaSpacing.xs),
                  InlineError(message: failureText(state.failure!, l10n)),
                ],
                const SizedBox(height: AtaSpacing.sm),
                AtaButton(
                  key: const ValueKey<String>('csat-submit'),
                  label: l10n.csatSubmit,
                  height: AtaSizes.buttonCompact,
                  loading: state.submitting,
                  onPressed: state.canSubmit ? cubit.submit : null,
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}
