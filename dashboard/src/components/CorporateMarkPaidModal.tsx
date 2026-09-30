import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { corporateInvoices } from '../lib/admin'
import { invoiceRemaining } from '../lib/corporate'
import { formatMoney } from '../lib/format'
import { fromLocalInput } from '../lib/pricing'
import type { CorporateInvoice } from '../lib/types'
import { Button } from './Button'
import { Input } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

export interface CorporateMarkPaidModalProps {
  invoice: CorporateInvoice | null
  onClose: () => void
  onSaved: () => void
}

/** Records a (possibly partial) payment of an invoice (`POST …/mark-paid { amount, reference, paidAt? }`, §F19.4). */
export function CorporateMarkPaidModal({ invoice, ...props }: CorporateMarkPaidModalProps) {
  return invoice ? <MarkPaidDialog invoice={invoice} {...props} /> : null
}

function MarkPaidDialog({ invoice, onClose, onSaved }: Omit<CorporateMarkPaidModalProps, 'invoice'> & { invoice: CorporateInvoice }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const remaining = invoiceRemaining(invoice)
  const [amount, setAmount] = useState(String(remaining))
  const [reference, setReference] = useState('')
  const [paidAt, setPaidAt] = useState('')
  const [errors, setErrors] = useState<{ amount?: string; reference?: string }>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const value = Number(amount)
  const partial = Number.isFinite(value) && value > 0 && value < remaining

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const found: { amount?: string; reference?: string } = {}
    if (!Number.isFinite(value) || value <= 0 || value > remaining) found.amount = t('coErrPayAmount')
    if (!reference.trim()) found.reference = t('fieldRequired')
    setErrors(found)
    if (found.amount || found.reference) return
    setSaving(true)
    try {
      await corporateInvoices.markPaid(invoice.id, { amount: Math.round(value * 100) / 100, reference: reference.trim(), paidAt: fromLocalInput(paidAt) ?? undefined })
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
      title={`${t('coInvPayTitle')} · ${invoice.invoiceNumber}`}
      description={`${t('coInvRemaining')}: ${formatMoney(remaining)} ${t('sar')}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="corporate-pay-form" variant="brand" icon="check" loading={saving}>
            {t('markPaid')}
          </Button>
        </>
      }
    >
      <form id="corporate-pay-form" onSubmit={submit} noValidate className="space-y-4">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}
        <Input
          id="co-pay-amount"
          type="number"
          step="0.01"
          min={0}
          max={remaining}
          dir="ltr"
          label={`${t('coInvPayAmount')} (${t('sar')})`}
          hint={partial ? t('coInvPartialNote') : undefined}
          value={amount}
          error={errors.amount}
          onChange={(event) => {
            setAmount(event.target.value)
            setErrors({})
          }}
          autoFocus
        />
        <Input
          id="co-pay-reference"
          dir="ltr"
          label={t('coInvPayReference')}
          value={reference}
          error={errors.reference}
          maxLength={100}
          onChange={(event) => {
            setReference(event.target.value)
            setErrors({})
          }}
        />
        <Input id="co-pay-date" type="datetime-local" dir="ltr" label={t('paidAtOptional')} value={paidAt} onChange={(event) => setPaidAt(event.target.value)} />
      </form>
    </Modal>
  )
}
