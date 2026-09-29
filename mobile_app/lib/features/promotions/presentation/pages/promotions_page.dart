import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promotions_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promotions_state.dart';
import 'package:ata_app/features/promotions/presentation/widgets/promo_text.dart';
import 'package:ata_app/features/promotions/presentation/widgets/promotion_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/promotions` ("العروض", `ata://promotions`): available / used / expired
/// promo codes with copy and "use" (opens the home sheet with the code).
class PromotionsPage extends StatelessWidget {
  const PromotionsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<PromotionsCubit>(
      create: (_) => PromotionsCubit(getPromotions: getIt())..load(),
      child: PageWrap(
        children: <Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: PillButton.back(
              label: l10n.back,
              onTap: () => context.canPop()
                  ? context.pop()
                  : context.go(AppRoutes.wallet),
            ),
          ),
          const SizedBox(height: AtaSpacing.xl),
          ScreenTitle(
            eyebrow: l10n.walletEyebrow,
            title: l10n.promotionsTitle,
            copy: l10n.promotionsCopy,
          ),
          const SizedBox(height: AtaSpacing.xl),
          BlocBuilder<PromotionsCubit, PromotionsState>(
            builder: (BuildContext context, PromotionsState state) => Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                _Tabs(state: state),
                const SizedBox(height: AtaSpacing.lg),
                ..._body(context, state),
              ],
            ),
          ),
        ],
      ),
    );
  }

  List<Widget> _body(BuildContext context, PromotionsState state) {
    final AppLocalizations l10n = context.l10n;
    final PromotionsCubit cubit = context.read<PromotionsCubit>();
    if (state.loading && !state.isLoaded) {
      return const <Widget>[CenteredLoader()];
    }
    if (state.failure != null) {
      return <Widget>[
        FailureView(failure: state.failure!, onRetry: cubit.load),
      ];
    }
    if (state.isEmpty) {
      return <Widget>[
        Text(
          l10n.promotionsEmpty,
          style: AtaText.small,
          textAlign: TextAlign.center,
        ),
      ];
    }
    return <Widget>[
      for (final Promotion p in state.current) ...<Widget>[
        PromotionTile(
          promotion: p,
          onCopy: () => _copy(context, p.code),
          onUse: () => context.go(AppRoutes.homeWithPromo(p.code)),
        ),
        const SizedBox(height: AtaSpacing.sm),
      ],
    ];
  }

  Future<void> _copy(BuildContext context, String code) async {
    final ScaffoldMessengerState? messenger = ScaffoldMessenger.maybeOf(
      context,
    );
    final String copied = context.l10n.promoCopied(code);
    await Clipboard.setData(ClipboardData(text: code));
    messenger?.showSnackBar(SnackBar(content: Text(copied)));
  }
}

class _Tabs extends StatelessWidget {
  const _Tabs({required this.state});

  final PromotionsState state;

  @override
  Widget build(BuildContext context) {
    final PromotionsCubit cubit = context.read<PromotionsCubit>();
    return Row(
      children: <Widget>[
        for (final PromotionStatus tab in PromotionStatus.values) ...<Widget>[
          Expanded(
            child: SelectableTile(
              selected: state.tab == tab,
              onTap: () => cubit.selectTab(tab),
              padding: const EdgeInsets.symmetric(vertical: AtaSpacing.sm),
              child: Text(
                PromoText.tab(context.l10n, tab),
                textAlign: TextAlign.center,
                style: AtaText.label.copyWith(
                  color: state.tab == tab ? AtaColors.brand : AtaColors.ink,
                ),
              ),
            ),
          ),
          if (tab != PromotionStatus.values.last)
            const SizedBox(width: AtaSpacing.sm),
        ],
      ],
    );
  }
}
