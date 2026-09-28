import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { PageHeader } from '../../../components/business/ui'
import { Action, Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Field, Input, Select } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { LocationPicker, type LatLng, type PickTarget } from '../../../components/LocationPicker'
import { Notice } from '../../../components/Notice'
import { useI18n } from '../../../i18n'
import { catalogApi, corporateApi, isApiError } from '../../../lib/api'
import { describeViolation, violationsFrom } from '../../../lib/corporate'
import { describeError } from '../../../lib/errors'
import { formatMinutes, formatMoney, isValidLocalPhone, normalizeLocalPhone } from '../../../lib/format'
import { geocoderEnabled, reverseGeocode, searchPlaces, type GeocodeResult } from '../../../lib/geocode'
import type { CorporateQuote, Place, PolicyViolation } from '../../../lib/types'
import { useDebounced } from '../../../lib/useDebounced'
import { useResource } from '../../../lib/useResource'

type RiderKind = 'employee' | 'guest'

interface PointState {
  point: LatLng | null
  name: string
  address: string
}

const emptyPoint: PointState = { point: null, name: '', address: '' }
const MAX_SCHEDULE_DAYS = 7

/** `yyyy-MM-ddTHH:mm` in local time for <input type="datetime-local">. */
function localInputValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** Allowed `datetime-local` range for scheduled bookings: +30 min … +7 days. */
function scheduleBounds(): { min: string; max: string } {
  const now = new Date()
  const min = new Date(now.getTime() + 30 * 60_000)
  const max = new Date(now.getTime() + MAX_SCHEDULE_DAYS * 24 * 3600_000)
  return { min: localInputValue(min), max: localInputValue(max) }
}

