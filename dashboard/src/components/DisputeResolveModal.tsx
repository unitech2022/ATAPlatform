import { useState } from 'react'
import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { disputes } from '../lib/admin'
import { REFUND_AUTO_APPROVE_LIMIT } from '../lib/finance'
import { formatMoney } from '../lib/format'
import { DISPUTE_REASON_KEY, DISPUTE_RESOLUTION_KEY, DISPUTE_RESOLUTIONS, disputeRefundOf, validateDisputeResolution, type DisputeFormErrors } from '../lib/support'
import { disputeStatusMeta, refundStatusMeta } from '../lib/status'
import type { DisputeResolution, DisputeResolveResult, FareDispute } from '../lib/types'
import { MetaBadge } from './Badge'
import { Button } from './Button'
import { Textarea, Input } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

/**
 * Resolve a fare dispute (`POST /admin/support/disputes/{id}/resolve`, needs `support.disputes`): full refund, partial refund
 * (`0 < amount ≤ charged`) or no refund, with a mandatory note. The refund is created through F11 and, above the auto-approve limit,
 * waits for a second admin (four-eyes) — the result step shows that state and links to `/refunds`.
 */
export function DisputeResolveModal({ dispute, onClose, onResolved }: { dispute: FareDispute | null; onClose: () => void; onResolved: (result: DisputeResolveResult) => void }) {
  // Keyed by dispute so every use starts from a clean form.
  return dispute ? <ResolveDialog key={dispute.id} dispute={dispute} onClose={onClose} onResolved={onResolved} /> : null
}

