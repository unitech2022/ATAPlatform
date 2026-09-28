import 'dart:async';

import 'package:ata_app/core/utils/countdown.dart';

/// Merges realtime hub events with periodic polling into one stream.
///
/// On listen it polls once, then forwards every realtime value and polls
/// every [interval] (only every [connectedBackoff]th tick while the hub is
/// connected). Consecutive equal values are skipped, so consumers only see
/// changes. `null` means "nothing active".
class TripWatcher<T> {
  const TripWatcher({
    required this.realtime,
    required this.poll,
    this.interval = const Duration(seconds: 5),
    this.isRealtimeConnected,
    this.onListen,
    this.onCancel,
    this.ticker = periodicTicker,
  });

  final Stream<T?> realtime;
  final Future<T?> Function() poll;
  final Duration interval;
  final bool Function()? isRealtimeConnected;
  final Future<void> Function()? onListen;
  final Future<void> Function()? onCancel;
  final Ticker ticker;

  /// Poll every Nth tick while the hub is connected.
  static const int connectedBackoff = 6;

  Stream<T?> watch() {
    late final StreamController<T?> controller;
    StreamSubscription<T?>? realtimeSub;
    StreamSubscription<void>? tickerSub;
    bool emitted = false;
    T? last;
    int ticks = 0;

    void emit(T? value) {
      if (controller.isClosed) return;
      if (emitted && value == last) return;
      emitted = true;
      last = value;
      controller.add(value);
    }

    Future<void> runPoll() async {
      try {
        emit(await poll());
      } on Object {
        // Keep the last known value; the next tick retries.
      }
    }

    controller = StreamController<T?>(
      onListen: () async {
        await onListen?.call();
        realtimeSub = realtime.listen(emit);
        tickerSub = ticker(interval).listen((_) {
          ticks++;
          final bool connected = isRealtimeConnected?.call() ?? false;
          if (!connected || ticks % connectedBackoff == 0) runPoll();
        });
        await runPoll();
      },
      onCancel: () async {
        await realtimeSub?.cancel();
        await tickerSub?.cancel();
        await onCancel?.call();
      },
    );
    return controller.stream;
  }
}

/// Merges several streams into one (order of arrival).
Stream<T> mergeStreams<T>(List<Stream<T>> streams) {
  late final StreamController<T> controller;
  final List<StreamSubscription<T>> subscriptions = <StreamSubscription<T>>[];
  controller = StreamController<T>(
    onListen: () {
      for (final Stream<T> stream in streams) {
        subscriptions.add(stream.listen(controller.add));
      }
    },
    onCancel: () async {
      for (final StreamSubscription<T> subscription in subscriptions) {
        await subscription.cancel();
      }
    },
  );
  return controller.stream;
}
