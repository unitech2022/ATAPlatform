/// A circle primitive in the 24x24 icon view box.
class IconCircle {
  const IconCircle(this.cx, this.cy, this.r);

  final double cx;
  final double cy;
  final double r;
}

/// The stroked icon set of the prototype (`viewBox 0 0 24 24`, stroke 1.8,
/// round caps and joins).
enum AtaIcons {
  arrow(paths: <String>['m15 18-6-6 6-6']),
  bell(
    paths: <String>['M6 9a6 6 0 0 1 12 0c0 7 3 7 3 7H3s3 0 3-7', 'M10 20h4'],
  ),
  car(
    paths: <String>[
      'M5 17H3v-5l2-5h14l2 5v5h-2',
      'M5 12h14M7 17h10M7.5 7 9 4h6l1.5 3',
    ],
    circles: <IconCircle>[IconCircle(7, 15, 1), IconCircle(17, 15, 1)],
  ),
  check(paths: <String>['m5 12 4 4L19 6']),
  chevron(paths: <String>['m9 18 6-6-6-6']),
  clock(
    paths: <String>['M12 7v5l3 2'],
    circles: <IconCircle>[IconCircle(12, 12, 9)],
  ),
  document(paths: <String>['M6 2h8l4 4v16H6z', 'M14 2v5h5M9 12h6M9 16h6']),
  home(paths: <String>['m4 11 8-7 8 7v9H4z', 'M9 20v-6h6v6']),
  location(
    paths: <String>['M12 2V0M12 24v-2M2 12H0M24 12h-2'],
    circles: <IconCircle>[IconCircle(12, 12, 8), IconCircle(12, 12, 2)],
  ),
  menu(paths: <String>['M4 7h16M4 12h16M4 17h16']),
  pin(
    paths: <String>['M20 10c0 5-8 12-8 12S4 15 4 10a8 8 0 1 1 16 0Z'],
    circles: <IconCircle>[IconCircle(12, 10, 2.5)],
  ),
  plus(paths: <String>['M12 5v14M5 12h14']),
  phone(
    paths: <String>[
      'M7 3h3l1 5-2 1c1 3 3 5 6 6l1-2 5 1v3c0 2-2 4-4 4C9 20 4 15 3 7c0-2 2-4 4-4Z',
    ],
  ),
  search(
    paths: <String>['m20 20-4-4'],
    circles: <IconCircle>[IconCircle(11, 11, 7)],
  ),
  shield(
    paths: <String>[
      'M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6z',
      'm9 12 2 2 4-4',
    ],
  ),
  upload(paths: <String>['M12 16V4M7 9l5-5 5 5', 'M5 14v6h14v-6']),
  user(
    paths: <String>['M4 21c1-5 4-7 8-7s7 2 8 7'],
    circles: <IconCircle>[IconCircle(12, 8, 4)],
  ),
  wallet(paths: <String>['M3 6h17v13H3zM3 9h17', 'M15 13h5v3h-5z']);

  const AtaIcons({required this.paths, this.circles = const <IconCircle>[]});

  /// SVG path data in the 24x24 view box.
  final List<String> paths;

  /// Circle primitives in the 24x24 view box.
  final List<IconCircle> circles;

  /// Size of the SVG view box.
  static const double viewBox = 24;
}
