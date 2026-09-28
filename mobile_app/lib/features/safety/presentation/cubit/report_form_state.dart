import 'package:ata_app/core/errors/failures.dart';
import 'package:equatable/equatable.dart';

/// Form state shared by the lost item and safety report cubits: a
/// category of type [C], a description and the submitted result [R].
class ReportFormState<C, R> extends Equatable {
  const ReportFormState({
    this.category,
    this.description = '',
    this.contactPhone = '',
    this.submitting = false,
    this.showErrors = false,
    this.result,
    this.failure,
  });

  static const int minDescription = 3;

  final C? category;
  final String description;

  /// Optional (lost items; defaults to the passenger's phone).
  final String contactPhone;
  final bool submitting;

  /// Highlight missing fields after a submit attempt.
  final bool showErrors;
  final R? result;
  final Failure? failure;

  bool get categoryMissing => category == null;
  bool get descriptionMissing => description.trim().length < minDescription;
  bool get isValid => !categoryMissing && !descriptionMissing;

  ReportFormState<C, R> copyWith({
    C? category,
    String? description,
    String? contactPhone,
    bool? submitting,
    bool? showErrors,
    R? result,
    Failure? failure,
    bool clearFailure = false,
  }) => ReportFormState<C, R>(
    category: category ?? this.category,
    description: description ?? this.description,
    contactPhone: contactPhone ?? this.contactPhone,
    submitting: submitting ?? this.submitting,
    showErrors: showErrors ?? this.showErrors,
    result: result ?? this.result,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    category,
    description,
    contactPhone,
    submitting,
    showErrors,
    result,
    failure,
  ];
}
