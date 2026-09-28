import { useState } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { fromLocalInput } from '../lib/pricing'
import { Button } from './Button'
import { Input } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

export interface MarkPaidModalProps {
  open: boolean
  title: string
  description?: string
  /** Single payouts accept an optional transfer time; batches do not. */
  withPaidAt?: boolean
  onClose: () => void
  onSubmit: (bankReference: string, paidAt?: string) => Promise<void>
}

/** Records a bank transfer reference (payout or payout batch → `paid`). */
export function MarkPaidModal({ open, ...props }: MarkPaidModalProps) {
  return open ? <MarkPaidDialog {...props} /> : null
}

function MarkPaidDialog({ title, description, withPaidAt = false, onClose, onSubmit }: Omit<MarkPaidModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [reference, setReference] = useState('')
  const [paidAt, setPaidAt] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    if (!reference.trim()) {
      setError(t('fieldRequired'))
      return
    }
    setSubmitting(true)
    try {
      await onSubmit(reference.trim(), withPaidAt ? (fromLocalInput(paidAt) ?? undefined) : undefined)
    } catch (caught) {
      setFormError(describe(caught))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={title}
      description={description}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button variant="brand" icon="check" onClick={submit} loading={submitting}>
            {t('markPaid')}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <Input
          id="bank-reference"
          dir="ltr"
          label={t('bankReference')}
          value={reference}
          error={error}
          onChange={(event) => {
            setReference(event.target.value)
            setError(null)
          }}
          maxLength={100}
          autoFocus
        />
        {withPaidAt && (
          <Input id="paid-at" type="datetime-local" dir="ltr" label={t('paidAtOptional')} value={paidAt} onChange={(event) => setPaidAt(event.target.value)} />
        )}
        {formError && (
          <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>{formError}</p>
          </div>
        )}
      </div>
    </Modal>
  )
}
