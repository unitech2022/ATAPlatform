import 'package:equatable/equatable.dart';

/// `GET/PUT /driver/status`.
class DriverStatus extends Equatable {
  const DriverStatus({
    required this.isOnline,
    required this.canGoOnline,
    this.reason,
  });

  final bool isOnline;
  final bool canGoOnline;
  final String? reason;

  @override
  List<Object?> get props => <Object?>[isOnline, canGoOnline, reason];
}
