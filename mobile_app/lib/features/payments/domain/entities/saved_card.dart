import 'package:equatable/equatable.dart';

/// A tokenised card of the passenger (`PaymentMethod` of `docs/08` §F11.5).
/// The app never sees the card number, only brand and last four digits.
class SavedCard extends Equatable {
  const SavedCard({
    required this.id,
    required this.brand,
    required this.last4,
    required this.expiryMonth,
    required this.expiryYear,
    this.holderName,
    this.status = active,
    this.isDefault = false,
    this.isExpired = false,
  });

  static const String active = 'active';
  static const String pendingVerification = 'pending_verification';

  final String id;

  /// `mada`, `visa` or `mastercard`.
  final String brand;
  final String last4;
  final int expiryMonth;
  final int expiryYear;
  final String? holderName;
  final String status;
  final bool isDefault;
  final bool isExpired;

  bool get isUsable => status == active && !isExpired;
  bool get isPending => status == pendingVerification;

  /// `08/29`.
  String get expiryLabel =>
      '${expiryMonth.toString().padLeft(2, '0')}/'
      '${(expiryYear % 100).toString().padLeft(2, '0')}';

  SavedCard copyWith({bool? isDefault}) => SavedCard(
    id: id,
    brand: brand,
    last4: last4,
    expiryMonth: expiryMonth,
    expiryYear: expiryYear,
    holderName: holderName,
    status: status,
    isDefault: isDefault ?? this.isDefault,
    isExpired: isExpired,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    brand,
    last4,
    expiryMonth,
    expiryYear,
    holderName,
    status,
    isDefault,
    isExpired,
  ];
}
