import 'package:flutter_bloc/flutter_bloc.dart';

/// Tabs of the driver dashboard.
enum DriverTab { overview, documents, settings }

/// Selected dashboard tab.
class DriverTabsCubit extends Cubit<DriverTab> {
  DriverTabsCubit() : super(DriverTab.overview);

  void select(DriverTab tab) => emit(tab);
}
