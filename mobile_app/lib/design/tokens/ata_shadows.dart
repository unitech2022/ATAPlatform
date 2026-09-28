import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:flutter/material.dart';

/// Shadow tokens.
abstract final class AtaShadows {
  static final List<BoxShadow> soft = <BoxShadow>[
    BoxShadow(
      color: AtaColors.ink.withValues(alpha: 0.06),
      offset: const Offset(0, 1),
      blurRadius: 12,
    ),
  ];

  /// Panel shadow for sheets (mobile direction: upwards).
  static final List<BoxShadow> panel = <BoxShadow>[
    BoxShadow(
      color: AtaColors.ink.withValues(alpha: 0.12),
      offset: const Offset(0, -12),
      blurRadius: 40,
    ),
  ];

  static final List<BoxShadow> float = <BoxShadow>[
    BoxShadow(
      color: AtaColors.ink.withValues(alpha: 0.20),
      offset: const Offset(0, 8),
      blurRadius: 24,
    ),
  ];

  static final List<BoxShadow> brand = <BoxShadow>[
    BoxShadow(
      color: AtaColors.brand.withValues(alpha: 0.12),
      offset: const Offset(0, 8),
      blurRadius: 24,
    ),
  ];

  static final List<BoxShadow> button = <BoxShadow>[
    BoxShadow(
      color: AtaColors.ink.withValues(alpha: 0.20),
      offset: const Offset(0, 12),
      blurRadius: 24,
    ),
  ];
}
