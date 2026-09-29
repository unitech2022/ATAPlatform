import 'package:equatable/equatable.dart';

/// Tabs of `/driver/incentives` (`?status=`).
enum IncentiveTab {
  active('active'),
  upcoming('upcoming'),
  completed('completed');

  const IncentiveTab(this.apiValue);

  final String apiValue;
}

/// `driver_incentive_progress.status`.
enum IncentiveProgressStatus {
  inProgress('in_progress'),
  achieved('achieved'),
  paid('paid'),
  expired('expired'),
  voided('voided');

  const IncentiveProgressStatus(this.apiValue);

  final String apiValue;

  static IncentiveProgressStatus parse(String? value) => values.firstWhere(
    (IncentiveProgressStatus s) => s.apiValue == value,
    orElse: () => inProgress,
  );
}

/// Days (0 = Sunday … 6 = Saturday) and local hours a quest counts in.
class IncentiveWindow extends Equatable {
  const IncentiveWindow({this.daysOfWeek = const <int>[], this.from, this.to});

  final List<int> daysOfWeek;

  /// `HH:mm` (Riyadh time).
  final String? from;
  final String? to;

  @override
  List<Object?> get props => <Object?>[daysOfWeek, from, to];
}

/// A zone the pickup must be in.
class IncentiveZone extends Equatable {
  const IncentiveZone({required this.id, this.name = ''});

  final String id;
  final String name;

  @override
  List<Object?> get props => <Object?>[id, name];
}

/// The driver's progress in the current period.
class IncentiveProgress extends Equatable {
  const IncentiveProgress({
    this.completedTrips = 0,
    this.status = IncentiveProgressStatus.inProgress,
    this.rewardAmount,
    this.paidAt,
  });

  final int completedTrips;
  final IncentiveProgressStatus status;

  /// Reward actually paid (after the reliability multiplier).
  final double? rewardAmount;
  final DateTime? paidAt;

  @override
  List<Object?> get props => <Object?>[
    completedTrips,
    status,
    rewardAmount,
    paidAt,
  ];
}

/// A driver quest (`GET /driver/incentives`, `docs/10` §F15.10).
class Incentive extends Equatable {
  const Incentive({
    required this.id,
    required this.name,
    this.description = '',
    this.type = '',
    this.targetTrips = 0,
    this.rewardAmount = 0,
    this.periodStart,
    this.periodEnd,
    this.window,
    this.zones,
    this.rideCategoryCodes,
    this.requiresOptIn = false,
    this.optedIn = false,
    this.progress,
  });

  final String id;
  final String name;
  final String description;

  /// `daily`, `weekly`, `zone_quest` or `one_time`.
  final String type;
  final int targetTrips;
  final double rewardAmount;
  final DateTime? periodStart;
  final DateTime? periodEnd;
  final IncentiveWindow? window;
  final List<IncentiveZone>? zones;
  final List<String>? rideCategoryCodes;
  final bool requiresOptIn;
  final bool optedIn;
  final IncentiveProgress? progress;

  int get completedTrips => progress?.completedTrips ?? 0;

  /// 0..1 of [targetTrips].
  double get progressValue => targetTrips <= 0
      ? 0
      : (completedTrips / targetTrips).clamp(0, 1).toDouble();

  int get tripsLeft {
    final int left = targetTrips - completedTrips;
    return left < 0 ? 0 : left;
  }

  IncentiveProgressStatus get status =>
      progress?.status ?? IncentiveProgressStatus.inProgress;
  bool get isAchieved =>
      status == IncentiveProgressStatus.achieved ||
      status == IncentiveProgressStatus.paid;

  /// Must join before trips count.
  bool get needsOptIn => requiresOptIn && !optedIn;

  /// Expected payout after the reliability [multiplier] (`docs/09`).
  double rewardWith(double multiplier) =>
      (rewardAmount * multiplier * 100).roundToDouble() / 100;

  Incentive joined() => Incentive(
    id: id,
    name: name,
    description: description,
    type: type,
    targetTrips: targetTrips,
    rewardAmount: rewardAmount,
    periodStart: periodStart,
    periodEnd: periodEnd,
    window: window,
    zones: zones,
    rideCategoryCodes: rideCategoryCodes,
    requiresOptIn: requiresOptIn,
    optedIn: true,
    progress: progress,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    name,
    description,
    type,
    targetTrips,
    rewardAmount,
    periodStart,
    periodEnd,
    window,
    zones,
    rideCategoryCodes,
    requiresOptIn,
    optedIn,
    progress,
  ];
}
