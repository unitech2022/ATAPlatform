import { useMemo, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { rideCategories, scheduledRules } from '../lib/admin'
import { isApiError } from '../lib/api'
import { localName } from '../lib/pricing'
import {
  DEFAULT_SCHEDULED_RULE,
  formatOffsetLabel,
  formatOffsets,
  parseOffsets,
  SCHEDULED_FEE_TYPE_KEY,
  SCHEDULED_FEE_TYPES,
  scheduledRuleInputOf,
  scheduledRuleWarnings,
  validateScheduledRule,
  type ScheduledRuleErrorField,
} from '../lib/scheduling'
import type { ScheduledFeeType, ScheduledRule, ScheduledRuleInput } from '../lib/types'
import { Button } from './Button'
import { CityField } from './CityField'
import { Input, Select, Toggle } from './Field'
import { FormSection } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'

/** Every numeric column as text so half-typed values survive; converted in `toInput`. */
type NumericKey = Exclude<
  keyof ScheduledRuleInput,
  'cityId' | 'rideCategoryId' | 'lockDemandNormal' | 'marketplaceEnabled' | 'riderReminderOffsets' | 'driverReminderOffsets' | 'lateCancelFeeType' | 'lateCancelFeeAmount' | 'lateCancelFeePercent' | 'isActive'
>

interface FormState {
  cityId: string
  rideCategoryId: string
  numbers: Record<NumericKey, string>
  lockDemandNormal: boolean
  marketplaceEnabled: boolean
  riderOffsets: string
  driverOffsets: string
  lateCancelFeeType: ScheduledFeeType
  lateCancelFeeAmount: string
  lateCancelFeePercent: string
  isActive: boolean
}

const NUMERIC_KEYS: NumericKey[] = [
  'maxDaysAhead',
  'minLeadMinutes',
  'maxOpenPerPassenger',
  'marketplaceRadiusKm',
  'favoriteExclusiveMinutes',
  'driverAssignmentLeadMinutes',
  'confirmationTimeoutMinutes',
  'finalConfirmationMinutesBefore',
  'finalConfirmationTimeoutMinutes',
  'searchStartMinutesBefore',
  'freeCancelMinutesBefore',
  'lateCancelDriverCompensationPercent',
  'driverFreeReleaseMinutesBefore',
  'driverLateReleasePenaltyPoints',
  'driverConfirmationMissedPenaltyPoints',
  'driverNoShowPenaltyPoints',
  'driverNoShowGraceMinutes',
  'maxReservationsPerDriver',
  'reservationGapMinutes',
]

const str = (value: number | null | undefined) => (value === null || value === undefined ? '' : String(value))

function toForm(rule: ScheduledRule | null): FormState {
  const base: ScheduledRuleInput = rule ? scheduledRuleInputOf(rule) : DEFAULT_SCHEDULED_RULE
  return {
    cityId: base.cityId ?? '',
    rideCategoryId: base.rideCategoryId ?? '',
    numbers: Object.fromEntries(NUMERIC_KEYS.map((key) => [key, String(base[key])])) as Record<NumericKey, string>,
    lockDemandNormal: base.lockDemandNormal,
    marketplaceEnabled: base.marketplaceEnabled,
    riderOffsets: formatOffsets(base.riderReminderOffsets),
    driverOffsets: formatOffsets(base.driverReminderOffsets),
    lateCancelFeeType: base.lateCancelFeeType,
    lateCancelFeeAmount: str(base.lateCancelFeeAmount),
    lateCancelFeePercent: str(base.lateCancelFeePercent),
    isActive: base.isActive,
  }
}

const num = (value: string) => (value.trim() === '' ? Number.NaN : Number(value))

function toInput(form: FormState): ScheduledRuleInput {
  const n = (key: NumericKey) => num(form.numbers[key])
  const fixed = form.lateCancelFeeType === 'fixed'
  const percent = form.lateCancelFeeType === 'percent'
  return {
    cityId: form.cityId || null,
    rideCategoryId: form.rideCategoryId || null,
    maxDaysAhead: n('maxDaysAhead'),
    minLeadMinutes: n('minLeadMinutes'),
    maxOpenPerPassenger: n('maxOpenPerPassenger'),
    lockDemandNormal: form.lockDemandNormal,
    marketplaceEnabled: form.marketplaceEnabled,
    marketplaceRadiusKm: n('marketplaceRadiusKm'),
    favoriteExclusiveMinutes: n('favoriteExclusiveMinutes'),
    driverAssignmentLeadMinutes: n('driverAssignmentLeadMinutes'),
    confirmationTimeoutMinutes: n('confirmationTimeoutMinutes'),
    finalConfirmationMinutesBefore: n('finalConfirmationMinutesBefore'),
    finalConfirmationTimeoutMinutes: n('finalConfirmationTimeoutMinutes'),
    searchStartMinutesBefore: n('searchStartMinutesBefore'),
    // An unparsable list becomes [NaN] so the validator flags it instead of silently dropping the text.
    riderReminderOffsets: parseOffsets(form.riderOffsets) ?? [Number.NaN],
    driverReminderOffsets: parseOffsets(form.driverOffsets) ?? [Number.NaN],
    freeCancelMinutesBefore: n('freeCancelMinutesBefore'),
    lateCancelFeeType: form.lateCancelFeeType,
    lateCancelFeeAmount: fixed ? num(form.lateCancelFeeAmount) : null,
    lateCancelFeePercent: percent ? num(form.lateCancelFeePercent) : null,
    lateCancelDriverCompensationPercent: n('lateCancelDriverCompensationPercent'),
    driverFreeReleaseMinutesBefore: n('driverFreeReleaseMinutesBefore'),
    driverLateReleasePenaltyPoints: n('driverLateReleasePenaltyPoints'),
    driverConfirmationMissedPenaltyPoints: n('driverConfirmationMissedPenaltyPoints'),
    driverNoShowPenaltyPoints: n('driverNoShowPenaltyPoints'),
    driverNoShowGraceMinutes: n('driverNoShowGraceMinutes'),
    maxReservationsPerDriver: n('maxReservationsPerDriver'),
    reservationGapMinutes: n('reservationGapMinutes'),
    isActive: form.isActive,
  }
}

export interface ScheduledRuleFormModalProps {
  open: boolean
  /** Rule to edit; null creates one. */
  rule: ScheduledRule | null
  /** Every rule (used to reject a second rule for the same city + category). */
  rules: ScheduledRule[]
  onClose: () => void
  onSaved: (rule: ScheduledRule | null) => void
}

export function ScheduledRuleFormModal({ open, ...props }: ScheduledRuleFormModalProps) {
  return open ? <ScheduledRuleDialog {...props} /> : null
}

function ScheduledRuleDialog({ rule, rules, onClose, onSaved }: Omit<ScheduledRuleFormModalProps, 'open'>) {
  const { t, lang } = useLang()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(() => toForm(rule))
  const [errors, setErrors] = useState<Partial<Record<ScheduledRuleErrorField, TranslationKey>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const others = useMemo(() => rules.filter((other) => other.id !== rule?.id), [rules, rule])

  const touch = () => {
    setErrors({})
    setFormError(null)
  }
  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    touch()
  }
  const setNumber = (key: NumericKey, value: string) => {
    setForm((current) => ({ ...current, numbers: { ...current.numbers, [key]: value } }))
    touch()
  }

  const draft = toInput(form)
  const warnings = scheduledRuleWarnings(draft)
  const err = (key: ScheduledRuleErrorField) => (errors[key] ? t(errors[key]) : undefined)

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const input = toInput(form)
    const found = validateScheduledRule(input, others)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t('fixErrors'))
      return
    }
    setSaving(true)
    try {
      const saved = rule ? await scheduledRules.update(rule.id, input) : await scheduledRules.create(input)
      onSaved(saved ?? null)
    } catch (error) {
      if (isApiError(error) && error.status === 422 && error.details) {
        const fields = Object.keys(error.details).filter((key): key is ScheduledRuleErrorField => key in input)
        setErrors(Object.fromEntries(fields.map((key) => [key, 'invalidNumber' as TranslationKey])))
      }
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  const field = (key: NumericKey, label: TranslationKey, options: { hint?: TranslationKey; min?: number; step?: string; suffix?: TranslationKey } = {}) => (
    <Input
      id={`sd-${key}`}
      type="number"
      inputMode="decimal"
      min={options.min ?? 0}
      step={options.step ?? '1'}
      dir="ltr"
      label={options.suffix ? `${t(label)} (${t(options.suffix)})` : t(label)}
      hint={options.hint ? t(options.hint) : undefined}
      value={form.numbers[key]}
      error={err(key)}
      onChange={(event) => setNumber(key, event.target.value)}
    />
  )

  const offsetsField = (key: 'riderOffsets' | 'driverOffsets', errorKey: 'riderReminderOffsets' | 'driverReminderOffsets', label: TranslationKey) => {
    const parsed = parseOffsets(form[key])
    return (
      <div>
        <Input
          id={`sd-${key}`}
          dir="ltr"
          label={`${t(label)} (${t('min')})`}
          hint={t('sdOffsetsHint')}
          value={form[key]}
          error={err(errorKey)}
          onChange={(event) => set(key, event.target.value)}
        />
        {parsed && parsed.length > 0 && (
          <p className="ltr-nums mt-2 flex flex-wrap gap-1.5 text-xs" aria-label={t('sdOffsetsPreview')}>
            {parsed.map((offset) => (
              <span key={offset} className="rounded-full bg-cloud px-2.5 py-1 font-bold text-muted">
                T−{formatOffsetLabel(offset)}
              </span>
            ))}
          </p>
        )}
      </div>
    )
  }

  return (
    <Modal
      open
      size="xl"
      title={rule ? t('sdRuleEdit') : t('sdRuleNew')}
      description={t('sdRuleFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="scheduled-rule-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="scheduled-rule-form" onSubmit={save} noValidate className="space-y-5">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}

        <FormSection title={t('sdSectionScope')} description={t('sdScopeCopy')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <CityField id="sd-city" label={t('city')} value={form.cityId} onChange={(value) => set('cityId', value)} />
            <Select id="sd-category" label={t('rideCategory')} value={form.rideCategoryId} onChange={(event) => set('rideCategoryId', event.target.value)}>
              <option value="">{t('allCategories')}</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {localName(category, lang)}
                </option>
              ))}
            </Select>
          </div>
          {errors.scope && <p className="mt-2 text-xs font-bold text-danger">{t(errors.scope)}</p>}
          <p className="mt-3 flex items-start gap-2 text-xs text-muted">
            <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
            {t('sdScopePrecedence')}
          </p>
        </FormSection>

        <FormSection title={t('sdSectionWindow')} description={t('sdWindowCopy')}>
          <div className="grid gap-4 sm:grid-cols-3">
            {field('maxDaysAhead', 'sdMaxDaysAhead', { min: 1, hint: 'sdMaxDaysAheadHint', suffix: 'sdDays' })}
            {field('minLeadMinutes', 'sdMinLead', { suffix: 'min' })}
            {field('maxOpenPerPassenger', 'sdMaxOpen', { min: 1 })}
          </div>
          <div className="mt-4">
            <Toggle checked={form.lockDemandNormal} onChange={(value) => set('lockDemandNormal', value)} label={t('sdLockDemand')} description={t('sdLockDemandCopy')} />
          </div>
        </FormSection>

        <FormSection title={t('sdSectionMarketplace')} description={t('sdMarketplaceCopy')}>
          <Toggle checked={form.marketplaceEnabled} onChange={(value) => set('marketplaceEnabled', value)} label={t('sdMarketplaceEnabled')} description={t('sdMarketplaceEnabledCopy')} />
          <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {field('marketplaceRadiusKm', 'sdMarketplaceRadius', { min: 1, suffix: 'km' })}
            {field('favoriteExclusiveMinutes', 'sdFavoriteExclusive', { hint: 'sdFavoriteExclusiveHint', suffix: 'min' })}
            {field('maxReservationsPerDriver', 'sdMaxReservations', { min: 1 })}
            {field('reservationGapMinutes', 'sdReservationGap', { suffix: 'min' })}
          </div>
        </FormSection>

        <FormSection title={t('sdSectionConfirmations')} description={t('sdConfirmationsCopy')}>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {field('driverAssignmentLeadMinutes', 'sdAssignmentLead', { min: 1, hint: 'sdAssignmentLeadHint', suffix: 'min' })}
            {field('confirmationTimeoutMinutes', 'sdConfirmTimeout', { min: 1, suffix: 'min' })}
            {field('finalConfirmationMinutesBefore', 'sdFinalConfirm', { min: 1, hint: 'sdFinalConfirmHint', suffix: 'min' })}
            {field('finalConfirmationTimeoutMinutes', 'sdFinalTimeout', { min: 1, suffix: 'min' })}
            {field('searchStartMinutesBefore', 'sdSearchStart', { hint: 'sdSearchStartHint', suffix: 'min' })}
          </div>
          <TimelinePreview input={draft} />
          {warnings.length > 0 && (
            <ul className="mt-3 space-y-1.5">
              {warnings.map((key) => (
                <li key={key} className="flex items-start gap-2 rounded-2xl bg-amber-50 px-4 py-2.5 text-xs font-bold text-amber-800">
                  <Icon name="alert" className="mt-0.5 size-3.5 shrink-0" />
                  {t(key)}
                </li>
              ))}
            </ul>
          )}
        </FormSection>

        <FormSection title={t('sdSectionReminders')} description={t('sdRemindersCopy')}>
          <div className="grid gap-4 sm:grid-cols-2">
            {offsetsField('riderOffsets', 'riderReminderOffsets', 'sdRiderReminders')}
            {offsetsField('driverOffsets', 'driverReminderOffsets', 'sdDriverReminders')}
          </div>
        </FormSection>

        <FormSection title={t('sdSectionCancellation')} description={t('sdCancellationCopy')}>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {field('freeCancelMinutesBefore', 'sdFreeCancel', { hint: 'sdFreeCancelHint', suffix: 'min' })}
            <Select id="sd-fee-type" label={t('sdLateFeeType')} value={form.lateCancelFeeType} onChange={(event) => set('lateCancelFeeType', event.target.value as ScheduledFeeType)}>
              {SCHEDULED_FEE_TYPES.map((value) => (
                <option key={value} value={value}>
                  {t(SCHEDULED_FEE_TYPE_KEY[value])}
                </option>
              ))}
            </Select>
            {form.lateCancelFeeType === 'fixed' && (
              <Input
                id="sd-fee-amount"
                type="number"
                inputMode="decimal"
                min={0}
                step="0.01"
                dir="ltr"
                label={`${t('sdLateFeeAmount')} (${t('sar')})`}
                value={form.lateCancelFeeAmount}
                error={err('lateCancelFeeAmount')}
                onChange={(event) => set('lateCancelFeeAmount', event.target.value)}
              />
            )}
            {form.lateCancelFeeType === 'percent' && (
              <Input
                id="sd-fee-percent"
                type="number"
                inputMode="decimal"
                min={0}
                max={100}
                step="0.01"
                dir="ltr"
                label={`${t('sdLateFeePercent')} (%)`}
                value={form.lateCancelFeePercent}
                error={err('lateCancelFeePercent')}
                onChange={(event) => set('lateCancelFeePercent', event.target.value)}
              />
            )}
            {field('lateCancelDriverCompensationPercent', 'sdCompensation', { step: '0.01', hint: 'sdCompensationHint', suffix: 'sdPercentUnit' })}
          </div>
          {form.lateCancelFeeType === 'pricing_rule' && <p className="mt-3 text-xs text-muted">{t('sdFeePricingRuleCopy')}</p>}
        </FormSection>

        <FormSection title={t('sdSectionPenalties')} description={t('sdPenaltiesCopy')}>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {field('driverFreeReleaseMinutesBefore', 'sdFreeRelease', { hint: 'sdFreeReleaseHint', suffix: 'min' })}
            {field('driverLateReleasePenaltyPoints', 'sdLateReleasePoints', { suffix: 'sdPoints' })}
            {field('driverConfirmationMissedPenaltyPoints', 'sdConfirmMissedPoints', { suffix: 'sdPoints' })}
            {field('driverNoShowPenaltyPoints', 'sdNoShowPoints', { hint: 'sdNoShowPointsHint', suffix: 'sdPoints' })}
            {field('driverNoShowGraceMinutes', 'sdNoShowGrace', { suffix: 'min' })}
          </div>
        </FormSection>

        <FormSection title={t('sdSectionStatus')}>
          <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} description={t('sdActiveCopy')} />
        </FormSection>
      </form>
    </Modal>
  )
}

