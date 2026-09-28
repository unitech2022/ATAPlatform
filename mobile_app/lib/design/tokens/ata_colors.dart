import 'package:flutter/material.dart';

/// Color tokens from `docs/01-design-system.md`.
abstract final class AtaColors {
  static const Color brand = Color(0xFF19B7A5);
  static const Color brandSoft = Color(0xFFE8F8F5);
  static const Color ink = Color(0xFF123650);
  static const Color inkSoft = Color(0xFF194661);
  static const Color muted = Color(0xFF6B7F8E);
  static const Color line = Color(0xFFDBE5E8);
  static const Color cloud = Color(0xFFF3F7F7);
  static const Color canvas = Color(0xFFEEF3F2);
  static const Color map = Color(0xFFE5EEEB);
  static const Color danger = Color(0xFFC23B4A);
  static const Color dangerSoft = Color(0xFFFFF0F1);
  static const Color white = Color(0xFFFFFFFF);

  /// Map drawing colors (prototype `index.css`).
  static const Color mapBlockFill = Color(0xFFF3F7F5);
  static const Color mapBlockStroke = Color(0xFFD6E3DF);

  /// Translucent variants used on dark cards and backgrounds.
  static final Color white10 = white.withValues(alpha: 0.10);
  static final Color white25 = white.withValues(alpha: 0.25);
  static final Color white60 = white.withValues(alpha: 0.60);
  static final Color white70 = white.withValues(alpha: 0.70);
  static final Color white80 = white.withValues(alpha: 0.80);
  static final Color brand10 = brand.withValues(alpha: 0.10);
  static final Color brand20 = brand.withValues(alpha: 0.20);
  static final Color ink5 = ink.withValues(alpha: 0.05);
}