function PlaceSearch({ onSelect }: { onSelect: (result: GeocodeResult) => void }) {
  const { t, lang } = useI18n()
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<GeocodeResult[] | null>(null)
  const [busy, setBusy] = useState(false)
  const [failed, setFailed] = useState(false)

  const search = async () => {
    if (!query.trim()) return
    setBusy(true)
    setFailed(false)
    try {
      setResults(await searchPlaces(query, lang))
    } catch {
      setFailed(true)
      setResults(null)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="relative">
      <div className="flex gap-2">
        <Input
          type="search"
          value={query}
          placeholder={t('biz.book.searchPlaceholder')}
          aria-label={t('biz.book.search')}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault()
              void search()
            }
          }}
        />
        <Button variant="secondary" onClick={() => void search()} disabled={busy} aria-label={t('biz.book.search')} className="shrink-0 px-4">
          <Icon name="search" />
        </Button>
      </div>
      {failed && <p className="mt-2 text-xs font-bold text-danger">{t('biz.book.searchFailed')}</p>}
      {results && (
        <ul className="mt-2 max-h-60 divide-y divide-line overflow-y-auto rounded-2xl border border-line bg-white">
          {results.length === 0 && <li className="p-3 text-sm text-muted">{t('help.noResults')}</li>}
          {results.map((result) => (
            <li key={`${result.lat},${result.lng}`}>
              <Action
                onClick={() => {
                  onSelect(result)
                  setResults(null)
                  setQuery('')
                }}
                className="block w-full p-3 hover:bg-cloud"
              >
                <span className="block text-sm font-bold">{result.name}</span>
                <span className="block truncate text-xs text-muted">{result.address}</span>
              </Action>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

export function NewBooking() {
  const { t, lang } = useI18n()
  const navigate = useNavigate()
  const categories = useResource(() => catalogApi.rideCategories(), [lang])
  const costCenters = useResource(() => corporateApi.costCenters(), [lang])

  const [riderKind, setRiderKind] = useState<RiderKind>('employee')
  const [employeeSearch, setEmployeeSearch] = useState('')
  const debouncedEmployee = useDebounced(employeeSearch.trim())
  const employees = useResource(
    () => corporateApi.employees({ status: 'active', search: debouncedEmployee || undefined, page: 1 }),
    [debouncedEmployee, lang],
  )
  const [employeeId, setEmployeeId] = useState('')
  const [guestName, setGuestName] = useState('')
  const [guestPhone, setGuestPhone] = useState('')

  const [pickup, setPickup] = useState<PointState>(emptyPoint)
  const [dropoff, setDropoff] = useState<PointState>(emptyPoint)
  const [target, setTarget] = useState<PickTarget>('pickup')

  const [categoryId, setCategoryId] = useState('')
  const [bookingType, setBookingType] = useState<'now' | 'scheduled'>('now')
  const [scheduledAt, setScheduledAt] = useState('')
  const [purpose, setPurpose] = useState('')
  const [costCenterId, setCostCenterId] = useState('')
  const [note, setNote] = useState('')

  const [quote, setQuote] = useState<CorporateQuote | null>(null)
  const [quoting, setQuoting] = useState(false)
  const [booking, setBooking] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [violations, setViolations] = useState<PolicyViolation[]>([])
  const [touched, setTouched] = useState(false)

  const [bounds] = useState(scheduleBounds)

  // Any input change invalidates the quote (prices and policy checks depend on all of them).
  const invalidate = () => {
    setQuote(null)
    setViolations([])
    setError(null)
  }
  const change = <T,>(setter: (value: T) => void) => (value: T) => {
    setter(value)
    invalidate()
  }

  const setPoint = (which: PickTarget, next: PointState) => {
    ;(which === 'pickup' ? setPickup : setDropoff)(next)
    invalidate()
  }

  const onMapPick = (which: PickTarget, point: LatLng) => {
    const current = which === 'pickup' ? pickup : dropoff
    setPoint(which, { ...current, point })
    if (which === 'pickup' && !dropoff.point) setTarget('dropoff')
    void reverseGeocode(point.lat, point.lng, lang).then((result) => {
      if (!result) return
      const setter = which === 'pickup' ? setPickup : setDropoff
      setter((latest) =>
        latest.point && latest.point.lat === point.lat && latest.point.lng === point.lng
          ? { ...latest, name: latest.name || result.name, address: result.address }
          : latest,
      )
    })
  }

  const scheduledIso = bookingType === 'scheduled' && scheduledAt ? new Date(scheduledAt).toISOString() : undefined
  const guestValid = guestName.trim().length >= 2 && isValidLocalPhone(guestPhone)
  const riderValid = riderKind === 'employee' ? employeeId !== '' : guestValid
  const pointsValid = Boolean(pickup.point && dropoff.point && pickup.name.trim() && dropoff.name.trim())
  const scheduleValid = bookingType === 'now' || (scheduledAt !== '' && scheduledAt >= bounds.min && scheduledAt <= bounds.max)
  const formValid = riderValid && pointsValid && scheduleValid && categoryId !== '' && purpose.trim() !== ''

  const toPlace = (state: PointState): Place => ({
    name: state.name.trim(),
    address: state.address || null,
    lat: state.point?.lat ?? 0,
    lng: state.point?.lng ?? 0,
  })

  const handleError = (caught: unknown) => {
    setError(describeError(caught, t))
    setViolations(isApiError(caught) && caught.code === 'corporate_policy_violation' ? violationsFrom(caught.details) : [])
  }

  const getQuote = async (event: FormEvent) => {
    event.preventDefault()
    setTouched(true)
    if (!formValid) return
    setQuoting(true)
    setError(null)
    setViolations([])
    try {
      const response = await corporateApi.quote({
        pickup: toPlace(pickup),
        dropoff: toPlace(dropoff),
        stops: [],
        rideCategoryId: categoryId,
        bookingType,
        scheduledAt: scheduledIso,
        employeeId: riderKind === 'employee' ? employeeId : undefined,
        purpose: purpose.trim(),
        costCenterId: costCenterId || undefined,
      })
      setQuote(response)
    } catch (caught) {
      handleError(caught)
    } finally {
      setQuoting(false)
    }
  }

  const confirm = async () => {
    if (!quote) return
    setBooking(true)
    setError(null)
    try {
      const trip = await corporateApi.book({
        employeeId: riderKind === 'employee' ? employeeId : undefined,
        guest: riderKind === 'guest' ? { name: guestName.trim(), phoneNumber: `+966${guestPhone}` } : undefined,
        pickup: toPlace(pickup),
        dropoff: toPlace(dropoff),
        stops: [],
        rideCategoryId: categoryId,
        bookingType,
        scheduledAt: scheduledIso,
        quoteId: quote.quoteId,
        tripPurpose: purpose.trim(),
        costCenterId: costCenterId || undefined,
        riderNote: note.trim() || undefined,
      })
      navigate(`/business/app/bookings/${trip.id}`, { replace: true })
    } catch (caught) {
      handleError(caught)
      setQuote(null)
    } finally {
      setBooking(false)
    }
  }

  const money = (amount: number) => formatMoney(amount, lang)
  const selectedQuote = quote?.categories.find((item) => item.rideCategoryId === categoryId) ?? quote?.categories[0] ?? null
  const policyBlocked = quote?.corporate ? !quote.corporate.allowed : false
  const quoteViolations = quote?.corporate?.violations ?? []
  const shownViolations = violations.length > 0 ? violations : quoteViolations

  const pointEditor = (which: PickTarget, state: PointState) => (
    <div className={`rounded-2xl border-2 p-4 transition ${target === which ? 'border-brand bg-brand-soft/40' : 'border-line'}`}>
      <div className="mb-3 flex items-center justify-between gap-3">
        <span className="flex items-center gap-2 text-sm font-bold">
          <span className={`size-3 rounded-full ${which === 'pickup' ? 'bg-brand' : 'bg-ink'}`} />
          {t(which === 'pickup' ? 'biz.book.pickup' : 'biz.book.dropoff')}
        </span>
        <Action
          onClick={() => setTarget(which)}
          aria-pressed={target === which}
          className={`rounded-full px-3 py-1.5 text-xs font-bold ${target === which ? 'bg-brand text-white' : 'bg-cloud text-muted hover:text-ink'}`}
        >
          {target === which ? t('biz.book.clickMap') : t('biz.book.setOnMap')}
        </Action>
      </div>
      <Input
        aria-label={t(which === 'pickup' ? 'biz.book.pickupName' : 'biz.book.dropoffName')}
        placeholder={t('biz.book.placeName')}
        value={state.name}
        invalid={touched && !state.name.trim()}
        onChange={(event) => setPoint(which, { ...state, name: event.target.value })}
      />
      <p className="mt-2 text-xs text-muted" dir="ltr">
        {state.point ? `${state.point.lat.toFixed(5)}, ${state.point.lng.toFixed(5)}` : t('biz.book.noPoint')}
      </p>
      {touched && !state.point && <p className="mt-1 text-xs font-bold text-danger">{t('biz.book.pointRequired')}</p>}
    </div>
  )

  return (
    <>
      <title>{t('biz.bookings.new')} · ATA</title>
      <Link to="/business/app/bookings" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-muted hover:text-ink">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('biz.bookings.title')}
      </Link>
      <PageHeader title={t('biz.bookings.new')} subtitle={t('biz.book.subtitle')} />

      <form onSubmit={getQuote} noValidate className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="space-y-4">
          <Card>
            <h2 className="mb-4 font-bold">{t('biz.book.rider')}</h2>
            <div role="radiogroup" className="mb-4 grid grid-cols-2 gap-2 rounded-2xl bg-cloud p-1">
              {(['employee', 'guest'] as const).map((kind) => (
                <Action
                  key={kind}
                  role="radio"
                  aria-checked={riderKind === kind}
                  onClick={() => change(setRiderKind)(kind)}
                  className={`rounded-xl px-3 py-2.5 text-center text-sm font-bold ${riderKind === kind ? 'bg-white text-ink shadow-soft' : 'text-muted'}`}
                >
                  {t(kind === 'employee' ? 'biz.book.forEmployee' : 'biz.book.forGuest')}
                </Action>
              ))}
            </div>
            {riderKind === 'employee' ? (
              <div className="grid gap-3 sm:grid-cols-2">
                <Input
                  type="search"
                  aria-label={t('biz.employees.searchPlaceholder')}
                  placeholder={t('biz.employees.searchPlaceholder')}
                  value={employeeSearch}
                  onChange={(event) => setEmployeeSearch(event.target.value)}
                />
                <Select
                  aria-label={t('biz.book.employee')}
                  value={employeeId}
                  invalid={touched && !employeeId}
                  onChange={(event) => change(setEmployeeId)(event.target.value)}
                >
                  <option value="">{t('biz.book.chooseEmployee')}</option>
                  {(employees.data?.items ?? []).map((employee) => (
                    <option key={employee.id} value={employee.id}>
                      {employee.fullName ?? employee.phoneNumber}
                      {employee.department ? ` · ${employee.department}` : ''}
                    </option>
                  ))}
                </Select>
              </div>
            ) : (
              <div className="grid gap-3 sm:grid-cols-2">
                <Field label={t('biz.book.guestName')} htmlFor="guest-name" error={touched && guestName.trim().length < 2 ? t('form.required') : null}>
                  <Input id="guest-name" value={guestName} onChange={(event) => change(setGuestName)(event.target.value)} />
                </Field>
                <Field
                  label={t('biz.book.guestPhone')}
                  htmlFor="guest-phone"
                  hint={t('biz.book.guestHint')}
                  error={touched && !isValidLocalPhone(guestPhone) ? t('login.phone.invalid') : null}
                >
                  <div className="flex items-center gap-2" dir="ltr">
                    <span className="grid h-14 place-items-center rounded-2xl bg-cloud px-3 text-sm font-bold">+966</span>
                    <Input
                      id="guest-phone"
                      type="tel"
                      inputMode="numeric"
                      placeholder="5X XXX XXXX"
                      value={guestPhone}
                      onChange={(event) => change(setGuestPhone)(normalizeLocalPhone(event.target.value))}
                    />
                  </div>
                </Field>
              </div>
            )}
          </Card>

          <Card className="space-y-3">
            <h2 className="font-bold">{t('biz.book.route')}</h2>
            {geocoderEnabled && (
              <PlaceSearch
                onSelect={(result) => {
                  const next = { point: { lat: result.lat, lng: result.lng }, name: result.name, address: result.address }
                  setPoint(target, next)
                  if (target === 'pickup' && !dropoff.point) setTarget('dropoff')
                }}
              />
            )}
            <LocationPicker
              className="h-72 sm:h-96"
              pickup={pickup.point}
              dropoff={dropoff.point}
              target={target}
              onPick={onMapPick}
            />
            <div className="grid gap-3 sm:grid-cols-2">
              {pointEditor('pickup', pickup)}
              {pointEditor('dropoff', dropoff)}
            </div>
          </Card>

          <Card className="grid gap-4 sm:grid-cols-2">
            <Field label={t('biz.book.category')} htmlFor="book-category" error={touched && !categoryId ? t('form.required') : null}>
              <Select id="book-category" value={categoryId} onChange={(event) => change(setCategoryId)(event.target.value)}>
                <option value="">{t('biz.book.chooseCategory')}</option>
                {(categories.data ?? []).map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label={t('biz.book.when')} htmlFor="book-type">
              <Select
                id="book-type"
                value={bookingType}
                onChange={(event) => change(setBookingType)(event.target.value === 'scheduled' ? 'scheduled' : 'now')}
              >
                <option value="now">{t('biz.book.now')}</option>
                <option value="scheduled">{t('biz.book.scheduled')}</option>
              </Select>
            </Field>
            {bookingType === 'scheduled' && (
              <Field
                label={t('biz.book.scheduledAt')}
                htmlFor="book-at"
                hint={t('biz.book.scheduleHint', { n: MAX_SCHEDULE_DAYS })}
                error={touched && !scheduleValid ? t('biz.book.scheduleInvalid') : null}
                className="sm:col-span-2"
              >
                <Input
                  id="book-at"
                  type="datetime-local"
                  min={bounds.min}
                  max={bounds.max}
                  value={scheduledAt}
                  onChange={(event) => change(setScheduledAt)(event.target.value)}
                />
              </Field>
            )}
            <Field label={t('biz.book.purpose')} htmlFor="book-purpose" error={touched && !purpose.trim() ? t('form.required') : null}>
              <Input id="book-purpose" maxLength={200} value={purpose} onChange={(event) => change(setPurpose)(event.target.value)} />
            </Field>
            <Field label={t('biz.employee.costCenter')} htmlFor="book-cost-center">
              <Select id="book-cost-center" value={costCenterId} onChange={(event) => change(setCostCenterId)(event.target.value)}>
                <option value="">{t('biz.none')}</option>
                {(costCenters.data ?? [])
                  .filter((item) => item.isActive)
                  .map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.code} · {item.name}
                    </option>
                  ))}
              </Select>
            </Field>
            <Field label={t('biz.book.note')} htmlFor="book-note" className="sm:col-span-2">
              <Input id="book-note" maxLength={200} value={note} onChange={(event) => setNote(event.target.value)} />
            </Field>
          </Card>
        </div>

        <div className="space-y-4 xl:sticky xl:top-24 xl:self-start">
          {error && (
            <Notice tone="error">
              {error}
              {shownViolations.length > 0 && (
                <ul className="mt-2 list-disc space-y-1 ps-5 text-xs">
                  {shownViolations.map((violation) => (
                    <li key={violation.rule}>{describeViolation(violation, t, money)}</li>
                  ))}
                </ul>
              )}
            </Notice>
          )}

          {quote && selectedQuote ? (
            <Card tone="dark" className="space-y-4">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-bold text-white/70">{selectedQuote.name}</p>
                  <p className="mt-1 text-3xl font-bold">{money(selectedQuote.total)}</p>
                  <p className="mt-1 text-xs text-white/60">{t('biz.book.vatIncluded')}</p>
                </div>
                {selectedQuote.etaMinutes !== null && (
                  <span className="rounded-full bg-white/10 px-3 py-1.5 text-xs font-bold">
                    {t('biz.book.eta', { time: formatMinutes(selectedQuote.etaMinutes, lang) })}
                  </span>
                )}
              </div>
              <p className="text-sm text-white/70">
                {t('biz.book.distance', {
                  km: (quote.distanceMeters / 1000).toFixed(1),
                  time: formatMinutes(Math.max(1, Math.round(quote.durationSeconds / 60)), lang),
                })}
              </p>
              {quote.corporate?.remainingBudget !== null && quote.corporate?.remainingBudget !== undefined && (
                <p className="text-sm font-bold text-brand">{t('biz.book.remaining', { amount: money(quote.corporate.remainingBudget) })}</p>
              )}
              {policyBlocked ? (
                <div className="rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger">
                  {t('biz.book.blocked')}
                  <ul className="mt-2 list-disc space-y-1 ps-5 text-xs">
                    {quoteViolations.map((violation) => (
                      <li key={violation.rule}>{describeViolation(violation, t, money)}</li>
                    ))}
                  </ul>
                </div>
              ) : (
                <p className="flex items-center gap-2 text-sm font-bold text-brand">
                  <Icon name="check" className="size-5" />
                  {t('biz.book.allowed')}
                </p>
              )}
              <Button variant="brand" block onClick={() => void confirm()} disabled={booking || policyBlocked}>
                {booking ? t('biz.book.booking') : t('biz.book.confirm')}
              </Button>
            </Card>
          ) : (
            <Button type="submit" block disabled={quoting}>
              {quoting ? t('biz.book.quoting') : t('biz.book.getQuote')}
            </Button>
          )}
          {touched && !formValid && !quote && <p className="text-center text-xs font-bold text-danger">{t('biz.book.incomplete')}</p>}
        </div>
      </form>
    </>
  )
}
