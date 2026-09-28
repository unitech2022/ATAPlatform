import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/account/domain/usecases/register_push_device.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/cancel_payout.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_payouts.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/request_payout.dart';
import 'package:ata_app/features/notifications/domain/usecases/mark_notification_opened.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_incoming_notifications.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_opened_notifications.dart';
import 'package:ata_app/features/payments/domain/usecases/add_payment_method.dart';
import 'package:ata_app/features/payments/domain/usecases/get_payment_methods.dart';
import 'package:ata_app/features/payments/domain/usecases/get_trip_receipt.dart';
import 'package:ata_app/features/payments/domain/usecases/remove_payment_method.dart';
import 'package:ata_app/features/payments/domain/usecases/set_default_payment_method.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_with_method.dart';

/// F11 (payments, driver wallet) and F13 (push, deep links) use cases.
void registerPaymentAndPushUseCases() {
  getIt
    // payments
    ..registerLazySingleton<GetPaymentMethods>(() => GetPaymentMethods(getIt()))
    ..registerLazySingleton<AddPaymentMethod>(
      () => AddPaymentMethod(getIt(), getIt()),
    )
    ..registerLazySingleton<SetDefaultPaymentMethod>(
      () => SetDefaultPaymentMethod(getIt()),
    )
    ..registerLazySingleton<RemovePaymentMethod>(
      () => RemovePaymentMethod(getIt()),
    )
    ..registerLazySingleton<GetTripReceipt>(() => GetTripReceipt(getIt()))
    ..registerLazySingleton<TopUpWithMethod>(() => TopUpWithMethod(getIt()))
    // driver wallet
    ..registerLazySingleton<GetEarningsStatement>(
      () => GetEarningsStatement(getIt()),
    )
    ..registerLazySingleton<GetPayoutSummary>(() => GetPayoutSummary(getIt()))
    ..registerLazySingleton<RequestPayout>(() => RequestPayout(getIt()))
    ..registerLazySingleton<GetPayouts>(() => GetPayouts(getIt()))
    ..registerLazySingleton<CancelPayout>(() => CancelPayout(getIt()))
    // push & notifications
    ..registerLazySingleton<RegisterPushDevice>(
      () => RegisterPushDevice(getIt()),
    )
    ..registerLazySingleton<MarkNotificationOpened>(
      () => MarkNotificationOpened(getIt()),
    )
    ..registerLazySingleton<WatchIncomingNotifications>(
      () => WatchIncomingNotifications(getIt()),
    )
    ..registerLazySingleton<WatchOpenedNotifications>(
      () => WatchOpenedNotifications(getIt()),
    );
}
