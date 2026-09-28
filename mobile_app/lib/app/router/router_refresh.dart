import 'dart:async';

import 'package:flutter/foundation.dart';

/// Turns a stream (a cubit's state stream) into a [Listenable] for
/// `GoRouter.refreshListenable`.
class StreamRefreshListenable extends ChangeNotifier {
  StreamRefreshListenable(Stream<Object?> stream) {
    _subscription = stream.listen((_) => notifyListeners());
  }

  late final StreamSubscription<Object?> _subscription;

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }
}
