import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { corporateAccounts } from '../lib/admin'
import { Button } from './Button'
import { Input } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

export interface CorporateAdjustmentModalProps {
  open: boolean
  accountId: string
  onClose: () => void
  onSaved: () => void
}

/** Signed VAT-inclusive adjustment billed on the next invoice (`POST …/adjustments`, §F19.4). */
export function CorporateAdjustmentModal({ open, ...props }: CorporateAdjustmentModalProps) {
  return open ? <AdjustmentDialog {...props} /> : null
}

function AdjustmentDialog({ accountId, onClose, onSaved }: Omit<CorporateAdjustmentModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [amount, setAmount] = useState('')
  const [description, setDescription] = useState('')
  const [errors, setErrors] = useState<{ amount?: string; description?: string }>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const value = Number(amount)
    const found: { amount?: string; description?: string } = {}
    if (!amount.trim() || !Number.isFinite(value) || value === 0) found.amount = t('coErrAdjustmentAmount')
    if (!description.trim()) found.description = t('fieldRequired')
    setErrors(found)
    if (found.amount || found.description) return
    setSaving(true)
    try {
      await corporateAccounts.addAdjustment(accountId, { amount: Math.round(value * 100) / 100, description: description.trim() })
      onSaved()
    } catch (error) {
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={t('coAdjTitle')}
      description={t('coAdjCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="corporate-adjustment-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="corporate-adjustment-form" onSubmit={submit} noValidate className="space-y-4">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}
        <Input
          id="co-adj-amount"
          type="number"
          step="0.01"
          dir="ltr"
          label={`${t('coAdjAmount')} (${t('sar')})`}
          hint={t('coAdjAmountHint')}
          value={amount}
          error={errors.amount}
          onChange={(event) => {
            setAmount(event.target.value)
            setErrors({})
          }}
          autoFocus
        />
        <Input
          id="co-adj-description"
          label={t('coAdjDescription')}
          value={description}
          error={errors.description}
          maxLength={255}
          onChange={(event) => {
            setDescription(event.target.value)
            setErrors({})
          }}
        />
      </form>
    </Modal>
  )
}
