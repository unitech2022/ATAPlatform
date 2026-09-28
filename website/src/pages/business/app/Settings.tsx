import { useState, type FormEvent } from 'react'
import { ConfirmModal, PageHeader, Pill, TableWrap, Td, Th } from '../../../components/business/ui'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Field, Input } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { useFlash } from '../../../lib/useFlash'
import { corporateApi, isApiError } from '../../../lib/api'
import { describeError, fieldErrorsFrom } from '../../../lib/errors'
import { formatDate, formatMoney, isValidLocalPhone, normalizeLocalPhone } from '../../../lib/format'
import type { CorporateAccount, CorporateApiKey, NationalAddress } from '../../../lib/types'
import { useResource } from '../../../lib/useResource'

const emptyAddress: NationalAddress = {
  buildingNumber: '',
  street: '',
  district: '',
  city: '',
  postalCode: '',
  additionalNumber: '',
  countryCode: 'SA',
}

const addressFields: { key: Exclude<keyof NationalAddress, 'countryCode'>; digits?: number }[] = [
  { key: 'buildingNumber', digits: 4 },
  { key: 'street' },
  { key: 'district' },
  { key: 'city' },
  { key: 'postalCode', digits: 5 },
  { key: 'additionalNumber', digits: 4 },
]

function AccountForm({ account, onSaved }: { account: CorporateAccount; onSaved: () => void }) {
  const { t } = useI18n()
  const [email, setEmail] = useState(account.billingEmail)
  const [contactName, setContactName] = useState(account.contactName)
  const [contactPhone, setContactPhone] = useState(normalizeLocalPhone(account.contactPhone))
  const [address, setAddress] = useState<NationalAddress>({ ...emptyAddress, ...(account.billingAddress ?? {}) })
  const [busy, setBusy] = useState(false)
  const [touched, setTouched] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  const emailInvalid = !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())
  const phoneInvalid = !isValidLocalPhone(contactPhone)
  const nameInvalid = contactName.trim() === ''
  const addressInvalid = addressFields.some(({ key, digits }) => {
    const value = address[key].trim()
    return value === '' || (digits !== undefined && !new RegExp(`^\\d{${digits}}$`).test(value))
  })

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setTouched(true)
    if (emailInvalid || phoneInvalid || nameInvalid || addressInvalid) return
    setBusy(true)
    setError(null)
    setFieldErrors({})
    try {
      await corporateApi.updateAccount({
        billingEmail: email.trim(),
        contactName: contactName.trim(),
        contactPhone: `+966${contactPhone}`,
        billingAddress: { ...address, countryCode: 'SA' },
      })
      onSaved()
    } catch (caught) {
      setError(describeError(caught, t))
      setFieldErrors(fieldErrorsFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={submit} noValidate className="grid gap-4 sm:grid-cols-2">
      <Field label={t('biz.settings.billingEmail')} htmlFor="acc-email" error={(touched && emailInvalid ? t('form.invalidEmail') : null) ?? fieldErrors.billingEmail}>
        <Input id="acc-email" type="email" dir="ltr" value={email} onChange={(event) => setEmail(event.target.value)} />
      </Field>
      <Field label={t('biz.settings.contactName')} htmlFor="acc-contact" error={(touched && nameInvalid ? t('form.required') : null) ?? fieldErrors.contactName}>
        <Input id="acc-contact" value={contactName} onChange={(event) => setContactName(event.target.value)} />
      </Field>
      <Field
        label={t('biz.settings.contactPhone')}
        htmlFor="acc-phone"
        error={(touched && phoneInvalid ? t('login.phone.invalid') : null) ?? fieldErrors.contactPhone}
      >
        <div className="flex items-center gap-2" dir="ltr">
          <span className="grid h-14 place-items-center rounded-2xl bg-cloud px-3 text-sm font-bold">+966</span>
          <Input
            id="acc-phone"
            type="tel"
            inputMode="numeric"
            value={contactPhone}
            onChange={(event) => setContactPhone(normalizeLocalPhone(event.target.value))}
          />
        </div>
      </Field>
      <div className="sm:col-span-2">
        <p className="mb-3 text-sm font-bold">{t('biz.settings.address')}</p>
        <div className="grid gap-4 sm:grid-cols-3">
          {addressFields.map(({ key, digits }) => {
            const value = address[key].trim()
            const invalid = touched && (value === '' || (digits !== undefined && !new RegExp(`^\\d{${digits}}$`).test(value)))
            return (
              <Field
                key={key}
                label={t(`biz.address.${key}`)}
                htmlFor={`addr-${key}`}
                error={invalid ? (digits ? t('biz.address.digits', { n: digits }) : t('form.required')) : null}
              >
                <Input
                  id={`addr-${key}`}
                  value={address[key]}
                  inputMode={digits ? 'numeric' : undefined}
                  dir={digits ? 'ltr' : undefined}
                  maxLength={digits}
                  invalid={invalid}
                  onChange={(event) => setAddress({ ...address, [key]: digits ? event.target.value.replace(/\D/g, '') : event.target.value })}
                />
              </Field>
            )
          })}
        </div>
      </div>
      {error && (
        <Notice tone="error" className="sm:col-span-2">
          {error}
        </Notice>
      )}
      <div className="flex justify-end sm:col-span-2">
        <Button type="submit" disabled={busy}>
          {busy ? t('action.saving') : t('action.save')}
        </Button>
      </div>
    </form>
  )
}

