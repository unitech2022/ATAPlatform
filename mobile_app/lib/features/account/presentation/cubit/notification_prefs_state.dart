import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:equatable/equatable.dart';

/// State of the notification-preferences panel.
class NotificationPrefsState extends Equatable {
  const NotificationPrefsState({
    this.preferences = const NotificationPreferences(),
    this.loading = false,
    this.saving = false,
    this.failure,
  });

  final NotificationPreferences preferences;
  final bool loading;
  final bool saving;
  final Failure? failure;

  NotificationPrefsState copyWith({
    NotificationPreferences? preferences,
    bool? loading,
    bool? saving,
    Failure? failure,
    bool clearFailure = false,
  }) => NotificationPrefsState(
    preferences: preferences ?? this.preferences,
    loading: loading ?? this.loading,
    saving: saving ?? this.saving,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[preferences, loading, saving, failure];
}
