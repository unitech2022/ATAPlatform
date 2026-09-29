import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/usecases/validate_promo_code.dart';
import 'package:equatable/equatable.dart';

/// Where the promo entry stands.
enum PromoCodeStatus { empty, validating, applied, error }

/// State of [PromoCodeCubit].
class PromoCodeState extends Equatable {
  const PromoCodeState({
    this.input = '',
    this.inputVersion = 0,
    this.status = PromoCodeStatus.empty,
    this.applied,
    this.failure,
  });

  /// Text typed in the promo sheet.
  final String input;

  /// Bumped when the input is replaced programmatically (keys the field).
  final int inputVersion;
  final PromoCodeStatus status;

  /// The validated code sent with the quote and the trip request.
  final PromoValidation? applied;

  /// Localized through `failureText` (`promo_*` codes and reasons).
  final Failure? failure;

  String? get appliedCode => applied?.code;
  bool get isValidating => status == PromoCodeStatus.validating;
  bool get canValidate =>
      !isValidating && ValidatePromoCode.isWellFormed(input);

  PromoCodeState copyWith({
    String? input,
    int? inputVersion,
    PromoCodeStatus? status,
    PromoValidation? applied,
    Failure? failure,
    bool clearApplied = false,
    bool clearFailure = false,
  }) => PromoCodeState(
    input: input ?? this.input,
    inputVersion: inputVersion ?? this.inputVersion,
    status: status ?? this.status,
    applied: clearApplied ? null : applied ?? this.applied,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    input,
    inputVersion,
    status,
    applied,
    failure,
  ];
}
