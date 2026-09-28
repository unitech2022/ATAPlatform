import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:equatable/equatable.dart';

/// State of the first-time rider screen (name + terms).
class TermsState extends Equatable {
  const TermsState({
    this.fullName = '',
    this.accepted = false,
    this.submitting = false,
    this.failure,
    this.user,
  });

  final String fullName;
  final bool accepted;
  final bool submitting;
  final Failure? failure;

  /// Set once the profile was saved.
  final User? user;

  static const int minNameLength = 2;

  bool get canSubmit =>
      fullName.trim().length >= minNameLength && accepted && !submitting;

  TermsState copyWith({
    String? fullName,
    bool? accepted,
    bool? submitting,
    Failure? failure,
    User? user,
    bool clearFailure = false,
  }) => TermsState(
    fullName: fullName ?? this.fullName,
    accepted: accepted ?? this.accepted,
    submitting: submitting ?? this.submitting,
    failure: clearFailure ? null : failure ?? this.failure,
    user: user ?? this.user,
  );

  @override
  List<Object?> get props => <Object?>[
    fullName,
    accepted,
    submitting,
    failure,
    user,
  ];
}
