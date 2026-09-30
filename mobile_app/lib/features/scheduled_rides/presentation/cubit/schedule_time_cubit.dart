import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Date / time picker of the request sheet (`docs/11` §F17.3): the time must
/// fall between `now + min_lead_minutes` and `now + max_days_ahead × 24 h`
/// (both from the scheduling rules, measured from the booking moment, ends
/// included). Replaces the old local "schedule" flag of `HomeCubit`.
class ScheduleTimeCubit extends Cubit<ScheduleTimeState> {
  ScheduleTimeCubit({required this._getRules, DateTime Function()? now})
    : _clock = now ?? DateTime.now,
      super(ScheduleTimeState(now: (now ?? DateTime.now)()));

  final GetSchedulingRules _getRules;
  final DateTime Function() _clock;

  String? _rulesCategoryId;

  /// Opens the picker: refreshes the clock, loads the rules (for
  /// [rideCategoryId]) and starts from the confirmed time, or from the
  /// earliest bookable slot.
  void open({String? rideCategoryId}) {
    final DateTime now = _clock();
    emit(state.copyWith(now: now));
    final DateTime? start = state.confirmed;
    _pick(
      start != null && state.issueFor(start) == null
          ? start
          : _roundUp(state.minAt),
    );
    if (!state.rulesLoaded || rideCategoryId != _rulesCategoryId) {
      loadRules(rideCategoryId: rideCategoryId);
    }
  }

  Future<void> loadRules({String? rideCategoryId}) async {
    _rulesCategoryId = rideCategoryId;
    emit(state.copyWith(loadingRules: true, clearRulesFailure: true));
    final result = await _getRules(rideCategoryId);
    if (isClosed) return;
    result.fold(
      (Failure failure) =>
          emit(state.copyWith(loadingRules: false, rulesFailure: failure)),
      (SchedulingRules rules) {
        emit(
          state.copyWith(
            rules: rules,
            rulesLoaded: true,
            loadingRules: false,
            now: _clock(),
          ),
        );
        // The window may have moved: keep the picked time inside it.
        final DateTime? value = state.draft;
        if (value != null) _pick(_clamp(value));
      },
    );
  }

  /// Picks [day]; when the current time of day is outside the window on
  /// that day it snaps to the nearest bookable slot.
  void selectDay(DateTime day) {
    final DateTime? current = state.draft;
    final DateTime next = DateTime(
      day.year,
      day.month,
      day.day,
      current?.hour ?? state.hour ?? _roundUp(state.minAt).hour,
      current?.minute ?? state.minute ?? _roundUp(state.minAt).minute,
    );
    _pick(_clamp(next));
  }

  void selectHour(int hour) {
    final DateTime day = state.day ?? _roundUp(state.minAt);
    emit(
      state.copyWith(
        day: DateTime(day.year, day.month, day.day),
        hour: hour,
        minute: state.minute ?? 0,
      ),
    );
  }

  void selectMinute(int minute) {
    final DateTime day = state.day ?? _roundUp(state.minAt);
    emit(
      state.copyWith(
        day: DateTime(day.year, day.month, day.day),
        hour: state.hour ?? day.hour,
        minute: minute,
      ),
    );
  }

  /// Sets an exact date and time (no snapping); the issue, if any, shows in
  /// [ScheduleTimeState.draftIssue].
  void setDateTime(DateTime value) => emit(
    state.copyWith(
      day: DateTime(value.year, value.month, value.day),
      hour: value.hour,
      minute: value.minute,
    ),
  );

  /// Confirms the picked time; `false` when it is outside the window.
  bool confirm() {
    final DateTime? value = state.draft;
    if (value == null || state.issueFor(value) != null) return false;
    emit(state.copyWith(confirmed: value));
    return true;
  }

  /// Re-checks the confirmed time against the clock right now (call before
  /// booking, the window moves with time). `true` = still bookable.
  bool recheck() {
    emit(state.copyWith(now: _clock()));
    return state.confirmed != null && state.confirmedIssue == null;
  }

  /// Back to "now": forgets the picked and confirmed time.
  void clear() => emit(state.copyWith(clearConfirmed: true, clearDraft: true));

  void _pick(DateTime value) => emit(
    state.copyWith(
      day: DateTime(value.year, value.month, value.day),
      hour: value.hour,
      minute: value.minute,
    ),
  );

  /// The nearest bookable slot to [value].
  DateTime _clamp(DateTime value) {
    final DateTime min = _roundUp(state.minAt);
    final DateTime max = _roundDown(state.maxAt);
    if (value.isBefore(min)) return min;
    if (value.isAfter(max)) return max;
    return value;
  }

  static DateTime _roundUp(DateTime v) {
    const int step = ScheduleTimeState.minuteStep;
    final DateTime floor = DateTime(v.year, v.month, v.day, v.hour, v.minute);
    final int extra = (step - v.minute % step) % step;
    final bool exact = extra == 0 && v.second == 0 && v.millisecond == 0;
    return exact
        ? floor
        : floor.add(Duration(minutes: extra == 0 ? step : extra));
  }

  static DateTime _roundDown(DateTime v) {
    const int step = ScheduleTimeState.minuteStep;
    return DateTime(v.year, v.month, v.day, v.hour, v.minute - v.minute % step);
  }
}
