import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/features/corporate/domain/entities/trip_corporate.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of the corporate parts of the quote (`corporate`) and of
/// the trip / receipt (`corporate`).
abstract final class CorporateCheckModel {
  /// `corporate: { allowed, violations: [ … ], remainingBudget }` of a quote.
  static CorporateQuoteCheck? quoteCheck(Map<String, dynamic> json) {
    final Map<String, dynamic>? corporate = JsonReaders.object(
      json,
      'corporate',
    );
    if (corporate == null) return null;
    return CorporateQuoteCheck(
      allowed: corporate['allowed'] != false,
      violations: PolicyViolation.listFrom(corporate['violations']),
      remainingBudget: JsonReaders.optionalNumber(corporate, 'remainingBudget'),
    );
  }

  static Map<String, dynamic>? quoteCheckJson(CorporateQuoteCheck? check) =>
      check == null
      ? null
      : <String, dynamic>{
          'allowed': check.allowed,
          'violations': check.violations
              .map(
                (PolicyViolation v) => <String, dynamic>{
                  'rule': v.rule.apiValue,
                  'limit': v.limit,
                  'allowed': v.allowed,
                },
              )
              .toList(growable: false),
          'remainingBudget': check.remainingBudget,
        };

  /// `corporate: { companyName, purpose, costCenter, isGuest, guestName }`
  /// of a trip or receipt; `null` when absent.
  static TripCorporate? trip(Map<String, dynamic> json) {
    final Map<String, dynamic>? corporate = JsonReaders.object(
      json,
      'corporate',
    );
    if (corporate == null) return null;
    return TripCorporate(
      companyName: JsonReaders.string(corporate, 'companyName'),
      purpose: JsonReaders.optionalString(corporate, 'purpose'),
      costCenter: JsonReaders.optionalString(corporate, 'costCenter'),
      isGuest: corporate['isGuest'] == true,
      guestName: JsonReaders.optionalString(corporate, 'guestName'),
    );
  }

  static Map<String, dynamic>? tripJson(TripCorporate? corporate) =>
      corporate == null
      ? null
      : <String, dynamic>{
          'companyName': corporate.companyName,
          'purpose': corporate.purpose,
          'costCenter': corporate.costCenter,
          'isGuest': corporate.isGuest,
          'guestName': corporate.guestName,
        };
}
