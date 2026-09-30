import 'package:equatable/equatable.dart';

/// A cost center the employee may charge a trip to.
class CostCenter extends Equatable {
  const CostCenter({required this.id, required this.code, required this.name});

  final String id;
  final String code;
  final String name;

  @override
  List<Object?> get props => <Object?>[id, code, name];
}
