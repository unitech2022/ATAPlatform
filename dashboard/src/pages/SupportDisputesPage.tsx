import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DisputeResolveModal } from '../components/DisputeResolveModal'
import { Icon } from '../components/Icon'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { PermissionError } from '../components/PermissionError'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { disputes } from '../lib/admin'
import { lookupKey } from '../lib/finance'
import { formatDateTime } from '../lib/format'
import { DISPUTE_REASON_KEY, DISPUTE_RESOLUTION_KEY, DISPUTE_STATUSES, disputeRefundOf, isOpenDispute } from '../lib/support'
import { disputeStatusMeta, refundStatusMeta } from '../lib/status'
import type { DisputeStatus, FareDispute } from '../lib/types'

const PAGE_SIZE = 20

/** Fare disputes (`/support/disputes`): the open queue by default, with the resolve modal and the F11 refund / four-eyes state. */
export function SupportDisputesPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const [resolving, setResolving] = useState<FareDispute | null>(null)

  // Opens on "open"; `?status=all` shows everything (same convention as the refunds queue).
  const raw = params.get('status')
  const status: DisputeStatus | '' = raw === 'all' ? '' : (DISPUTE_STATUSES as string[]).includes(raw ?? '') ? (raw as DisputeStatus) : 'open'

  const query = useQuery(() => disputes.list({ status, page, pageSize: PAGE_SIZE }), `support-disputes:${status}:${page}`)

  const columns: Column<FareDispute>[] = [
    {
      key: 'ticket',
      header: t('spTicket'),
      render: (row) => (
        <span className="block">
          <Link to={`/support/tickets/${row.ticketId}`} onClick={(event) => event.stopPropagation()} className="ltr-nums block font-bold text-brand hover:underline">
            {row.ticketNumber ?? t('spViewTicket')}
          </Link>
          <span className="block text-xs text-muted">{formatDateTime(row.createdAt, lang)}</span>
        </span>
      ),
    },
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (
        <Link to={`/trips/${row.tripId}`} onClick={(event) => event.stopPropagation()} className="ltr-nums font-bold text-brand hover:underline">
          {row.tripNumber ?? t('viewTrip')}
        </Link>
      ),
    },
    { key: 'requester', header: t('spRequester'), render: (row) => <span className="font-bold">{row.requesterName || t('unnamed')}</span> },
    { key: 'reason', header: t('spDisputeReason'), render: (row) => t(lookupKey(DISPUTE_REASON_KEY, row.reason) ?? 'spReasonOther') },
    { key: 'charged', header: t('spChargedAmount'), className: 'text-end', render: (row) => <Money value={row.chargedAmount} strong /> },
    {
      key: 'requested',
      header: t('spRequestedAmount'),
      className: 'text-end',
      render: (row) => (row.requestedRefundAmount === null ? <span className="text-muted">—</span> : <Money value={row.requestedRefundAmount} />),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={disputeStatusMeta} value={row.status} /> },
    {
      key: 'resolution',
      header: t('spResolution'),
      render: (row) =>
        row.resolution ? (
          <span className="block">
            <span className="block text-sm font-bold">{t(DISPUTE_RESOLUTION_KEY[row.resolution])}</span>
            <span className="block text-xs text-muted">
              <Money value={row.approvedRefundAmount ?? 0} />
            </span>
          </span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'refund',
      header: t('spRefund'),
      render: (row) => {
        const refund = disputeRefundOf(row)
        if (!refund) return <span className="text-muted">—</span>
        return (
          <span className="flex flex-col items-start gap-1">
            <MetaBadge record={refundStatusMeta} value={refund.status} />
            <Link
              to={refund.status === 'pending_approval' ? '/refunds?status=pending_approval' : '/refunds?status=all'}
              onClick={(event) => event.stopPropagation()}
              className="inline-flex items-center gap-1 text-xs font-bold text-brand hover:underline"
            >
              <span className="ltr-nums">{refund.refundNumber ?? t('spOpenRefunds')}</span>
              <Icon name="chevron" className="size-3 rtl:rotate-180" />
            </Link>
            {refund.status === 'pending_approval' && <span className="text-[11px] font-bold text-amber-700">{t('spFourEyesPending')}</span>}
          </span>
        )
      },
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) =>
        isOpenDispute(row.status) ? (
          <Button
            variant="brand"
            size="sm"
            icon="check"
            onClick={(event) => {
              event.stopPropagation()
              setResolving(row)
            }}
          >
            {t('spResolveDispute')}
          </Button>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader title={t('spDisputesTitle')} description={t('spDisputesCopy')} />

      <Tabs
        className="mb-4"
        value={status === '' ? 'all' : status}
        onChange={(value) => setFilter('status', value === 'open' ? '' : value)}
        options={[
          ...DISPUTE_STATUSES.map((value) => ({ value: value as DisputeStatus | 'all', label: t(disputeStatusMeta[value].key) })),
          { value: 'all' as const, label: t('statusAll') },
        ]}
      />

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="support.view" onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={query.data?.items ?? []}
              rowKey={(row) => row.id}
              loading={query.loading}
              onRowClick={(row) => navigate(`/support/tickets/${row.ticketId}`)}
              emptyTitle={t('spNoDisputes')}
              emptyDescription=""
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <DisputeResolveModal dispute={resolving} onClose={() => setResolving(null)} onResolved={() => query.reload()} />
    </>
  )
}
