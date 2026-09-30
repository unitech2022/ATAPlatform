import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { corporateAccounts } from '../lib/admin'
import { periodStartOf, previousMonth } from '../lib/corporate'
import type { CorporateInvoice } from '../lib/types'
import { Button } from './Button'
import { Input, Select } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

export interface CorporateInvoiceGenerateModalProps {
  open: boolean
  /** Fixed company (detail page); when omitted the admin picks one from the account list. */
  accountId?: string
  onClose: () => void
  onGenerated: (invoice: CorporateInvoice | null) => void
}

/** Manual invoice generation for a month (`POST /admin/corporate/accounts/{id}/invoices/generate { periodStart }`, §F19.4). */
export function CorporateInvoiceGenerateModal({ open, ...props }: CorporateInvoiceGenerateModalProps) {
  return open ? <GenerateDialog {...props} /> : null
}

function GenerateDialog({ accountId, onClose, onGenerated }: Omit<CorporateInvoiceGenerateModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const accounts = useQuery(() => (accountId ? Promise.resolve(null) : corporateAccounts.list({ page: 1, pageSize: 200 })), `corporate-accounts-all:${accountId ?? ''}`)
  const [selected, setSelected] = useState(accountId ?? '')
  const [month, setMonth] = useState(previousMonth())
  const [errors, setErrors] = useState<{ account?: string; month?: string }>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const periodStart = periodStartOf(month)
    const found: { account?: string; month?: string } = {}
    if (!selected) found.account = t('fieldRequired')
    if (!periodStart) found.month = t('fieldRequired')
    setErrors(found)
    if (!periodStart || !selected) return
    setSaving(true)
    try {
      onGenerated((await corporateAccounts.generateInvoice(selected, periodStart)) ?? null)
    } catch (error) {
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={t('coInvGenerateTitle')}
      description={t('coInvGenerateCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="corporate-generate-form" icon="receipt" loading={saving}>
            {t('coInvGenerate')}
          </Button>
        </>
      }
    >
      <form id="corporate-generate-form" onSubmit={submit} noValidate className="space-y-4">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}
        {!accountId && (
          <Select
            id="co-gen-account"
            label={t('coColCompany')}
            value={selected}
            error={errors.account}
            disabled={accounts.loading && !accounts.data}
            onChange={(event) => {
              setSelected(event.target.value)
              setErrors({})
            }}
          >
            <option value="">{t('select')}</option>
            {(accounts.data?.items ?? []).map((account) => (
              <option key={account.id} value={account.id}>
                {account.displayName} · {account.accountNumber}
              </option>
            ))}
          </Select>
        )}
        <Input
          id="co-gen-month"
          type="month"
          dir="ltr"
          label={t('coInvMonth')}
          hint={t('coInvMonthHint')}
          value={month}
          error={errors.month}
          onChange={(event) => {
            setMonth(event.target.value)
            setErrors({})
          }}
        />
      </form>
    </Modal>
  )
}
