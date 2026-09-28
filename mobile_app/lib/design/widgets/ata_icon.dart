import 'package:ata_app/design/painting/svg_path_parser.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// Draws one of the prototype's stroked icons.
///
/// The color defaults to the ambient [IconTheme] color. Set [mirrored] to
/// flip horizontally (the prototype's `rotate-180` arrow).
class AtaIcon extends StatelessWidget {
  const AtaIcon(
    this.icon, {
    super.key,
    this.size = AtaSizes.iconDefault,
    this.color,
    this.mirrored = false,
  });

  final AtaIcons icon;
  final double size;
  final Color? color;
  final bool mirrored;

  @override
  Widget build(BuildContext context) {
    final Color resolved =
        color ??
        IconTheme.of(context).color ??
        Theme.of(context).colorScheme.onSurface;
    return SizedBox(
      width: size,
      height: size,
      child: CustomPaint(
        painter: _IconPainter(icon: icon, color: resolved, mirrored: mirrored),
      ),
    );
  }
}

class _IconPainter extends CustomPainter {
  _IconPainter({
    required this.icon,
    required this.color,
    required this.mirrored,
  });

  final AtaIcons icon;
  final Color color;
  final bool mirrored;

  static final Map<AtaIcons, List<Path>> _cache = <AtaIcons, List<Path>>{};

  List<Path> _paths() => _cache.putIfAbsent(
    icon,
    () => icon.paths.map(SvgPathParser.parse).toList(growable: false),
  );

  @override
  void paint(Canvas canvas, Size size) {
    final double scale = size.width / AtaIcons.viewBox;
    final Paint paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = AtaSizes.iconStroke * scale
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;

    canvas.save();
    if (mirrored) {
      canvas.translate(size.width, 0);
      canvas.scale(-1, 1);
    }
    canvas.scale(scale);
    for (final Path path in _paths()) {
      canvas.drawPath(path, paint);
    }
    for (final IconCircle circle in icon.circles) {
      canvas.drawCircle(Offset(circle.cx, circle.cy), circle.r, paint);
    }
    canvas.restore();
  }

  @override
  bool shouldRepaint(_IconPainter oldDelegate) =>
      oldDelegate.icon != icon ||
      oldDelegate.color != color ||
      oldDelegate.mirrored != mirrored;
}
