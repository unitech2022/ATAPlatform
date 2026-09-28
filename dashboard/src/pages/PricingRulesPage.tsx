import { useEffect, useEffectEvent, useMemo, useState, type FormEvent } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { DefinitionList } from '../components/DefinitionList'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { Input, Select, Toggle } from '../components/Field'
import { MapView } from '../components/MapView'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { pricingRules, rideCategories, zones } from '../lib/admin'
import { formatDate, formatKm, formatMinutes, formatMoney, formatNumber } from '../lib/format'
import { escapeHtml, L, pinIcon, RIYADH, routeLineStyle } from '../lib/leaflet'
import { fromLocalInput, isValidLatLng, isValidTime, localName, numberOrNull, round6, tint, toLocalInput, WEEKDAYS } from '../lib/pricing'
import type { GeoPoint, PricingRule, PricingRuleInput, RideCategory, SimulateResult, SimulatedCategory, TimeMultiplier } from '../lib/types'

const NUMBER_FIELDS = [
  'baseFare',
  'perKm',
  'perMinute',
  'bookingFee',
  'serviceFeePercent',
  'minFare',
  'waitingPerMinute',
  'freeWaitingMinutes',
  'cancellationFee',
  'driverSharePercent',
  'priority',
] as const

type NumberField = (typeof NUMBER_FIELDS)[number]

type MultiplierRow = { id?: string; dayOfWeek: string; fromTime: string; toTime: string; multiplier: string; label: string }

type FormState = Record<NumberField, string> & {
  rideCategoryId: string
  zoneId: string
  name: string
  effectiveFrom: string
  effectiveTo: string
  isActive: boolean
  timeMultipliers: MultiplierRow[]
}

type FormErrors = Partial<Record<keyof FormState, string>>

const EMPTY_FORM: FormState = {
  rideCategoryId: '',
  zoneId: '',
  name: '',
  baseFare: '5',
  perKm: '1.5',
  perMinute: '0.3',
  bookingFee: '2',
  serviceFeePercent: '0',
  minFare: '10',
  waitingPerMinute: '0.5',
  freeWaitingMinutes: '3',
  cancellationFee: '5',
  driverSharePercent: '80',
  priority: '0',
  effectiveFrom: toLocalInput(new Date().toISOString()),
  effectiveTo: '',
  isActive: true,
  timeMultipliers: [],
}

const EMPTY_MULTIPLIER: MultiplierRow = { dayOfWeek: '', fromTime: '22:00', toTime: '06:00', multiplier: '1.2', label: 'night' }

function toForm(rule: PricingRule): FormState {
  return {
    rideCategoryId: rule.rideCategoryId,
    zoneId: rule.zoneId ?? '',
    name: rule.name,
    baseFare: String(rule.baseFare),
    perKm: String(rule.perKm),
    perMinute: String(rule.perMinute),
    bookingFee: String(rule.bookingFee),
    serviceFeePercent: String(rule.serviceFeePercent),
    minFare: String(rule.minFare),
    waitingPerMinute: String(rule.waitingPerMinute),
    freeWaitingMinutes: String(rule.freeWaitingMinutes),
    cancellationFee: String(rule.cancellationFee),
    driverSharePercent: String(rule.driverSharePercent),
    priority: String(rule.priority),
    effectiveFrom: toLocalInput(rule.effectiveFrom),
    effectiveTo: toLocalInput(rule.effectiveTo),
    isActive: rule.isActive,
    timeMultipliers: (rule.timeMultipliers ?? []).map((row) => ({
      id: row.id,
      dayOfWeek: row.dayOfWeek === null ? '' : String(row.dayOfWeek),
      fromTime: row.fromTime.slice(0, 5),
      toTime: row.toTime.slice(0, 5),
      multiplier: String(row.multiplier),
      label: row.label ?? '',
    })),
  }
}

