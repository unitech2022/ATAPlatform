import { useState } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { cancellations } from '../lib/admin'
import { ACTOR_KEY, STAGE_KEY } from '../lib/cancellation'
import { formatNumber } from '../lib/format'
import type { CancellationEvent, TripActor } from '../lib/types'
import { Button } from './Button'
import { Textarea } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { Money } from './Money'

/** The subset of a cancellation event the review dialog needs (queue rows and trip sections both fit). */
export interface ReviewTarget {
  id: string
  tripNumber: string
  actor?: TripActor | null
  userName?: string | null
  reasonName: string | null
  reasonCode: string
  note?: string | null
  stage: CancellationEvent['stage']
  feeAmount: number | null
  feeCharged: number | null
  penaltyPoints?: number | null
}

export interface ExcuseReviewModalProps {
  target: ReviewTarget | null
  decision: 'approve' | 'reject'
  onClose: () => void
  onReviewed: (event: CancellationEvent) => void
}

/**
 * Approve / reject an excuse (`POST /admin/cancellations/{id}/review`, §F14.3). Spells out the money impact:
 * approving waives the fee (and refunds whatever was already charged), rejecting charges it and applies points.
 */
export function ExcuseReviewModal({ target, ...props }: ExcuseReviewModalProps) {
  return target ? <ReviewDialog key={`${target.id}:${props.decision}`} target={target} {...props} /> : null
}

function ReviewDialog({ target, decision, onClose, onReviewed }: Omit<ExcuseReviewModalProps, 'target'> & { target: ReviewTarget }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const approve = decision === 'approve'
  const fee = target.feeAmount ?? 0
  const charged = target.feeCharged ?? 0

  const submit = async () => {
    if (!approve && !note.trim()) {
      setError(t('reasonRequired'))
      return
    }
    setSaving(true)
    try {
      const updated = await cancellations.review(target.id, decision, note.trim())
      toast.success(approve ? t('cxExcuseApprovedToast') : t('cxExcuseRejectedToast'))
      onReviewed(updated)
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={approve ? t('cxApproveExcuse') : t('cxRejectExcuse')}
      description={
        <>
          <span className="ltr-nums">{target.tripNumber}</span> · {target.actor ? `${t(ACTOR_KEY[target.actor] ?? 'actorSystem')} · ` : ''}
          {target.userName ? `${target.userName} · ` : ''}
          {t(STAGE_KEY[target.stage] ?? 'cxStageAfterAccept')}
        </>
      }
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button variant={approve ? 'brand' : 'danger'} icon={approve ? 'check' : 'x'} onClick={submit} loading={saving}>
            {approve ? t('approve') : t('reject')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4">
        <div className="rounded-2xl bg-cloud px-4 py-3 text-sm">
          <p className="font-bold">{target.reasonName ?? target.reasonCode}</p>
          {target.note && <p className="mt-1 whitespace-pre-wrap break-words text-muted">“{target.note}”</p>}
        </div>
        <div className={`flex items-start gap-3 rounded-2xl p-4 text-sm ${approve ? 'bg-brand-soft text-ink' : 'bg-danger-soft text-danger'}`}>
          <Icon name={approve ? 'check' : 'alert'} className="mt-0.5 size-4 shrink-0" />
          <div className="space-y-1">
            {approve ? (
              <>
                <p>
                  {t('cxApproveImpactFee')} <Money value={fee} strong />
                </p>
                {charged > 0 && (
                  <p>
                    {t('cxApproveImpactRefund')} <Money value={charged} strong />
                  </p>
                )}
                <p>{t('cxApproveImpactRate')}</p>
              </>
            ) : (
              <>
                <p>
                  {t('cxRejectImpactFee')} <Money value={fee} strong />
                </p>
                {typeof target.penaltyPoints === 'number' && target.penaltyPoints > 0 && (
                  <p>
                    {t('cxRejectImpactPoints')} <span className="ltr-nums font-bold">+{formatNumber(target.penaltyPoints)}</span>
                  </p>
                )}
                <p>{t('cxRejectImpactRate')}</p>
              </>
            )}
          </div>
        </div>
        <Textarea
          id="review-note"
          label={approve ? t('noteOptional') : t('reviewNote')}
          placeholder={t('reasonPlaceholder')}
          value={note}
          maxLength={500}
          error={error}
          autoFocus
          onChange={(event) => {
            setNote(event.target.value)
            setError(null)
          }}
        />
      </div>
    </Modal>
  )
}
