import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Builds the app [ThemeData] from the design tokens.
abstract final class AtaTheme {
  static ThemeData light() {
    const ColorScheme scheme = ColorScheme(
      brightness: Brightness.light,
      primary: AtaColors.ink,
      onPrimary: AtaColors.white,
      secondary: AtaColors.brand,
      onSecondary: AtaColors.white,
      error: AtaColors.danger,
      onError: AtaColors.white,
      surface: AtaColors.white,
      onSurface: AtaColors.ink,
      surfaceContainerHighest: AtaColors.cloud,
      outline: AtaColors.line,
    );
    final TextTheme textTheme = const TextTheme(
      displaySmall: AtaText.display,
      headlineMedium: AtaText.title,
      headlineSmall: AtaText.headline,
      titleMedium: AtaText.section,
      bodyLarge: AtaText.body,
      bodyMedium: AtaText.body,
      bodySmall: AtaText.caption,
      labelLarge: AtaText.label,
      labelMedium: AtaText.labelMuted,
      labelSmall: AtaText.caption,
    ).apply(fontFamily: AtaText.fontFamily);

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      fontFamily: AtaText.fontFamily,
      scaffoldBackgroundColor: AtaColors.canvas,
      canvasColor: AtaColors.canvas,
      textTheme: textTheme,
      dividerTheme: const DividerThemeData(
        color: AtaColors.line,
        thickness: 1,
        space: 1,
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: AtaColors.white,
        shape: RoundedRectangleBorder(borderRadius: AtaRadii.sheetTopRadius),
        showDragHandle: false,
      ),
      dialogTheme: const DialogThemeData(
        backgroundColor: AtaColors.white,
        shape: RoundedRectangleBorder(borderRadius: AtaRadii.cardRadius),
      ),
      inputDecorationTheme: const InputDecorationTheme(
        filled: true,
        fillColor: AtaColors.cloud,
        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 18),
        border: OutlineInputBorder(
          borderRadius: AtaRadii.itemRadius,
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: AtaRadii.itemRadius,
          borderSide: BorderSide(color: AtaColors.brand, width: 2),
        ),
        hintStyle: AtaText.bodyMuted,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: AtaColors.ink,
        contentTextStyle: AtaText.label.copyWith(color: AtaColors.white),
        behavior: SnackBarBehavior.floating,
        shape: const RoundedRectangleBorder(borderRadius: AtaRadii.itemRadius),
      ),
    );
  }
}
