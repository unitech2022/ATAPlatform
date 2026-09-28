import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';

/// JSON mapping for [DriverSummary].
class DriverSummaryModel extends DriverSummary {
  const DriverSummaryModel({
    required super.applicationNumber,
    required super.applicationStatus,
    super.isOnline,
  });

  factory DriverSummaryModel.fromJson(Map<String, dynamic> json) =>
      DriverSummaryModel(
        applicationNumber: json['applicationNumber'] as String? ?? '',
        applicationStatus: DriverApplicationStatus.parse(
          json['applicationStatus'] as String?,
        ),
        isOnline: json['isOnline'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'applicationNumber': applicationNumber,
    'applicationStatus': applicationStatus.apiValue,
    'isOnline': isOnline,
  };
}
