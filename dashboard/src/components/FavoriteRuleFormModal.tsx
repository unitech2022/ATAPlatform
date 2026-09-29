import { useMemo, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { favorites, rideCategories, zones } from '../lib/admin'
import { isApiError } from '../lib/api'
import { BOOKING_TYPE_KEY } from '../lib/cancellation'
import { FAVORITE_BOOKING_TYPES, FAVORITE_MAX_PERCENT, FAVORITE_MIN_PERCENT, findTies, validateFavoriteRule, type FavoriteRuleErrorField } from '../lib/favorites'
import { fromLocalInput, localName, toLocalInput } from '../lib/pricing'
import { listOrNull, optionalNumber } from '../lib/rewards'
import type { BookingType, FavoriteDiscountRule, FavoriteDiscountRuleInput } from '../lib/types'
import { Button } from './Button'
import { ChipGroup } from './ChipGroup'
import { Input, Toggle } from './Field'
import { ChoiceField, FormSection } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { ZonePicker } from './ZonePicker'

interface FormState {
  name: string
  discountPercent: string
  maxDiscountAmount: string
  minFare: string
  validFrom: string
  validTo: string
  priority: string
  stackableWithPromotions: boolean
  rideCategoryIds: string[]
  zoneIds: string[]
  bookingTypes: BookingType[]
  isActive: boolean
}

const str = (value: number | null | undefined) => (value === null || value === undefined ? '' : String(value))

function toForm(rule: FavoriteDiscountRule | null): FormState {
  return {
    name: rule?.name ?? '',
    discountPercent: rule ? String(rule.discountPercent) : '10',
    maxDiscountAmount: rule ? String(rule.maxDiscountAmount) : '10',
    minFare: str(rule?.minFare),
    validFrom: toLocalInput(rule?.validFrom ?? new Date().toISOString()),
    validTo: toLocalInput(rule?.validTo),
    priority: rule ? String(rule.priority) : '0',
    stackableWithPromotions: rule?.stackableWithPromotions ?? false,
    rideCategoryIds: rule?.rideCategoryIds ?? [],
    zoneIds: rule?.zoneIds ?? [],
    bookingTypes: rule?.bookingTypes ?? [],
    isActive: rule?.isActive ?? true,
  }
}

function toInput(form: FormState): FavoriteDiscountRuleInput {
  return {
    name: form.name.trim(),
    discountPercent: Number(form.discountPercent),
    maxDiscountAmount: Number(form.maxDiscountAmount),
    minFare: optionalNumber(form.minFare),
    stackableWithPromotions: form.stackableWithPromotions,
    validFrom: fromLocalInput(form.validFrom) ?? '',
    validTo: fromLocalInput(form.validTo),
    rideCategoryIds: listOrNull(form.rideCategoryIds),
    zoneIds: listOrNull(form.zoneIds),
    bookingTypes: listOrNull(form.bookingTypes),
    priority: form.priority.trim() === '' ? Number.NaN : Number(form.priority),
    isActive: form.isActive,
  }
}

export interface FavoriteRuleFormModalProps {
  open: boolean
  /** Rule to edit; null creates one. */
  rule: FavoriteDiscountRule | null
  /** Every rule (used to warn about same-priority overlaps). */
  rules: FavoriteDiscountRule[]
  onClose: () => void
  onSaved: (rule: FavoriteDiscountRule | null) => void
}

export function FavoriteRuleFormModal({ open, ...props }: FavoriteRuleFormModalProps) {
  return open ? <FavoriteRuleDialog {...props} /> : null
}

function FavoriteRuleDialog({ rule, rules, onClose, onSaved }: Omit<FavoriteRuleFormModalProps, 'open'>) {
  const { t, lang } = useLang()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(() => toForm(rule))
  const [errors, setErrors] = useState<Partial<Record<FavoriteRuleErrorField, TranslationKey>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data])

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors({})
    setFormError(null)
  }

  const draft = toInput(form)
  const ties = findTies(draft, rules.filter((other) => other.id !== rule?.id))

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const input = toInput(form)
    const found = validateFavoriteRule(input)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t('fixErrors'))
      return
    }
    setSaving(true)
    try {
      const saved = rule ? await favorites.update(rule.id, input) : await favorites.create(input)
      onSaved(saved ?? null)
    } catch (error) {
      if (isApiError(error) && error.status === 422 && error.details) {
        const fields = Object.keys(error.details).filter((key): key is FavoriteRuleErrorField => key in input)
        setErrors(Object.fromEntries(fields.map((key) => [key, 'invalidNumber' as TranslationKey])))
      }
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  const err = (key: FavoriteRuleErrorField) => (errors[key] ? t(errors[key]) : undefined)

  return (
    <Modal
      open
      size="xl"
      title={rule ? `${t('fvEdit')} · ${rule.name}` : t('fvNew')}
      description={t('fvFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="favorite-rule-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="favorite-rule-form" onSubmit={save} noValidate className="space-y-5">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}

        <FormSection title={t('fvSectionBasics')}>
          <Input id="fv-name" label={t('name')} value={form.name} error={err('name')} onChange={(event) => set('name', event.target.value)} />
        </FormSection>

        <FormSection title={t('fvSectionDiscount')} description={t('fvDiscountCopy')}>
          <div className="grid gap-4 sm:grid-cols-3">
            <Input
              id="fv-percent"
              type="number"
              min={FAVORITE_MIN_PERCENT}
              max={FAVORITE_MAX_PERCENT}
              step="0.01"
              dir="ltr"
              label={`${t('fvPercent')} (%)`}
              hint={`${FAVORITE_MIN_PERCENT}–${FAVORITE_MAX_PERCENT}`}
              value={form.discountPercent}
              error={err('discountPercent')}
              onChange={(event) => set('discountPercent', event.target.value)}
            />
            <Input
              id="fv-max"
              type="number"
              min={0.01}
              step="0.01"
              dir="ltr"
              label={`${t('fvMaxAmount')} (${t('sar')})`}
              value={form.maxDiscountAmount}
              error={err('maxDiscountAmount')}
              onChange={(event) => set('maxDiscountAmount', event.target.value)}
            />
            <Input
              id="fv-min-fare"
              type="number"
              min={0}
              step="0.01"
              dir="ltr"
              label={`${t('minFare')} (${t('sar')})`}
              hint={t('optional')}
              value={form.minFare}
              error={err('minFare')}
              onChange={(event) => set('minFare', event.target.value)}
            />
          </div>
          <div className="mt-4">
            <Toggle checked={form.stackableWithPromotions} onChange={(value) => set('stackableWithPromotions', value)} label={t('fvStackable')} description={t('fvStackableCopy')} />
          </div>
        </FormSection>

        <FormSection title={t('fvSectionWindow')}>
          <div className="grid gap-4 sm:grid-cols-3">
            <Input id="fv-from" type="datetime-local" dir="ltr" label={t('prValidFrom')} value={form.validFrom} error={err('validFrom')} onChange={(event) => set('validFrom', event.target.value)} />
            <Input id="fv-to" type="datetime-local" dir="ltr" label={t('prValidTo')} hint={t('fvOpenEnded')} value={form.validTo} error={err('validTo')} onChange={(event) => set('validTo', event.target.value)} />
            <Input
              id="fv-priority"
              type="number"
              min={0}
              step={1}
              dir="ltr"
              label={t('priority')}
              hint={t('fvPriorityHint')}
              value={form.priority}
              error={err('priority')}
              onChange={(event) => set('priority', event.target.value)}
            />
          </div>
        </FormSection>

        <FormSection title={t('fvSectionRestrictions')} description={t('fvRestrictionsCopy')}>
          <div className="space-y-4">
            <ChoiceField label={t('rideCategory')} hint={form.rideCategoryIds.length === 0 ? t('allCategories') : undefined}>
              <ChipGroup options={categories.map((category) => ({ value: category.id, label: localName(category, lang) }))} value={form.rideCategoryIds} onChange={(value) => set('rideCategoryIds', value)} />
            </ChoiceField>
            <ChoiceField label={t('pickupZone')}>
              <ZonePicker zones={zoneList} value={form.zoneIds} onChange={(value) => set('zoneIds', value)} />
            </ChoiceField>
            <ChoiceField label={t('bookingType')} hint={form.bookingTypes.length === 0 ? t('cxAnyBooking') : undefined}>
              <ChipGroup options={FAVORITE_BOOKING_TYPES.map((value) => ({ value, label: t(BOOKING_TYPE_KEY[value]) }))} value={form.bookingTypes} onChange={(value) => set('bookingTypes', value)} />
            </ChoiceField>
          </div>
        </FormSection>

        <FormSection title={t('fvSectionStatus')}>
          <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
          {ties.length > 0 && (
            <p className="mt-3 flex items-start gap-2 rounded-2xl bg-amber-50 px-4 py-3 text-xs font-bold text-amber-800">
              <Icon name="alert" className="mt-0.5 size-3.5 shrink-0" />
              <span>
                {t('fvTieWarning')}: {ties.map((other) => other.name).join(' · ')}
              </span>
            </p>
          )}
          <p className="mt-3 flex items-start gap-2 text-xs text-muted">
            <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
            {t('fvEngineNote')}
          </p>
        </FormSection>
      </form>
    </Modal>
  )
}
