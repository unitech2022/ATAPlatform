import 'package:equatable/equatable.dart';

/// `reliability_profiles.restriction_level` (F14).
enum RestrictionLevel {
  none('none'),
  warning('warning'),
  matchingDeprioritized('matching_deprioritized'),
  incentivesReduced('incentives_reduced'),
  temporarilyRestricted('temporarily_restricted'),
  suspended('suspended');

  const RestrictionLevel(this.apiValue);

  final String apiValue;

  static RestrictionLevel parse(String? value) {
    for (final RestrictionLevel level in values) {
      if (level.apiValue == value) return level;
    }
    return none;
  }

  /// Blocks requesting trips / going online (`403 account_restricted`).
  bool get isRestricted => this == temporarilyRestricted || this == suspended;

  /// Drivers get fewer offers (matching score × factor).
  bool get isDeprioritized =>
      this == matchingDeprioritized || this == incentivesReduced;
}

/// `details` of `403 account_restricted`.
class AccountRestriction extends Equatable {
  const AccountRestriction({required this.level, this.restrictedUntil});

  final RestrictionLevel level;

  /// `null` for `suspended` (no end until operations lift it).
  final DateTime? restrictedUntil;

  @override
  List<Object?> get props => <Object?>[level, restrictedUntil];
}
