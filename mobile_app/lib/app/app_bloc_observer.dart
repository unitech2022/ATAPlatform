import 'dart:developer' as developer;

import 'package:flutter_bloc/flutter_bloc.dart';

/// Logs cubit errors (and transitions in debug) through `dart:developer`.
class AppBlocObserver extends BlocObserver {
  const AppBlocObserver({this.logChanges = false});

  final bool logChanges;

  static const String _name = 'bloc';

  @override
  void onChange(BlocBase<dynamic> bloc, Change<dynamic> change) {
    super.onChange(bloc, change);
    if (logChanges) {
      developer.log('${bloc.runtimeType}: $change', name: _name);
    }
  }

  @override
  void onError(BlocBase<dynamic> bloc, Object error, StackTrace stackTrace) {
    developer.log(
      '${bloc.runtimeType} error',
      name: _name,
      error: error,
      stackTrace: stackTrace,
    );
    super.onError(bloc, error, stackTrace);
  }
}
