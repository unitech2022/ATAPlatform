import { useState } from 'react'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Field, Input, Select } from '../../components/Field'
import { Icon } from '../../components/Icon'
import { Notice } from '../../components/Notice'
import { useI18n } from '../../i18n'
import { driverApi } from '../../lib/api'
import { describeError, fieldErrorsFrom } from '../../lib/errors'
import { currentYear } from '../../lib/format'
import type { RideCategory, Vehicle } from '../../lib/types'
import type { Resource } from '../../lib/useResource'

interface VehicleFormProps {
  vehicle: Vehicle | null
  categories: Resource<RideCategory[]>
  onSaved: () => void
}

interface FormState {
  make: string
  model: string
  year: string
  color: string
  plateNumber: string
  seats: string
  rideCategoryId: string
}

type FormErrors = Partial<Record<keyof FormState, string>>

const MIN_YEAR = 1990

export function VehicleForm({ vehicle, categories, onSaved }: VehicleFormProps) {
  const { t } = useI18n()
  const [form, setForm] = useState<FormState>({
    make: vehicle?.make ?? '',
    model: vehicle?.model ?? '',
    year: vehicle ? String(vehicle.year) : '',
    color: vehicle?.color ?? '',
    plateNumber: vehicle?.plateNumber ?? '',
    seats: vehicle ? String(vehicle.seats) : '',
    rideCategoryId: vehicle?.rideCategoryId ?? '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [apiError, setApiError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const maxYear = currentYear() + 1
  const sortedCategories = [...(categories.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder)

  const update = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const chooseCategory = (id: string) => {
    const category = sortedCategories.find((item) => item.id === id)
    setForm((current) => ({
      ...current,
      rideCategoryId: id,
      seats: current.seats || !category ? current.seats : String(category.seats),
    }))
    setErrors((current) => ({ ...current, rideCategoryId: undefined }))
  }

  const validate = (): FormErrors => {
    const next: FormErrors = {}
    if (!form.make.trim()) next.make = t('validation.required')
    if (!form.model.trim()) next.model = t('validation.required')
    if (!form.color.trim()) next.color = t('validation.required')
    if (!form.plateNumber.trim()) next.plateNumber = t('validation.required')
    if (!form.rideCategoryId) next.rideCategoryId = t('validation.required')
    const year = Number(form.year)
    if (!form.year) next.year = t('validation.required')
    else if (!Number.isInteger(year) || year < MIN_YEAR || year > maxYear) next.year = t('validation.year')
    const seats = Number(form.seats)
    if (!form.seats) next.seats = t('validation.required')
    else if (!Number.isInteger(seats) || seats < 1 || seats > 8) next.seats = t('validation.seats')
    return next
  }

  const submit = async () => {
    const next = validate()
    setErrors(next)
    if (Object.keys(next).length > 0) return
    setBusy(true)
    setApiError(null)
    try {
      await driverApi.updateVehicle({
        make: form.make.trim(),
        model: form.model.trim(),
        year: Number(form.year),
        color: form.color.trim(),
        plateNumber: form.plateNumber.trim(),
        seats: Number(form.seats),
        rideCategoryId: form.rideCategoryId,
      })
      onSaved()
    } catch (caught) {
      setApiError(describeError(caught, t))
      setErrors(fieldErrorsFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          void submit()
        }}
        noValidate
      >
        <div className="mb-6 flex items-center gap-3">
          <div className="grid size-11 place-items-center rounded-xl bg-brand-soft text-brand">
            <Icon name="car" />
          </div>
          <p className="text-lg font-bold">{t('portal.steps.vehicle')}</p>
        </div>

        {apiError && (
          <Notice tone="error" className="mb-5">
            {apiError}
          </Notice>
        )}
        {categories.error !== null && (
          <Notice tone="error" className="mb-5" onRetry={() => categories.reload()}>
            {t('vehicle.categoriesError')}
          </Notice>
        )}

        <div className="grid gap-5 sm:grid-cols-2">
          <Field label={t('vehicle.make')} htmlFor="make" error={errors.make}>
            <Input id="make" invalid={Boolean(errors.make)} value={form.make} onChange={(event) => update('make', event.target.value)} />
          </Field>
          <Field label={t('vehicle.model')} htmlFor="model" error={errors.model}>
            <Input id="model" invalid={Boolean(errors.model)} value={form.model} onChange={(event) => update('model', event.target.value)} />
          </Field>
          <Field label={t('vehicle.year')} htmlFor="year" error={errors.year}>
            <Input
              id="year"
              dir="ltr"
              type="number"
              inputMode="numeric"
              min={MIN_YEAR}
              max={maxYear}
              invalid={Boolean(errors.year)}
              value={form.year}
              onChange={(event) => update('year', event.target.value.replace(/\D/g, '').slice(0, 4))}
            />
          </Field>
          <Field label={t('vehicle.color')} htmlFor="color" error={errors.color}>
            <Input id="color" invalid={Boolean(errors.color)} value={form.color} onChange={(event) => update('color', event.target.value)} />
          </Field>
          <Field label={t('vehicle.plate')} htmlFor="plateNumber" error={errors.plateNumber}>
            <Input
              id="plateNumber"
              className="tracking-widest"
              invalid={Boolean(errors.plateNumber)}
              value={form.plateNumber}
              onChange={(event) => update('plateNumber', event.target.value)}
            />
          </Field>
          <Field label={t('vehicle.seats')} htmlFor="seats" error={errors.seats}>
            <Input
              id="seats"
              dir="ltr"
              type="number"
              inputMode="numeric"
              min={1}
              max={8}
              invalid={Boolean(errors.seats)}
              value={form.seats}
              onChange={(event) => update('seats', event.target.value.replace(/\D/g, '').slice(0, 1))}
            />
          </Field>
          <Field label={t('vehicle.category')} htmlFor="rideCategoryId" error={errors.rideCategoryId} className="sm:col-span-2">
            <Select
              id="rideCategoryId"
              invalid={Boolean(errors.rideCategoryId)}
              disabled={categories.loading}
              value={form.rideCategoryId}
              onChange={(event) => chooseCategory(event.target.value)}
            >
              <option value="">{categories.loading ? t('state.loading') : t('profile.select')}</option>
              {sortedCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name} · {t('categories.seats', { n: category.seats })}
                </option>
              ))}
            </Select>
          </Field>
        </div>

        <Button type="submit" block className="mt-7" disabled={busy}>
          {busy ? t('action.saving') : t('action.save')}
        </Button>
      </form>
    </Card>
  )
}
