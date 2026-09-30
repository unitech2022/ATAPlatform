import { useState } from 'react'
import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { corporateAccounts } from '../lib/admin'
import { parseEnum, saveBlob } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { tripStatusMeta } from '../lib/status'
import { ALL_TRIP_STATUSES } from '../lib/trips'
import type { CorporateTripRow, TripStatus } from '../lib/types'
import { Badge, TripStatusBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { Input, SearchInput, Select } from './Field'
import { Money } from './Money'
import { PermissionError } from './PermissionError'
import { Pagination } from './Pagination'
import { Table, type Column } from './Table'

const PAGE_SIZE = 20

/** Company trips with filters and the authenticated CSV export (assumed `GET /admin/corporate/accounts/{id}/trips[/export]`). */
export function CorporateTripsTab({ accountId, accountNumber }: { accountId: string; accountNumber: string }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const status = parseEnum(params.get('status'), ALL_TRIP_STATUSES)
  const guest = parseEnum(params.get('guest'), ['true', 'false'] as const)
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))
  const filters = { status, isGuest: guest, from, to, search: search.value }
  const query = useQuery(() => corporateAccounts.trips(accountId, { ...filters, page, pageSize: PAGE_SIZE }), `corporate-trips:${accountId}:${status}:${guest}:${from}:${to}:${search.value}:${page}`)
  const [exporting, setExporting] = useState(false)

  const exportCsv = async () => {
    setExporting(true)
    try {
      const { blob, fileName } = await corporateAccounts.exportTrips(accountId, filters)
      saveBlob(blob, fileName ?? `${accountNumber}-trips.csv`)
      toast.success(t('exported'))
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setExporting(false)
    }
  }

  const columns: Column<CorporateTripRow>[] = [
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (
        <span className="block">
          <Link to={`/trips/${row.tripId}`} className="ltr-nums font-bold text-brand hover:underline">
            {row.tripNumber}
          </Link>
          <span className="block whitespace-nowrap text-xs text-muted">{formatDateTime(row.date, lang)}</span>
        </span>
      ),
    },
    {
      key: 'who',
      header: t('coColEmployee'),
      render: (row) =>
        row.guest ? (
          <span className="block">
            <span className="block max-w-44 truncate font-bold">{row.guest}</span>
            <Badge tone="warning">{t('coTripGuest')}</Badge>
          </span>
        ) : (
          <span className="block">
            <span className="block max-w-44 truncate font-bold">{row.employee ?? '—'}</span>
            <span className="ltr-nums block text-xs text-muted">{[row.employeeNumber, row.department].filter(Boolean).join(' · ')}</span>
          </span>
        ),
    },
    {
      key: 'purpose',
      header: t('purpose'),
      render: (row) => (
        <span className="block">
          <span className="block max-w-44 truncate">{row.purpose ?? '—'}</span>
          {row.costCenter && <span className="ltr-nums block text-xs text-muted">{row.costCenter}</span>}
        </span>
      ),
    },
    { key: 'category', header: t('coTripCategory'), render: (row) => row.category ?? '—' },
    {
      key: 'route',
      header: t('coTripRoute'),
      render: (row) => (
        <span className="block max-w-56 text-xs">
          <span className="block truncate">{row.pickup ?? '—'}</span>
          <span className="block truncate text-muted">← {row.dropoff ?? '—'}</span>
        </span>
      ),
    },
    {
      key: 'distance',
      header: t('coTripDistance'),
      className: 'text-end',
      render: (row) => <span className="ltr-nums">{typeof row.distanceKm === 'number' ? formatNumber(Math.round(row.distanceKm * 10) / 10) : '—'}</span>,
    },
    {
      key: 'amount',
      header: t('coTripAmount'),
      className: 'text-end',
      render: (row) => (
        <span className="block">
          <Money value={row.amountInclVat} strong />
          {typeof row.vat === 'number' && (
            <span className="block text-xs text-muted">
              {t('coInvVat')}: <Money value={row.vat} />
            </span>
          )}
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <TripStatusBadge status={row.status} /> },
  ]

  return (
    <>
      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto_auto_auto_auto]">
        <SearchInput placeholder={t('coTripSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} wrapperClassName="sm:col-span-2 lg:col-span-1" />
        <Select id="co-trip-status" aria-label={t('status')} value={status} onChange={(event) => setFilter('status', event.target.value)} wrapperClassName="lg:w-44">
          <option value="">{t('statusAll')}</option>
          {ALL_TRIP_STATUSES.map((value: TripStatus) => (
            <option key={value} value={value}>
              {t(tripStatusMeta[value].key)}
            </option>
          ))}
        </Select>
        <Select id="co-trip-guest" aria-label={t('coTripType')} value={guest} onChange={(event) => setFilter('guest', event.target.value)} wrapperClassName="lg:w-44">
          <option value="">{t('coTripTypeAll')}</option>
          <option value="false">{t('coTripTypeEmployee')}</option>
          <option value="true">{t('coTripTypeGuest')}</option>
        </Select>
        <Input id="co-trip-from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} wrapperClassName="lg:w-44" />
        <Input id="co-trip-to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} wrapperClassName="lg:w-44" />
        <Button variant="secondary" icon="download" className="h-12" loading={exporting} onClick={exportCsv}>
          {t('exportCsv')}
        </Button>
      </div>

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="corporate.manage" onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.tripId} loading={query.loading} emptyTitle={t('coTripsEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}
