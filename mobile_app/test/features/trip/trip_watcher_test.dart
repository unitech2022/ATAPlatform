import 'dart:async';

import 'package:ata_app/features/trip/data/datasources/trip_watcher.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test(
    'polls first, then merges realtime values and skips duplicates',
    () async {
      final StreamController<String?> realtime = StreamController<String?>();
      final StreamController<void> ticks = StreamController<void>();
      int polls = 0;
      final List<String?> pollResults = <String?>['a', 'a', 'c'];
      int acquired = 0;
      int released = 0;

      final TripWatcher<String> watcher = TripWatcher<String>(
        realtime: realtime.stream,
        poll: () async => pollResults[polls++ % pollResults.length],
        ticker: (_) => ticks.stream,
        onListen: () async => acquired++,
        onCancel: () async => released++,
      );

      final List<String?> seen = <String?>[];
      final StreamSubscription<String?> sub = watcher.watch().listen(seen.add);
      await Future<void>.delayed(Duration.zero);
      expect(seen, <String?>['a']);
      expect(acquired, 1);

      realtime.add('a');
      realtime.add('b');
      await Future<void>.delayed(Duration.zero);
      expect(seen, <String?>['a', 'b']);

      ticks.add(null); // poll -> 'a'
      await Future<void>.delayed(Duration.zero);
      ticks.add(null); // poll -> 'c'
      await Future<void>.delayed(Duration.zero);
      expect(seen, <String?>['a', 'b', 'a', 'c']);

      await sub.cancel();
      expect(released, 1);
      await realtime.close();
      await ticks.close();
    },
  );

  test('polling backs off while the hub is connected', () async {
    final StreamController<void> ticks = StreamController<void>();
    int polls = 0;
    final TripWatcher<int> watcher = TripWatcher<int>(
      realtime: const Stream<int?>.empty(),
      poll: () async => polls++,
      ticker: (_) => ticks.stream,
      isRealtimeConnected: () => true,
    );
    final StreamSubscription<int?> sub = watcher.watch().listen((_) {});
    await Future<void>.delayed(Duration.zero);
    for (int i = 0; i < TripWatcher.connectedBackoff; i++) {
      ticks.add(null);
    }
    await Future<void>.delayed(Duration.zero);
    expect(polls, 2); // initial poll + the sixth tick
    await sub.cancel();
    await ticks.close();
  });

  test('mergeStreams forwards both sources', () async {
    final StreamController<int> a = StreamController<int>();
    final StreamController<int> b = StreamController<int>();
    final List<int> seen = <int>[];
    final StreamSubscription<int> sub = mergeStreams<int>(<Stream<int>>[
      a.stream,
      b.stream,
    ]).listen(seen.add);
    a.add(1);
    b.add(2);
    await Future<void>.delayed(Duration.zero);
    expect(seen, <int>[1, 2]);
    await sub.cancel();
    await a.close();
    await b.close();
  });
}
