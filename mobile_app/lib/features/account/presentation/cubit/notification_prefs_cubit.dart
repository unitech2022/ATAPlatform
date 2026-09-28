import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:ata_app/features/account/domain/usecases/get_notification_preferences.dart';
import 'package:ata_app/features/account/domain/usecases/update_notification_preferences.dart';
import 'package:ata_app/features/account/presentation/cubit/notification_prefs_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Which notification category is being toggled.
enum NotificationChannel { trips, wallet, safety, offers }

/// Loads and saves notification preferences with optimistic toggles.
class NotificationPrefsCubit extends Cubit<NotificationPrefsState> {
  NotificationPrefsCubit({
    required this._getPreferences,
    required this._updatePreferences,
  }) : super(const NotificationPrefsState());

  final GetNotificationPreferences _getPreferences;
  final UpdateNotificationPreferences _updatePreferences;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getPreferences(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (prefs) => state.copyWith(loading: false, preferences: prefs),
      ),
    );
  }

  Future<void> toggle(NotificationChannel channel) async {
    final NotificationPreferences previous = state.preferences;
    final NotificationPreferences next = switch (channel) {
      NotificationChannel.trips => previous.copyWith(trips: !previous.trips),
      NotificationChannel.wallet => previous.copyWith(wallet: !previous.wallet),
      NotificationChannel.safety => previous.copyWith(safety: !previous.safety),
      NotificationChannel.offers => previous.copyWith(offers: !previous.offers),
    };
    emit(state.copyWith(preferences: next, saving: true, clearFailure: true));
    final result = await _updatePreferences(next);
    emit(
      result.fold(
        (failure) => state.copyWith(
          saving: false,
          preferences: previous,
          failure: failure,
        ),
        (saved) => state.copyWith(saving: false, preferences: saved),
      ),
    );
  }

  bool isEnabled(NotificationChannel channel) => switch (channel) {
    NotificationChannel.trips => state.preferences.trips,
    NotificationChannel.wallet => state.preferences.wallet,
    NotificationChannel.safety => state.preferences.safety,
    NotificationChannel.offers => state.preferences.offers,
  };
}
