import { useNavigate, Link } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { Input, SearchInput, Select } from '../components/Field'
import { Icon, type IconName } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { PermissionError } from '../components/PermissionError'
import { SlaCountdown } from '../components/SlaCountdown'
import { SupportKpis } from '../components/SupportKpis'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useNow } from '../hooks/useNow'
import { useQuery } from '../hooks/useQuery'
import { useSpanUnits } from '../hooks/useSpanUnits'
import { useSupportFeed } from '../hooks/useSupportFeed'
import { parseIsoDate, useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { support } from '../lib/admin'
import { lookupKey, parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import {
  awaitsAgent,
  formatMinuteSpan,
  isActiveTicket,
  nextDeadline,
  QUEUE_TABS,
  REQUESTER_ROLE_KEY,
  sortTickets,
  TICKET_CHANNEL_KEY,
  TICKET_CHANNELS,
  TICKET_PRIORITIES,
  TICKET_SLA_FILTERS,
  TICKET_STATUSES,
  TICKET_TYPE_KEY,
  TICKET_TYPES,
  type QueueTab,
} from '../lib/support'
import { slaStateMeta, ticketPriorityMeta, ticketStatusMeta } from '../lib/status'
import type { TicketListItem } from '../lib/types'

const PAGE_SIZE = 20

/** Support queue (`/support`): live counters, the new-user-message badge, filters and the ticket table with SLA colouring (docs/11 "لوحة الإدارة"). */
export function SupportTicketsPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const now = useNow(30_000)
  const units = useSpanUnits()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()

  const rawTab = params.get('tab')
  const tab: QueueTab = (QUEUE_TABS as readonly string[]).includes(rawTab ?? '') ? (rawTab as QueueTab) : 'all'
  const statusFilter = parseEnum(params.get('status'), TICKET_STATUSES)
  const priority = parseEnum(params.get('priority'), TICKET_PRIORITIES)
  const type = parseEnum(params.get('type'), TICKET_TYPES)
  const channel = parseEnum(params.get('channel'), TICKET_CHANNELS)
  const sla = parseEnum(params.get('sla'), TICKET_SLA_FILTERS)
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))

  // The tabs are shortcuts over the assignee / status filters (§ docs/11: لي، غير معيّنة، الكل، بانتظار المستخدم).
  const assignedTo = tab === 'mine' ? 'me' : tab === 'unassigned' ? 'unassigned' : ''
  const status = tab === 'pending_user' ? 'pending_user' : statusFilter

  const summary = useQuery(() => support.summary(), 'support-summary')
  const query = useQuery(
    () => support.tickets({ status, priority, type, channel, assignedTo, sla, from, to, search: search.value, page, pageSize: PAGE_SIZE }),
    `support-tickets:${status}:${priority}:${type}:${channel}:${assignedTo}:${sla}:${from}:${to}:${search.value}:${page}`,
  )

  const feed = useSupportFeed({
    watchQueue: true,
    onEvent: () => {
      query.reload()
      summary.reload()
    },
  })

  // `channel` is not part of the documented list query: narrow client-side when the rows carry it.
  const items = (query.data?.items ?? []).filter((row) => !channel || !row.channel || row.channel === channel)
  const rows = sortTickets(items)
  const s = summary.data

  const cards: { key: string; label: string; value: string | null; icon: IconName; tone: 'danger' | 'warning' | 'brand' | 'ink'; to?: string }[] = [
    { key: 'open', label: t('spOpenTickets'), value: s ? formatNumber(s.open) : null, icon: 'chat', tone: 'ink', to: '/support' },
    { key: 'unassigned', label: t('spUnassigned'), value: s ? formatNumber(s.unassigned) : null, icon: 'user', tone: 'warning', to: '/support?tab=unassigned' },
    { key: 'pending', label: t('spStatusPendingUser'), value: s ? formatNumber(s.pendingUser) : null, icon: 'pause', tone: 'ink', to: '/support?tab=pending_user' },
    { key: 'breachFr', label: t('spBreachingFirstResponse'), value: s ? formatNumber(s.breachingFirstResponse) : null, icon: 'alert', tone: 'danger', to: '/support?sla=breached' },
    { key: 'breachRes', label: t('spBreachingResolution'), value: s ? formatNumber(s.breachingResolution) : null, icon: 'clock', tone: 'danger', to: '/support?sla=breached' },
    { key: 'avgFr', label: t('spAvgFirstResponse'), value: s ? formatMinuteSpan(s.avgFirstResponseMinutes, units) : null, icon: 'activity', tone: 'brand' },
    { key: 'avgRes', label: t('spAvgResolution'), value: s ? formatMinuteSpan(s.avgResolutionHours === null ? null : s.avgResolutionHours * 60, units) : null, icon: 'check', tone: 'brand' },
    { key: 'csat', label: t('spCsatAvg'), value: s ? (s.csatAvg === null ? '—' : s.csatAvg.toFixed(2)) : null, icon: 'star', tone: 'brand' },
  ]

  const columns: Column<TicketListItem>[] = [
    {
      key: 'priority',
      header: t('priority'),
      render: (row) => (
        <span className="flex items-center gap-2">
          {awaitsAgent(row) && (
            <span className="relative flex size-2.5" role="img" aria-label={t('spAwaitsAgent')} title={t('spAwaitsAgent')}>
              <span className="absolute inline-flex size-full rounded-full bg-brand opacity-75 motion-safe:animate-ping" />
              <span className="relative inline-flex size-2.5 rounded-full bg-brand" />
            </span>
          )}
          <MetaBadge record={ticketPriorityMeta} value={row.priority} />
        </span>
      ),
    },
    {
      key: 'ticket',
      header: t('spTicket'),
      render: (row) => (
        <span className="block max-w-72">
          <span className="ltr-nums block font-bold">{row.ticketNumber}</span>
          <span className={`block truncate text-sm ${awaitsAgent(row) ? 'font-bold' : ''}`}>{row.subject}</span>
          <span className="block text-xs text-muted">
            {t(TICKET_TYPE_KEY[row.type] ?? 'spTypeOther')}
            {row.channel ? ` · ${t(lookupKey(TICKET_CHANNEL_KEY, row.channel) ?? 'spChannelApp')}` : ''}
          </span>
        </span>
      ),
    },
    {
      key: 'requester',
      header: t('spRequester'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.requesterName || t('unnamed')}</span>
          <span className="block text-xs text-muted">{t(lookupKey(REQUESTER_ROLE_KEY, row.requesterRole) ?? 'actorPassenger')}</span>
        </span>
      ),
    },
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (row.tripNumber ? <span className="ltr-nums">{row.tripNumber}</span> : <span className="text-muted">—</span>),
    },
    {
      key: 'assigned',
      header: t('spAssignedTo'),
      render: (row) => (row.assignedToName ? <span className="font-bold">{row.assignedToName}</span> : <Badge tone="warning">{t('spUnassigned')}</Badge>),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={ticketStatusMeta} value={row.status} /> },
    {
      key: 'sla',
      header: t('spSla'),
      render: (row) => {
        if (!isActiveTicket(row.status)) return <span className="text-muted">—</span>
        const deadline = nextDeadline(row)
        return (
          <span className="flex flex-col items-start gap-1" data-sla={row.slaState}>
            <MetaBadge record={slaStateMeta} value={row.slaState} />
            <SlaCountdown
              dueAt={deadline.dueAt}
              now={now}
              // `pending_user` freezes the resolution clock (§F18.2); the list has no pause timestamp, so it shows the frozen label at "now".
              pausedAt={row.status === 'pending_user' && deadline.kind === 'resolution' ? new Date(now).toISOString() : null}
              label={deadline.kind === 'firstResponse' ? t('spSlaFirstResponseShort') : t('spSlaResolutionShort')}
            />
          </span>
        )
      },
    },
    {
      key: 'last',
      header: t('spLastMessage'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDateTime(row.lastMessageAt, lang)}</span>
          <span className="block text-muted">{t(row.lastMessageBy === 'user' ? 'spUser' : row.lastMessageBy === 'agent' ? 'spAgent' : 'actorSystem')}</span>
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('spQueueTitle')}
        description={t('spQueueCopy')}
        actions={<Badge tone={feed.live ? 'brand' : 'muted'}>{feed.live ? t('liveViaHub') : t('liveViaPolling')}</Badge>}
      />

      {feed.incoming.length > 0 && (
        <div
          role="status"
          data-testid="new-user-messages"
          className="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-3xl bg-brand px-5 py-3 text-white shadow-brand"
        >
          <span className="flex items-center gap-2 text-sm font-bold">
            <Icon name="bell" className="size-4" />
            <span className="ltr-nums">{formatNumber(feed.incoming.length)}</span> {t('spNewUserMessages')}
          </span>
          <Button
            variant="secondary"
            size="sm"
            icon="refresh"
            onClick={() => {
              feed.clear()
              query.reload()
              summary.reload()
            }}
          >
            {t('spShowNew')}
          </Button>
        </div>
      )}

      {!summary.error && (
        <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-4 xl:grid-cols-8">
          {cards.map((card) => {
            const tone = { danger: 'bg-danger-soft text-danger', warning: 'bg-amber-50 text-amber-700', brand: 'bg-brand-soft text-brand', ink: 'bg-cloud text-ink' }[card.tone]
            const body = (
              <>
                <span className={`mb-3 grid size-9 place-items-center rounded-xl ${tone}`}>
                  <Icon name={card.icon} className="size-4" />
                </span>
                <span className="block text-xs font-bold text-muted">{card.label}</span>
                <span className="ltr-nums mt-1 block text-2xl font-bold">{card.value === null ? '…' : card.value}</span>
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

      <SupportKpis />

      <Tabs
        className="mb-4"
        value={tab}
        onChange={(value) => setFilter('tab', value === 'all' ? '' : value)}
        options={[
          { value: 'all' as QueueTab, label: t('statusAll') },
          { value: 'mine' as QueueTab, label: t('spTabMine') },
          { value: 'unassigned' as QueueTab, label: t('spUnassigned'), count: s?.unassigned ?? null },
          { value: 'pending_user' as QueueTab, label: t('spStatusPendingUser'), count: s?.pendingUser ?? null },
        ]}
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-4">
        <SearchInput placeholder={t('spSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} wrapperClassName="sm:col-span-2 lg:col-span-4" />
        <Select id="status" aria-label={t('status')} value={tab === 'pending_user' ? 'pending_user' : statusFilter} disabled={tab === 'pending_user'} onChange={(event) => setFilter('status', event.target.value)}>
          <option value="">{t('spAllStatuses')}</option>
          {TICKET_STATUSES.map((value) => (
            <option key={value} value={value}>
              {t(ticketStatusMeta[value].key)}
            </option>
          ))}
        </Select>
        <Select id="priority" aria-label={t('priority')} value={priority} onChange={(event) => setFilter('priority', event.target.value)}>
          <option value="">{t('spAllPriorities')}</option>
          {TICKET_PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {t(ticketPriorityMeta[value].key)}
            </option>
          ))}
        </Select>
        <Select id="type" aria-label={t('type')} value={type} onChange={(event) => setFilter('type', event.target.value)}>
          <option value="">{t('spAllTypes')}</option>
          {TICKET_TYPES.map((value) => (
            <option key={value} value={value}>
              {t(TICKET_TYPE_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="channel" aria-label={t('spChannel')} value={channel} onChange={(event) => setFilter('channel', event.target.value)}>
          <option value="">{t('spAllChannels')}</option>
          {TICKET_CHANNELS.map((value) => (
            <option key={value} value={value}>
              {t(TICKET_CHANNEL_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="sla" aria-label={t('spSla')} value={sla} onChange={(event) => setFilter('sla', event.target.value)}>
          <option value="">{t('spAnySla')}</option>
          <option value="due_soon">{t('spSlaDueSoon')}</option>
          <option value="breached">{t('spSlaBreached')}</option>
        </Select>
        <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
      </div>

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="support.view" onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={rows}
              rowKey={(row) => row.id}
              loading={query.loading}
              onRowClick={(row) => navigate(`/support/tickets/${row.id}`)}
              emptyTitle={t('spNoTickets')}
              emptyDescription=""
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}
