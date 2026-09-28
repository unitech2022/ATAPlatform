import { useState } from 'react'
import { Action, Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Field, Input, Select } from '../../components/Field'
import { Icon } from '../../components/Icon'
import { Notice } from '../../components/Notice'
import { useI18n } from '../../i18n'
import { driverApi } from '../../lib/api'
import { describeError, fieldErrorsFrom } from '../../lib/errors'
import { toDateInputValue, todayIsoDate } from '../../lib/format'
import type { City, DriverProfile } from '../../lib/types'
import type { Resource } from '../../lib/useResource'

interface ProfileFormProps {
  profile: DriverProfile
  cities: Resource<City[]>
  onSaved: () => void
}

interface FormState {
  fullName: string
  nationalId: string
  dateOfBirth: string
  cityId: string
  gender: '' | 'male' | 'female'
  iban: string
}

type FormErrors = Partial<Record<keyof FormState, string>>

const IBAN_PATTERN = /^SA\d{22}$/

export function ProfileForm({ profile, cities, onSaved }: ProfileFormProps) {
  const { t } = useI18n()
  const [form, setForm] = useState<FormState>({
    fullName: profile.fullName ?? '',
    nationalId: profile.nationalId ?? '',
    dateOfBirth: toDateInputValue(profile.dateOfBirth),
    cityId: profile.cityId ?? '',
    gender: profile.gender === 'male' || profile.gender === 'female' ? profile.gender : '',
    iban: profile.iban ?? '',
  })
  const [errors, setErrors] = useState<FormErrors>({})
  const [apiError, setApiError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const update = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const validate = (): FormErrors => {
    const next: FormErrors = {}
    if (!form.fullName.trim()) next.fullName = t('validation.required')
    if (!form.nationalId) next.nationalId = t('validation.required')
    else if (!/^\d{10}$/.test(form.nationalId)) next.nationalId = t('validation.nationalId')
    if (!form.dateOfBirth) next.dateOfBirth = t('validation.required')
    if (!form.cityId) next.cityId = t('validation.required')
    if (!form.gender) next.gender = t('validation.required')
    const iban = form.iban.replace(/\s+/g, '').toUpperCase()
    if (iban && !IBAN_PATTERN.test(iban)) next.iban = t('validation.iban')
    return next
  }

  const submit = async () => {
    const next = validate()
    setErrors(next)
    if (Object.keys(next).length > 0 || form.gender === '') return
    setBusy(true)
    setApiError(null)
    try {
      const iban = form.iban.replace(/\s+/g, '').toUpperCase()
      await driverApi.updateProfile({
        fullName: form.fullName.trim(),
        nationalId: form.nationalId,
        dateOfBirth: form.dateOfBirth,
        cityId: form.cityId,
        gender: form.gender,
        ...(iban ? { iban } : {}),
      })
      onSaved()
    } catch (caught) {
      setApiError(describeError(caught, t))
      setErrors(fieldErrorsFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  const today = todayIsoDate()

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
            <Icon name="user" />
          </div>
          <p className="text-lg font-bold">{t('portal.steps.profile')}</p>
        </div>

        {apiError && (
          <Notice tone="error" className="mb-5">
            {apiError}
          </Notice>
        )}
        {cities.error !== null && (
          <Notice tone="error" className="mb-5" onRetry={() => cities.reload()}>
            {t('profile.citiesError')}
          </Notice>
        )}

        <div className="grid gap-5 sm:grid-cols-2">
          <Field label={t('profile.fullName')} htmlFor="fullName" error={errors.fullName} className="sm:col-span-2">
            <Input
              id="fullName"
              autoComplete="name"
              invalid={Boolean(errors.fullName)}
              value={form.fullName}
              onChange={(event) => update('fullName', event.target.value)}
            />
          </Field>

          <Field label={t('profile.nationalId')} htmlFor="nationalId" hint={t('profile.nationalIdHint')} error={errors.nationalId}>
            <Input
              id="nationalId"
              dir="ltr"
              inputMode="numeric"
              maxLength={10}
              invalid={Boolean(errors.nationalId)}
              value={form.nationalId}
              onChange={(event) => update('nationalId', event.target.value.replace(/\D/g, '').slice(0, 10))}
            />
          </Field>

          <Field label={t('profile.dateOfBirth')} htmlFor="dateOfBirth" error={errors.dateOfBirth}>
            <Input
              id="dateOfBirth"
              dir="ltr"
              type="date"
              max={today}
              invalid={Boolean(errors.dateOfBirth)}
              value={form.dateOfBirth}
              onChange={(event) => update('dateOfBirth', event.target.value)}
            />
          </Field>

          <Field label={t('profile.city')} htmlFor="cityId" error={errors.cityId}>
            <Select
              id="cityId"
              invalid={Boolean(errors.cityId)}
              disabled={cities.loading}
              value={form.cityId}
              onChange={(event) => update('cityId', event.target.value)}
            >
              <option value="">{cities.loading ? t('state.loading') : t('profile.select')}</option>
              {(cities.data ?? []).map((city) => (
                <option key={city.id} value={city.id}>
                  {city.name}
                </option>
              ))}
            </Select>
          </Field>

          <div>
            <p className="mb-2 block text-sm font-bold text-ink">{t('profile.gender')}</p>
            <div className="grid grid-cols-2 gap-3" role="radiogroup" aria-label={t('profile.gender')}>
              {(['male', 'female'] as const).map((option) => (
                <Action
                  key={option}
                  role="radio"
                  aria-checked={form.gender === option}
                  onClick={() => update('gender', option)}
                  className={`h-14 rounded-2xl border-2 text-center font-bold ${
                    form.gender === option ? 'border-brand bg-brand-soft text-brand' : 'border-line bg-cloud'
                  }`}
                >
                  {t(`profile.${option}`)}
                </Action>
              ))}
            </div>
            {errors.gender && (
              <p className="mt-2 text-xs font-bold text-danger" role="alert">
                {errors.gender}
              </p>
            )}
          </div>

          <Field label={t('profile.iban')} htmlFor="iban" hint={t('profile.ibanHint')} error={errors.iban} className="sm:col-span-2">
            <Input
              id="iban"
              dir="ltr"
              autoCapitalize="characters"
              maxLength={24}
              placeholder="SA"
              invalid={Boolean(errors.iban)}
              value={form.iban}
              onChange={(event) => update('iban', event.target.value.toUpperCase())}
            />
          </Field>
        </div>

        <Button type="submit" block className="mt-7" disabled={busy}>
          {busy ? t('action.saving') : t('action.save')}
        </Button>
      </form>
    </Card>
  )
}
