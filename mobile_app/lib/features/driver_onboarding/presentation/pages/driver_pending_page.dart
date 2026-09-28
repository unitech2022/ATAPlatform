import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/env/env.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_panel.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_scaffold.dart';
import 'package:ata_app/features/driver_onboarding/presentation/cubit/driver_pending_cubit.dart';
import 'package:ata_app/features/driver_onboarding/presentation/cubit/driver_pending_state.dart';
import 'package:ata_app/features/driver_onboarding/presentation/widgets/application_steps.dart';
import 'package:ata_app/features/driver_onboarding/presentation/widgets/required_documents_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

/// "Your application was created / is under review" screen for drivers
/// that are not approved yet.
class DriverPendingPage extends StatelessWidget {
  const DriverPendingPage({super.key});

  static Future<bool> _openUrl(String url) =>
      launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication);

  @override
  Widget build(BuildContext context) {
    return BlocProvider<DriverPendingCubit>(
      create: (_) => DriverPendingCubit(
        getApplication: getIt(),
        openUrl: _openUrl,
        portalUrl: Env.driverPortalUrl,
      )..load(),
      child: MultiBlocListener(
        listeners: <BlocListener<dynamic, dynamic>>[
          BlocListener<DriverPendingCubit, DriverPendingState>(
            listenWhen: (DriverPendingState p, DriverPendingState c) =>
                p.application?.status != c.application?.status &&
                c.application != null,
            listener: (BuildContext context, DriverPendingState state) =>
                context.read<SessionCubit>().updateDriverStatus(
                  state.application!.status,
                ),
          ),
          BlocListener<DriverPendingCubit, DriverPendingState>(
            listenWhen: (DriverPendingState p, DriverPendingState c) =>
                !p.portalOpenFailed && c.portalOpenFailed,
            listener: (BuildContext context, DriverPendingState state) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text(context.l10n.portalOpenFailed)),
              );
              context.read<DriverPendingCubit>().portalFailureShown();
            },
          ),
        ],
        child: const AuthScaffold(child: _PendingPanel()),
      ),
    );
  }
}

class _PendingPanel extends StatelessWidget {
  const _PendingPanel();

  static const double _checkSize = 64;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DriverPendingCubit cubit = context.read<DriverPendingCubit>();
    return BlocBuilder<DriverPendingCubit, DriverPendingState>(
      builder: (BuildContext context, DriverPendingState state) {
        final DriverApplicationStatus status =
            state.application?.status ??
            context.read<SessionCubit>().state.driverStatus ??
            DriverApplicationStatus.draft;
        final String applicationNumber =
            state.application?.applicationNumber ??
            context
                .read<SessionCubit>()
                .state
                .session
                ?.driver
                ?.applicationNumber ??
            '';
        final _StatusCopy copy = _copyFor(l10n, status);
        return AuthPanel(
          children: <Widget>[
            const IconBox(
              icon: AtaIcons.check,
              size: _checkSize,
              iconSize: AtaSizes.iconHero,
              round: true,
            ),
            const SizedBox(height: AtaSpacing.lg),
            Text(copy.title, style: AtaText.title),
            const SizedBox(height: AtaSpacing.sm),
            Text(copy.body, style: AtaText.bodyMuted),
            if (state.application?.rejectionReason
                case final String reason?) ...<Widget>[
              const SizedBox(height: AtaSpacing.sm),
              InlineError(message: l10n.rejectionReason(reason)),
            ],
            const SizedBox(height: AtaSpacing.lg),
            _ApplicationNumber(
              label: l10n.applicationNumber,
              value: applicationNumber,
            ),
            const SizedBox(height: AtaSpacing.xl),
            ApplicationStepsList(activeIndex: _activeStep(status)),
            const SizedBox(height: AtaSpacing.xl),
            if (state.loading)
              const CenteredLoader()
            else
              RequiredDocumentsCard(application: state.application),
            if (state.failure != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.md),
              InlineError(message: failureText(state.failure!, l10n)),
            ],
            const SizedBox(height: AtaSpacing.xl),
            if (status.isApproved)
              AtaButton(
                label: l10n.openDriverPortal,
                icon: AtaIcons.car,
                onPressed: () => context.go(AppRoutes.driver),
              )
            else
              AtaButton(
                label: l10n.openUploadPortal,
                icon: AtaIcons.upload,
                onPressed: cubit.openPortal,
              ),
            const SizedBox(height: AtaSpacing.sm),
            AtaButton(
              label: l10n.refreshStatus,
              variant: AtaButtonVariant.outline,
              onPressed: state.loading ? null : cubit.load,
            ),
            const SizedBox(height: AtaSpacing.lg),
            TextButton(
              onPressed: () => context.read<SessionCubit>().signOut(),
              child: Text(l10n.backToLogin, style: AtaText.labelMuted),
            ),
          ],
        );
      },
    );
  }

  int _activeStep(DriverApplicationStatus status) => switch (status) {
    DriverApplicationStatus.draft ||
    DriverApplicationStatus.rejected ||
    DriverApplicationStatus.unknown => 0,
    DriverApplicationStatus.submitted ||
    DriverApplicationStatus.underReview ||
    DriverApplicationStatus.suspended => 1,
    DriverApplicationStatus.approved => 2,
  };

  _StatusCopy _copyFor(AppLocalizations l10n, DriverApplicationStatus status) =>
      switch (status) {
        DriverApplicationStatus.submitted ||
        DriverApplicationStatus.underReview => _StatusCopy(
          l10n.pendingReviewTitle,
          l10n.pendingReviewCopy,
        ),
        DriverApplicationStatus.rejected => _StatusCopy(
          l10n.pendingRejectedTitle,
          l10n.pendingRejectedCopy,
        ),
        DriverApplicationStatus.suspended => _StatusCopy(
          l10n.pendingSuspendedTitle,
          l10n.pendingSuspendedCopy,
        ),
        DriverApplicationStatus.approved => _StatusCopy(
          l10n.pendingApprovedTitle,
          l10n.pendingApprovedCopy,
        ),
        DriverApplicationStatus.draft || DriverApplicationStatus.unknown =>
          _StatusCopy(l10n.pendingTitle, l10n.pendingCopy),
      };
}

class _StatusCopy {
  const _StatusCopy(this.title, this.body);

  final String title;
  final String body;
}

class _ApplicationNumber extends StatelessWidget {
  const _ApplicationNumber({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.lg,
        vertical: AtaSpacing.md,
      ),
      decoration: const BoxDecoration(
        color: AtaColors.cloud,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        children: <Widget>[
          Text(label, style: AtaText.caption),
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            value,
            style: AtaText.bodyStrong,
            textDirection: TextDirection.ltr,
          ),
        ],
      ),
    );
  }
}
