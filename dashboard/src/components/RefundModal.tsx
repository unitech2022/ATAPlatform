import { useState } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { isApiError } from '../lib/api'
import { parseAmount, REFUND_AUTO_APPROVE_LIMIT, REFUND_DESTINATION_KEY, REFUND_REASON_CODES, REFUND_REASON_KEY } from '../lib/finance'
import { formatMoney } from '../lib/format'
import type { RefundDestination, RefundInput, RefundReasonCode } from '../lib/types'
import { Button } from './Button'
import { Input, Select, Textarea } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

export interface RefundModalProps {
  open: boolean
  /** Shown under the title (payment or trip reference). */
  description?: string
  /** Remaining refundable amount, when known. */
  refundable?: number | null
  /** Card payments may go back to the card; cash/wallet refunds always go to the wallet. */
  allowOriginalMethod: boolean
  onClose: () => void
  onSubmit: (input: RefundInput) => Promise<void>
}

export function RefundModal({ open, ...props }: RefundModalProps) {
  // Mounted only while open so every refund starts from a clean form.
  return open ? <RefundDialog {...props} /> : null
}

type Errors = { amount?: string; reason?: string; form?: string }

function RefundDialog({ description, refundable, allowOriginalMethod, onClose, onSubmit }: Omit<RefundModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [amount, setAmount] = useState(refundable && refundable > 0 ? String(refundable) : '')
  const [reasonCode, setReasonCode] = useState<RefundReasonCode>('fare_dispute')
  const [reason, setReason] = useState('')
  const [destination, setDestination] = useState<RefundDestination>(allowOriginalMethod ? 'original_method' : 'wallet')
  const [errors, setErrors] = useState<Errors>({})
  const [submitting, setSubmitting] = useState(false)

  const parsed = parseAmount(amount)
  const needsSecondApproval = parsed !== null && parsed >= REFUND_AUTO_APPROVE_LIMIT

  const submit = async () => {
    const next: Errors = {}
    if (parsed === null) next.amount = t('invalidAmount')
    else if (typeof refundable === 'number' && parsed > refundable) next.amount = `${t('errRefundExceeds')} (${formatMoney(refundable)} ${t('sar')})`
    if (!reason.trim()) next.reason = t('reasonRequired')
    setErrors(next)
    if (Object.keys(next).length > 0 || parsed === null) return
    setSubmitting(true)
    try {
      await onSubmit({ amount: parsed, reasonCode, reason: reason.trim(), destination: allowOriginalMethod ? destination : 'wallet' })
    } catch (error) {
      if (isApiError(error) && error.code === 'refund_exceeds_amount') {
        const max = Number(error.details?.refundable)
        setErrors({ amount: Number.isFinite(max) ? `${t('errRefundExceeds')} (${formatMoney(max)} ${t('sar')})` : t('errRefundExceeds') })
      } else {
        setErrors({ form: describe(error) })
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={t('refundTitle')}
      description={description}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button variant="primary" icon="refresh" onClick={submit} loading={submitting}>
            {t('createRefund')}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <Input
          id="refund-amount"
          type="number"
          inputMode="decimal"
          min="0.01"
          step="0.01"
          dir="ltr"
          label={`${t('amount')} (${t('sar')})`}
          hint={typeof refundable === 'number' ? `${t('refundable')}: ${formatMoney(refundable)} ${t('sar')}` : undefined}
          value={amount}
          error={errors.amount}
          onChange={(event) => {
            setAmount(event.target.value)
            setErrors((current) => ({ ...current, amount: undefined }))
          }}
          autoFocus
        />
        <Select id="refund-reason-code" label={t('reasonCode')} value={reasonCode} onChange={(event) => setReasonCode(event.target.value as RefundReasonCode)}>
          {REFUND_REASON_CODES.map((code) => (
            <option key={code} value={code}>
              {t(REFUND_REASON_KEY[code])}
            </option>
          ))}
        </Select>
        {allowOriginalMethod ? (
          <Select id="refund-destination" label={t('destination')} value={destination} onChange={(event) => setDestination(event.target.value as RefundDestination)}>
            {(['original_method', 'wallet'] as const).map((value) => (
              <option key={value} value={value}>
                {t(REFUND_DESTINATION_KEY[value])}
              </option>
            ))}
          </Select>
        ) : (
          <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">
            {t('destination')}: <span className="font-bold text-ink">{t('destinationWallet')}</span>
          </p>
        )}
        <Textarea
          id="refund-reason"
          label={t('reason')}
          placeholder={t('reasonPlaceholder')}
          value={reason}
          error={errors.reason}
          onChange={(event) => {
            setReason(event.target.value)
            setErrors((current) => ({ ...current, reason: undefined }))
          }}
          maxLength={500}
        />
        {needsSecondApproval && (
          <div className="flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
            <Icon name="shield" className="mt-0.5 size-4 shrink-0" />
            <p>{t('fourEyesHint')}</p>
          </div>
        )}
        {errors.form && (
          <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>{errors.form}</p>
          </div>
        )}
      </div>
    </Modal>
  )
}
