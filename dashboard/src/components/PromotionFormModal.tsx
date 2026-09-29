import { useMemo, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { promotions, rideCategories, zones } from '../lib/admin'
import { isApiError } from '../lib/api'
import { BOOKING_TYPE_KEY } from '../lib/cancellation'
import { fromLocalInput, localName, toLocalInput } from '../lib/pricing'
import {
  listOrNull,
  normalizePromoCode,
  optionalNumber,
  PROMO_BOOKING_TYPES,
  PROMO_PAYMENT_METHODS,
  PROMOTION_TYPE_KEY,
  PROMOTION_TYPES,
  validatePromotion,
  type PromotionErrorField,
} from '../lib/rewards'
import { PAYMENT_METHOD_KEY } from '../lib/trips'
import type { BookingType, PaymentMethod, Promotion, PromotionInput, PromotionType } from '../lib/types'
import { Button } from './Button'
import { ChipGroup } from './ChipGroup'
import { CityField } from './CityField'
import { Input, Select, Textarea, Toggle } from './Field'
import { ChoiceField, FormSection } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { ZonePicker } from './ZonePicker'

interface FormState {
  code: string
  nameAr: string
  nameEn: string
  descriptionAr: string
  descriptionEn: string
  type: PromotionType
  value: string
  maxDiscount: string
  minFare: string
  validFrom: string
  validTo: string
  totalUsageLimit: string
  perUserLimit: string
  budgetAmount: string
  firstTripOnly: boolean
  newUsersOnly: boolean
  newUserDays: string
  cityId: string
  rideCategoryIds: string[]
  zoneIds: string[]
  paymentMethods: PaymentMethod[]
  bookingTypes: BookingType[]
  isStackable: boolean
  isPublic: boolean
  isActive: boolean
}

const str = (value: number | null | undefined) => (value === null || value === undefined ? '' : String(value))

function toForm(promotion: Promotion | null): FormState {
  const now = new Date()
  const inMonth = new Date(now.getTime() + 30 * 24 * 3600 * 1000)
  return {
    code: promotion?.code ?? '',
    nameAr: promotion?.nameAr ?? '',
    nameEn: promotion?.nameEn ?? '',
    descriptionAr: promotion?.descriptionAr ?? '',
    descriptionEn: promotion?.descriptionEn ?? '',
    type: promotion?.type ?? 'percent',
    value: promotion ? String(promotion.value) : '10',
    maxDiscount: str(promotion?.maxDiscount),
    minFare: str(promotion?.minFare),
    validFrom: toLocalInput(promotion?.validFrom ?? now.toISOString()),
    validTo: toLocalInput(promotion?.validTo ?? inMonth.toISOString()),
    totalUsageLimit: str(promotion?.totalUsageLimit),
    perUserLimit: promotion ? String(promotion.perUserLimit) : '1',
    budgetAmount: str(promotion?.budgetAmount),
    firstTripOnly: promotion?.firstTripOnly ?? false,
    newUsersOnly: promotion?.newUsersOnly ?? false,
    newUserDays: promotion ? String(promotion.newUserDays) : '30',
    cityId: promotion?.cityId ?? '',
    rideCategoryIds: promotion?.rideCategoryIds ?? [],
    zoneIds: promotion?.zoneIds ?? [],
    paymentMethods: promotion?.paymentMethods ?? [],
    bookingTypes: promotion?.bookingTypes ?? [],
    isStackable: promotion?.isStackable ?? false,
    isPublic: promotion?.isPublic ?? false,
    isActive: promotion?.isActive ?? true,
  }
}

function toInput(form: FormState): PromotionInput {
  const percent = form.type === 'percent'
  return {
    code: normalizePromoCode(form.code),
    nameAr: form.nameAr.trim(),
    nameEn: form.nameEn.trim(),
    descriptionAr: form.descriptionAr.trim() || null,
    descriptionEn: form.descriptionEn.trim() || null,
    type: form.type,
    // free_booking_fee discounts exactly the booking fee; the column is still NOT NULL.
    value: form.type === 'free_booking_fee' ? 0 : Number(form.value),
    maxDiscount: percent ? optionalNumber(form.maxDiscount) : null,
    minFare: optionalNumber(form.minFare),
    validFrom: fromLocalInput(form.validFrom) ?? '',
    validTo: fromLocalInput(form.validTo) ?? '',
    totalUsageLimit: optionalNumber(form.totalUsageLimit),
    perUserLimit: Number(form.perUserLimit || 1),
    budgetAmount: optionalNumber(form.budgetAmount),
    firstTripOnly: form.firstTripOnly,
    newUsersOnly: form.newUsersOnly,
    newUserDays: Number(form.newUserDays || 30),
    cityId: form.cityId || null,
    rideCategoryIds: listOrNull(form.rideCategoryIds),
    zoneIds: listOrNull(form.zoneIds),
    paymentMethods: listOrNull(form.paymentMethods),
    bookingTypes: listOrNull(form.bookingTypes),
    isStackable: form.isStackable,
    isPublic: form.isPublic,
    isActive: form.isActive,
  }
}

export interface PromotionFormModalProps {
  open: boolean
  /** Full promotion to edit (from `GET /admin/promotions/{id}`); null creates one. */
  promotion: Promotion | null
  onClose: () => void
  onSaved: (promotion: Promotion | null) => void
}

export function PromotionFormModal({ open, ...props }: PromotionFormModalProps) {
  return open ? <PromotionDialog {...props} /> : null
}

function PromotionDialog({ promotion, onClose, onSaved }: Omit<PromotionFormModalProps, 'open'>) {
  const { t, lang } = useLang()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(() => toForm(promotion))
  const [errors, setErrors] = useState<Partial<Record<PromotionErrorField, TranslationKey>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data])
  // §F15.6: code and type cannot change after the first reservation.
  const locked = Boolean(promotion && promotion.usageCount > 0)

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors({})
    setFormError(null)
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const input = toInput(form)
    const found = validatePromotion(input)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t('fixErrors'))
      return
    }
    setSaving(true)
    try {
      const saved = promotion ? await promotions.update(promotion.id, input) : await promotions.create(input)
      onSaved(saved ?? null)
    } catch (error) {
      if (isApiError(error) && error.status === 409) setFormError(error.code === 'conflict' || locked ? t('prErrLocked') : describe(error))
      else if (isApiError(error) && error.status === 422 && error.details) {
        const fields = Object.keys(error.details).filter((key): key is PromotionErrorField => key in input)
        setErrors(Object.fromEntries(fields.map((key) => [key, 'invalidNumber' as TranslationKey])))
        setFormError(describe(error))
      } else setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  const err = (key: PromotionErrorField) => (errors[key] ? t(errors[key]) : undefined)

  return (
    <Modal
      open
      size="xl"
      title={promotion ? `${t('prEdit')} · ${promotion.code}` : t('prNew')}
      description={t('prFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="promotion-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="promotion-form" onSubmit={save} noValidate className="space-y-5">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}

        <FormSection title={t('prSectionBasics')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input
              id="pr-code"
              dir="ltr"
              label={t('prCode')}
              hint={locked ? t('prCodeLocked') : t('prCodeHint')}
              value={form.code}
              error={err('code')}
              disabled={locked}
              autoCapitalize="characters"
              maxLength={20}
              onChange={(event) => set('code', normalizePromoCode(event.target.value))}
              className="font-bold tracking-widest"
            />
            <Select id="pr-type" label={t('type')} value={form.type} disabled={locked} onChange={(event) => set('type', event.target.value as PromotionType)}>
              {PROMOTION_TYPES.map((value) => (
                <option key={value} value={value}>
                  {t(PROMOTION_TYPE_KEY[value])}
                </option>
              ))}
            </Select>
            <Input id="pr-name-ar" label={t('nameAr')} value={form.nameAr} error={err('nameAr')} onChange={(event) => set('nameAr', event.target.value)} />
            <Input id="pr-name-en" dir="ltr" label={t('nameEn')} value={form.nameEn} error={err('nameEn')} onChange={(event) => set('nameEn', event.target.value)} />
            <Textarea id="pr-desc-ar" label={t('descriptionAr')} value={form.descriptionAr} onChange={(event) => set('descriptionAr', event.target.value)} className="min-h-20" />
            <Textarea id="pr-desc-en" dir="ltr" label={t('descriptionEn')} value={form.descriptionEn} onChange={(event) => set('descriptionEn', event.target.value)} className="min-h-20" />
          </div>
        </FormSection>

        <FormSection title={t('prSectionValue')}>
          <div className="grid gap-4 sm:grid-cols-3">
            {form.type !== 'free_booking_fee' ? (
              <Input
                id="pr-value"
                type="number"
                min={form.type === 'percent' ? 1 : 0}
                max={form.type === 'percent' ? 100 : undefined}
                step={form.type === 'percent' ? 1 : '0.01'}
                dir="ltr"
                label={form.type === 'percent' ? t('prValuePercent') : `${t('prValueFixed')} (${t('sar')})`}
                value={form.value}
                error={err('value')}
                onChange={(event) => set('value', event.target.value)}
              />
            ) : (
              <p className="self-end rounded-2xl bg-cloud px-4 py-3 text-xs text-muted sm:col-span-1">{t('prFreeBookingFeeHint')}</p>
            )}
            {form.type === 'percent' && (
              <Input
                id="pr-max"
                type="number"
                min={0}
                step="0.01"
                dir="ltr"
                label={`${t('prMaxDiscount')} (${t('sar')})`}
                hint={t('optional')}
                value={form.maxDiscount}
                error={err('maxDiscount')}
                onChange={(event) => set('maxDiscount', event.target.value)}
              />
            )}
            <Input
              id="pr-min-fare"
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
        </FormSection>

        <FormSection title={t('prSectionLimits')}>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Input id="pr-from" type="datetime-local" dir="ltr" label={t('prValidFrom')} value={form.validFrom} error={err('validFrom')} onChange={(event) => set('validFrom', event.target.value)} />
            <Input id="pr-to" type="datetime-local" dir="ltr" label={t('prValidTo')} value={form.validTo} error={err('validTo')} onChange={(event) => set('validTo', event.target.value)} />
            <Input
              id="pr-total"
              type="number"
              min={1}
              step={1}
              dir="ltr"
              label={t('prTotalLimit')}
              hint={t('prUnlimitedHint')}
              value={form.totalUsageLimit}
              error={err('totalUsageLimit')}
              onChange={(event) => set('totalUsageLimit', event.target.value)}
            />
            <Input id="pr-per-user" type="number" min={1} step={1} dir="ltr" label={t('prPerUserLimit')} value={form.perUserLimit} error={err('perUserLimit')} onChange={(event) => set('perUserLimit', event.target.value)} />
            <Input
              id="pr-budget"
              type="number"
              min={0}
              step="0.01"
              dir="ltr"
              label={`${t('prBudget')} (${t('sar')})`}
              hint={t('prUnlimitedHint')}
              value={form.budgetAmount}
              error={err('budgetAmount')}
              onChange={(event) => set('budgetAmount', event.target.value)}
              wrapperClassName="sm:col-span-2 lg:col-span-1"
            />
          </div>
        </FormSection>

        <FormSection title={t('prSectionEligibility')}>
          <div className="grid gap-3 sm:grid-cols-2">
            <Toggle checked={form.firstTripOnly} onChange={(value) => set('firstTripOnly', value)} label={t('prFirstTripOnly')} description={t('prFirstTripOnlyCopy')} />
            <Toggle checked={form.newUsersOnly} onChange={(value) => set('newUsersOnly', value)} label={t('prNewUsersOnly')} description={t('prNewUsersOnlyCopy')} />
            {form.newUsersOnly && (
              <Input id="pr-new-days" type="number" min={1} step={1} dir="ltr" label={t('prNewUserDays')} value={form.newUserDays} error={err('newUserDays')} onChange={(event) => set('newUserDays', event.target.value)} />
            )}
            <CityField id="pr-city" label={t('city')} value={form.cityId} onChange={(value) => set('cityId', value)} />
          </div>
        </FormSection>

        <FormSection title={t('prSectionRestrictions')} description={t('prRestrictionsCopy')}>
          <div className="space-y-4">
            <ChoiceField label={t('rideCategory')} hint={form.rideCategoryIds.length === 0 ? t('allCategories') : undefined}>
              <ChipGroup options={categories.map((category) => ({ value: category.id, label: localName(category, lang) }))} value={form.rideCategoryIds} onChange={(value) => set('rideCategoryIds', value)} />
            </ChoiceField>
            <ChoiceField label={t('pickupZone')}>
              <ZonePicker zones={zoneList} value={form.zoneIds} onChange={(value) => set('zoneIds', value)} />
            </ChoiceField>
            <ChoiceField label={t('paymentMethod')} hint={form.paymentMethods.length === 0 ? t('prAllMethods') : undefined}>
              <ChipGroup options={PROMO_PAYMENT_METHODS.map((value) => ({ value, label: t(PAYMENT_METHOD_KEY[value]) }))} value={form.paymentMethods} onChange={(value) => set('paymentMethods', value)} />
            </ChoiceField>
            <ChoiceField label={t('bookingType')} hint={form.bookingTypes.length === 0 ? t('cxAnyBooking') : undefined}>
              <ChipGroup options={PROMO_BOOKING_TYPES.map((value) => ({ value, label: t(BOOKING_TYPE_KEY[value]) }))} value={form.bookingTypes} onChange={(value) => set('bookingTypes', value)} />
            </ChoiceField>
          </div>
        </FormSection>

        <FormSection title={t('prSectionVisibility')}>
          <div className="grid gap-3 sm:grid-cols-3">
            <Toggle checked={form.isStackable} onChange={(value) => set('isStackable', value)} label={t('prStackable')} description={t('prStackableCopy')} />
            <Toggle checked={form.isPublic} onChange={(value) => set('isPublic', value)} label={t('prPublic')} description={t('prPublicCopy')} />
            <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
          </div>
          <p className="mt-3 flex items-start gap-2 text-xs text-muted">
            <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
            {t('prEngineNote')}
          </p>
        </FormSection>
      </form>
    </Modal>
  )
}
