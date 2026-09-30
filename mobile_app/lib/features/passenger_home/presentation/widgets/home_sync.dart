import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_state.dart';
import 'package:ata_app/features/corporate/domain/entities/eligibility_draft.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_state.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_state.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_cubit.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_state.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_places.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

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
/// * scheduling (F17): the time confirmed in [ScheduleTimeCubit] and the
///   airport chosen in [AirportPickupCubit] go to [HomeCubit] (re-quote); a
///   booked scheduled ride opens its detail page;
/// * favourite driver (F16): the selection is sent with the quote; a category
///   change re-reads the available favourites; `not_favorite` on the request
///   clears the selection. A `not_stacked` promo stays applied (only
///   explained in the sheet).
/// * company account (F19): the request draft (category, time, quote with its
///   `corporate` evaluation) goes to [CorporatePaymentCubit], whose purpose /
///   cost center / readiness go back to [HomeCubit]; a lost membership
///   returns the payment to cash and a policy refusal re-prices the quote.
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
              p.scheduledAt != c.scheduledAt ||
              p.airport != c.airport ||
              p.effectivePromoCode != c.effectivePromoCode ||
              p.effectiveFavoriteDriverId != c.effectiveFavoriteDriverId ||
              p.isCorporate != c.isCorporate ||
              p.corporateBooking?.quoteKey != c.corporateBooking?.quoteKey,
          listener: (BuildContext context, HomeState state) => context
              .read<QuoteCubit>()
              .update(buildQuoteRequest(state, context.l10n)),
        ),
        BlocListener<HomeCubit, HomeState>(
          listenWhen: (HomeState p, HomeState c) =>
              p.selectedCategoryId != c.selectedCategoryId ||
              p.scheduledAt != c.scheduledAt ||
              p.rideTime != c.rideTime ||
              p.quote != c.quote ||
              p.payment != c.payment ||
              p.categories != c.categories,
          listener: _syncCorporate,
        ),
        BlocListener<CorporateMembershipCubit, CorporateMembershipState>(
          listenWhen:
              (CorporateMembershipState p, CorporateMembershipState c) =>
                  p.profile != c.profile,
          listener: _onMembership,
        ),
        BlocListener<CorporatePaymentCubit, CorporatePaymentState>(
          listenWhen: (CorporatePaymentState p, CorporatePaymentState c) =>
              p.booking != c.booking,
          listener: (BuildContext context, CorporatePaymentState state) =>
              context.read<HomeCubit>().applyCorporate(state.booking),
        ),
        BlocListener<ScheduleTimeCubit, ScheduleTimeState>(
          listenWhen: (ScheduleTimeState p, ScheduleTimeState c) =>
              p.confirmed != c.confirmed,
          listener: (BuildContext context, ScheduleTimeState state) =>
              context.read<HomeCubit>().applyScheduledAt(state.confirmed),
        ),
        BlocListener<AirportPickupCubit, AirportPickupState>(
          listenWhen: (AirportPickupState p, AirportPickupState c) =>
              p.selection != c.selection,
          listener: (BuildContext context, AirportPickupState state) =>
              context.read<HomeCubit>().applyAirport(state.selection),
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
            if (state.isCorporateRefusal) context.read<QuoteCubit>().refresh();
            if (state.isCorporateMembershipLost) {
              context.read<CorporateMembershipCubit>().refresh();
            }
            if (state.isScheduleRejected) {
              context.read<ScheduleTimeCubit>().recheck();
            }
          },
        ),
        BlocListener<TripRequestCubit, TripRequestState>(
          listenWhen: (TripRequestState p, TripRequestState c) =>
              !p.isScheduled && c.isScheduled && c.trip != null,
          // A scheduled booking is not an active trip: back to "now" and
          // on to the booking's detail page.
          listener: (BuildContext context, TripRequestState state) {
            final String tripId = state.trip!.id;
            context.read<TripRequestCubit>().reset();
            context.read<ScheduleTimeCubit>().clear();
            ScaffoldMessenger.of(context)
              ..hideCurrentSnackBar()
              ..showSnackBar(
                SnackBar(content: Text(context.l10n.scheduleBooked)),
              );
            context.go(AppRoutes.scheduledTrip(tripId));
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

  /// Feeds the company-account cubit with the request draft (F19).
  void _syncCorporate(BuildContext context, HomeState home) =>
      context.read<CorporatePaymentCubit>().sync(
        EligibilityDraft(
          profile: context.read<CorporateMembershipCubit>().state.profile,
          categoryCode: home.selectedCategory?.code,
          scheduledAt: home.isScheduled ? home.scheduledAt : null,
          estimatedFare: home.quoteCategory?.total,
          serverCheck: home.isCorporate ? home.quote?.corporate : null,
        ),
        selected: home.isCorporate,
      );

  /// A new / lost membership re-evaluates the draft; losing it while the
  /// company account is chosen goes back to cash.
  void _onMembership(BuildContext context, CorporateMembershipState state) {
    final HomeCubit home = context.read<HomeCubit>();
    if (!state.isActive && home.state.payment == PaymentOption.corporate) {
      home.selectPayment(PaymentOption.cash);
    }
    _syncCorporate(context, home.state);
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
