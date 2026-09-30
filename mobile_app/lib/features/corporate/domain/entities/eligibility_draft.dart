import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
import 'package:equatable/equatable.dart';

/// Input of `CheckCorporateEligibility`: the current request draft.
class EligibilityDraft extends Equatable {
  const EligibilityDraft({
    required this.profile,
    this.categoryCode,
    this.scheduledAt,
    this.estimatedFare,
    this.serverCheck,
  });

  final CorporateProfile? profile;
  final String? categoryCode;

  /// The booking time of a scheduled ride; `null` = now.
  final DateTime? scheduledAt;

  /// The quoted total of the category, `null` until a quote exists.
  final double? estimatedFare;

  /// The API's own evaluation (`corporate` of the quote).
  final CorporateQuoteCheck? serverCheck;

  bool get isScheduled => scheduledAt != null;

  @override
  List<Object?> get props => <Object?>[
    profile,
    categoryCode,
    scheduledAt,
    estimatedFare,
    serverCheck,
  ];
}
