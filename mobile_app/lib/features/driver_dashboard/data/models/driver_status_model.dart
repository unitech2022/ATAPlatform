import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';

/// JSON mapping for [DriverStatus].
class DriverStatusModel extends DriverStatus {
  const DriverStatusModel({
    required super.isOnline,
    required super.canGoOnline,
    super.reason,
  });

  factory DriverStatusModel.fromJson(Map<String, dynamic> json) =>
      DriverStatusModel(
        isOnline: json['isOnline'] as bool? ?? false,
        canGoOnline: json['canGoOnline'] as bool? ?? false,
        reason: json['reason'] as String?,
      );
}