/** Visual T−n ladder from the current form values, so an admin sees the order (search start last) before saving. */
function TimelinePreview({ input }: { input: ScheduledRuleInput }) {
  const { t } = useLang()
  const steps = [
    { label: t('sdStepFirstConfirm'), minutes: input.driverAssignmentLeadMinutes },
    { label: t('sdStepFinalConfirm'), minutes: input.finalConfirmationMinutesBefore },
    { label: t('sdStepSearchStart'), minutes: input.searchStartMinutesBefore },
  ]
  if (steps.some((step) => !Number.isFinite(step.minutes))) return null
  return (
    <ol className="mt-4 flex flex-wrap items-center gap-2 text-xs" aria-label={t('sdTimelinePreview')}>
      {steps.map((step, index) => (
        <li key={step.label} className="flex items-center gap-2">
          {index > 0 && <Icon name="chevron" className="size-3.5 text-muted rtl:rotate-180" />}
          <span className="rounded-full bg-cloud px-3 py-1 font-bold">
            {step.label} <span className="ltr-nums text-brand">T−{formatOffsetLabel(step.minutes)}</span>
          </span>
        </li>
      ))}
      <li className="flex items-center gap-2">
        <Icon name="chevron" className="size-3.5 text-muted rtl:rotate-180" />
        <span className="rounded-full bg-ink px-3 py-1 font-bold text-white">{t('sdStepPickup')} T</span>
      </li>
    </ol>
  )
}
