import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip_cancellation.dart';

/// JSON mapping of `GET /catalog/cancellation-reasons` rows.
abstract final class CancellationReasonModel {
  static CancellationReason fromJson(Map<String, dynamic> json) {
    final Object? stages = json['stages'];
    return CancellationReason(
      code: JsonReaders.string(json, 'code'),
      name:
          JsonReaders.optionalString(json, 'name') ??
          JsonReaders.string(json, 'code'),
      requiresNote: json['requiresNote'] as bool? ?? false,
      isExcusable: json['isExcusable'] as bool? ?? false,
      isEmergency: json['isEmergency'] as bool? ?? false,
      stages: stages is List<dynamic>
          ? stages.map((dynamic e) => '$e').toList(growable: false)
          : null,
    );
  }

  static List<CancellationReason> listFromJson(Object? body) {
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    if (items is! List<dynamic>) return const <CancellationReason>[];
    return items
        .whereType<Map<String, dynamic>>()
        .map(fromJson)
        .toList(growable: false);
  }
}

/// JSON mapping of the cancel preview (passenger and driver shapes).
abstract final class CancelPreviewModel {
  static CancelPreview fromJson(Map<String, dynamic> json) => CancelPreview(
    stage: JsonReaders.string(json, 'stage'),
    bookingType: JsonReaders.optionalString(json, 'bookingType') ?? 'now',
    fee: JsonReaders.number(json, 'fee'),
    penaltyPoints: JsonReaders.integer(json, 'penaltyPoints'),
    isFree: json['isFree'] as bool? ?? JsonReaders.number(json, 'fee') == 0,
    freeUntil: JsonReaders.date(json, 'freeUntil'),
    requiresReview: json['requiresReview'] as bool? ?? false,
    message: JsonReaders.string(json, 'message'),
  );
}

/// JSON mapping of `Trip.cancellation`.
abstract final class TripCancellationModel {
  static TripCancellation fromJson(Map<String, dynamic> json) =>
      TripCancellation(
        stage: JsonReaders.string(json, 'stage'),
        reasonCode: JsonReaders.string(json, 'reasonCode'),
        reasonName: JsonReaders.string(json, 'reasonName'),
        atFault: JsonReaders.optionalString(json, 'atFault') ?? 'none',
        fee: JsonReaders.optionalNumber(json, 'fee'),
        feeCharged: JsonReaders.optionalNumber(json, 'feeCharged'),
        feeStatus: JsonReaders.optionalString(json, 'feeStatus') ?? 'none',
        excuseStatus:
            JsonReaders.optionalString(json, 'excuseStatus') ??
            'not_applicable',
        compensation: JsonReaders.optionalNumber(json, 'compensation'),
      );

  static Map<String, dynamic> toJson(TripCancellation c) => <String, dynamic>{
    'stage': c.stage,
    'reasonCode': c.reasonCode,
    'reasonName': c.reasonName,
    'atFault': c.atFault,
    'fee': c.fee,
    'feeCharged': c.feeCharged,
    'feeStatus': c.feeStatus,
    'excuseStatus': c.excuseStatus,
    'compensation': c.compensation,
  };
}
