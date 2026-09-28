import 'dart:async';

/// Broadcasts cross-cutting session events raised by the network layer
/// (for example a failed token refresh) so the session cubit can react.
class SessionEvents {
  final StreamController<void> _expired = StreamController<void>.broadcast();

  /// Emits whenever the API reports that the session is no longer valid.
  Stream<void> get expired => _expired.stream;

  void notifyExpired() {
    if (!_expired.isClosed) _expired.add(null);
  }

  Future<void> dispose() => _expired.close();
}
