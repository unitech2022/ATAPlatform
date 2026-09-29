import { useMemo, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { incentives, rideCategories, zones } from '../lib/admin'
import { fromLocalInput, localName, toLocalInput, WEEKDAYS } from '../lib/pricing'
import { DRIVER_TIERS, INCENTIVE_TYPE_HINT_KEY, INCENTIVE_TYPE_KEY, INCENTIVE_TYPES, listOrNull, optionalNumber, validateIncentive, type IncentiveErrorField } from '../lib/rewards'
import { driverTierMeta } from '../lib/status'
import type { DriverTier, Incentive, IncentiveInput, IncentiveType } from '../lib/types'
import { Button } from './Button'
import { ChipGroup } from './ChipGroup'
import { CityField } from './CityField'
import { Input, Select, Textarea, Toggle } from './Field'
import { ChoiceField, FormSection } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { ZonePicker } from './ZonePicker'

interface FormState {
  nameAr: string
  nameEn: string
  descriptionAr: string
  descriptionEn: string
  type: IncentiveType
  cityId: string
  zoneIds: string[]
  rideCategoryIds: string[]
  targetTrips: string
  rewardAmount: string
  minTripFare: string
  startsAt: string
  endsAt: string
  daysOfWeek: string[]
  dailyFrom: string
  dailyTo: string
  minTier: DriverTier | ''
  minRating: string
  requiresOptIn: boolean
  maxParticipants: string
  budgetAmount: string
  notifyOnPublish: boolean
  isActive: boolean
}

const str = (value: number | null | undefined) => (value === null || value === undefined ? '' : String(value))

function toForm(incentive: Incentive | null): FormState {
  const start = new Date()
  start.setMinutes(0, 0, 0)
  const end = new Date(start.getTime() + 7 * 24 * 3600 * 1000)
  return {
    nameAr: incentive?.nameAr ?? '',
    nameEn: incentive?.nameEn ?? '',
    descriptionAr: incentive?.descriptionAr ?? '',
    descriptionEn: incentive?.descriptionEn ?? '',
    type: incentive?.type ?? 'weekly',
    cityId: incentive?.cityId ?? '',
    zoneIds: incentive?.zoneIds ?? [],
    rideCategoryIds: incentive?.rideCategoryIds ?? [],
    targetTrips: incentive ? String(incentive.targetTrips) : '10',
    rewardAmount: incentive ? String(incentive.rewardAmount) : '75',
    minTripFare: str(incentive?.minTripFare),
    startsAt: toLocalInput(incentive?.startsAt ?? start.toISOString()),
    endsAt: toLocalInput(incentive?.endsAt ?? end.toISOString()),
    daysOfWeek: (incentive?.daysOfWeek ?? []).map(String),
    dailyFrom: incentive?.dailyFrom?.slice(0, 5) ?? '',
    dailyTo: incentive?.dailyTo?.slice(0, 5) ?? '',
    minTier: incentive?.minTier ?? '',
    minRating: str(incentive?.minRating),
    requiresOptIn: incentive?.requiresOptIn ?? false,
    maxParticipants: str(incentive?.maxParticipants),
    budgetAmount: str(incentive?.budgetAmount),
    notifyOnPublish: incentive?.notifyOnPublish ?? true,
    isActive: incentive?.isActive ?? true,
  }
}

function toInput(form: FormState): IncentiveInput {
  return {
    nameAr: form.nameAr.trim(),
    nameEn: form.nameEn.trim(),
    descriptionAr: form.descriptionAr.trim() || null,
    descriptionEn: form.descriptionEn.trim() || null,
    type: form.type,
    cityId: form.cityId || null,
    zoneIds: listOrNull(form.zoneIds),
    rideCategoryIds: listOrNull(form.rideCategoryIds),
    targetTrips: Number(form.targetTrips),
    rewardAmount: Number(form.rewardAmount),
    minTripFare: optionalNumber(form.minTripFare),
    startsAt: fromLocalInput(form.startsAt) ?? '',
    endsAt: fromLocalInput(form.endsAt) ?? '',
    daysOfWeek: listOrNull(form.daysOfWeek.map(Number).sort((a, b) => a - b)),
    dailyFrom: form.dailyFrom || null,
    dailyTo: form.dailyTo || null,
    minTier: form.minTier || null,
    minRating: optionalNumber(form.minRating),
    requiresOptIn: form.requiresOptIn,
    maxParticipants: optionalNumber(form.maxParticipants),
    budgetAmount: optionalNumber(form.budgetAmount),
    notifyOnPublish: form.notifyOnPublish,
    isActive: form.isActive,
  }
}

export interface IncentiveFormModalProps {
  open: boolean
  incentive: Incentive | null
  onClose: () => void
  onSaved: (incentive: Incentive | null) => void
}

export function IncentiveFormModal({ open, ...props }: IncentiveFormModalProps) {
  return open ? <IncentiveDialog {...props} /> : null
}

function IncentiveDialog({ incentive, onClose, onSaved }: Omit<IncentiveFormModalProps, 'open'>) {
  const { t, lang } = useLang()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(() => toForm(incentive))
  const [errors, setErrors] = useState<Partial<Record<IncentiveErrorField, TranslationKey>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].filter((zone) => !form.cityId || !zone.cityId || zone.cityId === form.cityId).sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data, form.cityId])

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors({})
    setFormError(null)
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const input = toInput(form)
    const found = validateIncentive(input)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t('fixErrors'))
      return
    }
    setSaving(true)
    try {
      const saved = incentive ? await incentives.update(incentive.id, input) : await incentives.create(input)
      onSaved(saved ?? null)
    } catch (error) {
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  const err = (key: IncentiveErrorField) => (errors[key] ? t(errors[key]) : undefined)
  const recurring = form.type === 'daily' || form.type === 'weekly'

  return (
    <Modal
      open
      size="xl"
      title={incentive ? t('icEdit') : t('icNew')}
      description={t('icFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="incentive-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="incentive-form" onSubmit={save} noValidate className="space-y-5">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}

        <FormSection title={t('prSectionBasics')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="ic-name-ar" label={t('nameAr')} value={form.nameAr} error={err('nameAr')} onChange={(event) => set('nameAr', event.target.value)} />
            <Input id="ic-name-en" dir="ltr" label={t('nameEn')} value={form.nameEn} error={err('nameEn')} onChange={(event) => set('nameEn', event.target.value)} />
            <Textarea id="ic-desc-ar" label={t('descriptionAr')} value={form.descriptionAr} onChange={(event) => set('descriptionAr', event.target.value)} className="min-h-20" />
            <Textarea id="ic-desc-en" dir="ltr" label={t('descriptionEn')} value={form.descriptionEn} onChange={(event) => set('descriptionEn', event.target.value)} className="min-h-20" />
            <Select id="ic-type" label={t('type')} hint={t(INCENTIVE_TYPE_HINT_KEY[form.type])} value={form.type} onChange={(event) => set('type', event.target.value as IncentiveType)}>
              {INCENTIVE_TYPES.map((value) => (
                <option key={value} value={value}>
                  {t(INCENTIVE_TYPE_KEY[value])}
                </option>
              ))}
            </Select>
            <CityField
              id="ic-city"
              label={t('city')}
              value={form.cityId}
              error={err('cityId')}
              allowAll={false}
              onChange={(value) => {
                // Zones of another city can no longer match; drop them with the city change.
                const cityZones = new Set((zonesQuery.data ?? []).filter((zone) => !value || !zone.cityId || zone.cityId === value).map((zone) => zone.id))
                setForm((current) => ({ ...current, cityId: value, zoneIds: current.zoneIds.filter((id) => cityZones.has(id)) }))
                setErrors({})
                setFormError(null)
              }}
            />
          </div>
        </FormSection>

        <FormSection title={t('icSectionGoal')}>
          <div className="grid gap-4 sm:grid-cols-3">
            <Input id="ic-target" type="number" min={1} step={1} dir="ltr" label={t('icTargetTrips')} value={form.targetTrips} error={err('targetTrips')} onChange={(event) => set('targetTrips', event.target.value)} />
            <Input id="ic-reward" type="number" min={0} step="0.01" dir="ltr" label={`${t('icReward')} (${t('sar')})`} value={form.rewardAmount} error={err('rewardAmount')} onChange={(event) => set('rewardAmount', event.target.value)} />
            <Input id="ic-min-fare" type="number" min={0} step="0.01" dir="ltr" label={`${t('icMinTripFare')} (${t('sar')})`} hint={t('optional')} value={form.minTripFare} error={err('minTripFare')} onChange={(event) => set('minTripFare', event.target.value)} />
          </div>
        </FormSection>

        <FormSection title={t('icSectionWindow')} description={recurring ? t('icRecurringCopy') : t('icSinglePeriodCopy')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="ic-starts" type="datetime-local" dir="ltr" label={t('startsAt')} value={form.startsAt} error={err('startsAt')} onChange={(event) => set('startsAt', event.target.value)} />
            <Input id="ic-ends" type="datetime-local" dir="ltr" label={t('endsAt')} value={form.endsAt} error={err('endsAt')} onChange={(event) => set('endsAt', event.target.value)} />
            <div className="sm:col-span-2">
              <ChoiceField label={t('dayOfWeek')} hint={form.daysOfWeek.length === 0 ? t('everyDay') : undefined}>
                <ChipGroup options={WEEKDAYS.map(({ day, key }) => ({ value: String(day), label: t(key) }))} value={form.daysOfWeek} onChange={(value) => set('daysOfWeek', value)} />
              </ChoiceField>
            </div>
            <Input id="ic-from" type="time" dir="ltr" label={t('fromTime')} hint={t('icRiyadhTime')} value={form.dailyFrom} error={err('dailyFrom')} onChange={(event) => set('dailyFrom', event.target.value)} />
            <Input id="ic-to" type="time" dir="ltr" label={t('toTime')} hint={t('icWindowHint')} value={form.dailyTo} error={err('dailyTo')} onChange={(event) => set('dailyTo', event.target.value)} />
          </div>
        </FormSection>

        <FormSection title={t('prSectionRestrictions')}>
          <div className="space-y-4">
            <ChoiceField label={t('pickupZone')} error={err('zoneIds')}>
              <ZonePicker zones={zoneList} value={form.zoneIds} onChange={(value) => set('zoneIds', value)} />
            </ChoiceField>
            <ChoiceField label={t('rideCategory')} hint={form.rideCategoryIds.length === 0 ? t('allCategories') : undefined}>
              <ChipGroup options={categories.map((category) => ({ value: category.id, label: localName(category, lang) }))} value={form.rideCategoryIds} onChange={(value) => set('rideCategoryIds', value)} />
            </ChoiceField>
          </div>
        </FormSection>

        <FormSection title={t('icSectionEligibility')}>
          <div className="grid gap-4 sm:grid-cols-3">
            <Select id="ic-min-tier" label={t('icMinTier')} value={form.minTier} onChange={(event) => set('minTier', event.target.value as DriverTier | '')}>
              <option value="">{t('icAnyTier')}</option>
              {DRIVER_TIERS.map((tier) => (
                <option key={tier} value={tier}>
                  {t(driverTierMeta[tier].key)}
                </option>
              ))}
            </Select>
            <Input id="ic-min-rating" type="number" min={1} max={5} step="0.01" dir="ltr" label={t('icMinRating')} hint={t('optional')} value={form.minRating} error={err('minRating')} onChange={(event) => set('minRating', event.target.value)} />
            <Input id="ic-max" type="number" min={1} step={1} dir="ltr" label={t('icMaxParticipants')} hint={t('prUnlimitedHint')} value={form.maxParticipants} error={err('maxParticipants')} onChange={(event) => set('maxParticipants', event.target.value)} />
            <Input
              id="ic-budget"
              type="number"
              min={0}
              step="0.01"
              dir="ltr"
              label={`${t('prBudget')} (${t('sar')})`}
              hint={t('icBudgetHint')}
              value={form.budgetAmount}
              error={err('budgetAmount')}
              onChange={(event) => set('budgetAmount', event.target.value)}
            />
            <div className="grid gap-3 sm:col-span-2 sm:grid-cols-2">
              <Toggle checked={form.requiresOptIn} onChange={(value) => set('requiresOptIn', value)} label={t('icRequiresOptIn')} description={t('icRequiresOptInCopy')} />
              <Toggle checked={form.notifyOnPublish} onChange={(value) => set('notifyOnPublish', value)} label={t('icNotifyOnPublish')} description={t('icNotifyOnPublishCopy')} />
              <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
            </div>
          </div>
          <p className="mt-3 flex items-start gap-2 text-xs text-muted">
            <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
            {t('icReductionNote')}
          </p>
        </FormSection>
      </form>
    </Modal>
  )
}
