import 'package:equatable/equatable.dart';

/// Demand tiers of `demand_levels` (`code` column).
enum DemandCode {
  normal('normal'),
  moderate('moderate'),
  high('high'),
  veryHigh('very_high');

  const DemandCode(this.apiValue);

  final String apiValue;

  static DemandCode fromApi(String value) => DemandCode.values.firstWhere(
    (DemandCode code) => code.apiValue == value,
    orElse: () => DemandCode.normal,
  );

  /// Anything above normal is worth a badge on the home sheet.
  bool get isElevated => this != DemandCode.normal;
}

/// Current demand level at a location (`GET /pricing/demand`).
class DemandLevel extends Equatable {
  const DemandLevel({
    required this.code,
    required this.name,
    required this.multiplier,
    this.color = '',
  });

  static const DemandLevel normal = DemandLevel(
    code: DemandCode.normal,
    name: '',
    multiplier: 1,
  );

  final DemandCode code;
  final String name;
  final double multiplier;

  /// Hex colour configured by the admin (informational; the app maps
  /// [code] to its own palette tokens).
  final String color;

  @override
  List<Object?> get props => <Object?>[code, name, multiplier, color];
}
