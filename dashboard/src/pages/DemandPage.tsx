import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { Input, Select, Textarea, Toggle } from '../components/Field'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Spinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { ZonesMap } from '../components/ZonesMap'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useCurrentDemand } from '../hooks/useCurrentDemand'
import { useQuery } from '../hooks/useQuery'
import { demand, rideCategories, zones } from '../lib/admin'
import { formatDateTime, formatNumber, formatTime } from '../lib/format'
import { DEMAND_SOURCE_KEY, fromLocalInput, localName, numberOrNull, tint, toLocalInput } from '../lib/pricing'
import type { CurrentDemand, DemandLevel, DemandOverride, DemandOverrideInput, DemandRule, DemandRuleInput, RideCategory, Zone } from '../lib/types'

export function DemandPage() {
  const { t } = useLang()
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const levelsQuery = useQuery(() => demand.levels(), 'demand-levels')

  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data])
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const levels = useMemo(() => [...(levelsQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [levelsQuery.data])

  return (
    <>
      <PageHeader title={t('demandTitle')} description={t('demandCopy')} />
      <CurrentDemandSection zoneList={zoneList} categories={categories} />
      <div className="mb-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
        <LevelsSection levels={levels} loading={levelsQuery.loading} error={levelsQuery.error} onReload={levelsQuery.reload} />
        <RulesSection zoneList={zoneList} categories={categories} />
      </div>
      <OverridesSection zoneList={zoneList} categories={categories} levels={levels} />
    </>
  )
}

// ---------------------------------------------------------------------------
// Current demand grid + map
// ---------------------------------------------------------------------------

function CurrentDemandSection({ zoneList, categories }: { zoneList: Zone[]; categories: RideCategory[] }) {
  const { t, lang } = useLang()
  const { items, error, updatedAt, live, refresh } = useCurrentDemand()
  const [selectedZoneId, setSelectedZoneId] = useState<string | null>(null)
  const categoryById = useMemo(() => new Map(categories.map((category) => [category.id, category])), [categories])

  // One colour per zone on the map: the zone-wide entry wins, otherwise the highest multiplier of its categories.
  const zoneLevel = useMemo(() => {
    const byZone = new Map<string, CurrentDemand>()
    for (const item of items ?? []) {
      const current = byZone.get(item.zoneId)
      if (!current || item.rideCategoryId === null || (current.rideCategoryId !== null && item.level.multiplier > current.level.multiplier)) {
        byZone.set(item.zoneId, item)
      }
    }
    return byZone
  }, [items])

  const colorFor = useCallback((zone: Zone) => zoneLevel.get(zone.id)?.level.color ?? null, [zoneLevel])
  const labelFor = useCallback(
    (zone: Zone) => {
      const entry = zoneLevel.get(zone.id)
      return entry ? `${localName(zone, lang)} — ${entry.level.name} ×${entry.level.multiplier}` : localName(zone, lang)
    },
    [zoneLevel, lang],
  )

  const sorted = useMemo(
    () => [...(items ?? [])].sort((a, b) => b.level.multiplier - a.level.multiplier || a.zoneName.localeCompare(b.zoneName)),
    [items],
  )

  return (
    <div className="mb-6 grid gap-6 xl:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)]">
      <Card
        title={t('currentDemand')}
        description={
          <span className="flex flex-wrap items-center gap-2">
            <span className={`size-2 rounded-full ${live ? 'bg-brand' : 'bg-ink'}`} />
            {live ? t('liveViaHub') : t('autoRefresh30')}
            {updatedAt !== null && (
              <span className="ltr-nums">
                · {t('lastUpdated')} {formatTime(updatedAt, lang)}
              </span>
            )}
          </span>
        }
        action={<Button variant="secondary" size="sm" icon="refresh" onClick={refresh} aria-label={t('refresh')} title={t('refresh')} />}
      >
        {error && !items ? (
          <ErrorState error={error} onRetry={refresh} />
        ) : !items ? (
          <div className="grid min-h-40 place-items-center text-brand">
            <Spinner className="size-7" />
          </div>
        ) : sorted.length === 0 ? (
          <EmptyState icon="activity" title={t('noCurrentDemand')} />
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {sorted.map((item) => {
              const selected = item.zoneId === selectedZoneId
              const category = item.rideCategoryId ? categoryById.get(item.rideCategoryId) : null
              return (
                <li key={`${item.zoneId}:${item.rideCategoryId ?? 'all'}`}>
                  <button
                    type="button"
                    onClick={() => setSelectedZoneId(item.zoneId)}
                    className={`w-full rounded-2xl border-2 p-4 text-start transition ${selected ? 'border-ink' : 'border-transparent hover:border-line'}`}
                    style={{ background: tint(item.level.color, 14) }}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <span className="min-w-0">
                        <span className="block truncate font-bold">{item.zoneName}</span>
                        <span className="block text-xs text-muted">{category ? localName(category, lang) : t('allCategories')}</span>
                      </span>
                      <span className="inline-flex shrink-0 items-center gap-1.5 rounded-full bg-white/80 px-3 py-1 text-xs font-bold" style={{ color: item.level.color }}>
                        <span className="size-2 rounded-full" style={{ background: item.level.color }} />
                        {item.level.name} <span className="ltr-nums">×{formatNumber(item.level.multiplier)}</span>
                      </span>
                    </div>
                    <dl className="mt-3 grid grid-cols-3 gap-2 text-xs">
                      <Metric label={t('ratio')} value={item.ratio !== null ? formatNumber(Math.round(item.ratio * 100) / 100) : '—'} />
                      <Metric label={t('requests')} value={formatNumber(item.requestsCount)} />
                      <Metric label={t('drivers')} value={formatNumber(item.onlineDrivers)} />
                    </dl>
                    <p className="mt-2 flex flex-wrap items-center justify-between gap-2 text-[11px] text-muted">
                      <Badge tone={item.source === 'override' ? 'warning' : item.source === 'snapshot' ? 'brand' : 'muted'}>{t(DEMAND_SOURCE_KEY[item.source] ?? 'demandSourceDefault')}</Badge>
                      <span>
                        {t('computedAt')}: {formatDateTime(item.computedAt, lang)}
                      </span>
                    </p>
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </Card>

      <Card title={t('demandMap')} flush>
        <div className="px-5 pb-5 sm:px-6 sm:pb-6">
          <ZonesMap zones={zoneList} selectedId={selectedZoneId} onSelect={(zone) => setSelectedZoneId(zone.id)} colorFor={colorFor} labelFor={labelFor} className="h-80 w-full xl:h-[28rem]" />
        </div>
      </Card>
    </div>
  )
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-white/70 px-2.5 py-1.5">
      <dt className="font-bold text-muted">{label}</dt>
      <dd className="ltr-nums text-sm font-bold">{value}</dd>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Demand levels — inline multiplier editor
// ---------------------------------------------------------------------------

function LevelsSection({ levels, loading, error, onReload }: { levels: DemandLevel[]; loading: boolean; error: unknown; onReload: () => void }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [drafts, setDrafts] = useState<Record<string, string>>({})
  const [savingId, setSavingId] = useState<string | null>(null)

  const valueOf = (level: DemandLevel) => drafts[level.id] ?? String(level.multiplier)
  const dirty = (level: DemandLevel) => drafts[level.id] !== undefined && numberOrNull(drafts[level.id]) !== level.multiplier

  const save = async (level: DemandLevel) => {
    const multiplier = numberOrNull(valueOf(level))
    if (multiplier === null || multiplier <= 0) {
      toast.error(t('errorTitle'), t('invalidNumber'))
      return
    }
    setSavingId(level.id)
    try {
      await demand.updateLevel(level.id, multiplier)
      toast.success(t('levelSaved'))
      setDrafts((current) => {
        const next = { ...current }
        delete next[level.id]
        return next
      })
      onReload()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSavingId(null)
    }
  }

  return (
    <Card title={t('demandLevels')} description={t('demandLevelsCopy')}>
      {error ? (
        <ErrorState error={error} onRetry={onReload} />
      ) : loading && levels.length === 0 ? (
        <div className="grid min-h-32 place-items-center text-brand">
          <Spinner className="size-6" />
        </div>
      ) : (
        <ul className="divide-y divide-line">
          {levels.map((level) => (
            <li key={level.id} className="flex flex-wrap items-center gap-3 py-3">
              <span className="size-3 shrink-0 rounded-full" style={{ background: level.color }} />
              <span className="min-w-0 flex-1">
                <span className="block font-bold">{localName(level, lang)}</span>
                <span className="ltr-nums block text-xs text-muted">{level.code}</span>
              </span>
              <span className="flex items-center gap-2">
                <span className="text-sm text-muted">×</span>
                <Input
                  aria-label={`${t('multiplier')} — ${localName(level, lang)}`}
                  type="number"
                  step="0.05"
                  min={0.5}
                  dir="ltr"
                  className="h-10"
                  wrapperClassName="w-24"
                  value={valueOf(level)}
                  onChange={(e) => setDrafts((current) => ({ ...current, [level.id]: e.target.value }))}
                />
                <Button size="sm" icon="check" disabled={!dirty(level)} loading={savingId === level.id} onClick={() => save(level)}>
                  {t('save')}
                </Button>
              </span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

// ---------------------------------------------------------------------------
// Demand rules
// ---------------------------------------------------------------------------

type RuleForm = {
  zoneId: string
  rideCategoryId: string
  windowMinutes: string
  thresholdModerate: string
  thresholdHigh: string
  thresholdVeryHigh: string
  isActive: boolean
}

type RuleErrors = Partial<Record<keyof RuleForm, string>>

const EMPTY_RULE: RuleForm = { zoneId: '', rideCategoryId: '', windowMinutes: '10', thresholdModerate: '1', thresholdHigh: '2', thresholdVeryHigh: '3', isActive: true }

function ruleToForm(rule: DemandRule): RuleForm {
  return {
    zoneId: rule.zoneId ?? '',
    rideCategoryId: rule.rideCategoryId ?? '',
    windowMinutes: String(rule.windowMinutes),
    thresholdModerate: String(rule.thresholdModerate),
    thresholdHigh: String(rule.thresholdHigh),
    thresholdVeryHigh: String(rule.thresholdVeryHigh),
    isActive: rule.isActive,
  }
}

function ruleToInput(form: RuleForm): DemandRuleInput {
  return {
    zoneId: form.zoneId || null,
    rideCategoryId: form.rideCategoryId || null,
    metric: 'requests_per_driver',
    windowMinutes: Number(form.windowMinutes),
    thresholdModerate: Number(form.thresholdModerate),
    thresholdHigh: Number(form.thresholdHigh),
    thresholdVeryHigh: Number(form.thresholdVeryHigh),
    isActive: form.isActive,
  }
}

type RuleEditing = { mode: 'create' } | { mode: 'edit'; rule: DemandRule } | null

function RulesSection({ zoneList, categories }: { zoneList: Zone[]; categories: RideCategory[] }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => demand.rules(), 'demand-rules')
  const zoneById = useMemo(() => new Map(zoneList.map((zone) => [zone.id, zone])), [zoneList])
  const categoryById = useMemo(() => new Map(categories.map((category) => [category.id, category])), [categories])

  const [editing, setEditing] = useState<RuleEditing>(null)
  const [form, setForm] = useState<RuleForm>(EMPTY_RULE)
  const [errors, setErrors] = useState<RuleErrors>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<DemandRule | null>(null)

  const setField = <K extends keyof RuleForm>(key: K, value: RuleForm[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const validate = () => {
    const next: RuleErrors = {}
    for (const key of ['windowMinutes', 'thresholdModerate', 'thresholdHigh', 'thresholdVeryHigh'] as const) {
      const value = numberOrNull(form[key])
      if (value === null || value < 0) next[key] = t('invalidNumber')
    }
    if (Object.keys(next).length === 0) {
      const [moderate, high, veryHigh] = [form.thresholdModerate, form.thresholdHigh, form.thresholdVeryHigh].map(Number)
      if (!(moderate < high && high < veryHigh)) next.thresholdVeryHigh = t('thresholdsOrder')
    }
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!editing || !validate()) return
    setSaving(true)
    try {
      const input = ruleToInput(form)
      if (editing.mode === 'create') await demand.createRule(input)
      else await demand.updateRule(editing.rule.id, input)
      toast.success(t('demandRuleSaved'))
      setEditing(null)
      query.reload()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await demand.removeRule(deleting.id)
      toast.success(t('demandRuleDeleted'))
      setDeleting(null)
      query.reload()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    }
  }

  const scopeLabel = (zoneId: string | null, rideCategoryId: string | null) => (
    <span className="block min-w-0">
      <span className="block font-bold">{zoneId ? localName(zoneById.get(zoneId), lang) : t('allZones')}</span>
      <span className="block text-xs text-muted">{rideCategoryId ? localName(categoryById.get(rideCategoryId), lang) : t('allCategories')}</span>
    </span>
  )

  const columns: Column<DemandRule>[] = [
    { key: 'scope', header: t('scope'), render: (row) => scopeLabel(row.zoneId, row.rideCategoryId) },
    { key: 'window', header: t('windowMinutes'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.windowMinutes)}</span> },
    {
      key: 'thresholds',
      header: `${t('thresholdModerate')} / ${t('thresholdHigh')} / ${t('thresholdVeryHigh')}`,
      className: 'text-center',
      render: (row) => (
        <span className="ltr-nums whitespace-nowrap">
          {formatNumber(row.thresholdModerate)} / {formatNumber(row.thresholdHigh)} / {formatNumber(row.thresholdVeryHigh)}
        </span>
      ),
    },
    { key: 'isActive', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button
            variant="secondary"
            size="sm"
            icon="edit"
            aria-label={t('edit')}
            onClick={() => {
              setForm(ruleToForm(row))
              setErrors({})
              setEditing({ mode: 'edit', rule: row })
            }}
          />
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  return (
    <>
      <Card
        title={t('demandRules')}
        description={t('demandRulesCopy')}
        flush
        action={
          <Button
            size="sm"
            icon="plus"
            onClick={() => {
              setForm(EMPTY_RULE)
              setErrors({})
              setEditing({ mode: 'create' })
            }}
          >
            {t('addDemandRule')}
          </Button>
        }
      >
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={query.data ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('noDemandRules')} emptyDescription="" />
        )}
      </Card>

      <Modal
        open={editing !== null}
        size="lg"
        title={editing?.mode === 'edit' ? t('editDemandRule') : t('newDemandRule')}
        onClose={() => setEditing(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditing(null)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="demand-rule-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        <form id="demand-rule-form" onSubmit={save} noValidate className="grid gap-4 sm:grid-cols-2">
          <Select id="ruleZone" label={t('zone')} value={form.zoneId} onChange={(e) => setField('zoneId', e.target.value)}>
            <option value="">{t('allZones')}</option>
            {zoneList.map((zone) => (
              <option key={zone.id} value={zone.id}>
                {localName(zone, lang)}
              </option>
            ))}
          </Select>
          <Select id="ruleCategory" label={t('category')} value={form.rideCategoryId} onChange={(e) => setField('rideCategoryId', e.target.value)}>
            <option value="">{t('allCategories')}</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {localName(category, lang)}
              </option>
            ))}
          </Select>
          <Input id="metric" label={t('metric')} value={t('metricRequestsPerDriver')} readOnly />
          <Input id="windowMinutes" type="number" min={1} dir="ltr" label={t('windowMinutes')} value={form.windowMinutes} error={errors.windowMinutes} onChange={(e) => setField('windowMinutes', e.target.value)} />
          <div className="grid grid-cols-3 gap-3 sm:col-span-2">
            <Input id="thresholdModerate" type="number" step="0.1" dir="ltr" label={t('thresholdModerate')} value={form.thresholdModerate} error={errors.thresholdModerate} onChange={(e) => setField('thresholdModerate', e.target.value)} />
            <Input id="thresholdHigh" type="number" step="0.1" dir="ltr" label={t('thresholdHigh')} value={form.thresholdHigh} error={errors.thresholdHigh} onChange={(e) => setField('thresholdHigh', e.target.value)} />
            <Input id="thresholdVeryHigh" type="number" step="0.1" dir="ltr" label={t('thresholdVeryHigh')} value={form.thresholdVeryHigh} error={errors.thresholdVeryHigh} onChange={(e) => setField('thresholdVeryHigh', e.target.value)} />
          </div>
          <div className="sm:col-span-2">
            <Toggle id="ruleActive" checked={form.isActive} onChange={(value) => setField('isActive', value)} label={t('isActive')} />
          </div>
        </form>
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('deleteDemandRule')}
        description={t('deleteDemandRuleCopy')}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}

// ---------------------------------------------------------------------------
// Manual overrides
// ---------------------------------------------------------------------------

type OverrideForm = { zoneId: string; rideCategoryId: string; demandLevelId: string; reason: string; startsAt: string; endsAt: string }
type OverrideErrors = Partial<Record<keyof OverrideForm, string>>

function defaultOverride(zoneList: Zone[], levels: DemandLevel[]): OverrideForm {
  const now = new Date()
  const end = new Date(now.getTime() + 2 * 60 * 60 * 1000)
  return {
    zoneId: zoneList[0]?.id ?? '',
    rideCategoryId: '',
    demandLevelId: levels[levels.length - 1]?.id ?? '',
    reason: '',
    startsAt: toLocalInput(now.toISOString()),
    endsAt: toLocalInput(end.toISOString()),
  }
}

function overrideStatus(item: DemandOverride, now: number): { key: 'activeNow' | 'upcoming' | 'expired'; tone: 'brand' | 'warning' | 'muted' } {
  const starts = new Date(item.startsAt).getTime()
  const ends = new Date(item.endsAt).getTime()
  if (Number.isFinite(ends) && ends < now) return { key: 'expired', tone: 'muted' }
  if (Number.isFinite(starts) && starts > now) return { key: 'upcoming', tone: 'warning' }
  return { key: 'activeNow', tone: 'brand' }
}

function OverridesSection({ zoneList, categories, levels }: { zoneList: Zone[]; categories: RideCategory[]; levels: DemandLevel[] }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => demand.overrides(), 'demand-overrides')
  const zoneById = useMemo(() => new Map(zoneList.map((zone) => [zone.id, zone])), [zoneList])
  const categoryById = useMemo(() => new Map(categories.map((category) => [category.id, category])), [categories])
  const levelById = useMemo(() => new Map(levels.map((level) => [level.id, level])), [levels])

  const [open, setOpen] = useState(false)
  const [form, setForm] = useState<OverrideForm>(() => defaultOverride([], []))
  const [errors, setErrors] = useState<OverrideErrors>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<DemandOverride | null>(null)

  const setField = <K extends keyof OverrideForm>(key: K, value: OverrideForm[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const validate = () => {
    const next: OverrideErrors = {}
    if (!form.zoneId) next.zoneId = t('fieldRequired')
    if (!form.demandLevelId) next.demandLevelId = t('fieldRequired')
    if (!form.reason.trim()) next.reason = t('reasonRequired')
    const starts = fromLocalInput(form.startsAt)
    const ends = fromLocalInput(form.endsAt)
    if (!starts) next.startsAt = t('fieldRequired')
    if (!ends) next.endsAt = t('fieldRequired')
    if (starts && ends && ends <= starts) next.endsAt = t('endAfterStart')
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!validate()) return
    setSaving(true)
    try {
      const input: DemandOverrideInput = {
        zoneId: form.zoneId,
        rideCategoryId: form.rideCategoryId || null,
        demandLevelId: form.demandLevelId,
        reason: form.reason.trim(),
        startsAt: fromLocalInput(form.startsAt) ?? new Date().toISOString(),
        endsAt: fromLocalInput(form.endsAt) ?? new Date().toISOString(),
      }
      await demand.createOverride(input)
      toast.success(t('overrideSaved'))
      setOpen(false)
      query.reload()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await demand.removeOverride(deleting.id)
      toast.success(t('overrideDeleted'))
      setDeleting(null)
      query.reload()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    }
  }

  // Re-evaluated every minute so an override flips from "upcoming" to "active" without a reload.
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 60_000)
    return () => window.clearInterval(timer)
  }, [])
  const rows = useMemo(() => [...(query.data ?? [])].sort((a, b) => new Date(b.startsAt).getTime() - new Date(a.startsAt).getTime()), [query.data])

  const columns: Column<DemandOverride>[] = [
    {
      key: 'zone',
      header: t('zone'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block font-bold">{row.zoneName || localName(zoneById.get(row.zoneId), lang)}</span>
          <span className="block text-xs text-muted">{row.rideCategoryId ? localName(categoryById.get(row.rideCategoryId), lang) : t('allCategories')}</span>
        </span>
      ),
    },
    {
      key: 'level',
      header: t('level'),
      render: (row) => {
        const level = levelById.get(row.demandLevelId)
        return level ? (
          <span className="inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-3 py-1 text-xs font-bold" style={{ background: tint(level.color, 18), color: level.color }}>
            <span className="size-2 rounded-full" style={{ background: level.color }} />
            {localName(level, lang)} <span className="ltr-nums">×{formatNumber(level.multiplier)}</span>
          </span>
        ) : (
          <span className="ltr-nums text-xs text-muted">{row.demandLevelId}</span>
        )
      },
    },
    { key: 'reason', header: t('reason'), render: (row) => <span className="block max-w-64 truncate">{row.reason}</span> },
    {
      key: 'period',
      header: t('period'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDateTime(row.startsAt, lang)}</span>
          <span className="block text-muted">{formatDateTime(row.endsAt, lang)}</span>
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => {
        const status = overrideStatus(row, now)
        return <Badge tone={status.tone}>{t(status.key)}</Badge>
      },
    },
    { key: 'createdBy', header: t('createdBy'), render: (row) => <span className="text-xs text-muted">{row.createdByName || row.createdBy || '—'}</span> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('deleteOverride')} />,
    },
  ]

  return (
    <>
      <Card
        title={t('demandOverrides')}
        description={t('demandOverridesCopy')}
        flush
        action={
          <Button
            size="sm"
            icon="plus"
            onClick={() => {
              setForm(defaultOverride(zoneList, levels))
              setErrors({})
              setOpen(true)
            }}
          >
            {t('addOverride')}
          </Button>
        }
      >
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('noOverrides')} emptyDescription="" />
        )}
      </Card>

      <Modal
        open={open}
        size="lg"
        title={t('newOverride')}
        onClose={() => setOpen(false)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setOpen(false)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="demand-override-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        <form id="demand-override-form" onSubmit={save} noValidate className="grid gap-4 sm:grid-cols-2">
          <Select id="overrideZone" label={t('zone')} value={form.zoneId} error={errors.zoneId} onChange={(e) => setField('zoneId', e.target.value)}>
            <option value="">—</option>
            {zoneList.map((zone) => (
              <option key={zone.id} value={zone.id}>
                {localName(zone, lang)}
              </option>
            ))}
          </Select>
          <Select id="overrideCategory" label={t('category')} value={form.rideCategoryId} onChange={(e) => setField('rideCategoryId', e.target.value)}>
            <option value="">{t('allCategories')}</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {localName(category, lang)}
              </option>
            ))}
          </Select>
          <Select id="overrideLevel" label={t('level')} value={form.demandLevelId} error={errors.demandLevelId} onChange={(e) => setField('demandLevelId', e.target.value)} wrapperClassName="sm:col-span-2">
            <option value="">—</option>
            {levels.map((level) => (
              <option key={level.id} value={level.id}>
                {localName(level, lang)} (×{level.multiplier})
              </option>
            ))}
          </Select>
          <Input id="overrideStarts" type="datetime-local" dir="ltr" label={t('startsAt')} value={form.startsAt} error={errors.startsAt} onChange={(e) => setField('startsAt', e.target.value)} />
          <Input id="overrideEnds" type="datetime-local" dir="ltr" label={t('endsAt')} value={form.endsAt} error={errors.endsAt} min={form.startsAt || undefined} onChange={(e) => setField('endsAt', e.target.value)} />
          <Textarea id="overrideReason" label={t('reason')} placeholder={t('reasonPlaceholder')} className="min-h-24" value={form.reason} error={errors.reason} onChange={(e) => setField('reason', e.target.value)} wrapperClassName="sm:col-span-2" />
        </form>
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('deleteOverride')}
        description={deleting ? `${deleting.zoneName || localName(zoneById.get(deleting.zoneId), lang)} — ${t('deleteOverrideCopy')}` : undefined}
        confirmLabel={t('deleteOverride')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
