import 'package:equatable/equatable.dart';

/// `GET /passenger/scheduling/rules` (`docs/11` §F17.4): the booking window
/// and the cancellation terms of scheduled rides.
class SchedulingRules extends Equatable {
  const SchedulingRules({
    this.maxDaysAhead = 7,
    this.minLeadMinutes = 30,
    this.minScheduledAt,
    this.maxScheduledAt,
    this.freeCancelMinutesBefore = 60,
    this.lateCancelFee,
    this.reminderOffsets = const <int>[1440, 60, 15],
  });

  /// Seeded defaults, used until (or when) the rules cannot be loaded.
  static const SchedulingRules fallback = SchedulingRules();

  final int maxDaysAhead;
  final int minLeadMinutes;

  /// Bounds as computed by the API when the rules were read.
  final DateTime? minScheduledAt;
  final DateTime? maxScheduledAt;
  final int freeCancelMinutesBefore;

  /// Fee of a late cancellation; `null` = none.
  final double? lateCancelFee;

  /// Rider reminder offsets, in minutes before the pickup.
  final List<int> reminderOffsets;

  /// Earliest bookable time from [now] (`now + min_lead_minutes`).
  DateTime minAt(DateTime now) => now.add(Duration(minutes: minLeadMinutes));

  /// Latest bookable time from [now]: `now + max_days_ahead × 24 h`, measured
  /// from the booking moment.
  DateTime maxAt(DateTime now) => now.add(Duration(days: maxDaysAhead));

  /// End of the free cancellation window of a trip at [scheduledAt].
  DateTime freeCancelUntil(DateTime scheduledAt) =>
      scheduledAt.subtract(Duration(minutes: freeCancelMinutesBefore));

  @override
  List<Object?> get props => <Object?>[
    maxDaysAhead,
    minLeadMinutes,
    minScheduledAt,
    maxScheduledAt,
    freeCancelMinutesBefore,
    lateCancelFee,
    reminderOffsets,
  ];
}
