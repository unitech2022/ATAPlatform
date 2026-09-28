import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';

/// JSON mapping for [NotificationPreferences].
class NotificationPreferencesModel extends NotificationPreferences {
  const NotificationPreferencesModel({
    super.trips,
    super.wallet,
    super.safety,
    super.offers,
  });

  factory NotificationPreferencesModel.fromJson(Map<String, dynamic> json) =>
      NotificationPreferencesModel(
        trips: json['trips'] as bool? ?? true,
        wallet: json['wallet'] as bool? ?? true,
        safety: json['safety'] as bool? ?? true,
        offers: json['offers'] as bool? ?? true,
      );

  static Map<String, dynamic> toJson(NotificationPreferences prefs) =>
      <String, dynamic>{
        'trips': prefs.trips,
        'wallet': prefs.wallet,
        'safety': prefs.safety,
        'offers': prefs.offers,
      };
}
