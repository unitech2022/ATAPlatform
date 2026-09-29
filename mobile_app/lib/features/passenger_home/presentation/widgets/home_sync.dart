import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_state.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_places.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Cubit-to-cubit wiring of the rider home (listeners only, no state):
///
/// * route / time changes in [HomeCubit] → re-quote in [QuoteCubit];
/// * a usable quote in [QuoteCubit] → applied to [HomeCubit] (prices, offer
///   bounds, `quoteId`);
/// * `offer_out_of_range` → offer clamped; `quote_expired` → quote refreshed;
/// * a created trip → handed to the app-wide [ActiveTripCubit];
/// * promo code (F15): applied code → [HomeCubit] (re-quote with
///   `promoCode`); a quote or request refusing it → [PromoCodeCubit] error;
///   a pre-filled code is validated once the first quote arrives;
/// * favourite driver (F16): the selection is sent with the quote; a category
///   change re-reads the available favourites; `not_favorite` on the request
///   clears the selection. A `not_stacked` promo stays applied (only
///   explained in the sheet).
class HomeSync extends StatelessWidget {
  const HomeSync({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return MultiBlocListener(
      listeners: <BlocListener<dynamic, dynamic>>[
        BlocListener<HomeCubit, HomeState>(
          listenWhen: (HomeState p, HomeState c) =>
              p.stops != c.stops ||
              p.rideTime != c.rideTime ||
              p.effectivePromoCode != c.effectivePromoCode ||
              p.effectiveFavoriteDriverId != c.effectiveFavoriteDriverId,
          listener: (BuildContext context, HomeState state) => context
              .read<QuoteCubit>()
              .update(buildQuoteRequest(state, context.l10n)),
        ),
        BlocListener<HomeCubit, HomeState>(
          listenWhen: (HomeState p, HomeState c) =>
              p.selectedCategoryId != c.selectedCategoryId,
          listener: (BuildContext context, HomeState state) =>
              context.read<AvailableFavoritesCubit>().watch(
                TripPlaces.currentLocation,
                rideCategoryId: state.selectedCategory?.id,
              ),
        ),
        BlocListener<QuoteCubit, QuoteState>(
          listenWhen: (QuoteState p, QuoteState c) =>
              p.usableQuote != c.usableQuote,
          listener: (BuildContext context, QuoteState state) =>
              context.read<HomeCubit>().applyQuote(state.usableQuote),
        ),
        BlocListener<PromoCodeCubit, PromoCodeState>(
          listenWhen: (PromoCodeState p, PromoCodeState c) =>
              p.appliedCode != c.appliedCode,
          listener: (BuildContext context, PromoCodeState state) =>
              context.read<HomeCubit>().applyPromoCode(state.appliedCode),
        ),
        BlocListener<QuoteCubit, QuoteState>(
          listenWhen: (QuoteState p, QuoteState c) =>
              p.quote?.promotion != c.quote?.promotion,
          listener: (BuildContext context, QuoteState state) {
            final QuotePromotion? promotion = state.quote?.promotion;
            if (promotion != null &&
                !promotion.valid &&
                !promotion.isNotStacked) {
              context.read<PromoCodeCubit>().rejectedByQuote(promotion.reason);
            }
          },
        ),
        BlocListener<QuoteCubit, QuoteState>(
          listenWhen: (QuoteState p, QuoteState c) =>
              p.usableQuote == null && c.usableQuote != null,
          listener: _validatePrefilledPromo,
        ),
        BlocListener<TripRequestCubit, TripRequestState>(
          listenWhen: (TripRequestState p, TripRequestState c) =>
              p.failure != c.failure && c.failure != null,
          listener: (BuildContext context, TripRequestState state) {
            if (state.isPromoRejected) {
              context.read<PromoCodeCubit>().rejected(state.failure!);
            }
            if (state.isFavoriteRejected) {
              context.read<HomeCubit>().clearFavorite();
              context.read<AvailableFavoritesCubit>().refresh();
            }
            final OfferBounds? bounds = state.offerBounds;
            if (bounds != null) {
              context.read<HomeCubit>().clampOfferedPrice(bounds);
            }
            if (state.isQuoteExpired) context.read<QuoteCubit>().refresh();
          },
        ),
        BlocListener<TripRequestCubit, TripRequestState>(
          listenWhen: (TripRequestState p, TripRequestState c) =>
              !p.isSearching && c.isSearching && c.trip != null,
          // Hand the new trip to the app-wide feed; the router then
          // redirects to /trip.
          listener: (BuildContext context, TripRequestState state) {
            context.read<ActiveTripCubit>().adopt(state.trip!);
            context.read<TripRequestCubit>().reset();
          },
        ),
      ],
      child: child,
    );
  }

  void _validatePrefilledPromo(BuildContext context, QuoteState _) {
    final PromoCodeCubit promo = context.read<PromoCodeCubit>();
    final HomeState home = context.read<HomeCubit>().state;
    if (promo.state.status == PromoCodeStatus.empty &&
        promo.state.applied == null &&
        promo.state.canValidate &&
        home.canUsePromo) {
      promo.validate(buildPromoContext(home));
    }
  }
}
