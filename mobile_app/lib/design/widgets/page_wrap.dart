import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/decorative_background.dart';
import 'package:flutter/material.dart';

/// Scrollable page body with the radial background, page gutters and extra
/// bottom padding so content clears the floating bottom navigation.
class PageWrap extends StatelessWidget {
  const PageWrap({
    super.key,
    required this.children,
    this.bottomPadding = navClearance,
    this.center = false,
  });

  final List<Widget> children;
  final double bottomPadding;
  final bool center;

  /// Height reserved for the floating [BottomNav] plus its margins.
  static const double navClearance = 112;
  static const double _topPadding = 48;

  @override
  Widget build(BuildContext context) {
    final Widget column = Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisAlignment: center
          ? MainAxisAlignment.center
          : MainAxisAlignment.start,
      children: children,
    );
    return PageBackground(
      child: LayoutBuilder(
        builder: (BuildContext context, BoxConstraints constraints) {
          return SingleChildScrollView(
            padding: EdgeInsets.fromLTRB(
              AtaSpacing.gutter,
              _topPadding,
              AtaSpacing.gutter,
              bottomPadding,
            ),
            child: center
                ? ConstrainedBox(
                    constraints: BoxConstraints(
                      minHeight:
                          constraints.maxHeight - _topPadding - bottomPadding,
                    ),
                    child: column,
                  )
                : column,
          );
        },
      ),
    );
  }
}
