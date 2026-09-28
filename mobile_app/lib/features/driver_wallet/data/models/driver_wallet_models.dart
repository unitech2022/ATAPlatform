import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping for the driver wallet endpoints.
abstract final class DriverWalletModels {
  static EarningsStatement statement(Map<String, dynamic> json) {
    final Map<String, dynamic> t =
        JsonReaders.object(json, 'totals') ?? const <String, dynamic>{};
    return EarningsStatement(
      from: JsonReaders.date(json, 'from') ?? DateTime.now(),
      to: JsonReaders.date(json, 'to') ?? DateTime.now(),
      totals: StatementTotals(
        trips: JsonReaders.integer(t, 'trips'),
        grossFares: JsonReaders.number(t, 'grossFares'),
        commission: JsonReaders.number(t, 'commission'),
        earnings: JsonReaders.number(t, 'earnings'),
        cashCollected: JsonReaders.number(t, 'cashCollected'),
        incentives: JsonReaders.number(t, 'incentives'),
        cancellationCompensation: JsonReaders.number(
          t,
          'cancellationCompensation',
        ),
        adjustments: JsonReaders.number(t, 'adjustments'),
        payouts: JsonReaders.number(t, 'payouts'),
        net: JsonReaders.number(t, 'net'),
      ),
      days: JsonReaders.objects(json, 'days').map(_day).toList(),
    );
  }

  static StatementDay _day(Map<String, dynamic> json) => StatementDay(
    date: JsonReaders.date(json, 'date') ?? DateTime.now(),
    trips: JsonReaders.integer(json, 'trips'),
    earnings: JsonReaders.number(json, 'earnings'),
    cashCollected: JsonReaders.number(json, 'cashCollected'),
    incentives: JsonReaders.number(json, 'incentives'),
    onlineHours: JsonReaders.number(json, 'onlineHours'),
  );

  static Payout payout(Map<String, dynamic> json) => Payout(
    id: JsonReaders.string(json, 'id'),
    payoutNumber: JsonReaders.string(json, 'payoutNumber'),
    amount: JsonReaders.number(json, 'amount'),
    status: PayoutStatus.parse(JsonReaders.optionalString(json, 'status')),
    ibanMasked: JsonReaders.optionalString(json, 'ibanMasked'),
    requestedAt: JsonReaders.date(json, 'requestedAt'),
    approvedAt: JsonReaders.date(json, 'approvedAt'),
    paidAt: JsonReaders.date(json, 'paidAt'),
    rejectedReason: JsonReaders.optionalString(json, 'rejectedReason'),
    bankReference: JsonReaders.optionalString(json, 'bankReference'),
  );

  static PayoutSummary summary(Map<String, dynamic> json) {
    final Map<String, dynamic>? pending = JsonReaders.object(
      json,
      'pendingPayout',
    );
    return PayoutSummary(
      balance: JsonReaders.number(json, 'balance'),
      cashDebt: JsonReaders.number(json, 'cashDebt'),
      availableForPayout: JsonReaders.number(json, 'availableForPayout'),
      minPayoutAmount:
          JsonReaders.optionalNumber(json, 'minPayoutAmount') ??
          PayoutSummary.defaultMinPayout,
      cashDebtLimit:
          JsonReaders.optionalNumber(json, 'cashDebtLimit') ??
          PayoutSummary.defaultCashDebtLimit,
      pendingPayout: pending == null ? null : payout(pending),
      ibanMasked: JsonReaders.optionalString(json, 'ibanMasked'),
      canRequest: json['canRequest'] == true,
      reason: JsonReaders.optionalString(json, 'reason'),
    );
  }
}
