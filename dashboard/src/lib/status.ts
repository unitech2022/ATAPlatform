import type { TranslationKey } from '../i18n'
import type {
  CampaignStatus,
  DeliveryStatus,
  DocumentStatus,
  DriverStatus,
  PaymentStatus,
  PayoutBatchStatus,
  PayoutStatus,
  RefundStatus,
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

/** Looks up a meta record with an unknown (possibly newer) server value; falls back to a muted raw label. */
export function metaOf<K extends string>(record: Record<K, StatusMeta>, value: string | null | undefined): StatusMeta | null {
  if (!value) return null
  return (record as Record<string, StatusMeta | undefined>)[value] ?? null
}
