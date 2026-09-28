import 'package:flutter_bloc/flutter_bloc.dart';

/// Tabs of the driver dashboard.
enum DriverTab {
  overview,
  documents,
  settings;

  /// Tab named by the `tab` query parameter (`ata://driver/documents`).
  static DriverTab parse(String? value) {
    for (final DriverTab tab in values) {
      if (tab.name == value) return tab;
    }
    return overview;
  }
}

/// Selected dashboard tab.
class DriverTabsCubit extends Cubit<DriverTab> {
  DriverTabsCubit({DriverTab initial = DriverTab.overview}) : super(initial);

  void select(DriverTab tab) => emit(tab);
}
