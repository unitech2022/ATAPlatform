import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:equatable/equatable.dart';

/// Why a picked time cannot be booked.
enum ScheduleIssue {
  /// Earlier than `now + min_lead_minutes`.
  tooSoon,

  /// Later than `now + max_days_ahead × 24 h`.
  tooFar,
}

/// State of `ScheduleTimeCubit`: the booking window and the picked time.
class ScheduleTimeState extends Equatable {
  const ScheduleTimeState({
    required this.now,
    this.rules = SchedulingRules.fallback,
    this.rulesLoaded = false,
    this.loadingRules = false,
    this.rulesFailure,
    this.day,
    this.hour,
    this.minute,
    this.confirmed,
  });

  /// Time step of the picker, in minutes.
  static const int minuteStep = 5;

  /// The clock at the last `open` / `recheck` (bounds are measured from it).
  final DateTime now;
  final SchedulingRules rules;
  final bool rulesLoaded;
  final bool loadingRules;
  final Failure? rulesFailure;

  /// Picked calendar day (local), hour and minute; `null` until chosen.
  final DateTime? day;
  final int? hour;
  final int? minute;

  /// The time the rider confirmed; `null` = not scheduled.
  final DateTime? confirmed;

  DateTime get minAt => rules.minAt(now);
  DateTime get maxAt => rules.maxAt(now);

  /// The time currently picked (not yet confirmed).
  DateTime? get draft {
    final DateTime? d = day;
    if (d == null || hour == null || minute == null) return null;
    return DateTime(d.year, d.month, d.day, hour!, minute!);
  }

  ScheduleIssue? issueFor(DateTime value) {
    if (value.isBefore(minAt)) return ScheduleIssue.tooSoon;
    if (value.isAfter(maxAt)) return ScheduleIssue.tooFar;
    return null;
  }

  ScheduleIssue? get draftIssue {
    final DateTime? value = draft;
    return value == null ? null : issueFor(value);
  }

  bool get canConfirm => draft != null && draftIssue == null;
  bool get hasConfirmed => confirmed != null;

  /// The confirmed time no longer respects the window (time passed).
  ScheduleIssue? get confirmedIssue {
    final DateTime? value = confirmed;
    return value == null ? null : issueFor(value);
  }

  /// Bookable calendar days, from the earliest to the latest possible time.
  List<DateTime> get days {
    final DateTime first = _midnight(minAt);
    final DateTime last = _midnight(maxAt);
    final List<DateTime> out = <DateTime>[];
    for (
      DateTime d = first;
      !d.isAfter(last);
      d = DateTime(d.year, d.month, d.day + 1)
    ) {
      out.add(d);
    }
    return out;
  }

  /// An option of the picked day is inside the window.
  bool isAvailable(DateTime day, {required int hour, int minute = 0}) {
    final DateTime value = DateTime(day.year, day.month, day.day, hour, minute);
    return issueFor(value) == null;
  }

  /// Any minute of [hour] on [day] is inside the window.
  bool isHourAvailable(DateTime day, int hour) => <int>[
    for (int m = 0; m < 60; m += minuteStep) m,
  ].any((int m) => isAvailable(day, hour: hour, minute: m));

  static DateTime _midnight(DateTime v) => DateTime(v.year, v.month, v.day);

  ScheduleTimeState copyWith({
    DateTime? now,
    SchedulingRules? rules,
    bool? rulesLoaded,
    bool? loadingRules,
    Failure? rulesFailure,
    DateTime? day,
    int? hour,
    int? minute,
    DateTime? confirmed,
    bool clearRulesFailure = false,
    bool clearConfirmed = false,
    bool clearDraft = false,
  }) => ScheduleTimeState(
    now: now ?? this.now,
    rules: rules ?? this.rules,
    rulesLoaded: rulesLoaded ?? this.rulesLoaded,
    loadingRules: loadingRules ?? this.loadingRules,
    rulesFailure: clearRulesFailure ? null : rulesFailure ?? this.rulesFailure,
    day: clearDraft ? null : day ?? this.day,
    hour: clearDraft ? null : hour ?? this.hour,
    minute: clearDraft ? null : minute ?? this.minute,
    confirmed: clearConfirmed ? null : confirmed ?? this.confirmed,
  );

  @override
  List<Object?> get props => <Object?>[
    now,
    rules,
    rulesLoaded,
    loadingRules,
    rulesFailure,
    day,
    hour,
    minute,
    confirmed,
  ];
}
