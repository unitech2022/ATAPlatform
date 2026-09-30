import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Scrolling scaffold of the support pages: the rider variant sits inside
/// the passenger shell, the driver variant is a full page (`DriverSubpage`).
class SupportScaffold extends StatelessWidget {
  const SupportScaffold({
    super.key,
    required this.actor,
    required this.eyebrow,
    required this.title,
    required this.children,
    this.copy,
    this.backTo,
  });

  final TripActor actor;
  final String eyebrow;
  final String title;
  final String? copy;
  final List<Widget> children;

  /// Where "back" goes when there is nothing to pop (the help center by
  /// default).
  final String? backTo;

  bool get _driver => actor == TripActor.driver;

  @override
  Widget build(BuildContext context) {
    if (_driver) {
      return DriverSubpage.content(
        eyebrow: eyebrow,
        title: title,
        copy: copy,
        children: children,
      );
    }
    return PageWrap(
      children: <Widget>[
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: PillButton.back(
            label: context.l10n.back,
            onTap: () => context.canPop()
                ? context.pop()
                : context.go(backTo ?? AppRoutes.support),
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        ScreenTitle(eyebrow: eyebrow, title: title, copy: copy),
        const SizedBox(height: AtaSpacing.xl),
        ...children,
      ],
    );
  }
}
