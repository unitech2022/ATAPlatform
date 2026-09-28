import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:equatable/equatable.dart';

/// Whether a user is signed in.
enum SessionStatus { unknown, unauthenticated, authenticated }

/// App-wide authentication state consumed by the router.
class SessionState extends Equatable {
  const SessionState({required this.status, this.session});

  const SessionState.unknown() : this(status: SessionStatus.unknown);
  const SessionState.unauthenticated()
    : this(status: SessionStatus.unauthenticated);
  const SessionState.authenticated(AuthSession session)
    : this(status: SessionStatus.authenticated, session: session);

  final SessionStatus status;
  final AuthSession? session;

  bool get isAuthenticated =>
      status == SessionStatus.authenticated && session != null;
  UserRole? get activeRole => session?.activeRole;

  /// Riders must give a name and accept the terms once.
  bool get needsRiderOnboarding =>
      isAuthenticated &&
      session!.activeRole == UserRole.passenger &&
      !session!.user.hasAcceptedTerms;

  bool get isDriverPending =>
      isAuthenticated && session!.isDriver && !session!.isDriverApproved;

  DriverApplicationStatus? get driverStatus =>
      session?.driver?.applicationStatus;

  @override
  List<Object?> get props => <Object?>[status, session];
}
