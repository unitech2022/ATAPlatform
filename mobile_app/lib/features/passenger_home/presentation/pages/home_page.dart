import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/home_sync.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/map_section.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/request_sheet.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/presentation/cubit/demand_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip_places.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Rider home: full-screen map with the ride-request bottom sheet.
class HomePage extends StatelessWidget {
  const HomePage({super.key, this.promoCode});

  /// Code picked on the promotions page (`/home?promo=`), validated once
  /// the first quote arrives.
  final String? promoCode;

  @override
  Widget build(BuildContext context) {
    // Read here: a provider `create` must not subscribe to inherited widgets.
    final AppLocalizations l10n = context.l10n;
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<HomeCubit>(
          create: (_) => HomeCubit(
            getRideCategories: getIt(),
            updatePreferences: getIt(),
            preferFemaleDriver: _preferFemale(context),
          )..loadCategories(),
        ),
        BlocProvider<QuoteCubit>(
          create: (_) =>
              QuoteCubit(getFareQuote: getIt())
                ..update(buildQuoteRequest(const HomeState(), l10n)),
        ),
        BlocProvider<DemandCubit>(
          create: (_) =>
              DemandCubit(getDemand: getIt())
                ..watch(TripPlaces.currentLocation),
        ),
        BlocProvider<PromoCodeCubit>(
          create: (_) =>
              PromoCodeCubit(validate: getIt(), initialCode: promoCode),
        ),
        BlocProvider<TripRequestCubit>(
          create: (_) => TripRequestCubit(
            estimateTrip: getIt(),
            requestTrip: getIt(),
            cancelTrip: getIt(),
          ),
        ),
      ],
      child: HomeSync(
        child: LayoutBuilder(
          builder: (BuildContext context, BoxConstraints constraints) {
            return Stack(
              children: <Widget>[
                Positioned.fill(
                  child: BlocSelector<HomeCubit, HomeState, int?>(
                    selector: (HomeState state) => state.displayEta,
                    builder: (BuildContext context, int? eta) => MapSection(
                      etaLabel: eta == null
                          ? context.l10n.noDriversNearby
                          : context.l10n.minutesLabel(eta),
                    ),
                  ),
                ),
                Align(
                  alignment: Alignment.bottomCenter,
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      maxHeight:
                          constraints.maxHeight * AtaSizes.sheetMaxHeightFactor,
                    ),
                    child: const RequestSheet(),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  bool _preferFemale(BuildContext context) =>
      context.read<SessionCubit>().state.session?.user.gender == 'female';
}
