/// Emits the remaining seconds once per tick, ending with 0.
typedef Countdown = Stream<int> Function(int seconds);

/// Emits once per [interval] (injectable clock for cubits).
typedef Ticker = Stream<void> Function(Duration interval);

/// Default one-second countdown.
Stream<int> secondsCountdown(int seconds) => Stream<int>.periodic(
  const Duration(seconds: 1),
  (int tick) => seconds - tick - 1,
).take(seconds);

/// Default periodic ticker.
Stream<void> periodicTicker(Duration interval) =>
    Stream<void>.periodic(interval);
