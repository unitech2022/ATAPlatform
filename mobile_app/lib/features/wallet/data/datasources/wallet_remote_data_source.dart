import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/wallet/data/models/wallet_summary_model.dart';
import 'package:ata_app/features/wallet/data/models/wallet_transaction_model.dart';

/// `/wallet` endpoints.
class WalletRemoteDataSource {
  const WalletRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _walletPath = '/wallet';
  static const String _transactionsPath = '/wallet/transactions';
  static const String _topUpsPath = '/wallet/topups';
  static const String sandboxMethod = 'sandbox';

  Future<WalletSummaryModel> wallet() async => WalletSummaryModel.fromJson(
    await _api.get(_walletPath) as Map<String, dynamic>,
  );

  Future<PageResult<WalletTransactionModel>> transactions({
    required int page,
  }) async => PageResult<WalletTransactionModel>.fromJson(
    await _api.get(_transactionsPath, query: <String, dynamic>{'page': page})
        as Map<String, dynamic>,
    WalletTransactionModel.fromJson,
  );

  Future<TopUpResultModel> topUp({
    required double amount,
    required String idempotencyKey,
  }) async => TopUpResultModel.fromJson(
    await _api.post(
          _topUpsPath,
          body: <String, dynamic>{'amount': amount, 'method': sandboxMethod},
          headers: <String, dynamic>{
            ApiClient.idempotencyHeader: idempotencyKey,
          },
        )
        as Map<String, dynamic>,
  );
}
