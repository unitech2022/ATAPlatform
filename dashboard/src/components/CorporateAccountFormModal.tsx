import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import type { TranslationKey } from '../i18n'
import { corporateAccounts } from '../lib/admin'
import { isApiError } from '../lib/api'
import { digitsOnly, validateCorporateAccount, type CorporateAccountErrorField } from '../lib/corporate'
import type { CorporateAccount, CorporateAccountInput } from '../lib/types'
import { Button } from './Button'
import { CityField } from './CityField'
import { Input, Textarea } from './Field'
import { FormSection } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'

interface FormState {
  legalNameAr: string
  legalNameEn: string
  displayName: string
  crNumber: string
  vatNumber: string
  billingEmail: string
  creditLimit: string
  paymentTermsDays: string
  buildingNumber: string
  street: string
  district: string
  addressCity: string
  postalCode: string
  additionalNumber: string
  cityId: string
  contactName: string
  contactPhone: string
  notes: string
  adminPhone: string
  adminName: string
}

function toForm(account: CorporateAccount | null): FormState {
  const address = account?.billingAddress
  return {
    legalNameAr: account?.legalNameAr ?? '',
    legalNameEn: account?.legalNameEn ?? '',
    displayName: account?.displayName ?? '',
    crNumber: account?.crNumber ?? '',
    vatNumber: account?.vatNumber ?? '',
    billingEmail: account?.billingEmail ?? '',
    creditLimit: account ? String(account.creditLimit) : '0',
    paymentTermsDays: account ? String(account.paymentTermsDays) : '30',
    buildingNumber: address?.buildingNumber ?? '',
    street: address?.street ?? '',
    district: address?.district ?? '',
    addressCity: address?.city ?? '',
    postalCode: address?.postalCode ?? '',
    additionalNumber: address?.additionalNumber ?? '',
    cityId: account?.cityId ?? '',
    contactName: account?.contactName ?? '',
    contactPhone: account?.contactPhone ?? '',
    notes: account?.notes ?? '',
    adminPhone: '',
    adminName: '',
  }
}

function toInput(form: FormState): CorporateAccountInput {
  return {
    legalNameAr: form.legalNameAr.trim(),
    legalNameEn: form.legalNameEn.trim(),
    displayName: form.displayName.trim(),
    crNumber: form.crNumber,
    vatNumber: form.vatNumber ? form.vatNumber : null,
    billingEmail: form.billingEmail.trim(),
    billingAddress: {
      buildingNumber: form.buildingNumber.trim(),
      street: form.street.trim(),
      district: form.district.trim(),
      city: form.addressCity.trim(),
      postalCode: form.postalCode.trim(),
      additionalNumber: form.additionalNumber.trim(),
      countryCode: 'SA',
    },
    cityId: form.cityId || null,
    contactName: form.contactName.trim(),
    contactPhone: form.contactPhone.trim(),
    creditLimit: Number(form.creditLimit),
    billingCycle: 'monthly',
    paymentTermsDays: Number(form.paymentTermsDays),
    notes: form.notes.trim() || null,
  }
}

export interface CorporateAccountFormModalProps {
  open: boolean
  /** Full account to edit (`GET /admin/corporate/accounts/{id}`); null creates one (status `pending`). */
  account: CorporateAccount | null
  onClose: () => void
  /** `adminInvited` is false when the optional first-admin invitation was requested but failed. */
  onSaved: (account: CorporateAccount | null, adminInvited: boolean) => void
}

export function CorporateAccountFormModal({ open, ...props }: CorporateAccountFormModalProps) {
  return open ? <CorporateAccountDialog {...props} /> : null
}

