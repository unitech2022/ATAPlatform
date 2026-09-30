import type { TranslationKey } from '../i18n'
import type {
  DisputeStatus,
  TicketPriority,
  TicketSlaState,
  TicketStatus,
  DriverTier,
  FavoriteStatus,
  IncentiveListStatus,
  IncentiveProgressStatus,
  PromotionListStatus,
  RatingFlagStatus,
  RatingStatus,
  RedemptionStatus,
  CancellationFeeStatus,
  CorporateAccountStatus,
  CorporateInvoiceStatus,
  CorporateUserStatus,
  ExcuseStatus,
  LostItemStatus,
  RestrictionLevel,
  SafetyAlertStatus,
  SafetyCaseStatus,
  SafetyPriority,
  CampaignStatus,
  DeliveryStatus,
  DocumentStatus,
  DriverStatus,
  PaymentStatus,
  PayoutBatchStatus,
  PayoutStatus,
  RefundStatus,
  AirportQueueStatus,
  ReminderStatus,
  ScheduledReservationState,
  SettlementBatchStatus,
  SettlementDirection,
  TripStatus,
  UserStatus,
  WalletStatus,
  WebhookProcessingStatus,
} from './types'

export type StatusTone = 'brand' | 'ink' | 'muted' | 'danger' | 'warning'

export interface StatusMeta {
  tone: StatusTone
  key: TranslationKey
}

export const driverStatusMeta: Record<DriverStatus, StatusMeta> = {
  draft: { tone: 'muted', key: 'statusDraft' },
  submitted: { tone: 'warning', key: 'statusSubmitted' },
  under_review: { tone: 'ink', key: 'statusUnderReview' },
  approved: { tone: 'brand', key: 'statusApproved' },
  rejected: { tone: 'danger', key: 'statusRejected' },
  suspended: { tone: 'danger', key: 'statusSuspended' },
}

export const documentStatusMeta: Record<DocumentStatus, StatusMeta> = {
  pending: { tone: 'warning', key: 'docPending' },
  verified: { tone: 'brand', key: 'docVerified' },
  rejected: { tone: 'danger', key: 'docRejected' },
  expired: { tone: 'danger', key: 'docExpired' },
}

export const userStatusMeta: Record<UserStatus, StatusMeta> = {
  active: { tone: 'brand', key: 'statusActive' },
  suspended: { tone: 'danger', key: 'statusSuspended' },
  deleted: { tone: 'muted', key: 'statusDeleted' },
}

export function driverStatusKey(status: DriverStatus | null | undefined): TranslationKey | null {
  return status ? (driverStatusMeta[status]?.key ?? null) : null
}

export const tripStatusMeta: Record<TripStatus, StatusMeta> = {
  requested: { tone: 'muted', key: 'tripStatusRequested' },
  scheduled: { tone: 'warning', key: 'tripStatusScheduled' },
  searching: { tone: 'warning', key: 'tripStatusSearching' },
  driver_assigned: { tone: 'ink', key: 'tripStatusDriverAssigned' },
  driver_en_route: { tone: 'ink', key: 'tripStatusDriverEnRoute' },
  driver_arrived: { tone: 'ink', key: 'tripStatusDriverArrived' },
  waiting: { tone: 'warning', key: 'tripStatusWaiting' },
  pin_verified: { tone: 'ink', key: 'tripStatusPinVerified' },
  in_trip: { tone: 'brand', key: 'tripStatusInTrip' },
  completed: { tone: 'brand', key: 'tripStatusCompleted' },
  cancelled: { tone: 'danger', key: 'tripStatusCancelled' },
  no_drivers: { tone: 'muted', key: 'tripStatusNoDrivers' },
}

// F11 — payments & payouts

export const paymentStatusMeta: Record<PaymentStatus, StatusMeta> = {
  initiated: { tone: 'muted', key: 'payStatusInitiated' },
  authorized: { tone: 'warning', key: 'payStatusAuthorized' },
  captured: { tone: 'brand', key: 'payStatusCaptured' },
  failed: { tone: 'danger', key: 'payStatusFailed' },
  voided: { tone: 'muted', key: 'payStatusVoided' },
  refunded: { tone: 'ink', key: 'payStatusRefunded' },
  partially_refunded: { tone: 'ink', key: 'payStatusPartiallyRefunded' },
}

export const refundStatusMeta: Record<RefundStatus, StatusMeta> = {
  pending_approval: { tone: 'warning', key: 'refundStatusPendingApproval' },
  approved: { tone: 'ink', key: 'refundStatusApproved' },
  processing: { tone: 'ink', key: 'refundStatusProcessing' },
  succeeded: { tone: 'brand', key: 'refundStatusSucceeded' },
  failed: { tone: 'danger', key: 'refundStatusFailed' },
  rejected: { tone: 'muted', key: 'refundStatusRejected' },
}

