import { useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { payoutBatches } from '../lib/admin'
import { formatDateTime, formatNumber } from '../lib/format'
import { payoutBatchStatusMeta } from '../lib/status'
import type { PayoutBatch } from '../lib/types'

const PAGE_SIZE = 20

export function PayoutBatchesPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const { page, setPage } = useUrlState()
  const query = useQuery(() => payoutBatches.list({ page, pageSize: PAGE_SIZE }), `payout-batches:${page}`)

  const columns: Column<PayoutBatch>[] = [
    { key: 'number', header: t('batchNumber'), render: (row) => <span className="ltr-nums font-bold">{row.batchNumber}</span> },
    { key: 'count', header: t('payoutsCount'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.payoutsCount)}</span> },
    { key: 'total', header: t('total'), className: 'text-end', render: (row) => <Money value={row.totalAmount} strong /> },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={payoutBatchStatusMeta} value={row.status} /> },
    { key: 'reference', header: t('bankReference'), render: (row) => <span className="ltr-nums text-muted">{row.bankReference ?? '—'}</span> },
    {
      key: 'createdAt',
      header: t('createdAt'),
      render: (row) => (
        <span className="block whitespace-nowrap">
          {formatDateTime(row.createdAt, lang)}
          {row.createdByName && <span className="block text-xs text-muted">{row.createdByName}</span>}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader title={t('payoutBatchesTitle')} description={t('payoutBatchesCopy')} />
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
              onRowClick={(row) => navigate(`/payout-batches/${row.id}`)}
              emptyTitle={t('noBatches')}
              emptyDescription={t('noBatchesCopy')}
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}
