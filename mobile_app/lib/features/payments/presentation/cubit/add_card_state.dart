import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:equatable/equatable.dart';

/// Progress of the add-card form.
enum AddCardStatus { editing, submitting, saved, requiresAction }

/// Card form fields, validation and the save result.
class AddCardState extends Equatable {
  const AddCardState({
    this.number = '',
    this.expiry = '',
    this.cvc = '',
    this.holderName = '',
    this.setDefault = true,
    this.showErrors = false,
    this.errors = const <CardField>{},
    this.status = AddCardStatus.editing,
    this.action,
    this.failure,
  });

  final String number;

  /// `MM/YY`.
  final String expiry;
  final String cvc;
  final String holderName;
  final bool setDefault;

  /// Errors are shown after the first submit attempt.
  final bool showErrors;
  final Set<CardField> errors;
  final AddCardStatus status;
  final PaymentAction? action;
  final Failure? failure;

  bool get submitting => status == AddCardStatus.submitting;
  bool get isValid => errors.isEmpty;
  bool hasError(CardField field) => showErrors && errors.contains(field);

  AddCardState copyWith({
    String? number,
    String? expiry,
    String? cvc,
    String? holderName,
    bool? setDefault,
    bool? showErrors,
    Set<CardField>? errors,
    AddCardStatus? status,
    PaymentAction? action,
    Failure? failure,
    bool clearFailure = false,
  }) => AddCardState(
    number: number ?? this.number,
    expiry: expiry ?? this.expiry,
    cvc: cvc ?? this.cvc,
    holderName: holderName ?? this.holderName,
    setDefault: setDefault ?? this.setDefault,
    showErrors: showErrors ?? this.showErrors,
    errors: errors ?? this.errors,
    status: status ?? this.status,
    action: action ?? this.action,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    number,
    expiry,
    cvc,
    holderName,
    setDefault,
    showErrors,
    errors,
    status,
    action,
    failure,
  ];
}
