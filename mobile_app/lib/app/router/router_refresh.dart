import 'dart:async';

import 'package:flutter/foundation.dart';

/// Turns one or more streams (cubit state streams) into a [Listenable] for
/// `GoRouter.refreshListenable`.
class StreamRefreshListenable extends ChangeNotifier {
  StreamRefreshListenable(Stream<Object?> stream)
    : this.merge(<Stream<Object?>>[stream]);

  StreamRefreshListenable.merge(List<Stream<Object?>> streams) {
    for (final Stream<Object?> stream in streams) {
      _subscriptions.add(stream.listen((_) => notifyListeners()));
    }
  }

  final List<StreamSubscription<Object?>> _subscriptions =
      <StreamSubscription<Object?>>[];

  @override
  void dispose() {
    for (final StreamSubscription<Object?> subscription in _subscriptions) {
      subscription.cancel();
    }
    super.dispose();
  }
}
