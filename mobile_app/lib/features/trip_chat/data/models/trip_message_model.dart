import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';

/// JSON mapping of the chat objects.
abstract final class TripChatModels {
  static TripMessage message(Map<String, dynamic> json) => TripMessage(
    id: JsonReaders.string(json, 'id'),
    tripId: JsonReaders.string(json, 'tripId'),
    senderRole: JsonReaders.optionalString(json, 'senderRole') ?? 'system',
    kind: JsonReaders.optionalString(json, 'kind') ?? 'text',
    body: JsonReaders.string(json, 'body'),
    quickReplyCode: JsonReaders.optionalString(json, 'quickReplyCode'),
    isMine: json['isMine'] as bool? ?? false,
    readAt: JsonReaders.date(json, 'readAt'),
    createdAt: JsonReaders.date(json, 'createdAt'),
  );

  static QuickReply quickReply(Map<String, dynamic> json) => QuickReply(
    code: JsonReaders.string(json, 'code'),
    text: JsonReaders.string(json, 'text'),
  );

  static MaskedCall call(Map<String, dynamic> json) => MaskedCall(
    mode: JsonReaders.optionalString(json, 'mode') ?? 'unavailable',
    proxyNumber: JsonReaders.optionalString(json, 'proxyNumber'),
    pin: JsonReaders.optionalString(json, 'pin'),
    expiresAt: JsonReaders.date(json, 'expiresAt'),
  );

  static List<Map<String, dynamic>> list(Object? body) {
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    if (items is! List<dynamic>) return const <Map<String, dynamic>>[];
    return items.whereType<Map<String, dynamic>>().toList(growable: false);
  }
}
