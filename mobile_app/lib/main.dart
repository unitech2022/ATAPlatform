import 'package:ata_app/app/app_bloc_observer.dart';
import 'package:ata_app/app/bootstrap.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  Bloc.observer = const AppBlocObserver(logChanges: kDebugMode);
  await configureDependencies();
  runApp(bootstrapApp());
}
