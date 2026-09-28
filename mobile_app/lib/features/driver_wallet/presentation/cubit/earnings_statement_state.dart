import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:equatable/equatable.dart';

/// Selected period and the loaded statement.
class EarningsStatementState extends Equatable {
  const EarningsStatementState({
    this.period = StatementPeriod.week,
    this.statement,
    this.loading = false,
    this.failure,
  });

  final StatementPeriod period;
  final EarningsStatement? statement;
  final bool loading;
  final Failure? failure;

  EarningsStatementState copyWith({
    StatementPeriod? period,
    EarningsStatement? statement,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => EarningsStatementState(
    period: period ?? this.period,
    statement: statement ?? this.statement,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[period, statement, loading, failure];
}
