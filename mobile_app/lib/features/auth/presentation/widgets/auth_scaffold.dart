import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_logo.dart';
import 'package:ata_app/design/widgets/decorative_background.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:flutter/material.dart';

/// Registration-flow scaffold: decorative circles, logo header with an
/// optional "back" pill, and a centered scrollable body.
class AuthScaffold extends StatelessWidget {
  const AuthScaffold({
    super.key,
    required this.child,
    this.onBack,
    this.maxWidth = defaultMaxWidth,
  });

  final Widget child;
  final VoidCallback? onBack;
  final double maxWidth;

  static const double defaultMaxWidth = 480;
  static const double _bottomPadding = 40;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: DecorativeBackground(
        child: SafeArea(
          child: Column(
            children: <Widget>[
              SizedBox(
                height: AtaSizes.header,
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AtaSpacing.gutter,
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: <Widget>[
                      const AtaLogo(),
                      if (onBack != null)
                        PillButton.back(
                          label: context.l10n.back,
                          onTap: onBack,
                        ),
                    ],
                  ),
                ),
              ),
              Expanded(
                child: LayoutBuilder(
                  builder: (BuildContext context, BoxConstraints constraints) {
                    return SingleChildScrollView(
                      padding: const EdgeInsets.fromLTRB(
                        AtaSpacing.gutter,
                        AtaSpacing.md,
                        AtaSpacing.gutter,
                        _bottomPadding,
                      ),
                      child: ConstrainedBox(
                        constraints: BoxConstraints(
                          minHeight:
                              constraints.maxHeight -
                              AtaSpacing.md -
                              _bottomPadding,
                        ),
                        child: Center(
                          child: ConstrainedBox(
                            constraints: BoxConstraints(maxWidth: maxWidth),
                            child: child,
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
