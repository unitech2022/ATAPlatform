import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:equatable/equatable.dart';

/// Going online was refused because the cash debt exceeds the limit
/// (`403 cash_debt_limit_exceeded` with `details { cashDebt, limit }`).
class CashDebtBlock extends Equatable {
  const CashDebtBlock({this.cashDebt, this.limit});

  final double? cashDebt;
  final double? limit;

  @override
  List<Object?> get props => <Object?>[cashDebt, limit];
}

/// State of the online/offline toggle.
class OnlineStatusState extends Equatable {
  const OnlineStatusState({
    this.isOnline = false,
    this.canGoOnline = true,
    this.updating = false,
    this.failure,
    this.debtBlock,
    this.restriction,
  });

  final bool isOnline;
  final bool canGoOnline;
  final bool updating;
  final Failure? failure;

  /// Set while the driver cannot go online until the debt is settled.
  final CashDebtBlock? debtBlock;

  /// Reliability restriction (`403 account_restricted`, F14).
  final AccountRestriction? restriction;

  OnlineStatusState copyWith({
    bool? isOnline,
    bool? canGoOnline,
    bool? updating,
    Failure? failure,
    CashDebtBlock? debtBlock,
    bool clearFailure = false,
    AccountRestriction? restriction,
    bool clearDebtBlock = false,
    bool clearRestriction = false,
  }) => OnlineStatusState(
    isOnline: isOnline ?? this.isOnline,
    canGoOnline: canGoOnline ?? this.canGoOnline,
    updating: updating ?? this.updating,
    failure: clearFailure ? null : failure ?? this.failure,
    debtBlock: clearDebtBlock ? null : debtBlock ?? this.debtBlock,
    restriction: clearRestriction ? null : restriction ?? this.restriction,
  );

  @override
  List<Object?> get props => <Object?>[
    isOnline,
    canGoOnline,
    updating,
    failure,
    debtBlock,
    restriction,
  ];
}