function CorporateAccountDialog({ account, onClose, onSaved }: Omit<CorporateAccountFormModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(() => toForm(account))
  const [errors, setErrors] = useState<Partial<Record<CorporateAccountErrorField, TranslationKey>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors({})
    setFormError(null)
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const input = toInput(form)
    const found = validateCorporateAccount(input, form.adminPhone.trim())
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t('fixErrors'))
      return
    }
    setSaving(true)
    try {
      const saved = account ? await corporateAccounts.update(account.id, input) : await corporateAccounts.create(input)
      let invited = true
      const adminPhone = form.adminPhone.trim()
      if (!account && adminPhone && saved?.id) {
        try {
          await corporateAccounts.inviteAdmin(saved.id, { phoneNumber: adminPhone, fullName: form.adminName.trim() })
        } catch {
          invited = false
        }
      }
      onSaved(saved ?? null, invited)
    } catch (error) {
      if (isApiError(error) && error.status === 409 && error.code === 'conflict') {
        // `cr_number` is UNIQUE (§F19.1).
        setErrors({ crNumber: 'coErrCrTaken' })
        setFormError(describe(error))
      } else setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  const err = (key: CorporateAccountErrorField) => (errors[key] ? t(errors[key]) : undefined)

  return (
    <Modal
      open
      size="xl"
      title={account ? `${t('coFormEditTitle')} · ${account.accountNumber}` : t('coFormNewTitle')}
      description={account ? t('coFormEditCopy') : t('coFormNewCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="corporate-account-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="corporate-account-form" onSubmit={save} noValidate className="space-y-5">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}

        <FormSection title={t('coSecLegal')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="co-legal-ar" label={t('coLegalNameAr')} value={form.legalNameAr} error={err('legalNameAr')} maxLength={200} onChange={(event) => set('legalNameAr', event.target.value)} />
            <Input id="co-legal-en" dir="ltr" label={t('coLegalNameEn')} value={form.legalNameEn} error={err('legalNameEn')} maxLength={200} onChange={(event) => set('legalNameEn', event.target.value)} />
            <Input id="co-display" label={t('coDisplayName')} value={form.displayName} error={err('displayName')} maxLength={120} onChange={(event) => set('displayName', event.target.value)} />
            <CityField id="co-city" label={t('city')} value={form.cityId} allowAll={false} error={err('cityId')} onChange={(value) => set('cityId', value)} />
            <Input
              id="co-cr"
              dir="ltr"
              inputMode="numeric"
              label={t('coCrNumber')}
              hint={t('coCrHint')}
              value={form.crNumber}
              error={err('crNumber')}
              onChange={(event) => set('crNumber', digitsOnly(event.target.value, 10))}
            />
            <Input
              id="co-vat"
              dir="ltr"
              inputMode="numeric"
              label={t('coVatNumber')}
              hint={t('coVatHint')}
              value={form.vatNumber}
              error={err('vatNumber')}
              onChange={(event) => set('vatNumber', digitsOnly(event.target.value, 15))}
            />
          </div>
        </FormSection>

        <FormSection title={t('coSecBilling')}>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Input id="co-email" type="email" dir="ltr" label={t('coBillingEmail')} value={form.billingEmail} error={err('billingEmail')} maxLength={254} onChange={(event) => set('billingEmail', event.target.value)} wrapperClassName="lg:col-span-2" />
            <Input
              id="co-credit"
              type="number"
              min={0}
              step="0.01"
              dir="ltr"
              label={`${t('coCreditLimit')} (${t('sar')})`}
              value={form.creditLimit}
              error={err('creditLimit')}
              onChange={(event) => set('creditLimit', event.target.value)}
            />
            <Input
              id="co-terms"
              type="number"
              min={0}
              max={365}
              step={1}
              dir="ltr"
              label={t('coPaymentTerms')}
              value={form.paymentTermsDays}
              error={err('paymentTermsDays')}
              onChange={(event) => set('paymentTermsDays', event.target.value)}
            />
            <Input id="co-cycle" label={t('coBillingCycle')} value={t('coBillingMonthly')} disabled readOnly wrapperClassName="lg:col-span-2" />
          </div>
        </FormSection>

        <FormSection title={t('coSecAddress')} description={t('coSecAddressCopy')}>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Input id="co-building" dir="ltr" label={t('coBuildingNumber')} value={form.buildingNumber} maxLength={10} onChange={(event) => set('buildingNumber', event.target.value)} />
            <Input id="co-street" label={t('coStreet')} value={form.street} maxLength={120} onChange={(event) => set('street', event.target.value)} />
            <Input id="co-district" label={t('coDistrict')} value={form.district} maxLength={120} onChange={(event) => set('district', event.target.value)} />
            <Input id="co-address-city" label={t('coAddressCity')} value={form.addressCity} maxLength={120} onChange={(event) => set('addressCity', event.target.value)} />
            <Input id="co-postal" dir="ltr" label={t('coPostalCode')} value={form.postalCode} maxLength={10} onChange={(event) => set('postalCode', event.target.value)} />
            <Input id="co-additional" dir="ltr" label={t('coAdditionalNumber')} value={form.additionalNumber} maxLength={10} onChange={(event) => set('additionalNumber', event.target.value)} />
          </div>
        </FormSection>

        <FormSection title={t('coSecContact')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="co-contact-name" label={t('coContactName')} value={form.contactName} error={err('contactName')} maxLength={120} onChange={(event) => set('contactName', event.target.value)} />
            <Input id="co-contact-phone" type="tel" dir="ltr" label={t('coContactPhone')} hint={t('coPhoneHint')} value={form.contactPhone} error={err('contactPhone')} onChange={(event) => set('contactPhone', event.target.value.trim())} />
            <Textarea id="co-notes" label={t('coNotes')} value={form.notes} maxLength={1000} onChange={(event) => set('notes', event.target.value)} className="min-h-20" wrapperClassName="sm:col-span-2" />
          </div>
        </FormSection>

        {!account && (
          <FormSection title={t('coSecFirstAdmin')} description={t('coSecFirstAdminCopy')}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Input id="co-admin-phone" type="tel" dir="ltr" label={t('coAdminPhone')} hint={t('coPhoneHint')} value={form.adminPhone} error={err('adminPhone')} onChange={(event) => set('adminPhone', event.target.value.trim())} />
              <Input id="co-admin-name" label={t('coAdminName')} value={form.adminName} maxLength={120} onChange={(event) => set('adminName', event.target.value)} />
            </div>
          </FormSection>
        )}
      </form>
    </Modal>
  )
}
