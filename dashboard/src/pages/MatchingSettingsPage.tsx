import { useMemo, useState, type FormEvent } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input, Select, Toggle } from '../components/Field'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { StatCard } from '../components/StatCard'
import { Table, type Column } from '../components/Table'
import { usePermission } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { matching, rideCategories, zones } from '../lib/admin'
import { formatKm, formatNumber } from '../lib/format'
import { daysAgoIso, DEFAULT_WEIGHTS, localName, numberOrNull, todayIso, WEIGHT_KEYS, weightsTotal } from '../lib/pricing'
import type { MatchingSettings, MatchingSettingsInput, MatchingWeights } from '../lib/types'

const NUMBER_FIELDS = ['radiusMeters', 'maxRadiusMeters', 'radiusStepMeters', 'offerTimeoutSeconds', 'searchTimeoutSeconds', 'maxCandidates'] as const
type NumberField = (typeof NUMBER_FIELDS)[number]

type FormState = Record<NumberField, string> & {
  zoneId: string
  rideCategoryId: string
  weights: Record<keyof MatchingWeights, string>
  allowCategoryUpgrade: boolean
  preferFavoriteDriver: boolean
  isActive: boolean
}

type FormErrors = Partial<Record<NumberField | 'weights' | 'scope', string>>

const weightsToForm = (weights: MatchingWeights) =>
  Object.fromEntries(WEIGHT_KEYS.map(({ key }) => [key, String(weights[key] ?? 0)])) as Record<keyof MatchingWeights, string>

const EMPTY_FORM: FormState = {
  zoneId: '',
  rideCategoryId: '',
  radiusMeters: '5000',
  maxRadiusMeters: '12000',
  radiusStepMeters: '2500',
  offerTimeoutSeconds: '20',
  searchTimeoutSeconds: '120',
  maxCandidates: '8',
  weights: weightsToForm(DEFAULT_WEIGHTS),
  allowCategoryUpgrade: false,
  preferFavoriteDriver: true,
  isActive: true,
}

function toForm(settings: MatchingSettings): FormState {
  return {
    zoneId: settings.zoneId ?? '',
    rideCategoryId: settings.rideCategoryId ?? '',
    radiusMeters: String(settings.radiusMeters),
    maxRadiusMeters: String(settings.maxRadiusMeters),
    radiusStepMeters: String(settings.radiusStepMeters),
    offerTimeoutSeconds: String(settings.offerTimeoutSeconds),
    searchTimeoutSeconds: String(settings.searchTimeoutSeconds),
    maxCandidates: String(settings.maxCandidates),
    weights: weightsToForm({ ...DEFAULT_WEIGHTS, ...settings.weights }),
    allowCategoryUpgrade: settings.allowCategoryUpgrade,
    preferFavoriteDriver: settings.preferFavoriteDriver,
    isActive: settings.isActive,
  }
}

function toInput(form: FormState): MatchingSettingsInput {
  return {
    zoneId: form.zoneId || null,
    rideCategoryId: form.rideCategoryId || null,
    radiusMeters: Number(form.radiusMeters),
    maxRadiusMeters: Number(form.maxRadiusMeters),
    radiusStepMeters: Number(form.radiusStepMeters),
    offerTimeoutSeconds: Number(form.offerTimeoutSeconds),
    searchTimeoutSeconds: Number(form.searchTimeoutSeconds),
    maxCandidates: Number(form.maxCandidates),
    weights: {
      distance: Number(form.weights.distance),
      eta: Number(form.weights.eta),
      rating: Number(form.weights.rating),
      acceptance: Number(form.weights.acceptance),
      cancellation: Number(form.weights.cancellation),
      tier: Number(form.weights.tier),
      favorite: Number(form.weights.favorite),
    },
    allowCategoryUpgrade: form.allowCategoryUpgrade,
    preferFavoriteDriver: form.preferFavoriteDriver,
    isActive: form.isActive,
  }
}

