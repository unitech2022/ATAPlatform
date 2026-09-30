import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/support/domain/usecases/create_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/download_support_file.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_article.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_categories.dart';
import 'package:ata_app/features/support/domain/usecases/get_support_trips.dart';
import 'package:ata_app/features/support/domain/usecases/get_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/get_tickets.dart';
import 'package:ata_app/features/support/domain/usecases/pick_support_attachments.dart';
import 'package:ata_app/features/support/domain/usecases/rate_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/reply_to_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/search_help_articles.dart';
import 'package:ata_app/features/support/domain/usecases/send_article_feedback.dart';
import 'package:ata_app/features/support/domain/usecases/upload_support_attachment.dart';
import 'package:ata_app/features/support/domain/usecases/watch_ticket.dart';

/// F18 use cases: help center, tickets, attachments, fare disputes, CSAT.
void registerSupportUseCases() {
  getIt
    // help center
    ..registerLazySingleton<GetHelpCategories>(() => GetHelpCategories(getIt()))
    ..registerLazySingleton<SearchHelpArticles>(
      () => SearchHelpArticles(getIt()),
    )
    ..registerLazySingleton<GetHelpArticle>(() => GetHelpArticle(getIt()))
    ..registerLazySingleton<SendArticleFeedback>(
      () => SendArticleFeedback(getIt()),
    )
    // tickets
    ..registerLazySingleton<CreateTicket>(() => CreateTicket(getIt()))
    ..registerLazySingleton<GetTickets>(() => GetTickets(getIt()))
    ..registerLazySingleton<GetTicket>(() => GetTicket(getIt()))
    ..registerLazySingleton<ReplyToTicket>(() => ReplyToTicket(getIt()))
    ..registerLazySingleton<RateTicket>(() => RateTicket(getIt()))
    ..registerLazySingleton<WatchTicket>(() => WatchTicket(getIt()))
    ..registerLazySingleton<GetSupportTrips>(
      () => GetSupportTrips(getIt(), getIt()),
    )
    // attachments
    ..registerLazySingleton<PickSupportAttachments>(
      () => PickSupportAttachments(getIt()),
    )
    ..registerLazySingleton<UploadSupportAttachment>(
      () => UploadSupportAttachment(getIt()),
    )
    ..registerLazySingleton<DownloadSupportFile>(
      () => DownloadSupportFile(getIt()),
    );
}
