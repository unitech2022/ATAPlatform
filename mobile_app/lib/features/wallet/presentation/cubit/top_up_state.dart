import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:equatable/equatable.dart';

/// Steps of the top-up flow: choose → success, or choose → action when the
/// card needs 3-D Secure.
enum TopUpStep { choose, action, success }

/// State of the top-up flow.
class TopUpState extends Equatable {
  const TopUpState({
    this.amount = defaultAmount,
    this.step = TopUpStep.choose,
    this.submitting = false,
    this.newBalance,
    this.failure,
    this.cards = const <SavedCard>[],
    this.cardId,
    this.action,
  });

  static const List<double> presets = <double>[50, 100, 200];
  static const double defaultAmount = 100;

  final double amount;
  final TopUpStep step;
  final bool submitting;
  final double? newBalance;
  final Failure? failure;

  /// Usable saved cards.
  final List<SavedCard> cards;

  /// Selected card; `null` = sandbox payment.
  final String? cardId;

  /// 3-D Secure redirect returned with `202`.
  final PaymentAction? action;

  bool get canConfirm => !submitting && amount > 0;
  bool get usesCard => cardId != null;

  SavedCard? get selectedCard {
    for (final SavedCard card in cards) {
      if (card.id == cardId) return card;
    }
    return null;
  }

  TopUpState copyWith({
    double? amount,
    TopUpStep? step,
    bool? submitting,
    double? newBalance,
    Failure? failure,
    List<SavedCard>? cards,
    String? cardId,
    PaymentAction? action,
    bool clearFailure = false,
    bool clearCard = false,
  }) => TopUpState(
    amount: amount ?? this.amount,
    step: step ?? this.step,
    submitting: submitting ?? this.submitting,
    newBalance: newBalance ?? this.newBalance,
    failure: clearFailure ? null : failure ?? this.failure,
    cards: cards ?? this.cards,
    cardId: clearCard ? null : cardId ?? this.cardId,
    action: action ?? this.action,
  );

  @override
  List<Object?> get props => <Object?>[
    amount,
    step,
    submitting,
    newBalance,
    failure,
    cards,
    cardId,
    action,
  ];
}