export const payoutStatusMeta: Record<PayoutStatus, StatusMeta> = {
  requested: { tone: 'warning', key: 'payoutStatusRequested' },
  approved: { tone: 'ink', key: 'payoutStatusApproved' },
  paid: { tone: 'brand', key: 'payoutStatusPaid' },
  rejected: { tone: 'danger', key: 'payoutStatusRejected' },
  cancelled: { tone: 'muted', key: 'payoutStatusCancelled' },
}

export const payoutBatchStatusMeta: Record<PayoutBatchStatus, StatusMeta> = {
  open: { tone: 'warning', key: 'batchStatusOpen' },
  exported: { tone: 'ink', key: 'batchStatusExported' },
  paid: { tone: 'brand', key: 'batchStatusPaid' },
}

export const settlementBatchStatusMeta: Record<SettlementBatchStatus, StatusMeta> = {
  generating: { tone: 'warning', key: 'settlementStatusGenerating' },
  ready: { tone: 'ink', key: 'settlementStatusReady' },
  finalized: { tone: 'brand', key: 'settlementStatusFinalized' },
  failed: { tone: 'danger', key: 'settlementStatusFailed' },
}

export const settlementDirectionMeta: Record<SettlementDirection, StatusMeta> = {
  payable_to_driver: { tone: 'brand', key: 'directionPayable' },
  due_from_driver: { tone: 'danger', key: 'directionDue' },
  zero: { tone: 'muted', key: 'directionZero' },
}

export const walletStatusMeta: Record<WalletStatus, StatusMeta> = {
  active: { tone: 'brand', key: 'walletStatusActive' },
  frozen: { tone: 'danger', key: 'walletStatusFrozen' },
}

export const webhookStatusMeta: Record<WebhookProcessingStatus, StatusMeta> = {
  pending: { tone: 'warning', key: 'webhookPending' },
  processed: { tone: 'brand', key: 'webhookProcessed' },
  ignored: { tone: 'muted', key: 'webhookIgnored' },
  failed: { tone: 'danger', key: 'webhookFailed' },
}

// F13 — notifications

export const campaignStatusMeta: Record<CampaignStatus, StatusMeta> = {
  draft: { tone: 'muted', key: 'campaignStatusDraft' },
  scheduled: { tone: 'warning', key: 'campaignStatusScheduled' },
  sending: { tone: 'ink', key: 'campaignStatusSending' },
  sent: { tone: 'brand', key: 'campaignStatusSent' },
  cancelled: { tone: 'muted', key: 'campaignStatusCancelled' },
  failed: { tone: 'danger', key: 'campaignStatusFailed' },
}

export const deliveryStatusMeta: Record<DeliveryStatus, StatusMeta> = {
  queued: { tone: 'warning', key: 'deliveryQueued' },
  sent: { tone: 'brand', key: 'deliverySent' },
  failed: { tone: 'danger', key: 'deliveryFailed' },
  skipped: { tone: 'muted', key: 'deliverySkipped' },
}

// F12 — safety

export const safetyCaseStatusMeta: Record<SafetyCaseStatus, StatusMeta> = {
  open: { tone: 'danger', key: 'sfStatusOpen' },
  in_progress: { tone: 'warning', key: 'sfStatusInProgress' },
  escalated: { tone: 'ink', key: 'sfStatusEscalated' },
  resolved: { tone: 'brand', key: 'sfStatusResolved' },
}

export const safetyPriorityMeta: Record<SafetyPriority, StatusMeta> = {
  critical: { tone: 'danger', key: 'sfPriorityCritical' },
  high: { tone: 'warning', key: 'sfPriorityHigh' },
  medium: { tone: 'ink', key: 'sfPriorityMedium' },
  low: { tone: 'muted', key: 'sfPriorityLow' },
}

export const safetyAlertStatusMeta: Record<SafetyAlertStatus, StatusMeta> = {
  pending_rider: { tone: 'warning', key: 'sfAlertPendingRider' },
  resolved_ok: { tone: 'brand', key: 'sfAlertResolvedOk' },
  escalated: { tone: 'danger', key: 'sfAlertEscalated' },
  no_response: { tone: 'danger', key: 'sfAlertNoResponse' },
  dismissed: { tone: 'muted', key: 'sfAlertDismissed' },
}

