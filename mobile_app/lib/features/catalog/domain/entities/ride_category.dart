import 'package:equatable/equatable.dart';

/// Static Step-1 estimate attached to a category.
class RideEstimate extends Equatable {
  const RideEstimate({required this.etaMinutes, required this.price});

  final int etaMinutes;
  final double price;

  @override
  List<Object?> get props => <Object?>[etaMinutes, price];
}

/// A ride category such as economy / family / premium.
class RideCategory extends Equatable {
  const RideCategory({
    required this.id,
    required this.code,
    required this.name,
    required this.description,
    required this.icon,
    required this.seats,
    required this.maxStops,
    required this.sortOrder,
    this.estimate,
  });

  final String id;
  final String code;
  final String name;
  final String description;
  final String icon;
  final int seats;
  final int maxStops;
  final int sortOrder;
  final RideEstimate? estimate;

  @override
  List<Object?> get props => <Object?>[
    id,
    code,
    name,
    description,
    icon,
    seats,
    maxStops,
    sortOrder,
    estimate,
  ];
}
