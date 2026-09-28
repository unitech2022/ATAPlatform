import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// Visual variants of [AtaButton].
enum AtaButtonVariant {
  /// Dark `ink` background, white text, button shadow.
  primary,

  /// `brand` background, white text.
  brand,

  /// White background with `line` border.
  outline,

  /// `cloud` background, ink text.
  soft,

  /// White background, ink text (used on dark cards).
  white,

  /// `danger` background, white text.
  danger,

  /// `danger-soft` background, danger text.
  dangerSoft,

  /// `danger` border, danger text.
  dangerOutline,
}

/// The design-system button. Passing `null` to [onPressed] renders the
/// disabled state (`line` background).
class AtaButton extends StatelessWidget {
  const AtaButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.variant = AtaButtonVariant.primary,
    this.icon,
    this.trailingIcon,
    this.height = AtaSizes.button,
    this.expanded = true,
    this.loading = false,
  });

  final String label;
  final VoidCallback? onPressed;
  final AtaButtonVariant variant;
  final AtaIcons? icon;
  final AtaIcons? trailingIcon;
  final double height;
  final bool expanded;
  final bool loading;

  static const double _loaderSize = 20;
  static const double _loaderStroke = 2.5;

  bool get _enabled => onPressed != null && !loading;

  @override
  Widget build(BuildContext context) {
    final _ButtonColors colors = _colors();
    final Color foreground = _enabled ? colors.foreground : AtaColors.white;
    final Widget content = loading
        ? SizedBox(
            width: _loaderSize,
            height: _loaderSize,
            child: CircularProgressIndicator(
              strokeWidth: _loaderStroke,
              color: colors.foreground,
            ),
          )
        : Row(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: <Widget>[
              if (icon != null) ...<Widget>[
                AtaIcon(icon!, color: foreground),
                const SizedBox(width: AtaSpacing.sm),
              ],
              Flexible(
                child: Text(
                  label,
                  textAlign: TextAlign.center,
                  overflow: TextOverflow.ellipsis,
                  style: AtaText.bodyStrong.copyWith(color: foreground),
                ),
              ),
              if (trailingIcon != null) ...<Widget>[
                const SizedBox(width: AtaSpacing.sm),
                AtaIcon(trailingIcon!, color: foreground, mirrored: true),
              ],
            ],
          );

    return Semantics(
      button: true,
      enabled: _enabled,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        height: height,
        width: expanded ? double.infinity : null,
        decoration: BoxDecoration(
          color: _enabled ? colors.background : AtaColors.line,
          borderRadius: AtaRadii.itemRadius,
          border: _enabled && colors.border != null
              ? Border.all(color: colors.border!, width: AtaSizes.borderThin)
              : null,
          boxShadow: _enabled ? colors.shadow : null,
        ),
        child: Material(
          type: MaterialType.transparency,
          child: InkWell(
            onTap: _enabled ? onPressed : null,
            borderRadius: AtaRadii.itemRadius,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.lg),
              child: Center(child: content),
            ),
          ),
        ),
      ),
    );
  }

  _ButtonColors _colors() => switch (variant) {
    AtaButtonVariant.primary => _ButtonColors(
      background: AtaColors.ink,
      foreground: AtaColors.white,
      shadow: AtaShadows.button,
    ),
    AtaButtonVariant.brand => const _ButtonColors(
      background: AtaColors.brand,
      foreground: AtaColors.white,
    ),
    AtaButtonVariant.outline => const _ButtonColors(
      background: AtaColors.white,
      foreground: AtaColors.ink,
      border: AtaColors.line,
    ),
    AtaButtonVariant.soft => const _ButtonColors(
      background: AtaColors.cloud,
      foreground: AtaColors.ink,
    ),
    AtaButtonVariant.white => const _ButtonColors(
      background: AtaColors.white,
      foreground: AtaColors.ink,
    ),
    AtaButtonVariant.danger => const _ButtonColors(
      background: AtaColors.danger,
      foreground: AtaColors.white,
    ),
    AtaButtonVariant.dangerSoft => const _ButtonColors(
      background: AtaColors.dangerSoft,
      foreground: AtaColors.danger,
    ),
    AtaButtonVariant.dangerOutline => const _ButtonColors(
      background: AtaColors.white,
      foreground: AtaColors.danger,
      border: AtaColors.danger,
    ),
  };
}

class _ButtonColors {
  const _ButtonColors({
    required this.background,
    required this.foreground,
    this.border,
    this.shadow,
  });

  final Color background;
  final Color foreground;
  final Color? border;
  final List<BoxShadow>? shadow;
}
