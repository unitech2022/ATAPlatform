import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/map_canvas.dart';
import 'package:flutter/material.dart';

/// Map with the floating locate / zoom buttons in the bottom corner.
class MapSection extends StatelessWidget {
  const MapSection({super.key, required this.etaLabel});

  final String etaLabel;

  static const double _buttonSize = 48;
  static const double _bottomInset = 260;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: <Widget>[
        Positioned.fill(child: MapCanvas(etaLabel: etaLabel)),
        Positioned(
          left: AtaSpacing.gutter,
          bottom: _bottomInset,
          child: Column(
            children: <Widget>[
              _RoundButton(icon: AtaIcons.location, onTap: () {}),
              const SizedBox(height: AtaSpacing.sm),
              _RoundButton(icon: AtaIcons.plus, onTap: () {}),
            ],
          ),
        ),
      ],
    );
  }
}

class _RoundButton extends StatelessWidget {
  const _RoundButton({required this.icon, required this.onTap});

  final AtaIcons icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: MapSection._buttonSize,
      height: MapSection._buttonSize,
      decoration: BoxDecoration(
        color: AtaColors.white,
        shape: BoxShape.circle,
        boxShadow: AtaShadows.float,
      ),
      child: Material(
        type: MaterialType.transparency,
        shape: const CircleBorder(),
        child: InkWell(
          onTap: onTap,
          customBorder: const CircleBorder(),
          child: Center(child: AtaIcon(icon, color: AtaColors.ink)),
        ),
      ),
    );
  }
}
