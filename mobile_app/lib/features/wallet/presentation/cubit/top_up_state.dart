import 'package:ata_app/core/errors/failures.dart';
import 'package:equatable/equatable.dart';

/// Steps of the top-up flow.
enum TopUpStep { choose, success }

/// State of the top-up flow.
class TopUpState extends Equatable {
  const TopUpState({
    this.amount = defaultAmount,
    this.step = TopUpStep.choose,
    this.submitting = false,
    this.newBalance,
    this.failure,
  });

  static const List<double> presets = <double>[50, 100, 200];
  static const double defaultAmount = 100;

  final double amount;
  final TopUpStep step;
  final bool submitting;
  final double? newBalance;
  final Failure? failure;

  bool get canConfirm => !submitting && amount > 0;

  TopUpState copyWith({
    double? amount,
    TopUpStep? step,
    bool? submitting,
    double? newBalance,
    Failure? failure,
    bool clearFailure = false,
  }) => TopUpState(
    amount: amount ?? this.amount,
    step: step ?? this.step,
    submitting: submitting ?? this.submitting,
    newBalance: newBalance ?? this.newBalance,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    amount,
    step,
    submitting,
    newBalance,
    failure,
  ];
}
