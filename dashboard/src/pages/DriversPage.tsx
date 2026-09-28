import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { DriverStatusBadge } from '../components/Badge'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { SearchInput } from '../components/Field'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { drivers } from '../lib/admin'
import { formatDate, formatNumber } from '../lib/format'
import type { DriverListItem, DriverStatus } from '../lib/types'

const PAGE_SIZE = 20

type StatusFilter = DriverStatus | ''

const STATUS_TABS: { value: StatusFilter; key: TranslationKey }[] = [
  { value: '', key: 'statusAll' },
  { value: 'submitted', key: 'statusSubmitted' },
  { value: 'under_review', key: 'statusUnderReview' },
  { value: 'approved', key: 'statusApproved' },
  { value: 'rejected', key: 'statusRejected' },
  { value: 'suspended', key: 'statusSuspended' },
]

function parseStatus(value: string | null): StatusFilter {
  return STATUS_TABS.some((tab) => tab.value === value) ? (value as StatusFilter) : ''
}

export function DriversPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()

  const status = parseStatus(params.get('status'))
  const search = params.get('q') ?? ''
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
    () => drivers.list({ status, search, page, pageSize: PAGE_SIZE }),
    `drivers:${status}:${search}:${page}`,
  )

  const update = (mutate: (next: URLSearchParams) => void) => {
    setParams((current) => {
      const next = new URLSearchParams(current)
      mutate(next)
      return next
    })
  }

  const columns: Column<DriverListItem>[] = [
    {
      key: 'applicationNumber',
      header: t('applicationNumber'),
      render: (row) => <span className="ltr-nums font-bold">{row.applicationNumber}</span>,
    },
    { key: 'fullName', header: t('fullName'), render: (row) => <span className="font-bold">{row.fullName || t('unnamed')}</span> },
    { key: 'phoneNumber', header: t('phoneNumber'), render: (row) => <span className="ltr-nums">{row.phoneNumber ?? '—'}</span> },
    { key: 'cityName', header: t('city'), render: (row) => row.cityName ?? '—' },
    { key: 'vehicle', header: t('vehicle'), render: (row) => <span className="text-muted">{row.vehicle ?? t('noVehicle')}</span> },
    { key: 'submittedAt', header: t('submittedAt'), render: (row) => formatDate(row.submittedAt, lang) },
    {
      key: 'documentsPending',
      header: t('pendingDocs'),
      className: 'text-center',
      render: (row) => (
        <span className={`ltr-nums font-bold ${row.documentsPending > 0 ? 'text-danger' : 'text-muted'}`}>{formatNumber(row.documentsPending)}</span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <DriverStatusBadge status={row.status} /> },
  ]

  return (
    <>
      <PageHeader title={t('driversTitle')} description={t('driversCopy')} />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex gap-1 overflow-x-auto rounded-2xl bg-white p-1.5 shadow-soft">
          {STATUS_TABS.map((tab) => {
            const active = tab.value === status
            return (
              <button
                key={tab.value || 'all'}
                type="button"
                onClick={() =>
                  update((next) => {
                    if (tab.value) next.set('status', tab.value)
                    else next.delete('status')
                    next.delete('page')
                  })
                }
                className={`whitespace-nowrap rounded-xl px-4 py-2.5 text-sm font-bold transition ${
                  active ? 'bg-ink text-white' : 'text-muted hover:bg-cloud'
                }`}
              >
                {t(tab.key)}
              </button>
            )
          })}
        </div>
        <SearchInput
          wrapperClassName="lg:w-96"
          className="bg-white shadow-soft"
          placeholder={t('searchDrivers')}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
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
              onRowClick={(row) => navigate(`/drivers/${row.id}`)}
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
