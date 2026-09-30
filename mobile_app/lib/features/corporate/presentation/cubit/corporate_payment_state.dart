import 'package:ata_app/features/corporate/domain/entities/corporate_booking.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_eligibility.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/eligibility_draft.dart';
import 'package:equatable/equatable.dart';

/// State of [CorporatePaymentCubit]: the company profile and request draft
/// it evaluates, the typed purpose / cost center and the verdict.
class CorporatePaymentState extends Equatable {
  const CorporatePaymentState({
    this.draft = const EligibilityDraft(profile: null),
    this.selected = false,
    this.purpose = '',
    this.costCenterId,
    this.eligibility = const CorporateEligibility(),
  });

  final EligibilityDraft draft;

  /// The corporate payment is the chosen payment method.
  final bool selected;
  final String purpose;
  final String? costCenterId;
  final CorporateEligibility eligibility;

  CorporateProfile? get profile => draft.profile;

  /// The option is offered: an active member (`membership.status = active`).
  bool get available => profile?.isActive ?? false;

  /// The option can be used right now for the current category / time.
  bool get usable => available && eligibility.eligible;

  bool get purposeRequired => profile?.policy.requirePurpose ?? false;
  bool get costCenterRequired => profile?.policy.requireCostCenter ?? false;
  bool get purposeMissing => purposeRequired && purpose.trim().isEmpty;
  bool get costCenterMissing => costCenterRequired && costCenterId == null;

  /// The form is shown and the request may be sent.
  bool get showForm => selected && available;
  bool get ready =>
      showForm && eligibility.eligible && !purposeMissing && !costCenterMissing;

  CorporateBooking get booking => CorporateBooking(
    tripPurpose: purpose.trim().isEmpty ? null : purpose.trim(),
    costCenterId: costCenterId,
    ready: ready,
  );

  CorporatePaymentState copyWith({
    EligibilityDraft? draft,
    bool? selected,
    String? purpose,
    String? costCenterId,
    CorporateEligibility? eligibility,
    bool clearCostCenter = false,
  }) => CorporatePaymentState(
    draft: draft ?? this.draft,
    selected: selected ?? this.selected,
    purpose: purpose ?? this.purpose,
    costCenterId: clearCostCenter ? null : costCenterId ?? this.costCenterId,
    eligibility: eligibility ?? this.eligibility,
  );

  @override
  List<Object?> get props => <Object?>[
    draft,
    selected,
    purpose,
    costCenterId,
    eligibility,
  ];
}
