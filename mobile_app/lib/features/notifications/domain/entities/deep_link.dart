import 'package:equatable/equatable.dart';

/// A parsed `ata://` link: the go_router location to open and whether the
/// notifications sheet should be shown on top of it.
class DeepLink extends Equatable {
  const DeepLink({required this.route, this.opensInbox = false});

  final String route;
  final bool opensInbox;

  @override
  List<Object?> get props => <Object?>[route, opensInbox];
}
