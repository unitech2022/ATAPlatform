import 'package:ata_app/features/corporate/domain/entities/corporate_budget.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/cost_center.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping for `GET /passenger/corporate` (`docs/12` §F19.4).
///
/// Tolerant: a `null` / non-object body (or one without `membership`) means
/// "not a member"; an empty restriction list means "no restriction".
abstract final class CorporateProfileModel {
  static CorporateProfile? fromBody(Object? body) {
    if (body is! Map<String, dynamic>) return null;
    final Map<String, dynamic>? membership = JsonReaders.object(
      body,
      'membership',
    );
    if (membership == null) return null;
    return CorporateProfile(
      membership: _membership(membership),
      policy: _policy(JsonReaders.object(body, 'policy')),
      budget: _budget(JsonReaders.object(body, 'budget')),
      costCenters: JsonReaders.objects(
        body,
        'costCenters',
      ).map(_costCenter).toList(growable: false),
    );
  }

  static CorporateMembership _membership(Map<String, dynamic> json) =>
      CorporateMembership(
        corporateUserId: JsonReaders.string(json, 'corporateUserId'),
        accountId: JsonReaders.string(json, 'accountId'),
        companyName: JsonReaders.string(json, 'companyName'),
        role: CorporateRole.parse(JsonReaders.optionalString(json, 'role')),
        employeeNumber: JsonReaders.optionalString(json, 'employeeNumber'),
        department: JsonReaders.optionalString(json, 'department'),
        status: CorporateMemberStatus.parse(
          JsonReaders.optionalString(json, 'status'),
        ),
      );

  static CorporatePolicySummary _policy(Map<String, dynamic>? json) {
    if (json == null) return const CorporatePolicySummary();
    return CorporatePolicySummary(
      name: JsonReaders.string(json, 'name'),
      allowedRideCategoryCodes: _strings(json['allowedRideCategoryCodes']),
      timeWindows: _windows(json['timeWindows']),
      allowedDays: _days(json['allowedDays']),
      maxFarePerTrip: JsonReaders.optionalNumber(json, 'maxFarePerTrip'),
      requirePurpose: json['requirePurpose'] == true,
      requireCostCenter: json['requireCostCenter'] == true,
      allowScheduled: json['allowScheduled'] != false,
    );
  }

  static CorporateBudget? _budget(Map<String, dynamic>? json) {
    if (json == null) return null;
    final double monthly = JsonReaders.number(json, 'monthly');
    final double spent = JsonReaders.number(json, 'spent');
    return CorporateBudget(
      monthly: monthly,
      spent: spent,
      remaining:
          JsonReaders.optionalNumber(json, 'remaining') ?? monthly - spent,
    );
  }

  static CostCenter _costCenter(Map<String, dynamic> json) => CostCenter(
    id: JsonReaders.string(json, 'id'),
    code: JsonReaders.string(json, 'code'),
    name: JsonReaders.string(json, 'name'),
  );

  static List<String>? _strings(Object? raw) {
    if (raw is! List<dynamic> || raw.isEmpty) return null;
    return raw.map((Object? e) => e.toString()).toList(growable: false);
  }

  static List<int>? _days(Object? raw) {
    if (raw is! List<dynamic> || raw.isEmpty) return null;
    return raw
        .whereType<num>()
        .map((num e) => e.toInt())
        .toList(growable: false);
  }

  static List<TimeWindow>? _windows(Object? raw) {
    if (raw is! List<dynamic> || raw.isEmpty) return null;
    final List<TimeWindow> windows = raw
        .whereType<Map<String, dynamic>>()
        .map(
          (Map<String, dynamic> w) => TimeWindow(
            from: JsonReaders.string(w, 'from'),
            to: JsonReaders.string(w, 'to'),
          ),
        )
        .toList(growable: false);
    return windows.isEmpty ? null : windows;
  }
}