function numericWeights(weights: Record<keyof MatchingWeights, string>): Record<keyof MatchingWeights, number> {
  return Object.fromEntries(WEIGHT_KEYS.map(({ key }) => [key, numberOrNull(weights[key]) ?? Number.NaN])) as Record<keyof MatchingWeights, number>
}

type Editing = { mode: 'create' } | { mode: 'edit'; settings: MatchingSettings } | null

export function MatchingSettingsPage() {
  // F20: writing matching settings needs `matching.edit`.
  const canEdit = usePermission('matching.edit')
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()

  const query = useQuery(() => matching.settings(), 'matching-settings')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data])
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const zoneById = useMemo(() => new Map(zoneList.map((zone) => [zone.id, zone])), [zoneList])
  const categoryById = useMemo(() => new Map(categories.map((category) => [category.id, category])), [categories])

  const rows = useMemo(
    () =>
      [...(query.data ?? [])].sort((a, b) => {
        // Global first, then zone-only, then zone+category.
        const rank = (item: MatchingSettings) => (item.zoneId ? 1 : 0) + (item.rideCategoryId ? 2 : 0)
        return rank(a) - rank(b)
      }),
    [query.data],
  )

  const [editing, setEditing] = useState<Editing>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [errors, setErrors] = useState<FormErrors>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<MatchingSettings | null>(null)

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const setWeight = (key: keyof MatchingWeights, value: string) => {
    setForm((current) => ({ ...current, weights: { ...current.weights, [key]: value } }))
    setErrors((current) => ({ ...current, weights: undefined }))
  }

  const total = weightsTotal(numericWeights(form.weights))
  const weightsValid = Math.abs(total - 1) < 0.005 && WEIGHT_KEYS.every(({ key }) => {
    const value = numberOrNull(form.weights[key])
    return value !== null && value >= 0
  })

  const validate = () => {
    const next: FormErrors = {}
    for (const key of NUMBER_FIELDS) {
      const value = numberOrNull(form[key])
      if (value === null || value < 0) next[key] = t('invalidNumber')
    }
    if (!next.radiusMeters && !next.maxRadiusMeters && Number(form.maxRadiusMeters) < Number(form.radiusMeters)) next.maxRadiusMeters = t('radiusOrder')
    if (!weightsValid) next.weights = t('weightsInvalid')
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!editing || !validate()) return
    setSaving(true)
    try {
      const input = toInput(form)
      if (editing.mode === 'create') await matching.createSettings(input)
      else await matching.updateSettings(editing.settings.id, input)
      toast.success(t('matchingSettingsSaved'))
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
      await matching.removeSettings(deleting.id)
      toast.success(t('matchingSettingsDeleted'))
      setDeleting(null)
      query.reload()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    }
  }

  const columns: Column<MatchingSettings>[] = [
    {
      key: 'scope',
      header: t('scope'),
      render: (row) =>
        row.zoneId || row.rideCategoryId ? (
          <span className="block min-w-0">
            <span className="block font-bold">{row.zoneId ? localName(zoneById.get(row.zoneId), lang) : t('allZones')}</span>
            <span className="block text-xs text-muted">{row.rideCategoryId ? localName(categoryById.get(row.rideCategoryId), lang) : t('allCategories')}</span>
          </span>
        ) : (
          <Badge tone="ink">{t('global')}</Badge>
        ),
    },
    {
      key: 'radius',
      header: t('searchRadius'),
      render: (row) => (
        <span className="ltr-nums whitespace-nowrap text-sm">
          {formatKm(row.radiusMeters)} → {formatKm(row.maxRadiusMeters)} {t('km')} <span className="text-xs text-muted">(+{formatKm(row.radiusStepMeters)})</span>
        </span>
      ),
    },
    {
      key: 'timeouts',
      header: t('timeouts'),
      render: (row) => (
        <span className="ltr-nums whitespace-nowrap text-sm">
          {formatNumber(row.offerTimeoutSeconds)}
          {t('seconds')} / {formatNumber(row.searchTimeoutSeconds)}
          {t('seconds')}
        </span>
      ),
    },
    { key: 'maxCandidates', header: t('maxCandidates'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.maxCandidates)}</span> },
    {
      key: 'flags',
      header: `${t('allowCategoryUpgrade')} / ${t('preferFavoriteDriver')}`,
      render: (row) => (
        <span className="inline-flex flex-wrap gap-1">
          {row.allowCategoryUpgrade && <Badge tone="muted">{t('allowCategoryUpgrade')}</Badge>}
          {row.preferFavoriteDriver && <Badge tone="muted">{t('preferFavoriteDriver')}</Badge>}
          {!row.allowCategoryUpgrade && !row.preferFavoriteDriver && <span className="text-xs text-muted">—</span>}
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
            onClick={() => {
              setForm(toForm(row))
              setErrors({})
              setEditing({ mode: 'edit', settings: row })
            }}
          >
            {t('edit')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  const numberInput = (key: NumberField) => (
    <Input key={key} id={key} type="number" min={0} step="1" dir="ltr" label={t(key)} value={form[key]} error={errors[key]} onChange={(e) => setField(key, e.target.value)} />
  )

  return (
    <>
      <PageHeader
        title={t('matchingSettingsTitle')}
        description={t('matchingSettingsCopy')}
        actions={
          canEdit && (
            <Button
              icon="plus"
              onClick={() => {
                setForm(EMPTY_FORM)
                setErrors({})
                setEditing({ mode: 'create' })
              }}
            >
              {t('addMatchingSettings')}
            </Button>
          )
        }
      />

      <StatsSection />

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table columns={canEdit ? columns : columns.filter((column) => column.key !== 'actions')} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('noMatchingSettings')} emptyDescription="" />
        )}
      </Card>

      <Modal
        open={editing !== null}
        size="xl"
        title={editing?.mode === 'edit' ? t('editMatchingSettings') : t('newMatchingSettings')}
        onClose={() => setEditing(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditing(null)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="matching-settings-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        <form id="matching-settings-form" onSubmit={save} noValidate className="grid gap-6">
          <section className="grid gap-4 sm:grid-cols-2">
            <Select id="zoneId" label={t('zone')} value={form.zoneId} onChange={(e) => setField('zoneId', e.target.value)}>
              <option value="">{t('allZones')}</option>
              {zoneList.map((zone) => (
                <option key={zone.id} value={zone.id}>
                  {localName(zone, lang)}
                </option>
              ))}
            </Select>
            <Select id="rideCategoryId" label={t('category')} value={form.rideCategoryId} onChange={(e) => setField('rideCategoryId', e.target.value)}>
              <option value="">{t('allCategories')}</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {localName(category, lang)}
                </option>
              ))}
            </Select>
          </section>

          <section>
            <h3 className="mb-3 text-base font-bold">{t('searchRadius')}</h3>
            <div className="grid gap-4 sm:grid-cols-3">{(['radiusMeters', 'maxRadiusMeters', 'radiusStepMeters'] as const).map(numberInput)}</div>
          </section>

          <section>
            <h3 className="mb-3 text-base font-bold">{t('timeouts')}</h3>
            <div className="grid gap-4 sm:grid-cols-3">{(['offerTimeoutSeconds', 'searchTimeoutSeconds', 'maxCandidates'] as const).map(numberInput)}</div>
          </section>

          <section>
            <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
              <div>
                <h3 className="text-base font-bold">{t('weights')}</h3>
                <p className="text-xs text-muted">{t('weightsHint')}</p>
              </div>
              <span className={`ltr-nums rounded-full px-3 py-1 text-sm font-bold ${weightsValid ? 'bg-brand-soft text-brand' : 'bg-danger-soft text-danger'}`}>
                {t('weightsSum')}: {Number.isFinite(total) ? total.toFixed(2) : '—'} / 1.00
              </span>
            </div>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              {WEIGHT_KEYS.map(({ key, label }) => (
                <Input
                  key={key}
                  id={`weight-${key}`}
                  type="number"
                  step="0.05"
                  min={0}
                  max={1}
                  dir="ltr"
                  label={t(label)}
                  value={form.weights[key]}
                  onChange={(e) => setWeight(key, e.target.value)}
                />
              ))}
            </div>
            <div className="mt-3 flex h-2 overflow-hidden rounded-full bg-cloud">
              {WEIGHT_KEYS.map(({ key }, index) => {
                const value = Math.max(0, numberOrNull(form.weights[key]) ?? 0)
                return <span key={key} className={index % 2 === 0 ? 'bg-brand' : 'bg-ink'} style={{ width: `${Math.min(100, value * 100)}%` }} />
              })}
            </div>
            {errors.weights && <p className="mt-2 text-xs font-bold text-danger">{errors.weights}</p>}
          </section>

          <section className="grid gap-3">
            <Toggle id="allowCategoryUpgrade" checked={form.allowCategoryUpgrade} onChange={(value) => setField('allowCategoryUpgrade', value)} label={t('allowCategoryUpgrade')} description={t('allowCategoryUpgradeCopy')} />
            <Toggle id="preferFavoriteDriver" checked={form.preferFavoriteDriver} onChange={(value) => setField('preferFavoriteDriver', value)} label={t('preferFavoriteDriver')} description={t('preferFavoriteDriverCopy')} />
            <Toggle id="isActive" checked={form.isActive} onChange={(value) => setField('isActive', value)} label={t('isActive')} />
          </section>
        </form>
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('deleteMatchingSettings')}
        description={t('deleteMatchingSettingsCopy')}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}

