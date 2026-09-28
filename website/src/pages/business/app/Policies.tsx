import { useState, type FormEvent } from 'react'
import { ConfirmModal, Modal, PageHeader, Pill, Toggle } from '../../../components/business/ui'
import { Action, Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Field, Input, Select } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { EmptyState, ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { useFlash } from '../../../lib/useFlash'
import { catalogApi, corporateApi } from '../../../lib/api'
import { WEEK_DAYS } from '../../../lib/corporate'
import { describeError, fieldErrorsFrom } from '../../../lib/errors'
import { formatMoney } from '../../../lib/format'
import type { CorporatePolicy, CorporatePolicyPayload, RideCategory, TimeWindow } from '../../../lib/types'
import { useResource } from '../../../lib/useResource'

const emptyPolicy: CorporatePolicyPayload = {
  name: '',
  allowedRideCategoryIds: null,
  allowedDays: null,
  timeWindows: null,
  allowedZoneIds: null,
  zoneMatch: 'pickup_and_dropoff',
  maxFarePerTrip: null,
  monthlyBudgetPerEmployee: null,
  requirePurpose: false,
  requireCostCenter: false,
  allowScheduled: true,
  allowGuestBooking: true,
  isActive: true,
}

function toPayload(policy: CorporatePolicy): CorporatePolicyPayload {
  const { id: _id, isDefault: _isDefault, ...rest } = policy
  return rest
}

function parseAmount(value: string): number | null | 'invalid' {
  if (value.trim() === '') return null
  const number = Number(value)
  return Number.isFinite(number) && number >= 0 ? number : 'invalid'
}

function PolicyForm({
  initial,
  categories,
  onSubmit,
  onCancel,
}: {
  initial: CorporatePolicyPayload
  categories: RideCategory[]
  onSubmit: (payload: CorporatePolicyPayload) => Promise<void>
  onCancel: () => void
}) {
  const { t } = useI18n()
  const [form, setForm] = useState<CorporatePolicyPayload>(initial)
  const [maxFare, setMaxFare] = useState(initial.maxFarePerTrip === null ? '' : String(initial.maxFarePerTrip))
  const [budget, setBudget] = useState(initial.monthlyBudgetPerEmployee === null ? '' : String(initial.monthlyBudgetPerEmployee))
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [touched, setTouched] = useState(false)

  const patch = (next: Partial<CorporatePolicyPayload>) => setForm((current) => ({ ...current, ...next }))

  const restrictCategories = form.allowedRideCategoryIds !== null
  const restrictDays = form.allowedDays !== null
  const restrictTime = form.timeWindows !== null

  const toggleIn = <T,>(list: T[] | null, value: T): T[] =>
    (list ?? []).includes(value) ? (list ?? []).filter((item) => item !== value) : [...(list ?? []), value]

  const maxFareValue = parseAmount(maxFare)
  const budgetValue = parseAmount(budget)
  const windowsInvalid = (form.timeWindows ?? []).some((window) => !window.from || !window.to)
  const invalid =
    form.name.trim() === '' ||
    maxFareValue === 'invalid' ||
    budgetValue === 'invalid' ||
    windowsInvalid ||
    (restrictCategories && form.allowedRideCategoryIds?.length === 0) ||
    (restrictDays && form.allowedDays?.length === 0) ||
    (restrictTime && form.timeWindows?.length === 0)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setTouched(true)
    if (invalid) return
    setBusy(true)
    setError(null)
    setFieldErrors({})
    try {
      await onSubmit({ ...form, name: form.name.trim(), maxFarePerTrip: maxFareValue, monthlyBudgetPerEmployee: budgetValue })
    } catch (caught) {
      setError(describeError(caught, t))
      setFieldErrors(fieldErrorsFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  const updateWindow = (index: number, next: Partial<TimeWindow>) =>
    patch({ timeWindows: (form.timeWindows ?? []).map((window, i) => (i === index ? { ...window, ...next } : window)) })

  return (
    <form onSubmit={submit} noValidate className="space-y-6">
      <Field label={t('biz.policy.name')} htmlFor="policy-name" error={(touched && !form.name.trim() ? t('form.required') : null) ?? fieldErrors.name}>
        <Input id="policy-name" value={form.name} onChange={(event) => patch({ name: event.target.value })} />
      </Field>

      <fieldset className="space-y-3">
        <legend className="mb-2 text-sm font-bold">{t('biz.policy.categories')}</legend>
        <Toggle
          checked={restrictCategories}
          onChange={(on) => patch({ allowedRideCategoryIds: on ? categories.map((category) => category.id) : null })}
          label={t('biz.policy.restrictCategories')}
          hint={t('biz.policy.restrictCategoriesHint')}
        />
        {restrictCategories && (
          <div className="flex flex-wrap gap-2">
            {categories.map((category) => {
              const on = form.allowedRideCategoryIds?.includes(category.id) ?? false
              return (
                <Action
                  key={category.id}
                  aria-pressed={on}
                  onClick={() => patch({ allowedRideCategoryIds: toggleIn(form.allowedRideCategoryIds, category.id) })}
                  className={`rounded-full border px-4 py-2 text-sm font-bold ${on ? 'border-brand bg-brand-soft text-brand' : 'border-line text-muted'}`}
                >
                  {category.name}
                </Action>
              )
            })}
          </div>
        )}
        {touched && restrictCategories && form.allowedRideCategoryIds?.length === 0 && (
          <p className="text-xs font-bold text-danger">{t('biz.policy.pickOne')}</p>
        )}
      </fieldset>

      <fieldset className="space-y-3">
        <legend className="mb-2 text-sm font-bold">{t('biz.policy.days')}</legend>
        <Toggle
          checked={restrictDays}
          onChange={(on) => patch({ allowedDays: on ? [0, 1, 2, 3, 4] : null })}
          label={t('biz.policy.restrictDays')}
        />
        {restrictDays && (
          <div className="flex flex-wrap gap-2">
            {WEEK_DAYS.map((day) => {
              const on = form.allowedDays?.includes(day) ?? false
              return (
                <Action
                  key={day}
                  aria-pressed={on}
                  onClick={() => patch({ allowedDays: toggleIn(form.allowedDays, day).sort((a, b) => a - b) })}
                  className={`rounded-full border px-4 py-2 text-sm font-bold ${on ? 'border-brand bg-brand-soft text-brand' : 'border-line text-muted'}`}
                >
                  {t(`day.${day}`)}
                </Action>
              )
            })}
          </div>
        )}
        {touched && restrictDays && form.allowedDays?.length === 0 && <p className="text-xs font-bold text-danger">{t('biz.policy.pickOne')}</p>}
      </fieldset>

      <fieldset className="space-y-3">
        <legend className="mb-2 text-sm font-bold">{t('biz.policy.hours')}</legend>
        <Toggle
          checked={restrictTime}
          onChange={(on) => patch({ timeWindows: on ? [{ from: '07:00', to: '22:00' }] : null })}
          label={t('biz.policy.restrictHours')}
          hint={t('biz.policy.hoursHint')}
        />
        {restrictTime && (
          <div className="space-y-2">
            {(form.timeWindows ?? []).map((window, index) => (
              <div key={index} className="flex flex-wrap items-center gap-2" dir="ltr">
                <Input
                  type="time"
                  aria-label={t('biz.policy.from')}
                  className="max-w-36"
                  value={window.from}
                  onChange={(event) => updateWindow(index, { from: event.target.value })}
                />
                <span className="text-muted">–</span>
                <Input
                  type="time"
                  aria-label={t('biz.policy.to')}
                  className="max-w-36"
                  value={window.to}
                  onChange={(event) => updateWindow(index, { to: event.target.value })}
                />
                <Action
                  aria-label={t('action.delete')}
                  onClick={() => patch({ timeWindows: (form.timeWindows ?? []).filter((_, i) => i !== index) })}
                  className="grid size-11 place-items-center rounded-xl text-muted hover:bg-danger-soft hover:text-danger"
                >
                  <Icon name="trash" className="size-5" />
                </Action>
              </div>
            ))}
            <Button
              variant="ghost"
              size="sm"
              onClick={() => patch({ timeWindows: [...(form.timeWindows ?? []), { from: '07:00', to: '22:00' }] })}
            >
              <Icon name="plus" className="size-4" />
              {t('biz.policy.addWindow')}
            </Button>
          </div>
        )}
      </fieldset>

      {form.allowedZoneIds !== null && (
        <div className="space-y-3 rounded-2xl bg-cloud p-4">
          <p className="text-sm font-bold">{t('biz.policy.zones', { n: form.allowedZoneIds.length })}</p>
          <p className="text-xs leading-5 text-muted">{t('biz.policy.zonesHint')}</p>
          <Select
            aria-label={t('biz.policy.zoneMatch')}
            value={form.zoneMatch}
            onChange={(event) => patch({ zoneMatch: event.target.value === 'pickup_or_dropoff' ? 'pickup_or_dropoff' : 'pickup_and_dropoff' })}
          >
            <option value="pickup_and_dropoff">{t('biz.policy.zoneMatch.pickup_and_dropoff')}</option>
            <option value="pickup_or_dropoff">{t('biz.policy.zoneMatch.pickup_or_dropoff')}</option>
          </Select>
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <Field
          label={t('biz.policy.maxFare')}
          htmlFor="policy-max-fare"
          hint={t('biz.policy.noLimitHint')}
          error={(touched && maxFareValue === 'invalid' ? t('form.invalidAmount') : null) ?? fieldErrors.maxFarePerTrip}
        >
          <Input id="policy-max-fare" inputMode="decimal" dir="ltr" value={maxFare} onChange={(event) => setMaxFare(event.target.value.replace(/[^\d.]/g, ''))} />
        </Field>
        <Field
          label={t('biz.policy.budget')}
          htmlFor="policy-budget"
          hint={t('biz.policy.noLimitHint')}
          error={(touched && budgetValue === 'invalid' ? t('form.invalidAmount') : null) ?? fieldErrors.monthlyBudgetPerEmployee}
        >
          <Input id="policy-budget" inputMode="decimal" dir="ltr" value={budget} onChange={(event) => setBudget(event.target.value.replace(/[^\d.]/g, ''))} />
        </Field>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <Toggle checked={form.requirePurpose} onChange={(on) => patch({ requirePurpose: on })} label={t('biz.policy.requirePurpose')} />
        <Toggle checked={form.requireCostCenter} onChange={(on) => patch({ requireCostCenter: on })} label={t('biz.policy.requireCostCenter')} />
        <Toggle checked={form.allowScheduled} onChange={(on) => patch({ allowScheduled: on })} label={t('biz.policy.allowScheduled')} />
        <Toggle checked={form.allowGuestBooking} onChange={(on) => patch({ allowGuestBooking: on })} label={t('biz.policy.allowGuest')} />
        <Toggle checked={form.isActive} onChange={(on) => patch({ isActive: on })} label={t('biz.policy.active')} />
      </div>

      {touched && windowsInvalid && <p className="text-xs font-bold text-danger">{t('biz.policy.windowInvalid')}</p>}
      {error && <Notice tone="error">{error}</Notice>}

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
        <Button variant="secondary" onClick={onCancel} disabled={busy}>
          {t('action.cancel')}
        </Button>
        <Button type="submit" disabled={busy}>
          {busy ? t('action.saving') : t('action.save')}
        </Button>
      </div>
    </form>
  )
}

function PolicySummary({ policy, categories }: { policy: CorporatePolicy; categories: RideCategory[] }) {
  const { t, lang } = useI18n()
  const names = policy.allowedRideCategoryIds?.map((id) => categories.find((category) => category.id === id)?.name ?? id)
  const rows: [string, string][] = [
    [t('biz.policy.categories'), names ? names.join('، ') : t('biz.policy.all')],
    [t('biz.policy.days'), policy.allowedDays ? policy.allowedDays.map((day) => t(`day.${day as 0 | 1 | 2 | 3 | 4 | 5 | 6}`)).join('، ') : t('biz.policy.all')],
    [
      t('biz.policy.hours'),
      policy.timeWindows && policy.timeWindows.length > 0
        ? policy.timeWindows.map((window) => `${window.from}–${window.to}`).join(' · ')
        : t('biz.policy.anyTime'),
    ],
    [t('biz.policy.maxFare'), policy.maxFarePerTrip === null ? t('biz.policy.noLimit') : formatMoney(policy.maxFarePerTrip, lang)],
    [t('biz.policy.budget'), policy.monthlyBudgetPerEmployee === null ? t('biz.policy.noLimit') : formatMoney(policy.monthlyBudgetPerEmployee, lang)],
  ]
  return (
    <dl className="grid gap-3 text-sm sm:grid-cols-2">
      {rows.map(([label, value]) => (
        <div key={label} className="min-w-0">
          <dt className="text-xs font-bold text-muted">{label}</dt>
          <dd className="font-bold" dir={label === t('biz.policy.hours') ? 'ltr' : undefined}>
            {value}
          </dd>
        </div>
      ))}
    </dl>
  )
}

export function Policies() {
  const { t, lang } = useI18n()
  const policies = useResource(() => corporateApi.policies(), [lang])
  const categories = useResource(() => catalogApi.rideCategories(), [lang])
  const [editing, setEditing] = useState<CorporatePolicy | 'new' | null>(null)
  const [deleting, setDeleting] = useState<CorporatePolicy | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [flash, setFlash] = useFlash()
  const categoryList = categories.data ?? []

  const makeDefault = async (policy: CorporatePolicy) => {
    setError(null)
    try {
      await corporateApi.setDefaultPolicy(policy.id)
      setFlash(t('biz.policy.defaultSet', { name: policy.name }))
      policies.reload(true)
    } catch (caught) {
      setError(describeError(caught, t))
    }
  }

  const remove = async () => {
    if (!deleting) return
    setBusy(true)
    setError(null)
    try {
      await corporateApi.deletePolicy(deleting.id)
      setFlash(t('biz.deleted'))
      policies.reload(true)
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
      setDeleting(null)
    }
  }

  return (
    <>
      <title>{t('biz.nav.policies')} · ATA</title>
      <PageHeader
        title={t('biz.policies.title')}
        subtitle={t('biz.policies.subtitle')}
        actions={
          <Button size="sm" onClick={() => setEditing('new')}>
            <Icon name="plus" className="size-4" />
            {t('biz.policies.new')}
          </Button>
        }
      />
      {flash && (
        <Notice tone="success" className="mb-4">
          {flash}
        </Notice>
      )}
      {error && (
        <Notice tone="error" className="mb-4">
          {error}
        </Notice>
      )}

      {policies.loading && !policies.data ? (
        <LoadingState />
      ) : policies.error && !policies.data ? (
        <ErrorState error={policies.error} onRetry={() => policies.reload()} />
      ) : policies.data && policies.data.length > 0 ? (
        <div className="grid gap-4 xl:grid-cols-2">
          {policies.data.map((policy) => (
            <Card key={policy.id} className="flex flex-col">
              <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
                <div className="min-w-0">
                  <h2 className="truncate text-lg font-bold">{policy.name}</h2>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {policy.isDefault && <Pill tone="solid">{t('biz.policy.default')}</Pill>}
                    {!policy.isActive && <Pill tone="muted">{t('biz.policy.inactive')}</Pill>}
                    {policy.requirePurpose && <Pill tone="brand">{t('biz.policy.requirePurpose')}</Pill>}
                    {policy.requireCostCenter && <Pill tone="brand">{t('biz.policy.requireCostCenter')}</Pill>}
                  </div>
                </div>
              </div>
              <PolicySummary policy={policy} categories={categoryList} />
              <div className="mt-6 flex flex-wrap gap-2 border-t border-line pt-4">
                <Button variant="secondary" size="sm" onClick={() => setEditing(policy)}>
                  <Icon name="edit" className="size-4" />
                  {t('action.edit')}
                </Button>
                {!policy.isDefault && policy.isActive && (
                  <Button variant="ghost" size="sm" onClick={() => void makeDefault(policy)}>
                    {t('biz.policy.makeDefault')}
                  </Button>
                )}
                {!policy.isDefault && (
                  <Button variant="ghost" size="sm" className="text-danger hover:bg-danger-soft" onClick={() => setDeleting(policy)}>
                    <Icon name="trash" className="size-4" />
                    {t('action.delete')}
                  </Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      ) : (
        <EmptyState icon="shield" title={t('biz.policies.empty')} />
      )}

      <Modal
        open={editing !== null}
        title={editing === 'new' ? t('biz.policies.new') : t('biz.policies.edit')}
        onClose={() => setEditing(null)}
        wide
      >
        {editing !== null && (
          <PolicyForm
            initial={editing === 'new' ? emptyPolicy : toPayload(editing)}
            categories={categoryList}
            onCancel={() => setEditing(null)}
            onSubmit={async (payload) => {
              if (editing === 'new') await corporateApi.createPolicy(payload)
              else await corporateApi.updatePolicy(editing.id, payload)
              setEditing(null)
              setFlash(t('biz.saved'))
              policies.reload(true)
            }}
          />
        )}
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('biz.policies.deleteTitle')}
        message={t('biz.policies.deleteCopy', { name: deleting?.name ?? '' })}
        confirmLabel={t('action.delete')}
        danger
        busy={busy}
        onConfirm={() => void remove()}
        onClose={() => setDeleting(null)}
      />
    </>
  )
}
