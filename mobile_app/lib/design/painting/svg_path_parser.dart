import 'dart:ui';

/// Converts an SVG `d` attribute into a Flutter [Path].
///
/// Supports the commands used by the prototype icon set and map:
/// `M m L l H h V v C c S s A a Z z` with implicit repetition.
abstract final class SvgPathParser {
  static final RegExp _token = RegExp(
    r'[MmLlHhVvCcSsAaZz]|-?(?:\d+\.?\d*|\.\d+)(?:e-?\d+)?',
  );

  static Path parse(String data) {
    final List<String> tokens = _token
        .allMatches(data)
        .map((RegExpMatch m) => m.group(0)!)
        .toList(growable: false);
    return _Builder(tokens).build();
  }
}

class _Builder {
  _Builder(this._tokens);

  final List<String> _tokens;
  final Path _path = Path();
  int _index = 0;
  double _x = 0;
  double _y = 0;
  double _startX = 0;
  double _startY = 0;
  double? _ctrlX;
  double? _ctrlY;

  static bool _isCommand(String token) =>
      token.length == 1 && RegExp('[A-Za-z]').hasMatch(token);

  double _next() => double.parse(_tokens[_index++]);

  Path build() {
    String command = 'M';
    while (_index < _tokens.length) {
      final String token = _tokens[_index];
      if (_isCommand(token)) {
        command = token;
        _index++;
        if (command == 'Z' || command == 'z') {
          _close();
          continue;
        }
      }
      _apply(command);
      // After an initial moveTo, implicit coordinates behave as lineTo.
      if (command == 'M') command = 'L';
      if (command == 'm') command = 'l';
    }
    return _path;
  }

  void _close() {
    _path.close();
    _x = _startX;
    _y = _startY;
    _ctrlX = null;
    _ctrlY = null;
  }

  void _apply(String command) {
    switch (command) {
      case 'M':
        _moveTo(_next(), _next());
      case 'm':
        _moveTo(_x + _next(), _y + _next());
      case 'L':
        _lineTo(_next(), _next());
      case 'l':
        _lineTo(_x + _next(), _y + _next());
      case 'H':
        _lineTo(_next(), _y);
      case 'h':
        _lineTo(_x + _next(), _y);
      case 'V':
        _lineTo(_x, _next());
      case 'v':
        _lineTo(_x, _y + _next());
      case 'C':
        _cubic(_next(), _next(), _next(), _next(), _next(), _next());
      case 'c':
        final double x1 = _x + _next();
        final double y1 = _y + _next();
        final double x2 = _x + _next();
        final double y2 = _y + _next();
        _cubic(x1, y1, x2, y2, _x + _next(), _y + _next());
      case 'S':
        _smooth(_next(), _next(), _next(), _next());
      case 's':
        final double x2 = _x + _next();
        final double y2 = _y + _next();
        _smooth(x2, y2, _x + _next(), _y + _next());
      case 'A':
        _arc(relative: false);
      case 'a':
        _arc(relative: true);
      default:
        throw FormatException('Unsupported SVG path command: $command');
    }
  }

  void _moveTo(double x, double y) {
    _path.moveTo(x, y);
    _x = _startX = x;
    _y = _startY = y;
    _ctrlX = null;
    _ctrlY = null;
  }

  void _lineTo(double x, double y) {
    _path.lineTo(x, y);
    _x = x;
    _y = y;
    _ctrlX = null;
    _ctrlY = null;
  }

  void _cubic(double x1, double y1, double x2, double y2, double x, double y) {
    _path.cubicTo(x1, y1, x2, y2, x, y);
    _ctrlX = x2;
    _ctrlY = y2;
    _x = x;
    _y = y;
  }

  void _smooth(double x2, double y2, double x, double y) {
    final double x1 = _ctrlX == null ? _x : 2 * _x - _ctrlX!;
    final double y1 = _ctrlY == null ? _y : 2 * _y - _ctrlY!;
    _cubic(x1, y1, x2, y2, x, y);
  }

  void _arc({required bool relative}) {
    final double rx = _next();
    final double ry = _next();
    final double rotation = _next();
    final bool largeArc = _next() != 0;
    final bool sweep = _next() != 0;
    final double x = (relative ? _x : 0) + _next();
    final double y = (relative ? _y : 0) + _next();
    _path.arcToPoint(
      Offset(x, y),
      radius: Radius.elliptical(rx, ry),
      rotation: rotation,
      largeArc: largeArc,
      clockwise: sweep,
    );
    _x = x;
    _y = y;
    _ctrlX = null;
    _ctrlY = null;
  }
}
