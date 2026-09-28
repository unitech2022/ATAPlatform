import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_text.dart';
import 'package:flutter/material.dart';

/// Badge coloured by a case / lost item status.
class StatusBadge extends StatelessWidget {
  const StatusBadge({super.key, required this.status, required this.label});

  final String status;
  final String label;

  @override
  Widget build(BuildContext context) {
    final (Color background, Color foreground) = SafetyText.statusColors(
      status,
    );
    return AtaBadge(
      label: label,
      background: background,
      foreground: foreground,
    );
  }
}
