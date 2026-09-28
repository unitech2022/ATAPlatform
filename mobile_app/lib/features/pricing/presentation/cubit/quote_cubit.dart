import 'dart:async';

import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/pricing/domain/usecases/get_fare_quote.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Prices the current route (`POST /pricing/quote`) whenever the pickup,
/// destination, stops or booking time change, debounced by [debounce], and
/// tracks the quote's expiry so a stale `quoteId` is never sent.
class QuoteCubit extends Cubit<QuoteState> {
  QuoteCubit({
    required this._getFareQuote,
    this.debounce = defaultDebounce,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const QuoteState());

  static const Duration defaultDebounce = Duration(milliseconds: 600);

  final GetFareQuote _getFareQuote;
  final Duration debounce;
  final DateTime Function() _now;

  Timer? _debounceTimer;
  Timer? _expiryTimer;
  int _generation = 0;

  /// `true` when the current quote can no longer be used.
  bool get isExpired {
    final FareQuote? quote = state.quote;
    return quote == null || quote.isExpiredAt(_now());
  }

  /// Schedules a new quote for [request]. Identical input is ignored.
  void update(QuoteRequest request) {
    if (request == state.request && state.status != QuoteStatus.failure) {
      return;
    }
    emit(state.copyWith(request: request, status: QuoteStatus.loading));
    _debounceTimer?.cancel();
    _debounceTimer = Timer(debounce, () => _load(request));
  }

  /// Re-prices the last input right away (after expiry or a failure).
  Future<void> refresh() {
    final QuoteRequest? request = state.request;
    if (request == null) return Future<void>.value();
    _debounceTimer?.cancel();
    emit(state.copyWith(status: QuoteStatus.loading, clearFailure: true));
    return _load(request);
  }

  Future<void> _load(QuoteRequest request) async {
    final int generation = ++_generation;
    final result = await _getFareQuote(request);
    if (isClosed || generation != _generation) return;
    result.fold(
      (failure) =>
          emit(state.copyWith(status: QuoteStatus.failure, failure: failure)),
      (FareQuote quote) {
        emit(
          state.copyWith(
            status: QuoteStatus.ready,
            quote: quote,
            expired: quote.isExpiredAt(_now()),
            clearFailure: true,
          ),
        );
        _armExpiry(quote);
      },
    );
  }

  void _armExpiry(FareQuote quote) {
    _expiryTimer?.cancel();
    final Duration left = quote.expiresAt.difference(_now());
    if (left.isNegative) return;
    _expiryTimer = Timer(left, () {
      if (!isClosed && state.quote?.quoteId == quote.quoteId) {
        emit(state.copyWith(expired: true));
      }
    });
  }

  @override
  Future<void> close() {
    _debounceTimer?.cancel();
    _expiryTimer?.cancel();
    return super.close();
  }
}
