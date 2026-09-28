import 'package:ata_app/design/painting/map_painter.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// The Step-1 map: a painted abstract city with pickup pin, car marker and
/// an ETA chip placed along the route.
class MapCanvas extends StatelessWidget {
  const MapCanvas({super.key, required this.etaLabel});

  /// Text shown in the dark chip near the car (for example "دقيقتان").
  final String etaLabel;

  static const Offset _etaOffset = Offset(-40, 120);

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        final Size size = constraints.biggest;
        final Offset pin = MapPainter.project(MapPainter.routeStart, size);
        final Offset car = MapPainter.project(MapPainter.routeEnd, size);
        final Offset eta = car + _etaOffset;
        return Directionality(
          textDirection: TextDirection.ltr,
          child: Stack(
            fit: StackFit.expand,
            children: <Widget>[
              const CustomPaint(painter: MapPainter()),
              _Marker(
                center: pin,
                size: AtaSizes.mapMarkerSmall,
                color: AtaColors.ink,
                icon: AtaIcons.pin,
                iconSize: AtaSizes.iconMedium,
              ),
              _Marker(
                center: car,
                size: AtaSizes.mapMarkerLarge,
                color: AtaColors.brand,
                icon: AtaIcons.car,
                iconSize: AtaSizes.iconLarge,
              ),
              Positioned(
                left: eta.dx,
                top: eta.dy,
                child: FractionalTranslation(
                  translation: const Offset(-0.5, -0.5),
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AtaSpacing.sm,
                      vertical: AtaSpacing.xs,
                    ),
                    decoration: BoxDecoration(
                      color: AtaColors.ink,
                      borderRadius: AtaRadii.pillRadius,
                      boxShadow: AtaShadows.float,
                    ),
                    child: Text(
                      etaLabel,
                      style: AtaText.captionStrong.copyWith(
                        color: AtaColors.white,
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _Marker extends StatelessWidget {
  const _Marker({
    required this.center,
    required this.size,
    required this.color,
    required this.icon,
    required this.iconSize,
  });

  final Offset center;
  final double size;
  final Color color;
  final AtaIcons icon;
  final double iconSize;

  @override
  Widget build(BuildContext context) {
    return Positioned(
      left: center.dx - size / 2,
      top: center.dy - size / 2,
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          color: color,
          shape: BoxShape.circle,
          border: Border.all(
            color: AtaColors.white,
            width: AtaSizes.mapMarkerBorder,
          ),
          boxShadow: AtaShadows.float,
        ),
        alignment: Alignment.center,
        child: AtaIcon(icon, size: iconSize, color: AtaColors.white),
      ),
    );
  }
}
