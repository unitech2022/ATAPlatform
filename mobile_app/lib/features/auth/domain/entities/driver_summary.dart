import 'package:equatable/equatable.dart';

/// Lifecycle of a driver application.
enum DriverApplicationStatus {
  draft('draft'),
  submitted('submitted'),
  underReview('under_review'),
  approved('approved'),
  rejected('rejected'),
  suspended('suspended'),
  unknown('unknown');

  const DriverApplicationStatus(this.apiValue);

  final String apiValue;

  bool get isApproved => this == approved;

  static DriverApplicationStatus parse(String? value) {
    for (final DriverApplicationStatus status in values) {
      if (status.apiValue == value) return status;
    }
    return unknown;
  }
}

/// The `driver` object returned with an auth response.
class DriverSummary extends Equatable {
  const DriverSummary({
    required this.applicationNumber,
    required this.applicationStatus,
    this.isOnline = false,
  });

  final String applicationNumber;
  final DriverApplicationStatus applicationStatus;
  final bool isOnline;

  DriverSummary copyWith({
    DriverApplicationStatus? applicationStatus,
    bool? isOnline,
  }) => DriverSummary(
    applicationNumber: applicationNumber,
    applicationStatus: applicationStatus ?? this.applicationStatus,
    isOnline: isOnline ?? this.isOnline,
  );

  @override
  List<Object?> get props => <Object?>[
    applicationNumber,
    applicationStatus,
    isOnline,
  ];
}
