import 'dart:math' as math;
import 'dart:ui';

import 'package:ata_app/design/painting/svg_path_parser.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:flutter/rendering.dart';

/// Paints the prototype's abstract city map: blocks, white roads and a
/// dashed brand route. Coordinates live in a 1200x800 view box drawn with
/// `preserveAspectRatio="xMidYMid slice"` semantics (cover).
class MapPainter extends CustomPainter {
  const MapPainter();

  static const double viewWidth = 1200;
  static const double viewHeight = 800;

  static const double _blockStroke = 2;
  static const double _roadWidth = 18;
  static const double _roadWideWidth = 30;
  static const double _routeShadowWidth = 12;
  static const double _routeWidth = 7;
  static const double _dashLength = 2;
  static const double _dashGap = 16;

  static const String _blocks =
      'M-20 80 170 20l90 120-80 130L0 240ZM310 0h240l20 170-250 30-70-100Z'
      'M650 20l180 10 70 170-260 50-60-120ZM970-20l260 40v220l-240-30-70-120Z'
      'M30 340l190-40 100 150-80 160-260 20ZM400 290l210-40 90 160-50 170-260-10-80-140Z'
      'M780 290l170-40 120 130-100 180-250-20-20-150ZM1080 310l160-30v300l-190-40 20-130Z'
      'M30 700l250-50 110 170H0ZM460 650l210-20 80 190H370ZM820 620l210-30 200 160v80H780Z';
  static const String _roadWide =
      'M-30 680C210 560 240 330 480 310s300 100 450-10 190-230 320-240';
  static const String _roads =
      'M130-20c10 190 170 240 190 390s-70 250 0 460'
      'M700-20c-70 170 30 270 10 430s-100 230-80 420'
      'M1010-20c-50 160 30 270-20 390s-120 210-70 460'
      'M-20 180c190 10 290 90 460 40s260-90 400 10 250 40 400 80'
      'M-20 510c190 40 300-50 440 20s280 120 430 50 220-10 390 40';
  static const String _route =
      'M398 552c75-35 73-115 146-154 78-42 133 20 200-25 63-43 51-105 115-142';

  static final Path _blocksPath = SvgPathParser.parse(_blocks);
  static final Path _roadWidePath = SvgPathParser.parse(_roadWide);
  static final Path _roadsPath = SvgPathParser.parse(_roads);
  static final Path _routePath = SvgPathParser.parse(_route);
  static final Path _routeDashes = _dashed(_routePath);

  /// Route start (pickup pin) and end (car) in view-box coordinates.
  static const Offset routeStart = Offset(398, 552);
  static const Offset routeEnd = Offset(859, 231);

  /// Maps a view-box point to the painted [size] using cover scaling.
  static Offset project(Offset point, Size size) {
    final double scale = _scale(size);
    final Offset origin = _origin(size, scale);
    return origin + point * scale;
  }

  static double _scale(Size size) =>
      math.max(size.width / viewWidth, size.height / viewHeight);

  static Offset _origin(Size size, double scale) => Offset(
    (size.width - viewWidth * scale) / 2,
    (size.height - viewHeight * scale) / 2,
  );

  static Path _dashed(Path source) {
    final Path result = Path();
    for (final PathMetric metric in source.computeMetrics()) {
      double distance = 0;
      while (distance < metric.length) {
        final double end = math.min(distance + _dashLength, metric.length);
        result.addPath(metric.extractPath(distance, end), Offset.zero);
        distance += _dashLength + _dashGap;
      }
    }
    return result;
  }

  @override
  void paint(Canvas canvas, Size size) {
    canvas.drawRect(Offset.zero & size, Paint()..color = AtaColors.map);
    final double scale = _scale(size);
    final Offset origin = _origin(size, scale);
    canvas.save();
    canvas.translate(origin.dx, origin.dy);
    canvas.scale(scale);

    canvas.drawPath(_blocksPath, Paint()..color = AtaColors.mapBlockFill);
    canvas.drawPath(
      _blocksPath,
      _stroke(AtaColors.mapBlockStroke, _blockStroke),
    );
    canvas.drawPath(_roadWidePath, _stroke(AtaColors.white, _roadWideWidth));
    canvas.drawPath(_roadsPath, _stroke(AtaColors.white, _roadWidth));
    canvas.drawPath(_routePath, _stroke(AtaColors.white, _routeShadowWidth));
    canvas.drawPath(_routeDashes, _stroke(AtaColors.brand, _routeWidth));
    canvas.restore();
  }

  static Paint _stroke(Color color, double width) => Paint()
    ..color = color
    ..style = PaintingStyle.stroke
    ..strokeWidth = width
    ..strokeCap = StrokeCap.round
    ..strokeJoin = StrokeJoin.round;

  @override
  bool shouldRepaint(MapPainter oldDelegate) => false;
}
