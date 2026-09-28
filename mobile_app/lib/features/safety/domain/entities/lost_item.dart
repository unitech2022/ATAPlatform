import 'package:equatable/equatable.dart';

/// `itemCategory` of a lost item report.
enum LostItemCategory {
  phone('phone'),
  wallet('wallet'),
  bag('bag'),
  keys('keys'),
  documents('documents'),
  other('other');

  const LostItemCategory(this.apiValue);

  final String apiValue;

  static LostItemCategory parse(String? value) {
    for (final LostItemCategory c in values) {
      if (c.apiValue == value) return c;
    }
    return other;
  }
}

/// `LostItemReport` (passenger) or the driver list row.
class LostItemReport extends Equatable {
  const LostItemReport({
    required this.id,
    required this.reportNumber,
    this.tripId,
    this.tripNumber = '',
    this.itemCategory = LostItemCategory.other,
    this.description = '',
    this.status = 'open',
    this.driverResponse,
    this.supportTicketId,
    this.createdAt,
  });

  final String id;
  final String reportNumber;
  final String? tripId;
  final String tripNumber;
  final LostItemCategory itemCategory;
  final String description;

  /// `open`, `driver_contacted`, `found`, `returned`, `not_found`, `closed`.
  final String status;

  /// `found` / `not_found` once the driver answered.
  final String? driverResponse;
  final String? supportTicketId;
  final DateTime? createdAt;

  bool get awaitsDriver => status == 'open' && driverResponse == null;

  LostItemReport answered({required bool found}) => LostItemReport(
    id: id,
    reportNumber: reportNumber,
    tripId: tripId,
    tripNumber: tripNumber,
    itemCategory: itemCategory,
    description: description,
    status: found ? 'found' : 'not_found',
    driverResponse: found ? 'found' : 'not_found',
    supportTicketId: supportTicketId,
    createdAt: createdAt,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    reportNumber,
    tripId,
    tripNumber,
    itemCategory,
    description,
    status,
    driverResponse,
    supportTicketId,
    createdAt,
  ];
}

/// Body of `POST /passenger/trips/{id}/lost-items`.
class LostItemDraft extends Equatable {
  const LostItemDraft({
    required this.tripId,
    required this.category,
    required this.description,
    this.contactPhone,
  });

  final String tripId;
  final LostItemCategory category;
  final String description;
  final String? contactPhone;

  @override
  List<Object?> get props => <Object?>[
    tripId,
    category,
    description,
    contactPhone,
  ];
}
