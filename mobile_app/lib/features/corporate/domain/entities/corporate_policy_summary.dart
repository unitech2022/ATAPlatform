import 'package:equatable/equatable.dart';

/// One `{ from: "07:00", to: "22:00" }` window of the policy (Riyadh time,
/// evaluated on the pickup time).
class TimeWindow extends Equatable {
  const TimeWindow({required this.from, required this.to});

  /// `HH:mm` as sent by the API.
  final String from;
  final String to;

  /// Minutes after midnight of an `HH:mm` value, `null` when malformed.
  static int? minutesOf(String hhmm) {
    final List<String> parts = hhmm.split(':');
    if (parts.length < 2) return null;
    final int? h = int.tryParse(parts[0]);
    final int? m = int.tryParse(parts[1]);
    return h == null || m == null ? null : h * 60 + m;
  }

  /// Whether [minuteOfDay] falls inside the window (inclusive start,
  /// exclusive end); a window crossing midnight wraps. A malformed window
  /// never blocks.
  bool contains(int minuteOfDay) {
    final int? start = minutesOf(from);
    final int? end = minutesOf(to);
    if (start == null || end == null) return true;
    if (start <= end) return minuteOfDay >= start && minuteOfDay < end;
    return minuteOfDay >= start || minuteOfDay < end;
  }

  @override
  List<Object?> get props => <Object?>[from, to];
}

/// `policy` of `GET /passenger/corporate`: `null` lists mean "no
/// restriction".
class CorporatePolicySummary extends Equatable {
  const CorporatePolicySummary({
    this.name = '',
    this.allowedRideCategoryCodes,
    this.timeWindows,
    this.allowedDays,
    this.maxFarePerTrip,
    this.requirePurpose = false,
    this.requireCostCenter = false,
    this.allowScheduled = true,
  });

  final String name;
  final List<String>? allowedRideCategoryCodes;
  final List<TimeWindow>? timeWindows;

  /// `0..6`, Sunday = 0 (the .NET `DayOfWeek` numbering of the API).
  final List<int>? allowedDays;
  final double? maxFarePerTrip;
  final bool requirePurpose;
  final bool requireCostCenter;
  final bool allowScheduled;

  bool allowsCategory(String? code) =>
      allowedRideCategoryCodes == null ||
      code == null ||
      allowedRideCategoryCodes!.contains(code);

  @override
  List<Object?> get props => <Object?>[
    name,
    allowedRideCategoryCodes,
    timeWindows,
    allowedDays,
    maxFarePerTrip,
    requirePurpose,
    requireCostCenter,
    allowScheduled,
  ];
}
