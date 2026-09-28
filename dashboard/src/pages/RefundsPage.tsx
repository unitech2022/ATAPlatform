import { Link } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input } from '../components/Field'
import { Icon } from '../components/Icon'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { RefundActions } from '../components/RefundActions'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { refunds } from '../lib/admin'
import { lookupKey, REFUND_AUTO_APPROVE_LIMIT, REFUND_DESTINATION_KEY, REFUND_REASON_KEY, REFUND_STATUSES } from '../lib/finance'
import { formatDateTime, formatMoney } from '../lib/format'
import { refundStatusMeta } from '../lib/status'
import type { Refund, RefundStatus } from '../lib/types'

const PAGE_SIZE = 20

export function RefundsPage() {
  const { t, lang } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()

  // The queue opens on "pending approval"; `?status=all` shows everything.
  const rawStatus = params.get('status')
  const status: RefundStatus | '' = rawStatus === 'all' ? '' : ((REFUND_STATUSES as string[]).includes(rawStatus ?? '') ? (rawStatus as RefundStatus) : 'pending_approval')
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))

  const query = useQuery(() => refunds.list({ status, from, to, page, pageSize: PAGE_SIZE }), `refunds:${status}:${from}:${to}:${page}`)

  const columns: Column<Refund>[] = [
    {
      key: 'number',
      header: t('refundNumber'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold">{row.refundNumber}</span>
          <span className="block text-xs text-muted">{formatDateTime(row.createdAt, lang)}</span>
        </span>
      ),
    },
    {
      key: 'customer',
      header: t('customer'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block truncate font-bold">{row.userName || t('unnamed')}</span>
          {row.userPhone && <span className="ltr-nums block text-xs text-muted">{row.userPhone}</span>}
        </span>
      ),
    },
    {
      key: 'reference',
      header: t('reference'),
      render: (row) => (
        <span className="flex flex-col text-xs">
          {row.paymentId && (
            <Link to={`/payments/${row.paymentId}`} className="font-bold text-brand hover:underline">
              {t('viewPayment')}
            </Link>
          )}
          {row.tripId && (
            <Link to={`/trips/${row.tripId}`} className="ltr-nums font-bold text-brand hover:underline">
              {row.tripNumber ?? t('viewTrip')}
            </Link>
          )}
          {!row.paymentId && !row.tripId && <span className="text-muted">—</span>}
        </span>
      ),
    },
    { key: 'amount', header: t('amount'), className: 'text-end', render: (row) => <Money value={row.amount} strong /> },
    {
      key: 'reason',
      header: t('reason'),
      render: (row) => (
        <span className="block max-w-64">
          <span className="block font-bold">{t(lookupKey(REFUND_REASON_KEY, row.reasonCode) ?? 'reasonOther')}</span>
          <span className="block text-xs text-muted">{t(lookupKey(REFUND_DESTINATION_KEY, row.destination) ?? 'destinationWallet')}</span>
          {row.reason && <span className="block truncate text-xs text-muted">{row.reason}</span>}
        </span>
      ),
    },
    {
      key: 'requestedBy',
      header: t('requestedBy'),
      render: (row) => (
        <span className="block text-xs">
          <span className="block font-bold">{row.requestedByName ?? '—'}</span>
          {row.approvedByName && (
            <span className="block text-muted">
              {t('approvedBy')}: {row.approvedByName}
            </span>
          )}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={refundStatusMeta} value={row.status} />
          {(row.rejectedReason || row.failureMessage) && <span className="mt-1 block max-w-56 truncate text-xs text-danger">{row.rejectedReason ?? row.failureMessage}</span>}
        </span>
      ),
    },
    { key: 'actions', header: t('actions'), className: 'text-end', render: (row) => <RefundActions refund={row} onChanged={query.reload} /> },
  ]

  return (
    <>
      <PageHeader title={t('refundsTitle')} description={t('refundsCopy')} />

      <div className="mb-4 flex items-start gap-3 rounded-2xl bg-white p-4 text-sm text-muted shadow-soft">
        <Icon name="shield" className="mt-0.5 size-4 shrink-0 text-brand" />
        <p>
          {t('fourEyesPolicy')} <span className="ltr-nums font-bold text-ink">{formatMoney(REFUND_AUTO_APPROVE_LIMIT)} {t('sar')}</span>
        </p>
      </div>

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status || 'all'}
          onChange={(value) => setFilter('status', value === 'pending_approval' ? '' : value)}
          options={[
            ...REFUND_STATUSES.map((value) => ({ value: value as RefundStatus | 'all', label: t(refundStatusMeta[value].key), count: value === status ? (query.data?.total ?? null) : null })),
            { value: 'all', label: t('statusAll') },
          ]}
        />
        <div className="grid grid-cols-2 gap-3 lg:w-96">
          <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
          <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
        </div>
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('noRefunds')} />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}