function ApiKeys() {
  const { t, lang } = useI18n()
  const keys = useResource(() => corporateApi.apiKeys(), [lang])
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [created, setCreated] = useState<CorporateApiKey | null>(null)
  const [revoking, setRevoking] = useState<CorporateApiKey | null>(null)

  // `404` means the feature is disabled server-side (Corporate:ApiKeysEnabled=false): hide the section.
  // It also stays hidden until the first response so it never flashes in and out.
  if ((keys.loading && !keys.data) || (isApiError(keys.error) && keys.error.status === 404)) return null

  const create = async (event: FormEvent) => {
    event.preventDefault()
    if (!name.trim()) return
    setBusy(true)
    setError(null)
    try {
      setCreated(await corporateApi.createApiKey(name.trim()))
      setName('')
      keys.reload(true)
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
    }
  }

  const revoke = async () => {
    if (!revoking) return
    setBusy(true)
    try {
      await corporateApi.revokeApiKey(revoking.id)
      keys.reload(true)
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
      setRevoking(null)
    }
  }

  return (
    <Card>
      <h2 className="text-lg font-bold">{t('biz.apiKeys.title')}</h2>
      <p className="mt-1 text-sm leading-6 text-muted">{t('biz.apiKeys.copy')}</p>
      <form onSubmit={create} className="mt-4 flex flex-col gap-3 sm:flex-row">
        <Input aria-label={t('biz.apiKeys.name')} placeholder={t('biz.apiKeys.name')} value={name} onChange={(event) => setName(event.target.value)} />
        <Button type="submit" disabled={busy || !name.trim()} className="shrink-0">
          <Icon name="plus" className="size-4" />
          {t('biz.apiKeys.create')}
        </Button>
      </form>
      {created?.key && (
        <Notice tone="success" className="mt-4">
          {t('biz.apiKeys.createdOnce')}
          <code className="mt-2 block break-all rounded-xl bg-white p-3 text-xs text-ink" dir="ltr">
            {created.key}
          </code>
        </Notice>
      )}
      {error && (
        <Notice tone="error" className="mt-4">
          {error}
        </Notice>
      )}
      <div className="mt-4">
        {keys.error && !keys.data ? (
          <ErrorState error={keys.error} onRetry={() => keys.reload()} />
        ) : keys.data && keys.data.length > 0 ? (
          <TableWrap>
            <thead>
              <tr>
                <Th>{t('biz.apiKeys.name')}</Th>
                <Th>{t('biz.apiKeys.prefix')}</Th>
                <Th>{t('biz.apiKeys.lastUsed')}</Th>
                <Th>{t('biz.trip.status')}</Th>
                <Th>
                  <span className="sr-only">{t('biz.actions')}</span>
                </Th>
              </tr>
            </thead>
            <tbody>
              {keys.data.map((key) => (
                <tr key={key.id}>
                  <Td className="font-bold">{key.name}</Td>
                  <Td>
                    <code dir="ltr">ata_live_{key.keyPrefix}…</code>
                  </Td>
                  <Td className="whitespace-nowrap text-muted">{key.lastUsedAt ? formatDate(key.lastUsedAt, lang) : '—'}</Td>
                  <Td>{key.revokedAt ? <Pill tone="muted">{t('biz.apiKeys.revoked')}</Pill> : <Pill tone="brand">{t('biz.active')}</Pill>}</Td>
                  <Td>
                    {!key.revokedAt && (
                      <Button variant="ghost" size="sm" className="text-danger hover:bg-danger-soft" onClick={() => setRevoking(key)}>
                        {t('biz.apiKeys.revoke')}
                      </Button>
                    )}
                  </Td>
                </tr>
              ))}
            </tbody>
          </TableWrap>
        ) : (
          <p className="text-sm text-muted">{t('biz.apiKeys.empty')}</p>
        )}
      </div>
      <ConfirmModal
        open={revoking !== null}
        title={t('biz.apiKeys.revoke')}
        message={t('biz.apiKeys.revokeCopy', { name: revoking?.name ?? '' })}
        confirmLabel={t('biz.apiKeys.revoke')}
        danger
        busy={busy}
        onConfirm={() => void revoke()}
        onClose={() => setRevoking(null)}
      />
    </Card>
  )
}

