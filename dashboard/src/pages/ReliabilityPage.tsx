import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput, Select, Toggle } from '../components/Field'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { reliabilityProfiles, reliabilityThresholds } from '../lib/admin'
import { formatRate, optionalNumber, RELIABILITY_ROLES, RESTRICTION_LEVELS, ROLE_KEY } from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { restrictionLevelMeta } from '../lib/status'
import type { ReliabilityProfileListItem, ReliabilityThreshold, ReliabilityThresholdInput } from '../lib/types'

const PAGE_SIZE = 20
const TABS = ['profiles', 'thresholds'] as const

/** Reliability (`/reliability`): profiles list and, on the "thresholds" tab, the restriction ladder editor. */
export function ReliabilityPage() {
  const { t } = useLang()
  const { params, update } = useUrlState()
  const tab = parseEnum(params.get('tab'), TABS) || 'profiles'

  return (
    <>
      <PageHeader title={t('rlTitle')} description={t('rlCopy')} />
      <Tabs
        className="mb-4"
        value={tab}
        onChange={(value) =>
          update((next) => {
            for (const key of [...next.keys()]) next.delete(key)
            if (value !== 'profiles') next.set('tab', value)
          })
        }
        options={[
          { value: 'profiles', label: t('rlProfilesTab') },
          { value: 'thresholds', label: t('rlThresholdsTab') },
        ]}
      />
      {tab === 'profiles' ? <ProfilesTab /> : <ThresholdsTab />}
    </>
  )
}

