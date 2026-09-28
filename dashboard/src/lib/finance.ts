import type { Lang, TranslationKey } from '../i18n'
import { formatDate } from './format'
import type {
  PaymentChannel,
  PaymentPurpose,
  PaymentStatus,
  PayoutStatus,
  RefundDestination,
  RefundReasonCode,
  RefundStatus,
  SettlementDirection,
  WalletKind,
  WalletTransactionType,
} from './types'

/**
 * Server defaults from docs/08 §F11.8. The admin API does not expose them, so the dashboard only uses them
 * for hints; the server remains the source of truth (e.g. `409 four_eyes_required`).
 */
export const REFUND_AUTO_APPROVE_LIMIT = 50
export const MAX_CASH_DEBT = 500

export const PAYMENT_STATUSES: PaymentStatus[] = ['initiated', 'authorized', 'captured', 'failed', 'voided', 'partially_refunded', 'refunded']
export const PAYMENT_PURPOSES: PaymentPurpose[] = ['trip', 'topup', 'cancellation_fee']
export const PAYMENT_CHANNELS: PaymentChannel[] = ['card', 'apple_pay', 'sandbox']
export const REFUND_STATUSES: RefundStatus[] = ['pending_approval', 'approved', 'processing', 'succeeded', 'failed', 'rejected']
export const PAYOUT_STATUSES: PayoutStatus[] = ['requested', 'approved', 'paid', 'rejected', 'cancelled']
export const SETTLEMENT_DIRECTIONS: SettlementDirection[] = ['payable_to_driver', 'due_from_driver', 'zero']
export const WALLET_KINDS: WalletKind[] = ['passenger', 'driver']

export const PAYMENT_PURPOSE_KEY: Record<PaymentPurpose, TranslationKey> = {
  trip: 'purposeTrip',
  topup: 'purposeTopup',
  cancellation_fee: 'purposeCancellationFee',
}

export const PAYMENT_CHANNEL_KEY: Record<PaymentChannel, TranslationKey> = {
  card: 'paymentCard',
  apple_pay: 'channelApplePay',
  sandbox: 'channelSandbox',
}

export const REFUND_REASON_CODES: RefundReasonCode[] = [
  'fare_dispute',
  'trip_not_taken',
  'duplicate_charge',
  'service_issue',
  'cancellation_fee_waived',
  'goodwill',
  'other',
]

export const REFUND_REASON_KEY: Record<RefundReasonCode, TranslationKey> = {
  fare_dispute: 'reasonFareDispute',
  trip_not_taken: 'reasonTripNotTaken',
  duplicate_charge: 'reasonDuplicateCharge',
  service_issue: 'reasonServiceIssue',
  cancellation_fee_waived: 'reasonCancellationFeeWaived',
  goodwill: 'reasonGoodwill',
  other: 'reasonOther',
}

export const REFUND_DESTINATION_KEY: Record<RefundDestination, TranslationKey> = {
  original_method: 'destinationOriginal',
  wallet: 'destinationWallet',
}

export const WALLET_KIND_KEY: Record<WalletKind, TranslationKey> = {
  passenger: 'walletKindPassenger',
  driver: 'walletKindDriver',
}

export const WALLET_TX_KEY: Record<WalletTransactionType, TranslationKey> = {
  topup: 'txTopup',
  trip_payment: 'txTripPayment',
  trip_earning: 'txTripEarning',
  refund: 'txRefund',
  payout: 'txPayout',
  payout_reversal: 'txPayoutReversal',
  adjustment: 'txAdjustment',
  incentive: 'txIncentive',
  cancellation_fee: 'txCancellationFee',
  cancellation_compensation: 'txCancellationCompensation',
  cash_collection: 'txCashCollection',
}

/** Known general-ledger accounts (§F11.3); wallet accounts are grouped server-side. */
export const LEDGER_ACCOUNT_KEY: Record<string, TranslationKey> = {
  passenger_wallets: 'accPassengerWallets',
  driver_wallets: 'accDriverWallets',
  platform_cash: 'accPlatformCash',
  gateway_clearing: 'accGatewayClearing',
  trip_revenue: 'accTripRevenue',
  cash_collected: 'accCashCollected',
  payouts_pending: 'accPayoutsPending',
  refunds: 'accRefunds',
  cancellation_fees: 'accCancellationFees',
  discount_promotion: 'accDiscountPromotion',
  discount_favorite_driver: 'accDiscountFavoriteDriver',
  incentives: 'accIncentives',
  adjustments: 'accAdjustments',
}

/** `passenger_wallet:{id}` → `passenger_wallet`; used to label ledger lines. */
export function ledgerAccountKey(account: string): TranslationKey | null {
  const base = account.split(':')[0]
  if (base === 'passenger_wallet') return 'accPassengerWallet'
  if (base === 'driver_wallet') return 'accDriverWallet'
  if (base === 'corporate_receivable') return 'accCorporateReceivable'
  return LEDGER_ACCOUNT_KEY[base] ?? null
}

export function lookupKey<K extends string>(record: Record<K, TranslationKey>, value: string | null | undefined): TranslationKey | null {
  if (!value) return null
  return (record as Record<string, TranslationKey | undefined>)[value] ?? null
}

export function parseEnum<T extends string>(value: string | null, allowed: readonly T[]): T | '' {
  return value !== null && (allowed as readonly string[]).includes(value) ? (value as T) : ''
}

/** Cash debt of a driver wallet: the negative part of the balance (§F11.3). */
export function cashDebtOf(balance: number) {
  return Math.max(0, -balance)
}

/** Saves a Blob (fetched with the Bearer header) under `fileName`. */
export function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  // Give the browser a tick to start the download before releasing the URL.
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}

/** Two-decimal amount parsing for money inputs; null when empty, invalid or not positive. */
export function parseAmount(value: string): number | null {
  const trimmed = value.trim()
  if (!trimmed) return null
  const number = Number(trimmed)
  if (!Number.isFinite(number) || number <= 0) return null
  return Math.round(number * 100) / 100
}

/** Settlement periods are half-open `[start, end)`; the label shows the last included day. */
export function periodLabel(batch: { periodStart: string; periodEnd: string }, lang: Lang) {
  const end = new Date(batch.periodEnd)
  if (Number.isNaN(end.getTime())) return `${formatDate(batch.periodStart, lang)} – ${formatDate(batch.periodEnd, lang)}`
  end.setMilliseconds(end.getMilliseconds() - 1)
  return `${formatDate(batch.periodStart, lang)} – ${formatDate(end.toISOString(), lang)}`
}