function ResolveDialog({ dispute, onClose, onResolved }: { dispute: FareDispute; onClose: () => void; onResolved: (result: DisputeResolveResult) => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [resolution, setResolution] = useState<DisputeResolution>('refund_full')
  const [amountText, setAmountText] = useState(dispute.requestedRefundAmount ? String(dispute.requestedRefundAmount) : '')
  const [note, setNote] = useState('')
  const [errors, setErrors] = useState<DisputeFormErrors>({})
  const [saving, setSaving] = useState(false)
  const [result, setResult] = useState<DisputeResolveResult | null>(null)

  const refundAmount = resolution === 'refund_full' ? dispute.chargedAmount : resolution === 'refund_partial' ? Number(amountText.replace(',', '.')) || 0 : 0
  const needsSecondApprover = refundAmount > REFUND_AUTO_APPROVE_LIMIT

  const submit = async () => {
    const check = validateDisputeResolution(resolution, amountText, note, dispute.chargedAmount)
    setErrors(check.errors)
    if (Object.keys(check.errors).length > 0) return
    setSaving(true)
    try {
      const answer = await disputes.resolve(dispute.id, { resolution, amount: check.amount, note: note.trim() })
      toast.success(t('spDisputeResolved'))
      setResult(answer)
      onResolved(answer)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  if (result) return <ResultDialog result={result} onClose={onClose} />

  return (
    <Modal
      open
      size="lg"
      title={t('spResolveDispute')}
      description={`${dispute.ticketNumber ?? ''} ${dispute.tripNumber ? `· ${dispute.tripNumber}` : ''}`.trim() || undefined}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button variant="brand" icon="check" onClick={submit} loading={saving}>
            {t('spResolveConfirm')}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <dl className="grid grid-cols-2 gap-3 text-sm">
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <dt className="text-xs font-bold text-muted">{t('spChargedAmount')}</dt>
            <dd className="ltr-nums mt-1 font-bold">
              {formatMoney(dispute.chargedAmount)} {t('sar')}
            </dd>
          </div>
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <dt className="text-xs font-bold text-muted">{t('spRequestedAmount')}</dt>
            <dd className="ltr-nums mt-1 font-bold">{dispute.requestedRefundAmount === null ? '—' : `${formatMoney(dispute.requestedRefundAmount)} ${t('sar')}`}</dd>
          </div>
          <div className="col-span-2 rounded-2xl bg-cloud px-4 py-3">
            <dt className="text-xs font-bold text-muted">{t('spDisputeReason')}</dt>
            <dd className="mt-1 font-bold">{t(DISPUTE_REASON_KEY[dispute.reason] ?? 'spReasonOther')}</dd>
          </div>
        </dl>

        <fieldset>
          <legend className="mb-2 text-sm font-bold">{t('spResolution')}</legend>
          <div className="grid gap-2 sm:grid-cols-3">
            {DISPUTE_RESOLUTIONS.map((value) => (
              <label
                key={value}
                className={`flex cursor-pointer items-center gap-2 rounded-2xl border-2 px-4 py-3 text-sm font-bold transition ${resolution === value ? 'border-brand bg-brand-soft' : 'border-line hover:bg-cloud'}`}
              >
                <input
                  type="radio"
                  name="dispute-resolution"
                  value={value}
                  checked={resolution === value}
                  onChange={() => {
                    setResolution(value)
                    setErrors({})
                  }}
                  className="size-4 accent-[var(--color-brand)]"
                />
                {t(DISPUTE_RESOLUTION_KEY[value])}
              </label>
            ))}
          </div>
        </fieldset>

        {resolution === 'refund_partial' && (
          <Input
            id="dispute-amount"
            label={`${t('spRefundAmount')} (${t('sar')})`}
            hint={`${t('spAmountMax')} ${formatMoney(dispute.chargedAmount)} ${t('sar')}`}
            inputMode="decimal"
            dir="ltr"
            value={amountText}
            error={errors.amount === 'invalid' ? t('spAmountInvalid') : errors.amount === 'exceeds' ? t('spAmountExceeds') : undefined}
            onChange={(event) => {
              setAmountText(event.target.value)
              setErrors((current) => ({ ...current, amount: undefined }))
            }}
          />
        )}
        {resolution === 'refund_full' && (
          <p className="ltr-nums rounded-2xl bg-cloud px-4 py-3 text-sm font-bold">
            {t('spRefundAmount')}: {formatMoney(dispute.chargedAmount)} {t('sar')}
          </p>
        )}

        {needsSecondApprover && (
          <div className="flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
            <Icon name="shield" className="mt-0.5 size-4 shrink-0" />
            <p>
              {t('spFourEyesHint')} (<span className="ltr-nums">{formatMoney(REFUND_AUTO_APPROVE_LIMIT)}</span> {t('sar')})
            </p>
          </div>
        )}

        <Textarea
          id="dispute-note"
          label={t('spResolutionNote')}
          placeholder={t('spResolutionNotePlaceholder')}
          maxLength={1000}
          value={note}
          error={errors.note === 'required' ? t('reasonRequired') : errors.note === 'tooLong' ? t('spNoteTooLong') : undefined}
          onChange={(event) => {
            setNote(event.target.value)
            setErrors((current) => ({ ...current, note: undefined }))
          }}
        />
      </div>
    </Modal>
  )
}

function ResultDialog({ result, onClose }: { result: DisputeResolveResult; onClose: () => void }) {
  const { t } = useLang()
  const refund = disputeRefundOf(result)
  const pending = refund?.status === 'pending_approval'
  return (
    <Modal
      open
      title={t('spDisputeResolved')}
      onClose={onClose}
      footer={
        <>
          {refund && (
            <Link
              to={pending ? '/refunds?status=pending_approval' : '/refunds?status=all'}
              className="inline-flex h-11 items-center gap-2 rounded-2xl border border-line px-4 text-sm font-bold transition hover:bg-cloud"
            >
              <Icon name="receipt" className="size-4" />
              {t('spOpenRefunds')}
            </Link>
          )}
          <Button onClick={onClose}>{t('close')}</Button>
        </>
      }
    >
      <div className="space-y-3 text-sm">
        <p className="flex flex-wrap items-center gap-2">
          <span className="font-bold">{t('status')}:</span>
          <MetaBadge record={disputeStatusMeta} value={result.status} />
          {result.resolution && <span className="text-muted">{t(DISPUTE_RESOLUTION_KEY[result.resolution])}</span>}
        </p>
        {refund ? (
          <div className="rounded-2xl bg-cloud p-4">
            <p className="flex flex-wrap items-center gap-2">
              <span className="font-bold">{t('spRefund')}:</span>
              {refund.refundNumber && <span className="ltr-nums">{refund.refundNumber}</span>}
              <MetaBadge record={refundStatusMeta} value={refund.status} />
            </p>
            {pending && <p className="mt-2 text-amber-700">{t('spRefundPendingApproval')}</p>}
          </div>
        ) : (
          <p className="rounded-2xl bg-cloud p-4 text-muted">{t('spNoRefundCreated')}</p>
        )}
      </div>
    </Modal>
  )
}
