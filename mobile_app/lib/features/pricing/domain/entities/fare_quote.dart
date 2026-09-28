import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:equatable/equatable.dart';

/// The pickup zone the quote was priced in.
class QuoteZone extends Equatable {
  const QuoteZone({required this.id, required this.name});

  final String id;
  final String name;

  @override
  List<Object?> get props => <Object?>[id, name];
}

/// Result of `POST /pricing/quote`: a price per category, valid until
/// [expiresAt] (5 minutes server side).
class FareQuote extends Equatable {
  const FareQuote({
    required this.quoteId,
    required this.expiresAt,
    required this.distanceMeters,
    required this.durationSeconds,
    this.pickupZone,
    this.demand = DemandLevel.normal,
    this.categories = const <QuoteCategory>[],
  });

  final String quoteId;
  final DateTime expiresAt;
  final int distanceMeters;
  final int durationSeconds;
  final QuoteZone? pickupZone;
  final DemandLevel demand;
  final List<QuoteCategory> categories;

  QuoteCategory? forCategory(String? rideCategoryId) {
    for (final QuoteCategory category in categories) {
      if (category.rideCategoryId == rideCategoryId) return category;
    }
    return null;
  }

  bool isExpiredAt(DateTime now) => !now.isBefore(expiresAt);

  @override
  List<Object?> get props => <Object?>[
    quoteId,
    expiresAt,
    distanceMeters,
    durationSeconds,
    pickupZone,
    demand,
    categories,
  ];
}
