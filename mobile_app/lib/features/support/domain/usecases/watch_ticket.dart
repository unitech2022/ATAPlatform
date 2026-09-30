import 'package:ata_app/features/support/domain/repositories/support_repository.dart';

/// Ids of the tickets updated live (`SupportTicketUpdated`); the cubits add
/// a 15 s poll on top while a ticket is open.
class WatchTicket {
  const WatchTicket(this._repository);

  final SupportRepository _repository;

  Stream<String> call() => _repository.watchTicketUpdates();
}
