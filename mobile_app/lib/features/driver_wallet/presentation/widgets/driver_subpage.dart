import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/decorative_background.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Scaffold of the driver wallet pages (outside the dashboard): background,
/// back pill to the dashboard, title and a scrollable body.
class DriverSubpage extends StatelessWidget {
  const DriverSubpage({super.key, required this.child})
    : eyebrow = null,
      title = null,
      copy = null,
      children = const <Widget>[];

  const DriverSubpage.content({
    super.key,
    required String this.eyebrow,
    required String this.title,
    required this.children,
    this.copy,
  }) : child = null;

  final Widget? child;
  final String? eyebrow;
  final String? title;
  final String? copy;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final Widget? plain = child;
    return Scaffold(
      body: SafeArea(child: plain ?? PageBackground(child: _body(context))),
    );
  }

  Widget _body(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(
        AtaSpacing.gutter,
        AtaSpacing.xl,
        AtaSpacing.gutter,
        AtaSpacing.xxxl,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: PillButton.back(
              label: context.l10n.back,
              onTap: () => context.canPop()
                  ? context.pop()
                  : context.go(AppRoutes.driver),
            ),
          ),
          const SizedBox(height: AtaSpacing.xl),
          ScreenTitle(eyebrow: eyebrow!, title: title!, copy: copy),
          const SizedBox(height: AtaSpacing.xl),
          ...children,
        ],
      ),
    );
  }
}
