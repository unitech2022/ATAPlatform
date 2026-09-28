import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/keypad.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/phone_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/phone_state.dart';
import 'package:ata_app/features/auth/presentation/pages/otp_page_args.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_panel.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_scaffold.dart';
import 'package:ata_app/features/auth/presentation/widgets/phone_field.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Phone-number entry with the custom keypad.
class PhonePage extends StatelessWidget {
  const PhonePage({super.key, required this.role});

  final UserRole role;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<PhoneCubit>(
      create: (_) => PhoneCubit(
        requestOtp: getIt(),
        role: role,
        language: context.localeCode,
      ),
      child: BlocListener<PhoneCubit, PhoneState>(
        listenWhen: (PhoneState previous, PhoneState current) =>
            previous.result != current.result && current.result != null,
        listener: (BuildContext context, PhoneState state) {
          context.read<PhoneCubit>().consumeResult();
          context.push(
            AppRoutes.otp,
            extra: OtpPageArgs(request: state.result!, role: role),
          );
        },
        child: AuthScaffold(
          onBack: () => context.go(AppRoutes.role),
          child: _PhonePanel(role: role),
        ),
      ),
    );
  }
}

class _PhonePanel extends StatelessWidget {
  const _PhonePanel({required this.role});

  final UserRole role;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final PhoneCubit cubit = context.read<PhoneCubit>();
    return BlocBuilder<PhoneCubit, PhoneState>(
      builder: (BuildContext context, PhoneState state) {
        return AuthPanel(
          children: <Widget>[
            const IconBox(
              icon: AtaIcons.phone,
              size: AtaSizes.iconBoxLarge,
              iconSize: AtaSizes.iconLarge,
              radius: AtaSpacing.md,
            ),
            const SizedBox(height: AtaSpacing.xl),
            Text(l10n.phoneTitle, style: AtaText.title),
            const SizedBox(height: AtaSpacing.sm),
            Text(
              role == UserRole.driver
                  ? l10n.phoneCopyDriver
                  : l10n.phoneCopyRider,
              style: AtaText.bodyMuted,
            ),
            const SizedBox(height: AtaSpacing.xl),
            PhoneField(
              digits: state.digits,
              countryCode: l10n.phoneCountryCode,
              placeholder: l10n.phonePlaceholder,
            ),
            const SizedBox(height: AtaSpacing.xl),
            Keypad(
              onDigit: cubit.addDigit,
              onDelete: cubit.deleteDigit,
              deleteLabel: l10n.keypadDelete,
              enabled: !state.submitting,
            ),
            if (state.failure != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.md),
              InlineError(message: failureText(state.failure!, l10n)),
            ],
            const SizedBox(height: AtaSpacing.xl),
            AtaButton(
              label: l10n.phoneSend,
              onPressed: state.canSubmit ? cubit.submit : null,
              loading: state.submitting,
            ),
          ],
        );
      },
    );
  }
}