function toInput(form: FormState): PricingRuleInput {
  const numbers = Object.fromEntries(NUMBER_FIELDS.map((key) => [key, Number(form[key])])) as Record<NumberField, number>
  return {
    ...numbers,
    rideCategoryId: form.rideCategoryId,
    zoneId: form.zoneId || null,
    name: form.name.trim(),
    effectiveFrom: fromLocalInput(form.effectiveFrom) ?? new Date().toISOString(),
    effectiveTo: fromLocalInput(form.effectiveTo),
    isActive: form.isActive,
    timeMultipliers: form.timeMultipliers.map(
      (row): TimeMultiplier => ({
        ...(row.id ? { id: row.id } : {}),
        dayOfWeek: row.dayOfWeek === '' ? null : Number(row.dayOfWeek),
        fromTime: row.fromTime,
        toTime: row.toTime,
        multiplier: Number(row.multiplier),
        label: row.label.trim(),
      }),
    ),
  }
}

type Editing = { mode: 'create' } | { mode: 'edit'; rule: PricingRule } | null

export function PricingRulesPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()

  const [categoryFilter, setCategoryFilter] = useState('')
  const [zoneFilter, setZoneFilter] = useState('')
  const [activeFilter, setActiveFilter] = useState<'' | 'true' | 'false'>('')

  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const rulesQuery = useQuery(
    () =>
      pricingRules.list({
        rideCategoryId: categoryFilter || undefined,
        zoneId: zoneFilter && zoneFilter !== 'city' ? zoneFilter : undefined,
        isActive: activeFilter === '' ? '' : activeFilter === 'true',
      }),
    `pricing-rules:${categoryFilter}:${zoneFilter}:${activeFilter}`,
  )

  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data])
  const categoryById = useMemo(() => new Map(categories.map((category) => [category.id, category])), [categories])
  const zoneById = useMemo(() => new Map(zoneList.map((zone) => [zone.id, zone])), [zoneList])

  // The API may ignore unknown filters, so the same filters are applied client-side too.
  const rows = useMemo(() => {
    const items = rulesQuery.data ?? []
    return items
      .filter((rule) => !categoryFilter || rule.rideCategoryId === categoryFilter)
      .filter((rule) => !zoneFilter || (zoneFilter === 'city' ? rule.zoneId === null : rule.zoneId === zoneFilter))
      .filter((rule) => activeFilter === '' || rule.isActive === (activeFilter === 'true'))
      .sort((a, b) => b.priority - a.priority || a.name.localeCompare(b.name))
  }, [rulesQuery.data, categoryFilter, zoneFilter, activeFilter])

  const [editing, setEditing] = useState<Editing>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [errors, setErrors] = useState<FormErrors>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<PricingRule | null>(null)

  const openCreate = () => {
    setForm({ ...EMPTY_FORM, rideCategoryId: categories[0]?.id ?? '', effectiveFrom: toLocalInput(new Date().toISOString()) })
    setErrors({})
    setEditing({ mode: 'create' })
  }

  const openEdit = (rule: PricingRule) => {
    setForm(toForm(rule))
    setErrors({})
    setEditing({ mode: 'edit', rule })
  }

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const setMultiplier = (index: number, patch: Partial<MultiplierRow>) => {
    setForm((current) => ({
      ...current,
      timeMultipliers: current.timeMultipliers.map((row, i) => (i === index ? { ...row, ...patch } : row)),
    }))
    setErrors((current) => ({ ...current, timeMultipliers: undefined }))
  }

  const validate = (): boolean => {
    const next: FormErrors = {}
    if (!form.rideCategoryId) next.rideCategoryId = t('fieldRequired')
    if (!form.name.trim()) next.name = t('fieldRequired')
    for (const key of NUMBER_FIELDS) if (numberOrNull(form[key]) === null) next[key] = t('invalidNumber')
    if (!fromLocalInput(form.effectiveFrom)) next.effectiveFrom = t('fieldRequired')
    if (form.effectiveTo && !fromLocalInput(form.effectiveTo)) next.effectiveTo = t('invalidNumber')
    const badRow = form.timeMultipliers.some(
      (row) => !isValidTime(row.fromTime) || !isValidTime(row.toTime) || numberOrNull(row.multiplier) === null,
    )
    if (badRow) next.timeMultipliers = t('invalidTime')
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!editing || !validate()) return
    setSaving(true)
    try {
      const input = toInput(form)
      if (editing.mode === 'create') await pricingRules.create(input)
      else await pricingRules.update(editing.rule.id, input)
      toast.success(t('ruleSaved'))
      setEditing(null)
      rulesQuery.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await pricingRules.remove(deleting.id)
      toast.success(t('ruleDeleted'))
      setDeleting(null)
      rulesQuery.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<PricingRule>[] = [
    {
      key: 'name',
      header: t('ruleName'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block font-bold">{row.name}</span>
          <span className="block text-xs text-muted">
            {localName(categoryById.get(row.rideCategoryId), lang)} · {row.zoneId ? localName(zoneById.get(row.zoneId), lang) : t('wholeCity')}
          </span>
        </span>
      ),
    },
    { key: 'baseFare', header: t('baseFare'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatMoney(row.baseFare)}</span> },
    { key: 'perKm', header: t('perKm'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatMoney(row.perKm)}</span> },
    { key: 'perMinute', header: t('perMinute'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatMoney(row.perMinute)}</span> },
    { key: 'minFare', header: t('minFare'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatMoney(row.minFare)}</span> },
    {
      key: 'driverShare',
      header: t('driverSharePercent'),
      className: 'text-end',
      render: (row) => <span className="ltr-nums">{formatNumber(row.driverSharePercent)}%</span>,
    },
    { key: 'priority', header: t('priority'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.priority)}</span> },
    {
      key: 'validity',
      header: t('validity'),
      render: (row) => (
        <span className="whitespace-nowrap text-xs">
          {formatDate(row.effectiveFrom, lang)} → {row.effectiveTo ? formatDate(row.effectiveTo, lang) : '∞'}
        </span>
      ),
    },
    {
      key: 'multipliers',
      header: t('timeMultipliers'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums">{formatNumber(row.timeMultipliers?.length ?? 0)}</span>,
    },
    {
      key: 'isActive',
      header: t('status'),
      render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge>,
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => openEdit(row)}>
            {t('edit')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  const numberInput = (key: NumberField, step = '0.01') => (
    <Input
      key={key}
      id={key}
      type="number"
      step={step}
      dir="ltr"
      label={t(key)}
      value={form[key]}
      error={errors[key]}
      onChange={(e) => setField(key, e.target.value)}
    />
  )

  return (
    <>
      <PageHeader
        title={t('pricingRulesTitle')}
        description={t('pricingRulesCopy')}
        actions={
          <Button icon="plus" onClick={openCreate}>
            {t('addRule')}
          </Button>
        }
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-3">
        <Select id="filterCategory" aria-label={t('category')} value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)}>
          <option value="">{t('allCategories')}</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {localName(category, lang)}
            </option>
          ))}
        </Select>
        <Select id="filterZone" aria-label={t('zone')} value={zoneFilter} onChange={(e) => setZoneFilter(e.target.value)}>
          <option value="">{t('allZones')}</option>
          <option value="city">{t('wholeCity')}</option>
          {zoneList.map((zone) => (
            <option key={zone.id} value={zone.id}>
              {localName(zone, lang)}
            </option>
          ))}
        </Select>
        <Select id="filterActive" aria-label={t('status')} value={activeFilter} onChange={(e) => setActiveFilter(e.target.value as '' | 'true' | 'false')}>
          <option value="">{t('statusAll')}</option>
          <option value="true">{t('activeOnly')}</option>
          <option value="false">{t('inactiveOnly')}</option>
        </Select>
      </div>

      <Card flush className="mb-6">
        {rulesQuery.error ? (
          <ErrorState error={rulesQuery.error} onRetry={rulesQuery.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={rulesQuery.loading} emptyTitle={t('noRules')} emptyDescription="" />
        )}
      </Card>

      <Simulator categories={categories} />

      <Modal
        open={editing !== null}
        size="xl"
        title={editing?.mode === 'edit' ? t('editRule') : t('newRule')}
        onClose={() => setEditing(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditing(null)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="pricing-rule-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        <form id="pricing-rule-form" onSubmit={save} noValidate className="grid gap-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="name" label={t('ruleName')} value={form.name} error={errors.name} onChange={(e) => setField('name', e.target.value)} wrapperClassName="sm:col-span-2" />
            <Select id="rideCategoryId" label={t('category')} value={form.rideCategoryId} error={errors.rideCategoryId} onChange={(e) => setField('rideCategoryId', e.target.value)}>
              <option value="">—</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {localName(category, lang)}
                </option>
              ))}
            </Select>
            <Select id="zoneId" label={t('zone')} value={form.zoneId} onChange={(e) => setField('zoneId', e.target.value)}>
              <option value="">{t('wholeCity')}</option>
              {zoneList.map((zone) => (
                <option key={zone.id} value={zone.id}>
                  {localName(zone, lang)}
                </option>
              ))}
            </Select>
          </div>

          <section>
            <h3 className="mb-3 text-base font-bold">{t('fareParams')}</h3>
            <div className="grid gap-4 sm:grid-cols-3">
              {numberInput('baseFare')}
              {numberInput('perKm')}
              {numberInput('perMinute')}
              {numberInput('minFare')}
              {numberInput('waitingPerMinute')}
              {numberInput('freeWaitingMinutes', '1')}
            </div>
          </section>

          <section>
            <h3 className="mb-3 text-base font-bold">{t('fees')}</h3>
            <div className="grid gap-4 sm:grid-cols-3">
              {numberInput('bookingFee')}
              {numberInput('serviceFeePercent')}
              {numberInput('cancellationFee')}
              {numberInput('driverSharePercent')}
              {numberInput('priority', '1')}
            </div>
          </section>

          <section>
            <h3 className="mb-3 text-base font-bold">{t('validity')}</h3>
            <div className="grid gap-4 sm:grid-cols-2">
              <Input
                id="effectiveFrom"
                type="datetime-local"
                dir="ltr"
                label={t('effectiveFrom')}
                value={form.effectiveFrom}
                error={errors.effectiveFrom}
                onChange={(e) => setField('effectiveFrom', e.target.value)}
              />
              <Input
                id="effectiveTo"
                type="datetime-local"
                dir="ltr"
                label={t('effectiveTo')}
                hint={t('effectiveToHint')}
                value={form.effectiveTo}
                error={errors.effectiveTo}
                min={form.effectiveFrom || undefined}
                onChange={(e) => setField('effectiveTo', e.target.value)}
              />
              <div className="sm:col-span-2">
                <Toggle id="isActive" checked={form.isActive} onChange={(value) => setField('isActive', value)} label={t('isActive')} />
              </div>
            </div>
          </section>

          <section>
            <div className="mb-3 flex flex-wrap items-start justify-between gap-2">
              <div>
                <h3 className="text-base font-bold">{t('timeMultipliers')}</h3>
                <p className="text-xs text-muted">{t('timeMultipliersCopy')}</p>
              </div>
              <Button variant="secondary" size="sm" icon="plus" onClick={() => setField('timeMultipliers', [...form.timeMultipliers, { ...EMPTY_MULTIPLIER }])}>
                {t('addMultiplier')}
              </Button>
            </div>
            {form.timeMultipliers.length === 0 ? (
              <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('noMultipliers')}</p>
            ) : (
              <div className="overflow-hidden rounded-2xl border border-line">
                <div className="hidden grid-cols-[1.2fr_1fr_1fr_0.8fr_1fr_auto] gap-3 bg-cloud/60 px-4 py-2 text-xs font-bold text-muted sm:grid">
                  <span>{t('dayOfWeek')}</span>
                  <span>{t('fromTime')}</span>
                  <span>{t('toTime')}</span>
                  <span>{t('multiplier')}</span>
                  <span>{t('label')}</span>
                  <span />
                </div>
                {form.timeMultipliers.map((row, index) => (
                  <div key={row.id ?? index} className="grid grid-cols-2 items-end gap-3 border-t border-line px-4 py-3 sm:grid-cols-[1.2fr_1fr_1fr_0.8fr_1fr_auto] sm:items-center">
                    <Select aria-label={t('dayOfWeek')} className="h-10" value={row.dayOfWeek} onChange={(e) => setMultiplier(index, { dayOfWeek: e.target.value })}>
                      <option value="">{t('everyDay')}</option>
                      {WEEKDAYS.map(({ day, key }) => (
                        <option key={day} value={day}>
                          {t(key)}
                        </option>
                      ))}
                    </Select>
                    <Input aria-label={t('fromTime')} type="time" dir="ltr" className="h-10" value={row.fromTime} onChange={(e) => setMultiplier(index, { fromTime: e.target.value })} />
                    <Input aria-label={t('toTime')} type="time" dir="ltr" className="h-10" value={row.toTime} onChange={(e) => setMultiplier(index, { toTime: e.target.value })} />
                    <Input
                      aria-label={t('multiplier')}
                      type="number"
                      step="0.05"
                      min={0.5}
                      dir="ltr"
                      className="h-10"
                      value={row.multiplier}
                      onChange={(e) => setMultiplier(index, { multiplier: e.target.value })}
                    />
                    <Input aria-label={t('label')} dir="ltr" className="h-10" placeholder="night" value={row.label} onChange={(e) => setMultiplier(index, { label: e.target.value })} />
                    <Button
                      variant="danger-outline"
                      size="sm"
                      icon="trash"
                      aria-label={t('remove')}
                      onClick={() => setField('timeMultipliers', form.timeMultipliers.filter((_, i) => i !== index))}
                    />
                  </div>
                ))}
                {errors.timeMultipliers && <p className="px-4 py-2 text-xs font-bold text-danger">{errors.timeMultipliers}</p>}
              </div>
            )}
          </section>
        </form>
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('deleteRule')}
        description={deleting ? `${deleting.name} — ${t('deleteRuleCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}

// ---------------------------------------------------------------------------
// Simulator
// ---------------------------------------------------------------------------

type PointForm = { lat: string; lng: string }

function toPoint(form: PointForm): GeoPoint | null {
  const lat = numberOrNull(form.lat)
  const lng = numberOrNull(form.lng)
  return lat !== null && lng !== null && isValidLatLng(lat, lng) ? { lat, lng } : null
}

function Simulator({ categories }: { categories: RideCategory[] }) {
  const { t, lang } = useLang()
  const describe = useApiErrorMessage()
  const [pickup, setPickup] = useState<PointForm>({ lat: '', lng: '' })
  const [dropoff, setDropoff] = useState<PointForm>({ lat: '', lng: '' })
  const [categoryId, setCategoryId] = useState('')
  const [at, setAt] = useState('')
  const [running, setRunning] = useState(false)
  const [result, setResult] = useState<SimulateResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [map, setMap] = useState<L.Map | null>(null)
  const [expanded, setExpanded] = useState<string | null>(null)

  const pickupPoint = useMemo(() => toPoint(pickup), [pickup])
  const dropoffPoint = useMemo(() => toPoint(dropoff), [dropoff])

  // Two clicks: the first sets the pickup, the second the dropoff, a third starts over.
  const onMapClick = useEffectEvent((latlng: L.LatLng) => {
    const next = { lat: String(round6(latlng.lat)), lng: String(round6(latlng.lng)) }
    if (!pickupPoint) {
      setPickup(next)
    } else if (!dropoffPoint) {
      setDropoff(next)
    } else {
      setPickup(next)
      setDropoff({ lat: '', lng: '' })
      setResult(null)
    }
  })

  useEffect(() => {
    if (!map) return
    const handler = (event: L.LeafletMouseEvent) => onMapClick(event.latlng)
    map.on('click', handler)
    return () => {
      map.off('click', handler)
    }
  }, [map])

  useEffect(() => {
    if (!map) return
    const group = L.layerGroup().addTo(map)
    if (pickupPoint) L.marker([pickupPoint.lat, pickupPoint.lng], { icon: pinIcon('brand') }).bindTooltip(escapeHtml(t('pickup'))).addTo(group)
    if (dropoffPoint) L.marker([dropoffPoint.lat, dropoffPoint.lng], { icon: pinIcon('ink') }).bindTooltip(escapeHtml(t('dropoff'))).addTo(group)
    if (pickupPoint && dropoffPoint) {
      L.polyline(
        [
          [pickupPoint.lat, pickupPoint.lng],
          [dropoffPoint.lat, dropoffPoint.lng],
        ],
        routeLineStyle,
      ).addTo(group)
    }
    return () => {
      group.remove()
    }
  }, [map, pickupPoint, dropoffPoint, t])

  const run = async (event: FormEvent) => {
    event.preventDefault()
    if (!pickupPoint || !dropoffPoint) {
      setError(t('pointsRequired'))
      return
    }
    setError(null)
    setRunning(true)
    try {
      const response = await pricingRules.simulate({
        pickup: pickupPoint,
        dropoff: dropoffPoint,
        stops: [],
        rideCategoryId: categoryId || undefined,
        at: fromLocalInput(at) ?? undefined,
      })
      setResult(response)
      setExpanded(null)
    } catch (caught) {
      setError(describe(caught))
    } finally {
      setRunning(false)
    }
  }

  const clear = () => {
    setPickup({ lat: '', lng: '' })
    setDropoff({ lat: '', lng: '' })
    setResult(null)
    setError(null)
  }

  const columns: Column<SimulatedCategory>[] = [
    {
      key: 'category',
      header: t('category'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block font-bold">{row.name}</span>
          <span className="ltr-nums block text-xs text-muted">{row.code}</span>
        </span>
      ),
    },
    { key: 'eta', header: t('eta'), className: 'text-center', render: (row) => <span className="ltr-nums">{row.etaMinutes !== null ? `${formatNumber(row.etaMinutes)} ${t('min')}` : '—'}</span> },
    {
      key: 'total',
      header: t('total'),
      className: 'text-end',
      render: (row) => (
        <span className="ltr-nums font-bold">
          {formatMoney(row.total)} <span className="text-xs font-normal text-muted">{t('sar')}</span>
        </span>
      ),
    },
    { key: 'driverNet', header: t('driverNet'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatMoney(row.driverNetEarnings)}</span> },
    {
      key: 'offer',
      header: t('offerRange'),
      className: 'text-end',
      render: (row) => (
        <span className="ltr-nums whitespace-nowrap text-xs">
          {formatMoney(row.offerMin)} – {formatMoney(row.offerMax)}
        </span>
      ),
    },
    {
      key: 'breakdown',
      header: t('breakdown'),
      className: 'text-end',
      render: (row) => (
        <Button
          variant="secondary"
          size="sm"
          icon={expanded === row.rideCategoryId ? 'x' : 'eye'}
          onClick={() => setExpanded((current) => (current === row.rideCategoryId ? null : row.rideCategoryId))}
        >
          {expanded === row.rideCategoryId ? t('hideDetails') : t('showDetails')}
        </Button>
      ),
    },
  ]

  const pointInputs = (label: string, value: PointForm, set: (next: PointForm) => void, id: string) => (
    <fieldset className="grid grid-cols-2 gap-2">
      <legend className="mb-1.5 text-sm font-bold">{label}</legend>
      <Input id={`${id}-lat`} aria-label={t('lat')} placeholder={t('lat')} type="number" step="any" dir="ltr" className="h-10" value={value.lat} onChange={(e) => set({ ...value, lat: e.target.value })} />
      <Input id={`${id}-lng`} aria-label={t('lng')} placeholder={t('lng')} type="number" step="any" dir="ltr" className="h-10" value={value.lng} onChange={(e) => set({ ...value, lng: e.target.value })} />
    </fieldset>
  )

  return (
    <Card title={t('simulateTitle')} description={t('simulateCopy')}>
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <div>
          <p className="mb-2 text-xs text-muted">{t('simulateMapHint')}</p>
          <MapView className="h-72 w-full" center={RIYADH} zoom={11} onReady={setMap} onDispose={() => setMap(null)} />
        </div>
        <form onSubmit={run} noValidate className="grid content-start gap-4">
          {pointInputs(t('pickup'), pickup, setPickup, 'pickup')}
          {pointInputs(t('dropoff'), dropoff, setDropoff, 'dropoff')}
          <Select id="simCategory" label={t('category')} value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">{t('allCategories')}</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {localName(category, lang)}
              </option>
            ))}
          </Select>
          <Input id="simAt" type="datetime-local" dir="ltr" label={t('simulateAt')} value={at} onChange={(e) => setAt(e.target.value)} />
          {error && <p className="text-xs font-bold text-danger">{error}</p>}
          <div className="flex flex-wrap gap-2">
            <Button type="submit" icon="tag" loading={running}>
              {t('runSimulation')}
            </Button>
            <Button variant="secondary" onClick={clear} disabled={running}>
              {t('clearPoints')}
            </Button>
          </div>
        </form>
      </div>

      <div className="mt-6">
        {result ? (
          <>
            <div className="mb-4 flex flex-wrap items-center gap-3 rounded-2xl bg-cloud px-4 py-3 text-sm">
              <span>
                {t('distance')}: <span className="ltr-nums font-bold">{formatKm(result.distanceMeters)} {t('km')}</span>
              </span>
              <span>
                {t('duration')}: <span className="ltr-nums font-bold">{formatMinutes(result.durationSeconds)} {t('min')}</span>
              </span>
              <span>
                {t('pickupZone')}: <span className="font-bold">{result.pickupZone?.name ?? '—'}</span>
              </span>
              {result.demand && (
                <span className="flex items-center gap-2">
                  {t('demandLevel')}:
                  <span className="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold" style={{ background: tint(result.demand.color, 18), color: result.demand.color }}>
                    <span className="size-2 rounded-full" style={{ background: result.demand.color }} />
                    {result.demand.name} <span className="ltr-nums">×{formatNumber(result.demand.multiplier)}</span>
                  </span>
                </span>
              )}
            </div>
            <div className="-mx-5 sm:-mx-6">
              <Table
                columns={columns}
                rows={result.categories}
                rowKey={(row) => row.rideCategoryId}
                emptyTitle={t('noSimulationCategories')}
                emptyDescription=""
                renderExpanded={(row) => (expanded === row.rideCategoryId ? <Breakdown row={row} /> : null)}
              />
            </div>
          </>
        ) : (
          <EmptyState icon="tag" title={t('noSimulation')} description={t('simulateMapHint')} />
        )}
      </div>
    </Card>
  )
}

function Breakdown({ row }: { row: SimulatedCategory }) {
  const { t } = useLang()
  const b = row.breakdown
  const money = (value: number) => `${formatMoney(value)} ${t('sar')}`
  return (
    <DefinitionList
      items={[
        { label: t('baseFare'), value: money(b.baseFare), ltr: true },
        { label: t('distanceFare'), value: money(b.distanceFare), ltr: true },
        { label: t('timeFare'), value: money(b.timeFare), ltr: true },
        { label: t('minFareApplied'), value: b.minFareApplied ? t('yes') : t('no') },
        { label: t('timeMultiplier'), value: `×${formatNumber(b.timeMultiplier)}`, ltr: true },
        { label: t('demandMultiplier'), value: `×${formatNumber(b.demandMultiplier)}`, ltr: true },
        { label: t('bookingFee'), value: money(b.bookingFee), ltr: true },
        { label: t('serviceFee'), value: money(b.serviceFee), ltr: true },
        { label: t('discount'), value: money(b.discount), ltr: true },
        { label: t('total'), value: money(row.total), ltr: true },
      ]}
    />
  )
}
