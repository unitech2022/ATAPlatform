/// `type` of a support ticket (`docs/11` §F18.1).
enum TicketType {
  tripIssue('trip_issue'),
  paymentIssue('payment_issue'),
  lostItem('lost_item'),
  safety('safety'),
  account('account'),
  other('other');

  const TicketType(this.apiValue);

  final String apiValue;

  /// `tripId` is mandatory for these types.
  bool get needsTrip =>
      this == tripIssue || this == paymentIssue || this == lostItem;

  static TicketType? tryParse(String? value) {
    for (final TicketType t in values) {
      if (t.apiValue == value) return t;
    }
    return null;
  }

  static TicketType parse(String? value) => tryParse(value) ?? other;
}

/// `status` of a ticket.
enum TicketStatus {
  open('open'),
  pendingUser('pending_user'),
  inProgress('in_progress'),
  resolved('resolved'),
  closed('closed');

  const TicketStatus(this.apiValue);

  final String apiValue;

  bool get isClosed => this == closed;

  /// Resolved or closed: the user may rate the support (CSAT).
  bool get isFinished => this == resolved || this == closed;

  static TicketStatus parse(String? value) {
    for (final TicketStatus s in values) {
      if (s.apiValue == value) return s;
    }
    return open;
  }
}

/// `status` filter of `GET /support/tickets` (`open` = everything not
/// closed).
enum TicketFilter {
  open('open'),
  closed('closed');

  const TicketFilter(this.apiValue);

  final String apiValue;
}
