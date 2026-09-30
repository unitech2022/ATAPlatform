import { useState } from 'react'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { refunds } from '../lib/admin'
import { formatMoney } from '../lib/format'
import type { Refund } from '../lib/types'
import { Button } from './Button'
import { ConfirmModal } from './ConfirmModal'
import { ReasonModal } from './ReasonModal'

type Pending = 'approve' | 'reject' | 'retry' | null

/**
 * Four-eyes refund actions (docs/08 §F11.4): the requester cannot approve their own refund, so the approve
 * button is disabled for them with an explanation; the server enforces it too (`409 four_eyes_required`).
 */
export function RefundActions({ refund, onChanged, size = 'sm' }: { refund: Refund; onChanged: () => void; size?: 'sm' | 'md' }) {
  const { t } = useLang()
  const { user, can } = useAuth()
  // F20: approve / reject / retry need `payments.refund_approve` (docs/12 §F20.2).
  const allowed = can('payments.refund_approve')
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [pending, setPending] = useState<Pending>(null)

  const isRequester = Boolean(user?.id) && user?.id === refund.requestedBy
  const label = `${refund.refundNumber} — ${formatMoney(refund.amount)} ${t('sar')}`

  const run = async (action: Exclude<Pending, null>, reason = '') => {
    try {
      if (action === 'approve') await refunds.approve(refund.id)
      else if (action === 'reject') await refunds.reject(refund.id, reason)
      else await refunds.retry(refund.id)
      toast.success(t(action === 'approve' ? 'refundApproved' : action === 'reject' ? 'refundRejected' : 'refundRetried'))
      setPending(null)
      onChanged()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  if (!allowed || (refund.status !== 'pending_approval' && refund.status !== 'failed')) return null

  return (
    <span className="inline-flex flex-wrap items-center justify-end gap-2" onClick={(event) => event.stopPropagation()}>
      {refund.status === 'pending_approval' && (
        <>
          <Button
            variant="brand"
            size={size}
            icon="check"
            disabled={isRequester}
            title={isRequester ? t('fourEyesSelf') : undefined}
            onClick={() => setPending('approve')}
          >
            {t('approve')}
          </Button>
          <Button variant="danger-outline" size={size} icon="x" onClick={() => setPending('reject')}>
            {t('reject')}
          </Button>
          {isRequester && <span className="w-full text-end text-xs text-muted">{t('fourEyesSelf')}</span>}
        </>
      )}
      {refund.status === 'failed' && (
        <Button variant="secondary" size={size} icon="refresh" onClick={() => setPending('retry')}>
          {t('retry')}
        </Button>
      )}

      <ConfirmModal
        open={pending === 'approve'}
        title={t('approveRefundTitle')}
        description={`${label} — ${t('approveRefundCopy')}`}
        confirmLabel={t('approve')}
        confirmVariant="brand"
        onClose={() => setPending(null)}
        onConfirm={() => run('approve')}
      />
      <ConfirmModal
        open={pending === 'retry'}
        title={t('retryRefundTitle')}
        description={`${label}${refund.failureMessage ? ` — ${refund.failureMessage}` : ''}`}
        confirmLabel={t('retry')}
        confirmVariant="primary"
        onClose={() => setPending(null)}
        onConfirm={() => run('retry')}
      />
      <ReasonModal
        open={pending === 'reject'}
        title={t('rejectRefundTitle')}
        description={`${label} — ${t('rejectRefundCopy')}`}
        confirmLabel={t('reject')}
        onClose={() => setPending(null)}
        onConfirm={(reason) => run('reject', reason)}
      />
    </span>
  )
}
