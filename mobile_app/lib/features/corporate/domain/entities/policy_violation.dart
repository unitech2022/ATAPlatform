import 'package:equatable/equatable.dart';

/// The rule a trip breaks (`docs/12` §F19.2, `CorporatePolicyEvaluator`).
/// [budget] and [creditLimit] come from their own error codes.
enum PolicyRule {
  category('category'),
  day('day'),
  timeWindow('time_window'),
  zone('zone'),
  maxFare('max_fare'),
  scheduled('scheduled'),
  purposeRequired('purpose_required'),
  costCenterRequired('cost_center_required'),
  budget('budget'),
  creditLimit('credit_limit'),
  unknown('');

  const PolicyRule(this.apiValue);

  final String apiValue;

  static PolicyRule parse(String? value) => PolicyRule.values.firstWhere(
    (PolicyRule r) => r.apiValue.isNotEmpty && r.apiValue == value,
    orElse: () => PolicyRule.unknown,
  );
}

/// One entry of `violations: [ { rule, limit?, allowed? } ]`.
class PolicyViolation extends Equatable {
  const PolicyViolation({
    required this.rule,
    this.limit,
    this.allowed = const <String>[],
  });

  final PolicyRule rule;

  /// Numeric limit (`max_fare`) or remaining amount (`budget`).
  final double? limit;

  /// Allowed options of the rule (category codes, days `0..6`, zone names).
  final List<String> allowed;

  /// Reads one violation from JSON: `{ rule, limit?, allowed? }` or a bare
  /// rule string. Anything else is an `unknown` violation.
  factory PolicyViolation.fromJson(Object? json) {
    if (json is String) return PolicyViolation(rule: PolicyRule.parse(json));
    if (json is! Map<String, dynamic>) {
      return const PolicyViolation(rule: PolicyRule.unknown);
    }
    final Object? allowed = json['allowed'];
    final Object? limit = json['limit'];
    return PolicyViolation(
      rule: PolicyRule.parse(json['rule']?.toString()),
      limit: limit is num ? limit.toDouble() : null,
      allowed: allowed is List<dynamic>
          ? allowed.map(_optionText).toList(growable: false)
          : const <String>[],
    );
  }

  /// `{ from, to }` windows become `from-to`; everything else its text.
  static String _optionText(Object? option) {
    if (option is Map<String, dynamic> &&
        option['from'] != null &&
        option['to'] != null) {
      return '${option['from']}-${option['to']}';
    }
    return option.toString();
  }

  /// Reads `violations` (a list) tolerantly; anything else is empty.
  static List<PolicyViolation> listFrom(Object? raw) => raw is List<dynamic>
      ? raw.map(PolicyViolation.fromJson).toList(growable: false)
      : const <PolicyViolation>[];

  @override
  List<Object?> get props => <Object?>[rule, limit, allowed];
}
