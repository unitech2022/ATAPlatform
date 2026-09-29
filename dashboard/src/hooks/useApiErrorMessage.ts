import { useCallback } from 'react'
import { useLang } from '../context/lang'
import type { TranslationKey } from '../i18n'
import { isApiError } from '../lib/api'

/** Business error codes with a dedicated message (docs/08 ErrorCatalog); others use the server message. */
const CODE_KEY: Record<string, TranslationKey> = {
  four_eyes_required: 'errFourEyes',
  refund_exceeds_amount: 'errRefundExceeds',
  settlement_period_overlap: 'errSettlementOverlap',
  campaign_not_editable: 'errCampaignNotEditable',
  template_placeholder_invalid: 'errPlaceholderInvalid',
  unknown_event_code: 'errUnknownEventCode',
  insufficient_balance: 'errInsufficientBalance',
  // F12 / F14 (docs/09)
  chat_closed: 'errChatClosed',
  lost_item_window_closed: 'errLostItemWindowClosed',
  share_not_found: 'errShareNotFound',
  share_expired: 'errShareExpired',
  cancellation_reason_invalid: 'errCancellationReasonInvalid',
  cancellation_fee_changed: 'errCancellationFeeChanged',
  account_restricted: 'errAccountRestricted',
  // F15 (docs/10)
  rating_window_closed: 'errRatingWindowClosed',
  rating_exists: 'errRatingExists',
  promo_not_found: 'errPromoNotFound',
  promo_expired: 'errPromoExpired',
  promo_not_eligible: 'errPromoNotEligible',
  promo_usage_limit_reached: 'errPromoUsageLimit',
  incentive_opt_in_closed: 'errIncentiveOptInClosed',
}

/** Turns any thrown value into a human-readable message in the current language. */
export function useApiErrorMessage() {
  const { t } = useLang()
  return useCallback(
    (error: unknown): string => {
      if (!isApiError(error)) return t('errorGeneric')
      if (error.code === 'network_error') return t('networkError')
      if (error.status === 401) return t('sessionExpired')
      if (error.status === 403) return t('unauthorized')
      const known = CODE_KEY[error.code]
      if (known) return t(known)
      if (error.status === 404) return t('notFound')
      return error.message || t('errorGeneric')
    },
    [t],
  )
}
