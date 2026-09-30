import 'package:flutter_bloc/flutter_bloc.dart';

/// Tabs of the driver's scheduled-rides page.
enum ScheduledTab { market, mine }

/// Selected tab of `/driver/scheduled`.
class ScheduledTabCubit extends Cubit<ScheduledTab> {
  ScheduledTabCubit({ScheduledTab initial = ScheduledTab.market})
    : super(initial);

  void select(ScheduledTab tab) => emit(tab);
}
