import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/map_section.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/request_sheet.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Rider home: full-screen map with the ride-request bottom sheet.
class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<HomeCubit>(
          create: (_) => HomeCubit(
            getRideCategories: getIt(),
            updatePreferences: getIt(),
            preferFemaleDriver: _preferFemale(context),
          )..loadCategories(),
        ),
        BlocProvider<TripRequestCubit>(
          create: (_) => TripRequestCubit(
            estimateTrip: getIt(),
            requestTrip: getIt(),
            cancelTrip: getIt(),
          ),
        ),
      ],
      child: BlocListener<TripRequestCubit, TripRequestState>(
        listenWhen: (TripRequestState p, TripRequestState c) =>
            !p.isSearching && c.isSearching && c.trip != null,
        // Hand the new trip to the app-wide feed; the router then redirects
        // to /trip.
        listener: (BuildContext context, TripRequestState state) {
          context.read<ActiveTripCubit>().adopt(state.trip!);
          context.read<TripRequestCubit>().reset();
        },
        child: LayoutBuilder(
          builder: (BuildContext context, BoxConstraints constraints) {
            return Stack(
              children: <Widget>[
                Positioned.fill(
                  child: BlocBuilder<HomeCubit, HomeState>(
                    buildWhen: (HomeState p, HomeState c) =>
                        p.estimate != c.estimate,
                    builder: (BuildContext context, HomeState state) =>
                        MapSection(
                          etaLabel: context.l10n.minutesLabel(
                            state.estimate.etaMinutes,
                          ),
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
