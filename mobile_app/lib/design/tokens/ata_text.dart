import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:flutter/material.dart';

/// Typography tokens. Weight 700 is the maximum used (design rule).
abstract final class AtaText {
  static const String fontFamily = 'IBM Plex Sans Arabic';
  static const double bodyLineHeight = 1.75;

  static const TextStyle display = TextStyle(
    fontSize: 36,
    fontWeight: FontWeight.w700,
    height: 1.15,
    color: AtaColors.ink,
  );
  static const TextStyle title = TextStyle(
    fontSize: 28,
    fontWeight: FontWeight.w700,
    height: 1.2,
    color: AtaColors.ink,
  );
  static const TextStyle headline = TextStyle(
    fontSize: 22,
    fontWeight: FontWeight.w700,
    height: 1.3,
    color: AtaColors.ink,
  );
  static const TextStyle section = TextStyle(
    fontSize: 18,
    fontWeight: FontWeight.w700,
    height: 1.4,
    color: AtaColors.ink,
  );
  static const TextStyle body = TextStyle(
    fontSize: 16,
    fontWeight: FontWeight.w400,
    height: bodyLineHeight,
    color: AtaColors.ink,
  );
  static const TextStyle bodyStrong = TextStyle(
    fontSize: 16,
    fontWeight: FontWeight.w700,
    height: 1.5,
    color: AtaColors.ink,
  );
  static const TextStyle bodyMuted = TextStyle(
    fontSize: 16,
    fontWeight: FontWeight.w400,
    height: bodyLineHeight,
    color: AtaColors.muted,
  );
  static const TextStyle label = TextStyle(
    fontSize: 14,
    fontWeight: FontWeight.w700,
    height: 1.4,
    color: AtaColors.ink,
  );
  static const TextStyle labelMuted = TextStyle(
    fontSize: 14,
    fontWeight: FontWeight.w600,
    height: 1.4,
    color: AtaColors.muted,
  );
  static const TextStyle small = TextStyle(
    fontSize: 14,
    fontWeight: FontWeight.w400,
    height: 1.6,
    color: AtaColors.muted,
  );
  static const TextStyle caption = TextStyle(
    fontSize: 12,
    fontWeight: FontWeight.w600,
    height: 1.5,
    color: AtaColors.muted,
  );
  static const TextStyle captionStrong = TextStyle(
    fontSize: 12,
    fontWeight: FontWeight.w700,
    height: 1.5,
    color: AtaColors.ink,
  );
  static const TextStyle eyebrow = TextStyle(
    fontSize: 14,
    fontWeight: FontWeight.w700,
    height: 1.4,
    color: AtaColors.brand,
  );
  static const TextStyle stat = TextStyle(
    fontSize: 30,
    fontWeight: FontWeight.w700,
    height: 1.2,
    color: AtaColors.ink,
  );
  static const TextStyle keypad = TextStyle(
    fontSize: 20,
    fontWeight: FontWeight.w700,
    color: AtaColors.ink,
  );
}
