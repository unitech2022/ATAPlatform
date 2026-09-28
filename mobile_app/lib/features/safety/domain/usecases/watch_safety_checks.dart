import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';

/// "Are you OK?" prompts from the hub (`SafetyCheck`) and foreground
/// `safety.check` pushes.
class WatchSafetyChecks {
  const WatchSafetyChecks(this._repository);

  final SafetyRepository _repository;

  Stream<SafetyAlert> call() => _repository.watchSafetyChecks();
}
