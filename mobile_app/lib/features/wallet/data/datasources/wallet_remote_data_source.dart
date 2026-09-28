import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/payments/data/datasources/payments_remote_data_source.dart';
import 'package:ata_app/features/wallet/data/models/wallet_summary_model.dart';
import 'package:ata_app/features/wallet/data/models/wallet_transaction_model.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';

/// `/wallet` endpoints.
class WalletRemoteDataSource {
  const WalletRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _walletPath = '/wallet';
  static const String _transactionsPath = '/wallet/transactions';
  static const String _topUpsPath = '/wallet/topups';
  static const String _kindParam = 'kind';

  Future<WalletSummaryModel> wallet({
    WalletKind kind = WalletKind.passenger,
  }) async => WalletSummaryModel.fromJson(
    await _api.get(_walletPath, query: _kindQuery(kind))
        as Map<String, dynamic>,
  );

  Future<PageResult<WalletTransactionModel>> transactions({
    required int page,
  }) async => PageResult<WalletTransactionModel>.fromJson(
    await _api.get(_transactionsPath, query: <String, dynamic>{'page': page})
        as Map<String, dynamic>,
    WalletTransactionModel.fromJson,
  );

  /// `POST /wallet/topups` (`?kind=driver` for the driver wallet).
  Future<TopUpResultModel> topUp(
    TopUpParams params, {
    required String idempotencyKey,
  }) async => TopUpResultModel.fromJson(
    await _api.post(
          params.kind == WalletKind.driver
              ? '$_topUpsPath?$_kindParam=${params.kind.apiValue}'
              : _topUpsPath,
          body: <String, dynamic>{
            'amount': params.amount,
            'method': params.method.apiValue,
            'paymentMethodId': ?params.paymentMethodId,
            if (params.method == TopUpMethod.card)
              'returnUrl': PaymentsRemoteDataSource.returnUrl,
          },
          headers: <String, dynamic>{
            ApiClient.idempotencyHeader: idempotencyKey,
          },
        )
        as Map<String, dynamic>,
  );

  static Map<String, dynamic>? _kindQuery(WalletKind kind) =>
      kind == WalletKind.driver
      ? <String, dynamic>{_kindParam: kind.apiValue}
      : null;
}
