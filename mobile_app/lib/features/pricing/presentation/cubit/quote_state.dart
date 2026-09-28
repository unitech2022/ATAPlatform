import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:equatable/equatable.dart';

/// Lifecycle of the fare quote shown on the home sheet.
enum QuoteStatus { idle, loading, ready, failure }

/// State of [QuoteCubit].
class QuoteState extends Equatable {
  const QuoteState({
    this.status = QuoteStatus.idle,
    this.request,
    this.quote,
    this.expired = false,
    this.failure,
  });

  final QuoteStatus status;

  /// The last input the quote was (or is being) computed for.
  final QuoteRequest? request;

  /// The latest successful quote; kept while a refresh is loading so the
  /// list can keep showing prices.
  final FareQuote? quote;

  /// `true` once [FareQuote.expiresAt] passed (the quote id is no longer
  /// accepted by `POST /passenger/trips`).
  final bool expired;
  final Failure? failure;

  bool get isLoading => status == QuoteStatus.loading;
  bool get hasQuote => quote != null;

  /// A quote that can be attached to a request right now.
  FareQuote? get usableQuote => expired ? null : quote;

  QuoteState copyWith({
    QuoteStatus? status,
    QuoteRequest? request,
    FareQuote? quote,
    bool? expired,
    Failure? failure,
    bool clearFailure = false,
  }) => QuoteState(
    status: status ?? this.status,
    request: request ?? this.request,
    quote: quote ?? this.quote,
    expired: expired ?? this.expired,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    status,
    request,
    quote,
    expired,
    failure,
  ];
}