function StatsSection() {
  const { t } = useLang()
  const [from, setFrom] = useState(() => daysAgoIso(7))
  const [to, setTo] = useState(() => todayIso())
  const query = useQuery(() => matching.stats({ from, to }), `matching-stats:${from}:${to}`)
  const stats = query.data

  const percent = (value: number | null | undefined) => (typeof value === 'number' ? `${formatNumber(Math.round(value * 1000) / 10)}%` : '—')
  const seconds = (value: number | null | undefined) => (typeof value === 'number' ? `${formatNumber(Math.round(value))} ${t('seconds')}` : '—')
  const noDriversRate = stats && stats.tripsRequested > 0 ? stats.noDrivers / stats.tripsRequested : null

  return (
    <Card
      title={t('matchingStats')}
      className="mb-6"
      action={
        <div className="flex flex-wrap items-center gap-2">
          <Input id="statsFrom" type="date" aria-label={t('fromDate')} dir="ltr" className="h-10" value={from} max={to || undefined} onChange={(e) => setFrom(e.target.value)} />
          <Input id="statsTo" type="date" aria-label={t('toDate')} dir="ltr" className="h-10" value={to} min={from || undefined} onChange={(e) => setTo(e.target.value)} />
        </div>
      }
    >
      {query.error ? (
        <ErrorState error={query.error} onRetry={query.reload} />
      ) : (
        <div className={`grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 ${query.loading ? 'opacity-60' : ''}`}>
          <StatCard title={t('statTripsRequested')} value={formatNumber(stats?.tripsRequested)} icon="route" />
          <StatCard title={t('statAssigned')} value={formatNumber(stats?.assigned)} icon="check" meta={stats && stats.tripsRequested > 0 ? percent(stats.assigned / stats.tripsRequested) : undefined} />
          <StatCard title={t('statNoDrivers')} value={formatNumber(stats?.noDrivers)} icon="alert" tone="danger" meta={percent(noDriversRate)} />
          <StatCard title={t('statAvgAssign')} value={seconds(stats?.averageAssignSeconds)} icon="clock" />
          <StatCard title={t('statAcceptance')} value={percent(stats?.offerAcceptanceRate)} icon="target" />
          <StatCard title={t('statAvgRounds')} value={typeof stats?.averageRounds === 'number' ? formatNumber(Math.round(stats.averageRounds * 10) / 10) : '—'} icon="refresh" />
        </div>
      )}
    </Card>
  )
}
