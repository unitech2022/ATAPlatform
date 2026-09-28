import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_state.dart';
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
/// * a created trip → handed to the app-wide [ActiveTripCubit].
class HomeSync extends StatelessWidget {
  const HomeSync({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return MultiBlocListener(
      listeners: <BlocListener<dynamic, dynamic>>[
        BlocListener<HomeCubit, HomeState>(
          listenWhen: (HomeState p, HomeState c) =>
              p.stops != c.stops || p.rideTime != c.rideTime,
          listener: (BuildContext context, HomeState state) => context
              .read<QuoteCubit>()
              .update(buildQuoteRequest(state, context.l10n)),
        ),
        BlocListener<QuoteCubit, QuoteState>(
          listenWhen: (QuoteState p, QuoteState c) =>
              p.usableQuote != c.usableQuote,
          listener: (BuildContext context, QuoteState state) =>
              context.read<HomeCubit>().applyQuote(state.usableQuote),
        ),
        BlocListener<TripRequestCubit, TripRequestState>(
          listenWhen: (TripRequestState p, TripRequestState c) =>
              p.failure != c.failure && c.failure != null,
          listener: (BuildContext context, TripRequestState state) {
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
}
