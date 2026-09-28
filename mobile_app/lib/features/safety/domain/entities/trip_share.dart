import 'package:equatable/equatable.dart';

/// `channel` of `POST /safety/trips/{tripId}/shares`.
enum ShareChannel {
  link('link'),
  sms('sms');

  const ShareChannel(this.apiValue);

  final String apiValue;
}

/// A public tracking link of a trip (`/t/{token}`).
class TripShare extends Equatable {
  const TripShare({
    required this.id,
    required this.url,
    this.channel = 'link',
    this.trustedContactId,
    this.trustedContactName,
    this.viewCount = 0,
    this.expiresAt,
    this.revokedAt,
    this.createdAt,
  });

  final String id;
  final String url;

  /// `link`, `sms` or `auto`.
  final String channel;
  final String? trustedContactId;
  final String? trustedContactName;
  final int viewCount;
  final DateTime? expiresAt;
  final DateTime? revokedAt;
  final DateTime? createdAt;

  bool get isRevoked => revokedAt != null;

  TripShare revoked(DateTime at) => TripShare(
    id: id,
    url: url,
    channel: channel,
    trustedContactId: trustedContactId,
    trustedContactName: trustedContactName,
    viewCount: viewCount,
    expiresAt: expiresAt,
    revokedAt: at,
    createdAt: createdAt,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    url,
    channel,
    trustedContactId,
    trustedContactName,
    viewCount,
    expiresAt,
    revokedAt,
    createdAt,
  ];
}
