import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/trip_chat/data/models/trip_message_model.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';

/// Chat / call endpoints under `/passenger/trips/{id}` and
/// `/driver/trips/{id}`.
class TripChatRemoteDataSource {
  const TripChatRemoteDataSource(this._api);

  final ApiClient _api;

  /// [base] is `/passenger/trips/{id}` or `/driver/trips/{id}`.
  Future<List<TripMessage>> messages(String base, {String? after}) async =>
      TripChatModels.list(
        await _api.get(
          '$base/messages',
          query: <String, dynamic>{'after': ?after},
        ),
      ).map(TripChatModels.message).toList(growable: false);

  Future<TripMessage> send(String base, Map<String, dynamic> body) async =>
      TripChatModels.message(
        await _api.post('$base/messages', body: body) as Map<String, dynamic>,
      );

  Future<void> markRead(String base, String upToId) => _api.post(
    '$base/messages/read',
    body: <String, dynamic>{'upToId': upToId},
  );

  Future<MaskedCall> call(String base) async => TripChatModels.call(
    await _api.post('$base/call') as Map<String, dynamic>,
  );

  Future<List<QuickReply>> quickReplies(String role) async =>
      TripChatModels.list(
        await _api.get(
          '/catalog/chat-quick-replies',
          query: <String, dynamic>{'role': role},
        ),
      ).map(TripChatModels.quickReply).toList(growable: false);
}