function ProfilesTab() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const role = parseEnum(params.get('role'), RELIABILITY_ROLES) || 'driver'
  const level = parseEnum(params.get('level'), RESTRICTION_LEVELS)
  const query = useQuery(() => reliabilityProfiles.list({ role, level, search: search.value, page, pageSize: PAGE_SIZE }), `reliability:${role}:${level}:${search.value}:${page}`)

  const columns: Column<ReliabilityProfileListItem>[] = [
    {
      key: 'name',
      header: t('fullName'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.name || t('unnamed')}</span>
          {row.phone && <span className="ltr-nums block text-xs text-muted">{row.phone}</span>}
        </span>
      ),
    },
    {
      key: 'level',
      header: t('rlLevel'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={restrictionLevelMeta} value={row.level} />
          {row.restrictedUntil && (
            <span className="mt-1 block text-xs text-muted">
              {t('rlUntil')} {formatDateTime(row.restrictedUntil, lang)}
            </span>
          )}
        </span>
      ),
    },
    { key: 'cancel', header: t('rlCancellationRate'), className: 'text-end', render: (row) => <span className="ltr-nums font-bold">{formatRate(row.cancellationRate)}</span> },
    { key: 'reliability', header: t('rlReliabilityRate'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatRate(row.reliabilityRate)}</span> },
    { key: 'points', header: t('rlPenaltyPoints'), className: 'text-center', render: (row) => <span className="ltr-nums font-bold">{formatNumber(row.penaltyPoints)}</span> },
    { key: 'noShow', header: t('rlNoShows'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.noShowCount)}</span> },
    { key: 'accepted', header: t('rlTripsAccepted'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.tripsAccepted)}</span> },
    { key: 'computed', header: t('computedAt'), render: (row) => <span className="whitespace-nowrap text-xs text-muted">{formatDateTime(row.lastComputedAt, lang)}</span> },
  ]

  return (
    <>
      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-[auto_1fr_auto]">
        <div className="flex gap-1 rounded-2xl bg-cloud p-1">
          {RELIABILITY_ROLES.map((value) => (
            <button
              key={value}
              type="button"
              aria-pressed={role === value}
              onClick={() => setFilter('role', value === 'driver' ? '' : value)}
              className={`flex-1 rounded-xl px-4 py-2 text-sm font-bold transition ${role === value ? 'bg-ink text-white' : 'text-muted hover:bg-white'}`}
            >
              {t(ROLE_KEY[value])}
            </button>
          ))}
        </div>
        <SearchInput placeholder={t('rlSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
        <Select id="rl-level" aria-label={t('rlLevel')} value={level} onChange={(event) => setFilter('level', event.target.value)} wrapperClassName="sm:w-56">
          <option value="">{t('rlAllLevels')}</option>
          {RESTRICTION_LEVELS.map((value) => (
            <option key={value} value={value}>
              {t(restrictionLevelMeta[value].key)}
            </option>
          ))}
        </Select>
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={query.data?.items ?? []}
              rowKey={(row) => `${row.userId}:${row.role}`}
              loading={query.loading}
              onRowClick={(row) => navigate(`/reliability/${row.userId}?role=${row.role}`)}
              emptyTitle={t('rlNoProfiles')}
              emptyDescription=""
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}

function ThresholdsTab() {
  const { t } = useLang()
  const { params, setFilter } = useUrlState()
  const role = parseEnum(params.get('role'), RELIABILITY_ROLES) || 'driver'
  const query = useQuery(() => reliabilityThresholds.list(role), `reliability-thresholds:${role}`)
  const rows = (query.data ?? []).filter((row) => row.role === role).sort((a, b) => a.sortOrder - b.sortOrder)

  return (
    <>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div className="flex gap-1 rounded-2xl bg-white p-1 shadow-soft">
          {RELIABILITY_ROLES.map((value) => (
            <button
              key={value}
              type="button"
              aria-pressed={role === value}
              onClick={() => setFilter('role', value === 'driver' ? '' : value)}
              className={`rounded-xl px-4 py-2 text-sm font-bold transition ${role === value ? 'bg-ink text-white' : 'text-muted hover:bg-cloud'}`}
            >
              {t(ROLE_KEY[value])}
            </button>
          ))}
        </div>
        <p className="flex items-center gap-2 text-sm text-muted">
          <Icon name="info" className="size-4 shrink-0" />
          {t('rlLadderHint')}
        </p>
      </div>

      {query.loading && !query.data ? (
        <PageSpinner />
      ) : query.error ? (
        <Card>
          <ErrorState error={query.error} onRetry={query.reload} />
        </Card>
      ) : rows.length === 0 ? (
        <Card>
          <p className="text-sm text-muted">{t('rlNoThresholds')}</p>
        </Card>
      ) : (
        <ol className="space-y-4">
          {rows.map((row, index) => (
            <ThresholdRow key={`${row.id}:${row.sortOrder}:${row.minPenaltyPoints ?? ''}:${row.minCancellationRate ?? ''}`} threshold={row} step={index + 1} previous={rows[index - 1] ?? null} onSaved={query.reload} />
          ))}
        </ol>
      )}
    </>
  )
}

type ThresholdForm = Record<'minPenaltyPoints' | 'minCancellationRate' | 'minTripsForRate' | 'restrictionHours' | 'deprioritizeFactor' | 'incentiveReductionPercent' | 'sortOrder', string> & { isActive: boolean }

const str = (value: number | null) => (value === null ? '' : String(value))

function ThresholdRow({ threshold, step, previous, onSaved }: { threshold: ReliabilityThreshold; step: number; previous: ReliabilityThreshold | null; onSaved: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const initial: ThresholdForm = {
    minPenaltyPoints: str(threshold.minPenaltyPoints),
    minCancellationRate: threshold.minCancellationRate === null ? '' : String(Math.round(threshold.minCancellationRate * 10000) / 100),
    minTripsForRate: String(threshold.minTripsForRate),
    restrictionHours: str(threshold.restrictionHours),
    deprioritizeFactor: str(threshold.deprioritizeFactor),
    incentiveReductionPercent: str(threshold.incentiveReductionPercent),
    sortOrder: String(threshold.sortOrder),
    isActive: threshold.isActive,
  }
  const [form, setForm] = useState<ThresholdForm>(initial)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const dirty = (Object.keys(initial) as (keyof ThresholdForm)[]).some((key) => initial[key] !== form[key])
  const driver = threshold.role === 'driver'
  const showRestriction = threshold.level === 'temporarily_restricted'
  const showFactor = driver && (threshold.level === 'matching_deprioritized' || threshold.level === 'incentives_reduced')
  const showReduction = driver && threshold.level === 'incentives_reduced'

  const set = <K extends keyof ThresholdForm>(key: K, value: ThresholdForm[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setError(null)
  }

  const points = optionalNumber(form.minPenaltyPoints)
  const ratePercent = optionalNumber(form.minCancellationRate)
  const weakerThanPrevious =
    previous !== null &&
    ((points !== null && previous.minPenaltyPoints !== null && points <= previous.minPenaltyPoints) ||
      (ratePercent !== null && previous.minCancellationRate !== null && ratePercent / 100 <= previous.minCancellationRate))

  const save = async () => {
    const input: ReliabilityThresholdInput = {
      minPenaltyPoints: points,
      minCancellationRate: ratePercent === null ? null : Math.round(ratePercent * 100) / 10000,
      minTripsForRate: Number(form.minTripsForRate || 0),
      restrictionHours: showRestriction ? optionalNumber(form.restrictionHours) : threshold.restrictionHours,
      deprioritizeFactor: showFactor ? optionalNumber(form.deprioritizeFactor) : threshold.deprioritizeFactor,
      incentiveReductionPercent: showReduction ? optionalNumber(form.incentiveReductionPercent) : threshold.incentiveReductionPercent,
      sortOrder: Number(form.sortOrder || 0),
      isActive: form.isActive,
    }
    const numbers = [input.minPenaltyPoints, input.minCancellationRate, input.minTripsForRate, input.restrictionHours, input.deprioritizeFactor, input.incentiveReductionPercent, input.sortOrder]
    if (numbers.some((value) => value !== null && (!Number.isFinite(value) || value < 0))) {
      setError(t('invalidNumber'))
      return
    }
    if (input.minPenaltyPoints === null && input.minCancellationRate === null) {
      setError(t('rlErrNeedTrigger'))
      return
    }
    if ((input.minCancellationRate ?? 0) > 1 || (input.incentiveReductionPercent ?? 0) > 100 || (input.deprioritizeFactor ?? 0) > 1) {
      setError(t('cxErrPercentRange'))
      return
    }
    if (showRestriction && !input.restrictionHours) {
      setError(t('rlErrRestrictionHours'))
      return
    }
    setSaving(true)
    try {
      await reliabilityThresholds.update(threshold.id, input)
      toast.success(t('rlThresholdSaved'))
      onSaved()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <li className={`relative rounded-3xl bg-white p-5 shadow-soft sm:p-6 ${form.isActive ? '' : 'opacity-70'}`}>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span className="ltr-nums grid size-9 shrink-0 place-items-center rounded-xl bg-ink text-sm font-bold text-white">{step}</span>
          <MetaBadge record={restrictionLevelMeta} value={threshold.level} />
          {!form.isActive && <Badge tone="muted">{t('inactive')}</Badge>}
        </div>
        <div className="flex items-center gap-2">
          {dirty && (
            <Button variant="ghost" size="sm" onClick={() => setForm(initial)} disabled={saving}>
              {t('reset')}
            </Button>
          )}
          <Button size="sm" icon="check" disabled={!dirty} loading={saving} onClick={save}>
            {t('save')}
          </Button>
        </div>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Input id={`th-points-${threshold.id}`} type="number" min={0} dir="ltr" label={t('rlMinPoints')} value={form.minPenaltyPoints} onChange={(event) => set('minPenaltyPoints', event.target.value)} />
        <Input id={`th-rate-${threshold.id}`} type="number" min={0} max={100} step="0.5" dir="ltr" label={t('rlMinRate')} value={form.minCancellationRate} onChange={(event) => set('minCancellationRate', event.target.value)} />
        <Input id={`th-trips-${threshold.id}`} type="number" min={0} dir="ltr" label={t('rlMinTrips')} hint={t('rlMinTripsHint')} value={form.minTripsForRate} onChange={(event) => set('minTripsForRate', event.target.value)} />
        <Input id={`th-sort-${threshold.id}`} type="number" min={0} dir="ltr" label={t('rlSeverity')} hint={t('rlSeverityHint')} value={form.sortOrder} onChange={(event) => set('sortOrder', event.target.value)} />
        {showRestriction && (
          <Input id={`th-hours-${threshold.id}`} type="number" min={1} dir="ltr" label={t('rlRestrictionHours')} value={form.restrictionHours} onChange={(event) => set('restrictionHours', event.target.value)} />
        )}
        {showFactor && (
          <Input
            id={`th-factor-${threshold.id}`}
            type="number"
            min={0}
            max={1}
            step="0.05"
            dir="ltr"
            label={t('rlDeprioritizeFactor')}
            hint={t('rlDeprioritizeHint')}
            value={form.deprioritizeFactor}
            onChange={(event) => set('deprioritizeFactor', event.target.value)}
          />
        )}
        {showReduction && (
          <Input
            id={`th-reduction-${threshold.id}`}
            type="number"
            min={0}
            max={100}
            dir="ltr"
            label={t('rlIncentiveReduction')}
            value={form.incentiveReductionPercent}
            onChange={(event) => set('incentiveReductionPercent', event.target.value)}
          />
        )}
        <div className="self-end sm:col-span-2 lg:col-span-1">
          <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
        </div>
      </div>
      {weakerThanPrevious && (
        <p className="mt-3 flex items-center gap-2 text-xs font-bold text-amber-700">
          <Icon name="alert" className="size-3.5" />
          {t('rlWeakerThanPrevious')}
        </p>
      )}
      {error && <p className="mt-3 text-xs font-bold text-danger">{error}</p>}
    </li>
  )
}
