import { useMemo, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import type { TranslationKey } from '../i18n'
import { airports } from '../lib/admin'
import { AIRPORT_ZONE_KIND_KEY, AIRPORT_ZONE_KINDS, airportZoneInputOf, INSTRUCTIONS_MAX, validateAirportZone, type AirportZoneErrorField } from '../lib/airports'
import { isApiError } from '../lib/api'
import { closeRing, polygonCenter } from '../lib/pricing'
import type { Airport, AirportZone, AirportZoneInput, AirportZoneKind, LatLngTuple } from '../lib/types'
import { Button } from './Button'
import { Input, Select, Textarea, Toggle } from './Field'
import { FormSection } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { PointPicker } from './PointPicker'
import { PolygonEditor, type PolygonContext } from './PolygonEditor'

interface FormState {
  kind: AirportZoneKind
  code: string
  terminalCode: string
  nameAr: string
  nameEn: string
  polygon: LatLngTuple[]
  lat: string
  lng: string
  instructionsAr: string
  instructionsEn: string
  freeWaiting: string
  perMinute: string
  sortOrder: string
  isActive: boolean
}

const str = (value: number | null | undefined) => (value === null || value === undefined || Number.isNaN(value) ? '' : String(value))
const num = (value: string) => (value.trim() === '' ? Number.NaN : Number(value))
const optional = (value: string) => (value.trim() === '' ? null : Number(value))
const text = (value: string) => (value.trim() === '' ? null : value.trim())

function toForm(zone: AirportZone | null, airport: Airport, nextSort: number): FormState {
  if (!zone) {
    return {
      kind: 'pickup_zone',
      code: '',
      terminalCode: '',
      nameAr: '',
      nameEn: '',
      polygon: [],
      lat: str(airport.lat),
      lng: str(airport.lng),
      instructionsAr: '',
      instructionsEn: '',
      freeWaiting: '',
      perMinute: '',
      sortOrder: String(nextSort),
      isActive: true,
    }
  }
  const input = airportZoneInputOf(zone)
  return {
    kind: input.kind,
    code: input.code,
    terminalCode: input.terminalCode ?? '',
    nameAr: input.nameAr,
    nameEn: input.nameEn,
    polygon: input.polygon ?? [],
    lat: str(input.lat),
    lng: str(input.lng),
    instructionsAr: input.instructionsAr ?? '',
    instructionsEn: input.instructionsEn ?? '',
    freeWaiting: str(input.freeWaitingMinutes),
    perMinute: str(input.waitingPerMinute),
    sortOrder: String(input.sortOrder),
    isActive: input.isActive,
  }
}

function toInput(form: FormState): AirportZoneInput {
  const waiting = form.kind === 'driver_waiting_area'
  const hasPolicy = form.kind === 'pickup_zone'
  const polygon = waiting ? closeRing(form.polygon) : null
  // A waiting area has no pin of its own: its reference point is the polygon centre.
  const center = waiting ? polygonCenter(form.polygon) : null
  return {
    kind: form.kind,
    code: form.code.trim(),
    terminalCode: form.kind === 'driver_waiting_area' ? null : text(form.terminalCode)?.toUpperCase() ?? null,
    nameAr: form.nameAr.trim(),
    nameEn: form.nameEn.trim(),
    polygon,
    lat: center ? center[0] : num(form.lat),
    lng: center ? center[1] : num(form.lng),
    instructionsAr: waiting ? null : text(form.instructionsAr),
    instructionsEn: waiting ? null : text(form.instructionsEn),
    freeWaitingMinutes: hasPolicy ? optional(form.freeWaiting) : null,
    waitingPerMinute: hasPolicy ? optional(form.perMinute) : null,
    sortOrder: form.sortOrder.trim() === '' ? Number.NaN : Number(form.sortOrder),
    isActive: form.isActive,
  }
}

export interface AirportZoneFormModalProps {
  open: boolean
  airport: Airport
  /** Zone to edit; null creates one. */
  zone: AirportZone | null
  /** Every zone of the airport (unique code check, terminal choices, muted polygons). */
  zones: AirportZone[]
  onClose: () => void
  onSaved: (zone: AirportZone | null) => void
}

export function AirportZoneFormModal({ open, ...props }: AirportZoneFormModalProps) {
  return open ? <AirportZoneDialog {...props} /> : null
}

function AirportZoneDialog({ airport, zone, zones, onClose, onSaved }: Omit<AirportZoneFormModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const nextSort = zones.reduce((max, other) => Math.max(max, other.sortOrder), 0) + 1
  const [form, setForm] = useState<FormState>(() => toForm(zone, airport, nextSort))
  const [errors, setErrors] = useState<Partial<Record<AirportZoneErrorField, TranslationKey>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const others = useMemo(() => zones.filter((other) => other.id !== zone?.id), [zones, zone])
  const terminals = useMemo(() => others.filter((other) => other.kind === 'terminal' && other.terminalCode), [others])
  const context = useMemo<PolygonContext[]>(
    () => [
      { id: airport.id, name: `${airport.code} · ${airport.nameEn}`, polygon: airport.geofence ?? [] },
      ...others.filter((other) => other.kind === 'driver_waiting_area' && other.polygon).map((other) => ({ id: other.id, name: other.nameEn, polygon: other.polygon ?? [] })),
    ],
    [airport, others],
  )

  const touch = () => {
    setErrors({})
    setFormError(null)
  }
  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    touch()
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const input = toInput(form)
    const found = validateAirportZone(input, others)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      setFormError(t('fixErrors'))
      return
    }
    setSaving(true)
    try {
      const saved = zone ? await airports.updateZone(airport.id, zone.id, input) : await airports.createZone(airport.id, input)
      onSaved(saved ?? null)
    } catch (error) {
      if (isApiError(error) && error.status === 409) setErrors({ code: 'apErrZoneCodeExists' })
      else if (isApiError(error) && error.status === 422 && error.details) {
        const fields = Object.keys(error.details).filter((key): key is AirportZoneErrorField => key in input)
        setErrors(Object.fromEntries(fields.map((key) => [key, 'invalidNumber' as TranslationKey])))
      }
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  const err = (key: AirportZoneErrorField) => (errors[key] ? t(errors[key]) : undefined)
  const waiting = form.kind === 'driver_waiting_area'
  const pickup = form.kind === 'pickup_zone'

  return (
    <Modal
      open
      size="xl"
      title={zone ? `${t('apZoneEdit')} · ${zone.code}` : t('apZoneNew')}
      description={`${airport.code} — ${t('apZoneFormCopy')}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="airport-zone-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="airport-zone-form" onSubmit={save} noValidate className="space-y-5">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}

        <FormSection title={t('apSectionBasics')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Select id="apz-kind" label={t('apZoneKind')} value={form.kind} onChange={(event) => set('kind', event.target.value as AirportZoneKind)} disabled={zone !== null}>
              {AIRPORT_ZONE_KINDS.map((kind) => (
                <option key={kind} value={kind}>
                  {t(AIRPORT_ZONE_KIND_KEY[kind])}
                </option>
              ))}
            </Select>
            <Input id="apz-code" dir="ltr" maxLength={30} label={t('code')} hint={t('apZoneCodeHint')} value={form.code} error={err('code')} onChange={(event) => set('code', event.target.value)} />
            {form.kind === 'terminal' && (
              <Input
                id="apz-terminal"
                dir="ltr"
                maxLength={10}
                label={t('apTerminalCode')}
                hint={t('apTerminalCodeHintOwn')}
                value={form.terminalCode}
                error={err('terminalCode')}
                onChange={(event) => set('terminalCode', event.target.value.toUpperCase())}
              />
            )}
            {pickup &&
              (terminals.length > 0 ? (
                <Select id="apz-terminal" label={t('apTerminalCode')} value={form.terminalCode} error={err('terminalCode')} onChange={(event) => set('terminalCode', event.target.value)}>
                  <option value="">{t('apNoTerminal')}</option>
                  {form.terminalCode && !terminals.some((terminal) => terminal.terminalCode === form.terminalCode) && <option value={form.terminalCode}>{form.terminalCode}</option>}
                  {terminals.map((terminal) => (
                    <option key={terminal.id} value={terminal.terminalCode ?? ''}>
                      {terminal.terminalCode} · {terminal.nameEn}
                    </option>
                  ))}
                </Select>
              ) : (
                <Input
                  id="apz-terminal"
                  dir="ltr"
                  maxLength={10}
                  label={t('apTerminalCode')}
                  hint={t('apTerminalCodeHintLink')}
                  value={form.terminalCode}
                  error={err('terminalCode')}
                  onChange={(event) => set('terminalCode', event.target.value.toUpperCase())}
                />
              ))}
            <Input id="apz-name-ar" dir="rtl" label={t('nameAr')} value={form.nameAr} error={err('nameAr')} onChange={(event) => set('nameAr', event.target.value)} />
            <Input id="apz-name-en" dir="ltr" label={t('nameEn')} value={form.nameEn} error={err('nameEn')} onChange={(event) => set('nameEn', event.target.value)} />
            <Input id="apz-sort" type="number" step={1} dir="ltr" label={t('sortOrder')} value={form.sortOrder} error={err('sortOrder')} onChange={(event) => set('sortOrder', event.target.value)} />
          </div>
        </FormSection>

        {waiting ? (
          <FormSection title={t('apSectionWaitingArea')} description={t('apWaitingAreaCopy')}>
            <PolygonEditor value={form.polygon} onChange={(points) => set('polygon', points)} context={context} error={err('polygon')} />
          </FormSection>
        ) : (
          <FormSection title={t('apSectionPoint')} description={t('apZonePointCopy')}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Input id="apz-lat" type="number" step="any" dir="ltr" label={t('lat')} value={form.lat} error={err('lat')} onChange={(event) => set('lat', event.target.value)} />
              <Input id="apz-lng" type="number" step="any" dir="ltr" label={t('lng')} value={form.lng} error={err('lng')} onChange={(event) => set('lng', event.target.value)} />
            </div>
            <div className="mt-4">
              <PointPicker
                lat={num(form.lat)}
                lng={num(form.lng)}
                context={[closeRing(airport.geofence ?? [])]}
                onChange={(lat, lng) => {
                  set('lat', String(lat))
                  set('lng', String(lng))
                }}
              />
            </div>
          </FormSection>
        )}

        {!waiting && (
          <FormSection title={t('apSectionInstructions')} description={t('apInstructionsCopy')}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Textarea
                id="apz-instr-ar"
                dir="rtl"
                label={t('apInstructionsAr')}
                maxLength={INSTRUCTIONS_MAX}
                hint={<span className="ltr-nums">{form.instructionsAr.length} / {INSTRUCTIONS_MAX}</span>}
                value={form.instructionsAr}
                error={err('instructionsAr')}
                onChange={(event) => set('instructionsAr', event.target.value)}
              />
              <Textarea
                id="apz-instr-en"
                dir="ltr"
                label={t('apInstructionsEn')}
                maxLength={INSTRUCTIONS_MAX}
                hint={<span className="ltr-nums">{form.instructionsEn.length} / {INSTRUCTIONS_MAX}</span>}
                value={form.instructionsEn}
                error={err('instructionsEn')}
                onChange={(event) => set('instructionsEn', event.target.value)}
              />
            </div>
          </FormSection>
        )}

        {pickup && (
          <FormSection title={t('apSectionPolicy')} description={t('apZonePolicyCopy')}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Input
                id="apz-free"
                type="number"
                min={0}
                step={1}
                dir="ltr"
                label={`${t('apFreeWaiting')} (${t('min')})`}
                hint={`${t('apZonePolicyHint')} ${airport.defaultFreeWaitingMinutes ?? '—'}`}
                value={form.freeWaiting}
                error={err('freeWaitingMinutes')}
                onChange={(event) => set('freeWaiting', event.target.value)}
              />
              <Input
                id="apz-per-minute"
                type="number"
                min={0}
                step="0.01"
                dir="ltr"
                label={`${t('apPerMinute')} (${t('sar')})`}
                hint={`${t('apZonePolicyHint')} ${airport.defaultWaitingPerMinute ?? '—'}`}
                value={form.perMinute}
                error={err('waitingPerMinute')}
                onChange={(event) => set('perMinute', event.target.value)}
              />
            </div>
          </FormSection>
        )}

        <FormSection title={t('apSectionStatus')}>
          <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
        </FormSection>
      </form>
    </Modal>
  )
}