export const lostItemStatusMeta: Record<LostItemStatus, StatusMeta> = {
  open: { tone: 'warning', key: 'liStatusOpen' },
  driver_contacted: { tone: 'ink', key: 'liStatusDriverContacted' },
  found: { tone: 'brand', key: 'liStatusFound' },
  returned: { tone: 'brand', key: 'liStatusReturned' },
  not_found: { tone: 'danger', key: 'liStatusNotFound' },
  closed: { tone: 'muted', key: 'liStatusClosed' },
}

// F14 — cancellation & reliability

export const feeStatusMeta: Record<CancellationFeeStatus, StatusMeta> = {
  none: { tone: 'muted', key: 'cxFeeNone' },
  charged: { tone: 'ink', key: 'cxFeeCharged' },
  pending_review: { tone: 'warning', key: 'cxFeePendingReview' },
  waived: { tone: 'brand', key: 'cxFeeWaived' },
  failed: { tone: 'danger', key: 'cxFeeFailed' },
  refunded: { tone: 'muted', key: 'cxFeeRefunded' },
}

export const excuseStatusMeta: Record<ExcuseStatus, StatusMeta> = {
  not_applicable: { tone: 'muted', key: 'cxExcuseNotApplicable' },
  pending: { tone: 'warning', key: 'cxExcusePending' },
  approved: { tone: 'brand', key: 'cxExcuseApproved' },
  rejected: { tone: 'danger', key: 'cxExcuseRejected' },
}

export const restrictionLevelMeta: Record<RestrictionLevel, StatusMeta> = {
  none: { tone: 'brand', key: 'rlLevelNone' },
  warning: { tone: 'warning', key: 'rlLevelWarning' },
  matching_deprioritized: { tone: 'warning', key: 'rlLevelDeprioritized' },
  incentives_reduced: { tone: 'danger', key: 'rlLevelIncentivesReduced' },
  temporarily_restricted: { tone: 'danger', key: 'rlLevelRestricted' },
  suspended: { tone: 'ink', key: 'rlLevelSuspended' },
}

// F15 — ratings, promotions, driver tiers, incentives

export const ratingStatusMeta: Record<RatingStatus, StatusMeta> = {
  visible: { tone: 'brand', key: 'rtStatusVisible' },
  hidden: { tone: 'muted', key: 'rtStatusHidden' },
}

export const ratingFlagStatusMeta: Record<RatingFlagStatus, StatusMeta> = {
  open: { tone: 'warning', key: 'rtFlagOpen' },
  dismissed: { tone: 'muted', key: 'rtFlagDismissed' },
  actioned: { tone: 'brand', key: 'rtFlagActioned' },
}

export const promotionStatusMeta: Record<PromotionListStatus, StatusMeta> = {
  active: { tone: 'brand', key: 'prStatusActive' },
  scheduled: { tone: 'warning', key: 'prStatusScheduled' },
  expired: { tone: 'muted', key: 'prStatusExpired' },
  inactive: { tone: 'danger', key: 'prStatusInactive' },
}

export const redemptionStatusMeta: Record<RedemptionStatus, StatusMeta> = {
  reserved: { tone: 'warning', key: 'prRedReserved' },
  applied: { tone: 'brand', key: 'prRedApplied' },
  released: { tone: 'muted', key: 'prRedReleased' },
}

export const driverTierMeta: Record<DriverTier, StatusMeta> = {
  bronze: { tone: 'muted', key: 'tierBronze' },
  silver: { tone: 'ink', key: 'tierSilver' },
  gold: { tone: 'warning', key: 'tierGold' },
  platinum: { tone: 'brand', key: 'tierPlatinum' },
}

export const incentiveStatusMeta: Record<IncentiveListStatus, StatusMeta> = {
  active: { tone: 'brand', key: 'icStatusActive' },
  upcoming: { tone: 'warning', key: 'icStatusUpcoming' },
  ended: { tone: 'muted', key: 'icStatusEnded' },
  inactive: { tone: 'danger', key: 'icStatusInactive' },
}

export const incentiveProgressStatusMeta: Record<IncentiveProgressStatus, StatusMeta> = {
  in_progress: { tone: 'ink', key: 'icProgInProgress' },
  achieved: { tone: 'warning', key: 'icProgAchieved' },
  paid: { tone: 'brand', key: 'icProgPaid' },
  expired: { tone: 'muted', key: 'icProgExpired' },
  voided: { tone: 'danger', key: 'icProgVoided' },
}

/** Looks up a meta record with an unknown (possibly newer) server value; falls back to a muted raw label. */
export function metaOf<K extends string>(record: Record<K, StatusMeta>, value: string | null | undefined): StatusMeta | null {
  if (!value) return null
  return (record as Record<string, StatusMeta | undefined>)[value] ?? null
}

