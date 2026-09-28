import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/account/presentation/cubit/account_cubit.dart';
import 'package:ata_app/features/account/presentation/cubit/account_state.dart';
import 'package:ata_app/features/account/presentation/widgets/profile_card.dart';
import 'package:ata_app/features/account/presentation/widgets/settings_list.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_cubit.dart';
import 'package:ata_app/features/trip/presentation/widgets/reliability_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Profile card, reliability summary (F14) and settings.
class AccountPage extends StatelessWidget {
  const AccountPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final User? sessionUser = context.read<SessionCubit>().state.session?.user;
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<AccountCubit>(
          create: (_) => AccountCubit(getProfile: getIt())..load(),
        ),
        BlocProvider<ReliabilityCubit>(
          create: (_) => ReliabilityCubit(
            getReliability: getIt(),
            role: TripActor.passenger,
          )..load(),
        ),
      ],
      child: BlocBuilder<AccountCubit, AccountState>(
        builder: (BuildContext context, AccountState state) {
          final User? user = state.profile?.user ?? sessionUser;
          final String name = user?.fullName ?? l10n.guestName;
          return PageWrap(
            children: <Widget>[
              ScreenTitle(
                eyebrow: l10n.accountEyebrow,
                title: l10n.accountWelcome(user?.firstName ?? l10n.guestName),
                copy: l10n.accountCopy,
              ),
              const SizedBox(height: AtaSpacing.xxl),
              ProfileCard(name: name, passenger: state.profile?.passenger),
              const SizedBox(height: AtaSpacing.xl),
              ReliabilityCard(
                onDetails: () => context.push(AppRoutes.accountReliability),
              ),
              const SizedBox(height: AtaSpacing.xl),
              const SettingsList(),
            ],
          );
        },
      ),
    );
  }
}
