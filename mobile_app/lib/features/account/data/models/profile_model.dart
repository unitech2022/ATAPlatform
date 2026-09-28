import 'package:ata_app/features/account/domain/entities/profile.dart';
import 'package:ata_app/features/auth/data/models/driver_summary_model.dart';
import 'package:ata_app/features/auth/data/models/user_model.dart';

/// JSON mapping for [Profile].
class ProfileModel extends Profile {
  const ProfileModel({required super.user, super.passenger, super.driver});

  factory ProfileModel.fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic>? passenger =
        json['passenger'] as Map<String, dynamic>?;
    final Map<String, dynamic>? driver =
        json['driver'] as Map<String, dynamic>?;
    return ProfileModel(
      user: UserModel.fromJson(json['user'] as Map<String, dynamic>),
      passenger: passenger == null
          ? null
          : PassengerProfile(
              ratingAvg: (passenger['ratingAvg'] as num?)?.toDouble() ?? 5,
              ratingCount: (passenger['ratingCount'] as num?)?.toInt() ?? 0,
              preferFemaleDriver:
                  passenger['preferFemaleDriver'] as bool? ?? false,
              defaultPaymentMethod:
                  passenger['defaultPaymentMethod'] as String? ?? 'cash',
              memberSince: '${passenger['memberSince'] ?? ''}',
            ),
      driver: driver == null ? null : DriverSummaryModel.fromJson(driver),
    );
  }
}