/** `trips.favorite_status` (docs/10 §F16.1). */
export const favoriteStatusMeta: Record<FavoriteStatus, StatusMeta> = {
  requested: { tone: 'warning', key: 'fvStatusRequested' },
  accepted: { tone: 'brand', key: 'fvStatusAccepted' },
  unavailable: { tone: 'muted', key: 'fvStatusUnavailable' },
  rejected: { tone: 'danger', key: 'fvStatusRejected' },
  expired: { tone: 'muted', key: 'fvStatusExpired' },
}

// F17 — scheduled rides and airport queue

export const reservationStateMeta: Record<ScheduledReservationState, StatusMeta> = {
  none: { tone: 'danger', key: 'sdResNone' },
  reserved: { tone: 'warning', key: 'sdResReserved' },
  confirmed: { tone: 'ink', key: 'sdResConfirmed' },
  assigned: { tone: 'brand', key: 'sdResAssigned' },
  released: { tone: 'muted', key: 'sdResReleased' },
  no_show: { tone: 'danger', key: 'sdResNoShow' },
  completed: { tone: 'brand', key: 'sdResCompleted' },
  cancelled: { tone: 'muted', key: 'sdResCancelled' },
}

export const reminderStatusMeta: Record<ReminderStatus, StatusMeta> = {
  pending: { tone: 'warning', key: 'sdRemPending' },
  sent: { tone: 'brand', key: 'sdRemSent' },
  skipped: { tone: 'muted', key: 'sdRemSkipped' },
  cancelled: { tone: 'muted', key: 'sdRemCancelled' },
}

export const airportQueueStatusMeta: Record<AirportQueueStatus, StatusMeta> = {
  waiting: { tone: 'warning', key: 'apQueueWaiting' },
  offered: { tone: 'brand', key: 'apQueueOffered' },
  dispatched: { tone: 'ink', key: 'apQueueDispatched' },
  left: { tone: 'muted', key: 'apQueueLeft' },
  removed: { tone: 'danger', key: 'apQueueRemoved' },
}

// F18 — support

export const ticketStatusMeta: Record<TicketStatus, StatusMeta> = {
  open: { tone: 'warning', key: 'spStatusOpen' },
  in_progress: { tone: 'ink', key: 'spStatusInProgress' },
  pending_user: { tone: 'muted', key: 'spStatusPendingUser' },
  resolved: { tone: 'brand', key: 'spStatusResolved' },
  closed: { tone: 'muted', key: 'spStatusClosed' },
}

export const ticketPriorityMeta: Record<TicketPriority, StatusMeta> = {
  urgent: { tone: 'danger', key: 'spPriorityUrgent' },
  high: { tone: 'warning', key: 'spPriorityHigh' },
  normal: { tone: 'ink', key: 'spPriorityNormal' },
  low: { tone: 'muted', key: 'spPriorityLow' },
}

export const slaStateMeta: Record<TicketSlaState, StatusMeta> = {
  ok: { tone: 'brand', key: 'spSlaOk' },
  due_soon: { tone: 'warning', key: 'spSlaDueSoon' },
  breached: { tone: 'danger', key: 'spSlaBreached' },
}

export const disputeStatusMeta: Record<DisputeStatus, StatusMeta> = {
  open: { tone: 'warning', key: 'spDisputeOpen' },
  under_review: { tone: 'ink', key: 'spDisputeUnderReview' },
  approved: { tone: 'brand', key: 'spDisputeApproved' },
  partially_approved: { tone: 'brand', key: 'spDisputePartiallyApproved' },
  rejected: { tone: 'muted', key: 'spDisputeRejected' },
}

/** F19 — corporate accounts, members and invoices (docs/12 §F19.1). */
export const corporateAccountStatusMeta: Record<CorporateAccountStatus, StatusMeta> = {
  pending: { tone: 'warning', key: 'coStatusPending' },
  active: { tone: 'brand', key: 'statusActive' },
  suspended: { tone: 'danger', key: 'statusSuspended' },
  closed: { tone: 'muted', key: 'coStatusClosed' },
}

export const corporateUserStatusMeta: Record<CorporateUserStatus, StatusMeta> = {
  invited: { tone: 'warning', key: 'coUserInvited' },
  active: { tone: 'brand', key: 'statusActive' },
  disabled: { tone: 'muted', key: 'coUserDisabled' },
}

export const corporateInvoiceStatusMeta: Record<CorporateInvoiceStatus, StatusMeta> = {
  draft: { tone: 'muted', key: 'coInvDraft' },
  issued: { tone: 'ink', key: 'coInvIssued' },
  paid: { tone: 'brand', key: 'coInvPaid' },
  overdue: { tone: 'danger', key: 'coInvOverdue' },
  void: { tone: 'muted', key: 'coInvVoid' },
}
