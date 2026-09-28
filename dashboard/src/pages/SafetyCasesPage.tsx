import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput, Select } from '../components/Field'
import { Icon, type IconName } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { SafetyCaseCreateModal } from '../components/SafetyCaseCreateModal'
import { SosBanner } from '../components/SosBanner'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useNow } from '../hooks/useNow'
import { useQuery } from '../hooks/useQuery'
import { useSafetyFeed } from '../hooks/useSafetyFeed'
import { parseIsoDate, useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { safety } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { formatDuration, isOpenCase, REPORTER_ROLE_KEY, SAFETY_CASE_STATUSES, SAFETY_CASE_TYPES, SAFETY_PRIORITIES, SAFETY_SOURCE_KEY, SAFETY_TYPE_KEY, sortCases } from '../lib/safety'
import { safetyCaseStatusMeta, safetyPriorityMeta } from '../lib/status'
import type { SafetyCaseListItem, SafetyCaseStatus } from '../lib/types'

const PAGE_SIZE = 20
const ASSIGNED_FILTERS = ['me', 'unassigned'] as const

/** Safety centre (`/safety`): summary, live SOS banner and the case queue (docs/09 "لوحة الإدارة"). */
export function SafetyCasesPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const now = useNow()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const [creating, setCreating] = useState(false)

  const status = parseEnum(params.get('status'), SAFETY_CASE_STATUSES)
  const priority = parseEnum(params.get('priority'), SAFETY_PRIORITIES)
  const type = parseEnum(params.get('type'), SAFETY_CASE_TYPES)
  const assignedTo = parseEnum(params.get('assignedTo'), ASSIGNED_FILTERS)
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))

  const summary = useQuery(() => safety.summary(), 'safety-summary')
  // `receivedAt` anchors the server-computed ages; they keep ticking locally from there.
  const query = useQuery(
    async () => ({
      page: await safety.cases({ status, priority, type, assignedTo, from, to, search: search.value, page, pageSize: PAGE_SIZE }),
      receivedAt: Date.now(),
    }),
    `safety-cases:${status}:${priority}:${type}:${assignedTo}:${from}:${to}:${search.value}:${page}`,
  )
  const fetchedAt = query.data?.receivedAt ?? now

  const feed = useSafetyFeed({
    watchCases: true,
    onEvent: () => {
      query.reload()
      summary.reload()
    },
  })

  const rows = sortCases(query.data?.page.items ?? [])
  const s = summary.data

  const cards: { key: string; label: string; value: number | string | null; icon: IconName; tone: 'danger' | 'warning' | 'brand' | 'ink'; to?: string }[] = [
    { key: 'critical', label: t('sfOpenCritical'), value: s?.open.critical ?? null, icon: 'siren', tone: 'danger', to: '/safety?status=open&priority=critical' },
    { key: 'high', label: t('sfOpenHigh'), value: s?.open.high ?? null, icon: 'alert', tone: 'warning', to: '/safety?status=open&priority=high' },
    { key: 'other', label: t('sfOpenOther'), value: s ? s.open.medium + s.open.low : null, icon: 'shield', tone: 'ink', to: '/safety?status=open' },
    { key: 'unassigned', label: t('sfUnassigned'), value: s?.unassigned ?? null, icon: 'user', tone: 'warning', to: '/safety?assignedTo=unassigned' },
    { key: 'alerts', label: t('sfPendingAlerts'), value: s?.pendingAlerts ?? null, icon: 'bell', tone: 'warning', to: '/safety/alerts?status=pending_rider' },
    { key: 'response', label: t('sfAvgFirstResponse'), value: s ? formatDuration(s.avgFirstResponseSeconds) : null, icon: 'clock', tone: 'brand' },
    { key: 'duty', label: t('sfOnDutyAgents'), value: s?.onDutyAgents ?? null, icon: 'users', tone: 'brand' },
  ]

  const ageOf = (row: SafetyCaseListItem) => row.ageSeconds + Math.max(0, (now - fetchedAt) / 1000)

  const columns: Column<SafetyCaseListItem>[] = [
    {
      key: 'priority',
      header: t('priority'),
      render: (row) => (
        <span className="flex items-center gap-2">
          {row.priority === 'critical' && isOpenCase(row.status) && (
            <span className="relative flex size-2.5" aria-hidden="true">
              <span className="absolute inline-flex size-full rounded-full bg-danger opacity-75 motion-safe:animate-ping" />
              <span className="relative inline-flex size-2.5 rounded-full bg-danger" />
            </span>
          )}
          <MetaBadge record={safetyPriorityMeta} value={row.priority} />
        </span>
      ),
    },
    {
      key: 'case',
      header: t('sfCaseNumber'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold">{row.caseNumber}</span>
          <span className="block text-xs text-muted">
            {t(SAFETY_TYPE_KEY[row.type] ?? 'sfTypeSafetyReport')} · {t(SAFETY_SOURCE_KEY[row.source] ?? 'sfSourceAdmin')}
          </span>
        </span>
      ),
    },
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) =>
        row.tripNumber ? (
          row.tripId ? (
            <Link to={`/trips/${row.tripId}`} onClick={(event) => event.stopPropagation()} className="ltr-nums font-bold text-brand hover:underline">
              {row.tripNumber}
            </Link>
          ) : (
            <span className="ltr-nums">{row.tripNumber}</span>
          )
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'reporter',
      header: t('sfReporter'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.reporterName || t('unnamed')}</span>
          <span className="block text-xs text-muted">{t(REPORTER_ROLE_KEY[row.reporterRole] ?? 'actorSystem')}</span>
        </span>
      ),
    },
    {
      key: 'assigned',
      header: t('sfAssignedTo'),
      render: (row) => (row.assignedToName ? <span className="font-bold">{row.assignedToName}</span> : <Badge tone="warning">{t('sfUnassigned')}</Badge>),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={safetyCaseStatusMeta} value={row.status} /> },
    {
      key: 'age',
      header: t('sfAge'),
      className: 'text-end',
      render: (row) =>
        isOpenCase(row.status) ? (
          <span className={`ltr-nums font-bold ${row.priority === 'critical' ? 'text-danger' : ''}`}>{formatDuration(ageOf(row))}</span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'opened',
      header: t('openedAt'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDateTime(row.openedAt, lang)}</span>
          <span className="block text-muted">
            {t('sfFirstResponse')}: {row.firstResponseAt ? formatDateTime(row.firstResponseAt, lang) : t('notYet')}
          </span>
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('sfCenterTitle')}
        description={t('sfCenterCopy')}
        actions={
          <>
            <Badge tone={feed.live ? 'brand' : 'muted'}>{feed.live ? t('liveViaHub') : t('liveViaPolling')}</Badge>
            <Button variant="danger" icon="plus" onClick={() => setCreating(true)}>
              {t('sfNewCase')}
            </Button>
          </>
        }
      />

      <SosBanner items={feed.incoming} onDismiss={feed.dismiss} onDismissAll={feed.dismissAll} />

      {!summary.error && (
        <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-7">
          {cards.map((card) => {
            const tone = { danger: 'bg-danger-soft text-danger', warning: 'bg-amber-50 text-amber-700', brand: 'bg-brand-soft text-brand', ink: 'bg-cloud text-ink' }[card.tone]
            const body = (
              <>
                <span className={`mb-3 grid size-9 place-items-center rounded-xl ${tone}`}>
                  <Icon name={card.icon} className="size-4" />
                </span>
                <span className="block text-xs font-bold text-muted">{card.label}</span>
                <span className="ltr-nums mt-1 block text-2xl font-bold">{card.value === null ? '…' : typeof card.value === 'number' ? formatNumber(card.value) : card.value}</span>
              </>
            )
            return card.to ? (
              <Link key={card.key} to={card.to} className="block rounded-3xl bg-white p-4 shadow-soft transition hover:-translate-y-0.5 hover:shadow-brand">
                {body}
              </Link>
            ) : (
              <div key={card.key} className="rounded-3xl bg-white p-4 shadow-soft">
                {body}
              </div>
            )
          })}
        </div>
      )}

      <Tabs
        className="mb-4"
        value={status}
        onChange={(value) => setFilter('status', value)}
        options={[
          { value: '' as SafetyCaseStatus | '', label: t('statusAll') },
          ...SAFETY_CASE_STATUSES.map((value) => ({ value, label: t(safetyCaseStatusMeta[value].key) })),
        ]}
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-[1fr_auto_auto_auto_auto_auto]">
        <SearchInput placeholder={t('sfSearchCases')} value={search.input} onChange={(event) => search.setInput(event.target.value)} wrapperClassName="sm:col-span-2 lg:col-span-3 xl:col-span-1" />
        <Select id="priority" aria-label={t('priority')} value={priority} onChange={(event) => setFilter('priority', event.target.value)} wrapperClassName="xl:w-36">
          <option value="">{t('sfAllPriorities')}</option>
          {SAFETY_PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {t(safetyPriorityMeta[value].key)}
            </option>
          ))}
        </Select>
        <Select id="type" aria-label={t('type')} value={type} onChange={(event) => setFilter('type', event.target.value)} wrapperClassName="xl:w-44">
          <option value="">{t('sfAllTypes')}</option>
          {SAFETY_CASE_TYPES.map((value) => (
            <option key={value} value={value}>
              {t(SAFETY_TYPE_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="assignedTo" aria-label={t('sfAssignedTo')} value={assignedTo} onChange={(event) => setFilter('assignedTo', event.target.value)} wrapperClassName="xl:w-40">
          <option value="">{t('sfAnyAssignee')}</option>
          <option value="me">{t('sfAssignedToMe')}</option>
          <option value="unassigned">{t('sfUnassigned')}</option>
        </Select>
        <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} wrapperClassName="xl:w-40" />
        <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} wrapperClassName="xl:w-40" />
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={rows}
              rowKey={(row) => row.id}
              loading={query.loading}
              onRowClick={(row) => navigate(`/safety/cases/${row.id}`)}
              emptyTitle={t('sfNoCases')}
              emptyDescription=""
            />
            {query.data && <Pagination page={query.data.page.page} pageSize={query.data.page.pageSize} total={query.data.page.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <SafetyCaseCreateModal open={creating} onClose={() => setCreating(false)} onCreated={(created) => navigate(`/safety/cases/${created.id}`)} />
    </>
  )
}