export function Settings() {
  const { t, lang } = useI18n()
  const account = useResource(() => corporateApi.account(), [lang])
  const [flash, setFlash] = useFlash()
  const data = account.data

  return (
    <>
      <title>{t('biz.nav.settings')} · ATA</title>
      <PageHeader title={t('biz.settings.title')} subtitle={t('biz.settings.subtitle')} />
      {flash && (
        <Notice tone="success" className="mb-4">
          {flash}
        </Notice>
      )}
      {account.loading && !data ? (
        <LoadingState />
      ) : account.error && !data ? (
        <ErrorState error={account.error} onRetry={() => account.reload()} />
      ) : data ? (
        <div className="space-y-4">
          <Card>
            <h2 className="mb-4 text-lg font-bold">{t('biz.settings.legal')}</h2>
            <dl className="grid gap-4 text-sm sm:grid-cols-2 lg:grid-cols-3">
              {(
                [
                  [t('biz.settings.accountNumber'), data.accountNumber, true],
                  [t('biz.settings.legalNameAr'), data.legalNameAr, false],
                  [t('biz.settings.legalNameEn'), data.legalNameEn, true],
                  [t('biz.settings.cr'), data.crNumber, true],
                  [t('biz.settings.vat'), data.vatNumber ?? '—', true],
                  [t('biz.settings.status'), t(`biz.accountStatus.${data.status}`), false],
                  [t('biz.settings.creditLimit'), formatMoney(data.creditLimit, lang), false],
                  [t('biz.settings.terms'), t('biz.settings.termsDays', { n: data.paymentTermsDays }), false],
                ] as const
              ).map(([label, value, ltr]) => (
                <div key={label}>
                  <dt className="text-xs font-bold text-muted">{label}</dt>
                  <dd className="font-bold" dir={ltr ? 'ltr' : undefined}>
                    {value}
                  </dd>
                </div>
              ))}
            </dl>
            <p className="mt-4 text-xs text-muted">{t('biz.settings.legalNote')}</p>
          </Card>
          <Card>
            <h2 className="mb-4 text-lg font-bold">{t('biz.settings.billing')}</h2>
            <AccountForm
              key={data.id}
              account={data}
              onSaved={() => {
                setFlash(t('biz.saved'))
                account.reload(true)
              }}
            />
          </Card>
          <ApiKeys />
        </div>
      ) : null}
    </>
  )
}
