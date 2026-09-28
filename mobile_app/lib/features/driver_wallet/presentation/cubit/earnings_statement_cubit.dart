import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/earnings_statement_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Earnings statement with the today / week / month filter.
class EarningsStatementCubit extends Cubit<EarningsStatementState> {
  EarningsStatementCubit({
    required this._getStatement,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const EarningsStatementState());

  final GetEarningsStatement _getStatement;
  final DateTime Function() _now;

  Future<void> load() => select(state.period);

  Future<void> select(StatementPeriod period) async {
    emit(state.copyWith(period: period, loading: true, clearFailure: true));
    final result = await _getStatement(period.rangeAt(_now()));
    if (state.period != period) return;
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (statement) => state.copyWith(loading: false, statement: statement),
      ),
    );
  }
}
