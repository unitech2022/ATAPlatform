import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/passenger_home/domain/entities/fare_estimate.dart';
import 'package:ata_app/features/passenger_home/domain/entities/favorite_selection.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:equatable/equatable.dart';

/// State of the rider home sheet.
class HomeState extends Equatable {
  const HomeState({
    this.categories = const <RideCategory>[],
    this.loadingCategories = false,
    this.categoriesFailure,
    this.selectedCategoryId,
    this.stops = const <String>[],
    this.rideTime = RideTime.now,
    this.preferFemaleDriver = false,
    this.payment = PaymentOption.cash,
    this.offeredPrice,
    this.estimate = const FareEstimate(price: 0, etaMinutes: 0),
    this.quote,
    this.promoCode,
    this.favorite,
  });

  /// Lowest price a rider may ever offer, and the fallback offer range used
  /// until a quote arrives (mirrors `Pricing:OfferMin/MaxPercent`).
  static const double minOfferedPrice = 5;
  static const double fallbackOfferMinFactor = 0.7;
  static const double fallbackOfferMaxFactor = 1.3;

  final List<RideCategory> categories;
  final bool loadingCategories;
  final Failure? categoriesFailure;
  final String? selectedCategoryId;
  final List<String> stops;
  final RideTime rideTime;
  final bool preferFemaleDriver;
  final PaymentOption payment;

  /// Optional price proposed by the rider (`pricingMode: offer`).
  final double? offeredPrice;

  /// Catalog estimate, shown until the quote arrives.
  final FareEstimate estimate;

  /// The usable (non-expired) fare quote applied from `QuoteCubit`.
  final FareQuote? quote;

  /// Promo code validated by `PromoCodeCubit` (F15).
  final String? promoCode;

  /// Favourite driver picked for this request (F16).
  final FavoriteSelection? favorite;

  RideCategory? get selectedCategory {
    for (final RideCategory category in categories) {
      if (category.id == selectedCategoryId) return category;
    }
    return categories.isEmpty ? null : categories.first;
  }

  /// Quoted line of the selected category, if the quote covers it.
  QuoteCategory? get quoteCategory => quote?.forCategory(selectedCategory?.id);

  /// Price and ETA shown for the selected category.
  double get displayPrice => quoteCategory?.total ?? estimate.price;

  /// ETA of the selected category: the quoted value once a quote covers it
  /// (`null` = no drivers nearby), the catalog estimate before that.
  int? get displayEta {
    final QuoteCategory? quoted = quoteCategory;
    return quoted != null ? quoted.etaMinutes : estimate.etaMinutes;
  }

  /// Accepted "offer your price" range.
  OfferBounds get offerBounds {
    final QuoteCategory? quoted = quoteCategory;
    if (quoted != null && quoted.offerBounds.isValid) return quoted.offerBounds;
    final double min = (estimate.price * fallbackOfferMinFactor)
        .floorToDouble()
        .clamp(minOfferedPrice, double.infinity);
    final double max = (estimate.price * fallbackOfferMaxFactor)
        .ceilToDouble()
        .clamp(min + 1, double.infinity);
    return OfferBounds(min: min, max: max);
  }

  int get maxStops => selectedCategory?.maxStops ?? 0;
  bool get canAddStop => stops.length < maxStops;
  bool get canRequest => selectedCategory != null;
  bool get hasOfferedPrice => offeredPrice != null;
  bool get hasQuote => quote != null;

  /// Promo codes are not offered with "offer your price" (`docs/10` §1).
  bool get canUsePromo => !hasOfferedPrice;

  /// The promo code sent with the quote and the request.
  String? get effectivePromoCode => canUsePromo ? promoCode : null;

  /// Favourite drivers are not offered with "offer your price" (`docs/10` §1).
  bool get canUseFavorite => !hasOfferedPrice;

  /// The favourite driver sent with the quote and the request.
  String? get effectiveFavoriteDriverId =>
      canUseFavorite ? favorite?.driverId : null;

  /// Favourite discount line of the selected category's quote, if any.
  FareDiscount? get favoriteDiscountLine =>
      quoteCategory?.breakdown.discountFrom(DiscountSource.favoriteDriver);

  /// Whether the favourite discount and the promo code were both wanted and
  /// only one won (`docs/10` §1: not stackable → the larger one).
  FavoritePromoOutcome get favoritePromoOutcome {
    final QuoteCategory? quoted = quoteCategory;
    if (quoted == null ||
        effectiveFavoriteDriverId == null ||
        effectivePromoCode == null) {
      return FavoritePromoOutcome.none;
    }
    final FareBreakdown b = quoted.breakdown;
    final bool hasFavorite =
        b.discountFrom(DiscountSource.favoriteDriver) != null;
    final bool hasPromo = b.discountFrom(DiscountSource.promotion) != null;
    if (quote?.promotion?.isNotStacked ?? false) {
      return FavoritePromoOutcome.promoNotApplied;
    }
    if (hasFavorite && !hasPromo) return FavoritePromoOutcome.promoNotApplied;
    if (hasPromo && !hasFavorite && favorite?.discount != null) {
      return FavoritePromoOutcome.favoriteNotApplied;
    }
    return FavoritePromoOutcome.none;
  }

  HomeState copyWith({
    List<RideCategory>? categories,
    bool? loadingCategories,
    Failure? categoriesFailure,
    String? selectedCategoryId,
    List<String>? stops,
    RideTime? rideTime,
    bool? preferFemaleDriver,
    PaymentOption? payment,
    double? offeredPrice,
    FareEstimate? estimate,
    FareQuote? quote,
    String? promoCode,
    FavoriteSelection? favorite,
    bool clearFailure = false,
    bool clearOfferedPrice = false,
    bool clearQuote = false,
    bool clearPromoCode = false,
    bool clearFavorite = false,
  }) => HomeState(
    categories: categories ?? this.categories,
    loadingCategories: loadingCategories ?? this.loadingCategories,
    categoriesFailure: clearFailure
        ? null
        : categoriesFailure ?? this.categoriesFailure,
    selectedCategoryId: selectedCategoryId ?? this.selectedCategoryId,
    stops: stops ?? this.stops,
    rideTime: rideTime ?? this.rideTime,
    preferFemaleDriver: preferFemaleDriver ?? this.preferFemaleDriver,
    payment: payment ?? this.payment,
    offeredPrice: clearOfferedPrice ? null : offeredPrice ?? this.offeredPrice,
    estimate: estimate ?? this.estimate,
    quote: clearQuote ? null : quote ?? this.quote,
    promoCode: clearPromoCode ? null : promoCode ?? this.promoCode,
    favorite: clearFavorite ? null : favorite ?? this.favorite,
  );

  @override
  List<Object?> get props => <Object?>[
    categories,
    loadingCategories,
    categoriesFailure,
    selectedCategoryId,
    stops,
    rideTime,
    preferFemaleDriver,
    payment,
    offeredPrice,
    estimate,
    quote,
    promoCode,
    favorite,
  ];
}
