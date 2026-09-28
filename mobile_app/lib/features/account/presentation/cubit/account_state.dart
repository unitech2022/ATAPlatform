import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/account/domain/entities/profile.dart';
import 'package:equatable/equatable.dart';

/// State of the account page (profile card).
class AccountState extends Equatable {
  const AccountState({this.profile, this.loading = false, this.failure});

  final Profile? profile;
  final bool loading;
  final Failure? failure;

  AccountState copyWith({
    Profile? profile,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => AccountState(
    profile: profile ?? this.profile,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[profile, loading, failure];
}
