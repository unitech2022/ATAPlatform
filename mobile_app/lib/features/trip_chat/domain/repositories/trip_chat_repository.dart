import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:fpdart/fpdart.dart';

/// In-trip chat and masked calls (`docs/09` §F12.4 / §F12.7).
abstract interface class TripChatRepository {
  Future<Either<Failure, List<TripMessage>>> getMessages(
    ChatTarget target, {
    String? after,
  });

  /// Exactly one of [body] / [quickReplyCode]; `409 chat_closed` after the
  /// trip.
  Future<Either<Failure, TripMessage>> send(
    ChatTarget target, {
    String? body,
    String? quickReplyCode,
  });

  Future<Either<Failure, Unit>> markRead(ChatTarget target, String upToId);
  Future<Either<Failure, List<QuickReply>>> getQuickReplies(TripActor role);
  Future<Either<Failure, MaskedCall>> requestCall(ChatTarget target);

  /// Hub `TripMessage` events merged with a 5 s polling fallback; each
  /// emission is a batch of new or updated messages.
  Stream<List<TripMessage>> watchMessages(ChatTarget target);
}
