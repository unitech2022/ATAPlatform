import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_watcher.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip_chat/data/datasources/trip_chat_remote_data_source.dart';
import 'package:ata_app/features/trip_chat/data/models/trip_message_model.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [TripChatRepository] over REST + the `/hubs/trips` `TripMessage` event.
class TripChatRepositoryImpl implements TripChatRepository {
  const TripChatRepositoryImpl({
    required this._remote,
    required this._realtime,
    this.pollInterval = const Duration(seconds: 5),
  });

  final TripChatRemoteDataSource _remote;
  final TripRealtimeDataSource _realtime;
  final Duration pollInterval;

  static String _base(ChatTarget t) => t.actor == TripActor.passenger
      ? '/passenger/trips/${t.tripId}'
      : '/driver/trips/${t.tripId}';

  @override
  Future<Either<Failure, List<TripMessage>>> getMessages(
    ChatTarget target, {
    String? after,
  }) => guard(() => _remote.messages(_base(target), after: after));

  @override
  Future<Either<Failure, TripMessage>> send(
    ChatTarget target, {
    String? body,
    String? quickReplyCode,
  }) => guard(
    () => _remote.send(_base(target), <String, dynamic>{
      'body': ?body,
      'quickReplyCode': ?quickReplyCode,
    }),
  );

  @override
  Future<Either<Failure, Unit>> markRead(ChatTarget target, String upToId) =>
      guard(() async {
        await _remote.markRead(_base(target), upToId);
        return unit;
      });

  @override
  Future<Either<Failure, List<QuickReply>>> getQuickReplies(TripActor role) =>
      guard(
        () => _remote.quickReplies(
          role == TripActor.passenger ? 'passenger' : 'driver',
        ),
      );

  @override
  Future<Either<Failure, MaskedCall>> requestCall(ChatTarget target) =>
      guard(() => _remote.call(_base(target)));

  @override
  Stream<List<TripMessage>> watchMessages(ChatTarget target) =>
      TripWatcher<List<TripMessage>>(
        realtime: _realtime.tripMessage
            .map(TripChatModels.message)
            .where((TripMessage m) => m.tripId == target.tripId)
            .map((TripMessage m) => <TripMessage>[m]),
        poll: () => _remote.messages(_base(target)),
        interval: pollInterval,
        isRealtimeConnected: () => _realtime.isConnected,
        onListen: _realtime.acquire,
        onCancel: _realtime.release,
      ).watch().where((List<TripMessage>? batch) => batch != null).cast();
}
