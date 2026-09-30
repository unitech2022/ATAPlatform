import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized labels of the F18 enums.
abstract final class SupportText {
  static String ticketType(AppLocalizations l10n, TicketType type) =>
      switch (type) {
        TicketType.tripIssue => l10n.ticketTypeTripIssue,
        TicketType.paymentIssue => l10n.ticketTypePaymentIssue,
        TicketType.lostItem => l10n.ticketTypeLostItem,
        TicketType.safety => l10n.ticketTypeSafety,
        TicketType.account => l10n.ticketTypeAccount,
        TicketType.other => l10n.ticketTypeOther,
      };

  /// Plain status text: no promised response time.
  static String ticketStatus(AppLocalizations l10n, TicketStatus status) =>
      switch (status) {
        TicketStatus.open => l10n.ticketStatusOpen,
        TicketStatus.pendingUser => l10n.ticketStatusPendingUser,
        TicketStatus.inProgress => l10n.ticketStatusInProgress,
        TicketStatus.resolved => l10n.ticketStatusResolved,
        TicketStatus.closed => l10n.ticketStatusClosed,
      };

  static String ticketBanner(AppLocalizations l10n, TicketStatus status) =>
      switch (status) {
        TicketStatus.open => l10n.ticketBannerOpen,
        TicketStatus.pendingUser => l10n.ticketBannerPendingUser,
        TicketStatus.inProgress => l10n.ticketBannerInProgress,
        TicketStatus.resolved => l10n.ticketBannerResolved,
        TicketStatus.closed => l10n.ticketBannerClosed,
      };

  static String disputeReason(AppLocalizations l10n, DisputeReason reason) =>
      switch (reason) {
        DisputeReason.overcharged => l10n.disputeReasonOvercharged,
        DisputeReason.routeLonger => l10n.disputeReasonRouteLonger,
        DisputeReason.waitingCharged => l10n.disputeReasonWaitingCharged,
        DisputeReason.cancellationFee => l10n.disputeReasonCancellationFee,
        DisputeReason.promoNotApplied => l10n.disputeReasonPromoNotApplied,
        DisputeReason.other => l10n.disputeReasonOther,
      };

  static String disputeStatus(AppLocalizations l10n, DisputeStatus status) =>
      switch (status) {
        DisputeStatus.open => l10n.disputeStatusOpen,
        DisputeStatus.underReview => l10n.disputeStatusUnderReview,
        DisputeStatus.approved => l10n.disputeStatusApproved,
        DisputeStatus.partiallyApproved => l10n.disputeStatusPartiallyApproved,
        DisputeStatus.rejected => l10n.disputeStatusRejected,
      };

  /// Icon of a help category (`icon` key of the API, a default otherwise).
  static AtaIcons categoryIcon(String key) => switch (key) {
    'car' || 'trips' || 'drivers' => AtaIcons.car,
    'wallet' || 'payments' => AtaIcons.wallet,
    'shield' || 'safety' => AtaIcons.shield,
    'user' || 'account' => AtaIcons.user,
    'clock' => AtaIcons.clock,
    'gift' => AtaIcons.gift,
    'pin' || 'location' => AtaIcons.pin,
    'phone' => AtaIcons.phone,
    _ => AtaIcons.document,
  };
}
