import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:flutter/material.dart';

/// Localized failure message with an optional retry button.
class FailureView extends StatelessWidget {
  const FailureView({super.key, required this.failure, this.onRetry});

  final Failure failure;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        InlineError(message: failureText(failure, context.l10n)),
        if (onRetry != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: context.l10n.retry,
            onPressed: onRetry,
            variant: AtaButtonVariant.outline,
            height: AtaSizes.buttonCompact,
          ),
        ],
      ],
    );
  }
}
