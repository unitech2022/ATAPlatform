import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:flutter/material.dart';

/// `canvas` background with the two decorative circles of the auth screens
/// (`brand/10` top-end, `ink/5` bottom-start).
class DecorativeBackground extends StatelessWidget {
  const DecorativeBackground({super.key, required this.child});

  final Widget child;

  static const double _size = 384;
  static const double _topOffset = -112;
  static const double _bottomOffset = -128;
  static const double _startOffset = -96;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: AtaColors.canvas,
      child: Stack(
        children: <Widget>[
          PositionedDirectional(
            end: _topOffset,
            top: _topOffset,
            child: _Circle(color: AtaColors.brand10),
          ),
          PositionedDirectional(
            start: _startOffset,
            bottom: _bottomOffset,
            child: _Circle(color: AtaColors.ink5),
          ),
          Positioned.fill(child: child),
        ],
      ),
    );
  }
}

/// Radial brand gradient over `canvas`, used behind inner pages.
class PageBackground extends StatelessWidget {
  const PageBackground({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: AtaColors.canvas,
        gradient: RadialGradient(
          center: const AlignmentDirectional(-0.76, -0.84),
          radius: 0.9,
          colors: <Color>[
            AtaColors.brand10,
            AtaColors.brand10.withValues(alpha: 0),
          ],
        ),
      ),
      child: child,
    );
  }
}

class _Circle extends StatelessWidget {
  const _Circle({required this.color});

  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: DecorativeBackground._size,
      height: DecorativeBackground._size,
      decoration: BoxDecoration(color: color, shape: BoxShape.circle),
    );
  }
}
