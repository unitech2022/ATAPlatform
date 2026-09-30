import { useMemo, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import type { TranslationKey } from '../i18n'
import { airports as airportsApi } from '../lib/admin'
import { airportInputOf, EMPTY_AIRPORT, validateAirport, type AirportErrorField } from '../lib/airports'
import { isApiError } from '../lib/api'
import { closeRing, polygonCenter } from '../lib/pricing'
import type { Airport, AirportInput, LatLngTuple } from '../lib/types'
import { Button } from './Button'
import { CityField } from './CityField'
import { Input, Toggle } from './Field'
import { FormSection } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { PointPicker } from './PointPicker'
import { PolygonEditor, type PolygonContext } from './PolygonEditor'

interface FormState {
  cityId: string
  code: string
  nameAr: string
  nameEn: string
  lat: string
  lng: string
  geofence: LatLngTuple[]
  requiresPickupZone: boolean
  freeWaiting: string
  perMinute: string
  queueEnabled: boolean
  isActive: boolean
}

const str = (value: number | null | undefined) => (value === null || value === undefined || Number.isNaN(value) ? '' : String(value))
const num = (value: string) => (value.trim() === '' ? Number.NaN : Number(value))
const optional = (value: string) => (value.trim() === '' ? null : Number(value))

function toForm(airport: Airport | null): FormState {
  const base = airport ? airportInputOf(airport) : EMPTY_AIRPORT
  return {
    cityId: base.cityId,
    code: base.code,
    nameAr: base.nameAr,
    nameEn: base.nameEn,
    lat: str(base.lat),
    lng: str(base.lng),
    geofence: base.geofence,
    requiresPickupZone: base.requiresPickupZone,
    freeWaiting: str(base.defaultFreeWaitingMinutes),
    perMinute: str(base.defaultWaitingPerMinute),
    queueEnabled: base.queueEnabled,
    isActive: base.isActive,
  }
}

function toInput(form: FormState): AirportInput {
  return {
    cityId: form.cityId,
    code: form.code.trim().toUpperCase(),
    nameAr: form.nameAr.trim(),
    nameEn: form.nameEn.trim(),
    lat: num(form.lat),
    lng: num(form.lng),
    geofence: closeRing(form.geofence),
    requiresPickupZone: form.requiresPickupZone,
    defaultFreeWaitingMinutes: optional(form.freeWaiting),
    defaultWaitingPerMinute: optional(form.perMinute),
    queueEnabled: form.queueEnabled,
    isActive: form.isActive,
  }
}

export interface AirportFormModalProps {
  open: boolean
  /** Airport to edit; null creates one. */
  airport: Airport | null
  /** Every airport (duplicate IATA check + muted geofences on the polygon map). */
  airports: Airport[]
  onClose: () => void
  onSaved: (airport: Airport | null) => void
}

export function AirportFormModal({ open, ...props }: AirportFormModalProps) {
  return open ? <AirportDialog {...props} /> : null
}

function AirportDialog({ airport, airports, onClose, onSaved }: Omit<AirportFormModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(() => toForm(airport))
  const [errors, setErrors] = useState<Partial<Record<AirportErrorField, TranslationKey>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const others = useMemo(() => airports.filter((other) => other.id !== airport?.id), [airports, airport])
  const context = useMemo<PolygonContext[]>(() => others.map((other) => ({ id: other.id, name: `${other.code} · ${other.nameEn}`, polygon: other.geofence ?? [] })), [others])

  const touch = () => {
    setErrors({})
    setFormError(null)
  }
  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    touch()
  }
  const setGeofence = (points: LatLngTuple[]) => {
    setForm((current) => {
      const center = polygonCenter(points)
      // The reference point defaults to the geofence centre until the admin picks one.
      const fill = center && current.lat.trim() === '' && current.lng.trim() === ''
      return { ...current, geofence: points, lat: fill ? String(center[0]) : current.lat, lng: fill ? String(center[1]) : current.lng }
    })
    touch()
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const input = toInput(form)
    const found = validateAirport(input, others)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t('fixErrors'))
      return
    }
    setSaving(true)
    try {
      const saved = airport ? await airportsApi.update(airport.id, input) : await airportsApi.create(input)
      onSaved(saved ?? null)
    } catch (error) {
      if (isApiError(error) && error.status === 409) setErrors({ code: 'apErrCodeExists' })
      else if (isApiError(error) && error.status === 422 && error.details) {
        const fields = Object.keys(error.details).filter((key): key is AirportErrorField => key in input)
        setErrors(Object.fromEntries(fields.map((key) => [key, 'invalidNumber' as TranslationKey])))
      }
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  const err = (key: AirportErrorField) => (errors[key] ? t(errors[key]) : undefined)
  const latNum = num(form.lat)
  const lngNum = num(form.lng)

  return (
    <Modal
      open
      size="xl"
      title={airport ? `${t('apEdit')} · ${airport.code}` : t('apNew')}
      description={t('apFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="airport-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="airport-form" onSubmit={save} noValidate className="space-y-5">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}

        <FormSection title={t('apSectionBasics')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input
              id="ap-code"
              dir="ltr"
              maxLength={3}
              autoCapitalize="characters"
              label={t('apCode')}
              hint={t('apCodeHint')}
              value={form.code}
              error={err('code')}
              onChange={(event) => set('code', event.target.value.toUpperCase())}
            />
            <CityField id="ap-city" label={t('city')} value={form.cityId} allowAll={false} error={err('cityId')} onChange={(value) => set('cityId', value)} />
            <Input id="ap-name-ar" dir="rtl" label={t('nameAr')} value={form.nameAr} error={err('nameAr')} onChange={(event) => set('nameAr', event.target.value)} />
            <Input id="ap-name-en" dir="ltr" label={t('nameEn')} value={form.nameEn} error={err('nameEn')} onChange={(event) => set('nameEn', event.target.value)} />
          </div>
        </FormSection>

        <FormSection title={t('apSectionGeofence')} description={t('apGeofenceCopy')}>
          <PolygonEditor value={form.geofence} onChange={setGeofence} context={context} error={err('geofence')} />
        </FormSection>

        <FormSection title={t('apSectionPoint')} description={t('apPointCopy')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="ap-lat" type="number" step="any" dir="ltr" label={t('lat')} value={form.lat} error={err('lat')} onChange={(event) => set('lat', event.target.value)} />
            <Input id="ap-lng" type="number" step="any" dir="ltr" label={t('lng')} value={form.lng} error={err('lng')} onChange={(event) => set('lng', event.target.value)} />
          </div>
          <div className="mt-4 space-y-2">
            <PointPicker
              lat={latNum}
              lng={lngNum}
              context={[closeRing(form.geofence)]}
              onChange={(lat, lng) => {
                set('lat', String(lat))
                set('lng', String(lng))
              }}
            />
            <Button
              variant="ghost"
              size="sm"
              icon="target"
              disabled={form.geofence.length < 3}
              onClick={() => {
                const center = polygonCenter(form.geofence)
                if (center) {
                  set('lat', String(center[0]))
                  set('lng', String(center[1]))
                }
              }}
            >
              {t('apUseCenter')}
            </Button>
          </div>
        </FormSection>

        <FormSection title={t('apSectionPolicy')} description={t('apPolicyCopy')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input
              id="ap-free-waiting"
              type="number"
              min={0}
              step={1}
              dir="ltr"
              label={`${t('apFreeWaiting')} (${t('min')})`}
              hint={t('apPolicyOptionalHint')}
              value={form.freeWaiting}
              error={err('defaultFreeWaitingMinutes')}
              onChange={(event) => set('freeWaiting', event.target.value)}
            />
            <Input
              id="ap-per-minute"
              type="number"
              min={0}
              step="0.01"
              dir="ltr"
              label={`${t('apPerMinute')} (${t('sar')})`}
              hint={t('apPolicyOptionalHint')}
              value={form.perMinute}
              error={err('defaultWaitingPerMinute')}
              onChange={(event) => set('perMinute', event.target.value)}
            />
          </div>
          <div className="mt-4 space-y-3">
            <Toggle checked={form.requiresPickupZone} onChange={(value) => set('requiresPickupZone', value)} label={t('apRequiresPickupZone')} description={t('apRequiresPickupZoneCopy')} />
            <Toggle checked={form.queueEnabled} onChange={(value) => set('queueEnabled', value)} label={t('apQueueEnabled')} description={t('apQueueEnabledCopy')} />
          </div>
          <p className="mt-3 flex items-start gap-2 text-xs text-muted">
            <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
            {t('apFlightNote')}
          </p>
        </FormSection>

        <FormSection title={t('apSectionStatus')}>
          <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} description={t('apActiveCopy')} />
        </FormSection>
      </form>
    </Modal>
  )
}
