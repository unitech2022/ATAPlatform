import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:equatable/equatable.dart';

/// Saved cards list with the default flag and pending actions.
class PaymentMethodsState extends Equatable {
  const PaymentMethodsState({
    this.cards = const <SavedCard>[],
    this.loading = false,
    this.failure,
    this.busyId,
    this.actionFailure,
  });

  final List<SavedCard> cards;
  final bool loading;

  /// Loading the list failed.
  final Failure? failure;

  /// Card being set as default or removed.
  final String? busyId;

  /// Setting the default or removing failed (e.g. `payment_method_in_use`).
  final Failure? actionFailure;

  bool get isEmpty => !loading && failure == null && cards.isEmpty;

  PaymentMethodsState copyWith({
    List<SavedCard>? cards,
    bool? loading,
    Failure? failure,
    String? busyId,
    Failure? actionFailure,
    bool clearFailure = false,
    bool clearBusy = false,
    bool clearActionFailure = false,
  }) => PaymentMethodsState(
    cards: cards ?? this.cards,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
    busyId: clearBusy ? null : busyId ?? this.busyId,
    actionFailure: clearActionFailure
        ? null
        : actionFailure ?? this.actionFailure,
  );

  @override
  List<Object?> get props => <Object?>[
    cards,
    loading,
    failure,
    busyId,
    actionFailure,
  ];
}
