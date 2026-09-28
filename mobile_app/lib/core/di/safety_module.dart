import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/safety/domain/usecases/add_trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/cancel_sos.dart';
import 'package:ata_app/features/safety/domain/usecases/create_trip_share.dart';
import 'package:ata_app/features/safety/domain/usecases/delete_trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/get_driver_lost_items.dart';
import 'package:ata_app/features/safety/domain/usecases/get_my_lost_items.dart';
import 'package:ata_app/features/safety/domain/usecases/get_my_safety_cases.dart';
import 'package:ata_app/features/safety/domain/usecases/get_pending_safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/get_safety_case.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trip_shares.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trusted_contacts.dart';
import 'package:ata_app/features/safety/domain/usecases/report_lost_item.dart';
import 'package:ata_app/features/safety/domain/usecases/respond_to_lost_item.dart';
import 'package:ata_app/features/safety/domain/usecases/respond_to_safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/revoke_trip_share.dart';
import 'package:ata_app/features/safety/domain/usecases/send_sos_location.dart';
import 'package:ata_app/features/safety/domain/usecases/submit_safety_report.dart';
import 'package:ata_app/features/safety/domain/usecases/trigger_sos.dart';
import 'package:ata_app/features/safety/domain/usecases/update_trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/watch_safety_checks.dart';
import 'package:ata_app/features/trip/domain/usecases/get_cancellation_reasons.dart';
import 'package:ata_app/features/trip/domain/usecases/get_reliability.dart';
import 'package:ata_app/features/trip/domain/usecases/mark_passenger_no_show.dart';
import 'package:ata_app/features/trip/domain/usecases/preview_cancellation.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/get_quick_replies.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/get_trip_messages.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/mark_messages_read.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/request_masked_call.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/send_trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/watch_trip_messages.dart';

/// F12 (safety, trip chat, lost items) and F14 (cancellation engine,
/// reliability) use cases.
void registerSafetyAndCancellationUseCases() {
  getIt
    // safety
    ..registerLazySingleton<GetTrustedContacts>(
      () => GetTrustedContacts(getIt()),
    )
    ..registerLazySingleton<AddTrustedContact>(() => AddTrustedContact(getIt()))
    ..registerLazySingleton<UpdateTrustedContact>(
      () => UpdateTrustedContact(getIt()),
    )
    ..registerLazySingleton<DeleteTrustedContact>(
      () => DeleteTrustedContact(getIt()),
    )
    ..registerLazySingleton<CreateTripShare>(() => CreateTripShare(getIt()))
    ..registerLazySingleton<GetTripShares>(() => GetTripShares(getIt()))
    ..registerLazySingleton<RevokeTripShare>(() => RevokeTripShare(getIt()))
    ..registerLazySingleton<TriggerSos>(() => TriggerSos(getIt()))
    ..registerLazySingleton<SendSosLocation>(() => SendSosLocation(getIt()))
    ..registerLazySingleton<CancelSos>(() => CancelSos(getIt()))
    ..registerLazySingleton<SubmitSafetyReport>(
      () => SubmitSafetyReport(getIt()),
    )
    ..registerLazySingleton<GetMySafetyCases>(() => GetMySafetyCases(getIt()))
    ..registerLazySingleton<GetSafetyCase>(() => GetSafetyCase(getIt()))
    ..registerLazySingleton<GetPendingSafetyAlert>(
      () => GetPendingSafetyAlert(getIt()),
    )
    ..registerLazySingleton<RespondToSafetyAlert>(
      () => RespondToSafetyAlert(getIt()),
    )
    ..registerLazySingleton<WatchSafetyChecks>(() => WatchSafetyChecks(getIt()))
    ..registerLazySingleton<ReportLostItem>(() => ReportLostItem(getIt()))
    ..registerLazySingleton<GetMyLostItems>(() => GetMyLostItems(getIt()))
    ..registerLazySingleton<GetDriverLostItems>(
      () => GetDriverLostItems(getIt()),
    )
    ..registerLazySingleton<RespondToLostItem>(() => RespondToLostItem(getIt()))
    // trip_chat
    ..registerLazySingleton<GetTripMessages>(() => GetTripMessages(getIt()))
    ..registerLazySingleton<SendTripMessage>(() => SendTripMessage(getIt()))
    ..registerLazySingleton<MarkMessagesRead>(() => MarkMessagesRead(getIt()))
    ..registerLazySingleton<GetQuickReplies>(() => GetQuickReplies(getIt()))
    ..registerLazySingleton<RequestMaskedCall>(() => RequestMaskedCall(getIt()))
    ..registerLazySingleton<WatchTripMessages>(() => WatchTripMessages(getIt()))
    // trip
    ..registerLazySingleton<GetCancellationReasons>(
      () => GetCancellationReasons(getIt()),
    )
    ..registerLazySingleton<PreviewCancellation>(
      () => PreviewCancellation(getIt()),
    )
    ..registerLazySingleton<MarkPassengerNoShow>(
      () => MarkPassengerNoShow(getIt()),
    )
    ..registerLazySingleton<GetReliability>(() => GetReliability(getIt()));
}
