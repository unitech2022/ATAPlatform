import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
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

/// `promotion: { code, valid, reason }` of a quote priced with a promo code:
/// an invalid code does not fail the quote (F15).
class QuotePromotion extends Equatable {
  const QuotePromotion({required this.code, this.valid = true, this.reason});

  final String code;
  final bool valid;
  final String? reason;

  /// The code was not applied because it cannot be combined with the larger
  /// favourite-driver discount (`docs/10` §1, `not_stacked`).
  bool get isNotStacked =>
      reason == 'not_stacked' || reason == 'promo_not_stacked';

  @override
  List<Object?> get props => <Object?>[code, valid, reason];
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
    this.promotion,
    this.favoriteDiscountConditional = false,
    this.corporate,
  });

  final String quoteId;
  final DateTime expiresAt;
  final int distanceMeters;
  final int durationSeconds;
  final QuoteZone? pickupZone;
  final DemandLevel demand;
  final List<QuoteCategory> categories;
  final QuotePromotion? promotion;

  /// The favourite-driver discount in the totals assumes the driver
  /// accepts (`favoriteDiscountConditional`, F16).
  final bool favoriteDiscountConditional;

  /// The company policy evaluation of a `paymentMethod: corporate` quote
  /// (F19); `null` for the other payment methods.
  final CorporateQuoteCheck? corporate;

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
    promotion,
    favoriteDiscountConditional,
    corporate,
  ];
}
