import 'dart:ui';

import 'package:ata_app/design/painting/map_painter.dart';
import 'package:ata_app/design/painting/svg_path_parser.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('every icon path parses to a non-empty path', () {
    for (final AtaIcons icon in AtaIcons.values) {
      for (final String data in icon.paths) {
        final Path path = SvgPathParser.parse(data);
        expect(
          path.computeMetrics().isNotEmpty,
          isTrue,
          reason: '${icon.name}: $data',
        );
      }
    }
  });

  test('relative and absolute commands land on the same point', () {
    final Rect relative = SvgPathParser.parse('m15 18-6-6 6-6').getBounds();
    final Rect absolute = SvgPathParser.parse('M15 18L9 12L15 6').getBounds();
    expect(relative, absolute);
  });

  test('map projection uses cover scaling centered in the canvas', () {
    const Size size = Size(600, 800);
    final Offset origin = MapPainter.project(Offset.zero, size);
    expect(origin.dx, lessThan(0));
    expect(origin.dy, 0);
  });
}
