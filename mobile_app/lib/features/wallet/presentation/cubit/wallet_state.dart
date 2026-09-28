import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';
import 'package:equatable/equatable.dart';

/// State of the wallet page.
class WalletState extends Equatable {
  const WalletState({this.wallet, this.loading = false, this.failure});

  final WalletSummary? wallet;
  final bool loading;
  final Failure? failure;

  double get balance => wallet?.balance ?? 0;

  WalletState copyWith({
    WalletSummary? wallet,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => WalletState(
    wallet: wallet ?? this.wallet,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[wallet, loading, failure];
}
