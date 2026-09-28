import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/driver_wallet/data/models/driver_wallet_models.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';

/// `/driver/earnings/statement` and `/driver/payouts*`.
class DriverWalletRemoteDataSource {
  const DriverWalletRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _statementPath = '/driver/earnings/statement';
  static const String _payoutsPath = '/driver/payouts';
  static const String _summaryPath = '/driver/payouts/summary';

  /// `yyyy-MM-dd` (local date).
  static String dateParam(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-'
      '${date.month.toString().padLeft(2, '0')}-'
      '${date.day.toString().padLeft(2, '0')}';

  Future<EarningsStatement> statement(DateRange range) async =>
      DriverWalletModels.statement(
        await _api.get(
              _statementPath,
              query: <String, dynamic>{
                'from': dateParam(range.from),
                'to': dateParam(range.to),
              },
            )
            as Map<String, dynamic>,
      );

  Future<PayoutSummary> summary() async => DriverWalletModels.summary(
    await _api.get(_summaryPath) as Map<String, dynamic>,
  );

  Future<Payout> requestPayout({
    required double amount,
    required String idempotencyKey,
  }) async => DriverWalletModels.payout(
    await _api.post(
          _payoutsPath,
          body: <String, dynamic>{'amount': amount},
          headers: <String, dynamic>{
            ApiClient.idempotencyHeader: idempotencyKey,
          },
        )
        as Map<String, dynamic>,
  );

  Future<PageResult<Payout>> payouts({required int page}) async =>
      PageResult<Payout>.fromJson(
        await _api.get(_payoutsPath, query: <String, dynamic>{'page': page})
            as Map<String, dynamic>,
        DriverWalletModels.payout,
      );

  Future<Payout> cancel(String id) async => DriverWalletModels.payout(
    await _api.post('$_payoutsPath/$id/cancel') as Map<String, dynamic>,
  );
}
