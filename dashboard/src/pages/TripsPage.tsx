import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { TripStatusBadge } from '../components/Badge'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput } from '../components/Field'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useQuery } from '../hooks/useQuery'
import { trips } from '../lib/admin'
import { formatDateTime, formatMoney } from '../lib/format'
import { tripStatusMeta } from '../lib/status'
import { parseTripStatus, TRIP_STATUS_GROUPS } from '../lib/trips'
import type { TripListItem, TripStatus } from '../lib/types'

const PAGE_SIZE = 20

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/

function parseDate(value: string | null) {
  return value && ISO_DATE.test(value) ? value : ''
}

export function TripsPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()

  const status = parseTripStatus(params.get('status'))
  const search = params.get('q') ?? ''
  const from = parseDate(params.get('from'))
  const to = parseDate(params.get('to'))
  const page = Math.max(1, Number(params.get('page')) || 1)

  const [searchInput, setSearchInput] = useState(search)
  const debouncedSearch = useDebouncedValue(searchInput.trim())

  useEffect(() => {
    if (debouncedSearch === search) return
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        if (debouncedSearch) next.set('q', debouncedSearch)
        else next.delete('q')
        next.delete('page')
        return next
      },
      { replace: true },
    )
  }, [debouncedSearch, search, setParams])

  const query = useQuery(
    () => trips.list({ status, search, from, to, page, pageSize: PAGE_SIZE }),
    `trips:${status}:${search}:${from}:${to}:${page}`,
  )

  const update = (mutate: (next: URLSearchParams) => void) => {
    setParams((current) => {
      const next = new URLSearchParams(current)
      mutate(next)
      return next
    })
  }

  const setStatus = (value: TripStatus | '') =>
    update((next) => {
      if (value) next.set('status', value)
      else next.delete('status')
      next.delete('page')
    })

  const setDate = (key: 'from' | 'to', value: string) =>
    update((next) => {
      if (value) next.set(key, value)
      else next.delete(key)
      next.delete('page')
    })

  const columns: Column<TripListItem>[] = [
    {
      key: 'tripNumber',
      header: t('tripNumber'),
      render: (row) => <span className="ltr-nums font-bold">{row.tripNumber}</span>,
    },
    {
      key: 'passenger',
      header: t('passenger'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block truncate font-bold">{row.passengerName || t('unnamed')}</span>
          {row.passengerPhone && <span className="ltr-nums block text-xs text-muted">{row.passengerPhone}</span>}
        </span>
      ),
    },
    {
      key: 'driver',
      header: t('driver'),
      render: (row) => (row.driverName ? <span className="font-bold">{row.driverName}</span> : <span className="text-muted">—</span>),
    },
    { key: 'category', header: t('category'), render: (row) => row.categoryName ?? '—' },
    {
      key: 'route',
      header: t('route'),
      render: (row) => (
        <span className="flex max-w-72 items-center gap-2 text-sm">
          <span className="truncate">{row.pickupName || '—'}</span>
          <Icon name="arrow" className="size-4 shrink-0 text-muted ltr:rotate-180" />
          <span className="truncate">{row.dropoffName || '—'}</span>
        </span>
      ),
    },
    {
      key: 'fare',
      header: t('fare'),
      className: 'text-end',
      render: (row) => (
        <span className="ltr-nums whitespace-nowrap">
          <span className={row.finalFare !== null ? 'font-bold' : 'text-muted'}>{formatMoney(row.finalFare ?? row.estimatedFare)}</span>{' '}
          <span className="text-xs text-muted">{t('sar')}</span>
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <TripStatusBadge status={row.status} /> },
    {
      key: 'requestedAt',
      header: t('requestedAt'),
      render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.requestedAt, lang)}</span>,
    },
  ]

  return (
    <>
      <PageHeader title={t('tripsTitle')} description={t('tripsCopy')} />

      <div className="mb-4 flex flex-wrap items-stretch gap-2">
        <div className="flex items-center rounded-2xl bg-white p-1.5 shadow-soft">
          <StatusTab active={status === ''} label={t('statusAll')} onClick={() => setStatus('')} />
        </div>
        {TRIP_STATUS_GROUPS.map((group) => (
          <div key={group.group} className="flex items-center gap-1 rounded-2xl bg-white p-1.5 shadow-soft">
            <span className="px-3 text-xs font-bold text-muted">{t(group.key)}</span>
            <div className="flex gap-1 overflow-x-auto">
              {group.statuses.map((value) => (
                <StatusTab key={value} active={status === value} label={t(tripStatusMeta[value].key)} onClick={() => setStatus(value)} />
              ))}
            </div>
          </div>
        ))}
      </div>

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto]">
        <SearchInput placeholder={t('searchTrips')} value={searchInput} onChange={(event) => setSearchInput(event.target.value)} wrapperClassName="sm:col-span-2 lg:col-span-1" />
        <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setDate('from', event.target.value)} wrapperClassName="lg:w-48" />
        <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setDate('to', event.target.value)} wrapperClassName="lg:w-48" />
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={query.data?.items ?? []}
              rowKey={(row) => row.id}
              loading={query.loading}
              onRowClick={(row) => navigate(`/trips/${row.id}`)}
            />
            {query.data && (
              <Pagination
                page={query.data.page}
                pageSize={query.data.pageSize}
                total={query.data.total}
                onChange={(nextPage) => update((next) => next.set('page', String(nextPage)))}
              />
            )}
          </>
        )}
      </Card>
    </>
  )
}

function StatusTab({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={`whitespace-nowrap rounded-xl px-3.5 py-2 text-sm font-bold transition ${active ? 'bg-ink text-white' : 'text-muted hover:bg-cloud'}`}
    >
      {label}
    </button>
  )
}
