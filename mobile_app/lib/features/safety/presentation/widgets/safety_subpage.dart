import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Rider safety sub-page: back pill, title and content.
class SafetySubpage extends StatelessWidget {
  const SafetySubpage({
    super.key,
    required this.title,
    required this.children,
    this.copy,
    this.backTo = AppRoutes.safety,
  });

  final String title;
  final String? copy;
  final List<Widget> children;

  /// Where "back" goes when there is nothing to pop.
  final String backTo;

  @override
  Widget build(BuildContext context) {
    return PageWrap(
      children: <Widget>[
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: PillButton.back(
            label: context.l10n.back,
            onTap: () => context.canPop() ? context.pop() : context.go(backTo),
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        ScreenTitle(
          eyebrow: context.l10n.safetyEyebrow,
          title: title,
          copy: copy,
        ),
        const SizedBox(height: AtaSpacing.xl),
        ...children,
      ],
    );
  }
}
