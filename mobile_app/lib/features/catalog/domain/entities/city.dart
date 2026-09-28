import 'package:equatable/equatable.dart';

/// A served city.
class City extends Equatable {
  const City({required this.id, required this.code, required this.name});

  final String id;
  final String code;
  final String name;

  @override
  List<Object?> get props => <Object?>[id, code, name];
}
