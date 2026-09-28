import 'package:flutter/material.dart';

/// Corner radius tokens.
abstract final class AtaRadii {
  static const double card = 24;
  static const double item = 16;
  static const double small = 12;
  static const double tiny = 8;
  static const double sheet = 32;
  static const double pill = 999;

  static const BorderRadius cardRadius = BorderRadius.all(
    Radius.circular(card),
  );
  static const BorderRadius itemRadius = BorderRadius.all(
    Radius.circular(item),
  );
  static const BorderRadius smallRadius = BorderRadius.all(
    Radius.circular(small),
  );
  static const BorderRadius tinyRadius = BorderRadius.all(
    Radius.circular(tiny),
  );
  static const BorderRadius pillRadius = BorderRadius.all(
    Radius.circular(pill),
  );
  static const BorderRadius sheetTopRadius = BorderRadius.vertical(
    top: Radius.circular(sheet),
  );
}
