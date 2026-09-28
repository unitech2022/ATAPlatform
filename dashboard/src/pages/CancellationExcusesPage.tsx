import { useState } from 'react'
import { Link } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { ExcuseReviewModal, type ReviewTarget } from '../components/ExcuseReviewModal'
import { Icon } from '../components/Icon'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { cancellations } from '../lib/admin'
import { ACTOR_KEY, EXCUSE_SLA_HOURS, STAGE_KEY } from '../lib/cancellation'
import { formatDateTime, formatNumber } from '../lib/format'
import { excuseStatusMeta, feeStatusMeta } from '../lib/status'
import type { ExcuseQueueItem, ExcuseStatus } from '../lib/types'

const PAGE_SIZE = 20
const QUEUE_STATUSES: Exclude<ExcuseStatus, 'not_applicable'>[] = ['pending', 'approved', 'rejected']

/** Excuse review queue (`/admin/cancellations/excuses`) with the SLA indicator and approve / reject. */
export function CancellationExcusesPage() {
  const { t, lang } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()
  const raw = params.get('status')
  const status: ExcuseStatus = (QUEUE_STATUSES as string[]).includes(raw ?? '') ? (raw as ExcuseStatus) : 'pending'
  const query = useQuery(() => cancellations.excuses({ status, page, pageSize: PAGE_SIZE }), `excuses:${status}:${page}`)
  const [review, setReview] = useState<{ target: ReviewTarget; decision: 'approve' | 'reject' } | null>(null)

  const rows = [...(query.data?.items ?? [])].sort((a, b) => Number(b.slaBreached) - Number(a.slaBreached) || b.ageHours - a.ageHours)

  const columns: Column<ExcuseQueueItem>[] = [
    {
      key: 'age',
      header: t('cxAge'),
      render: (row) =>
        row.excuseStatus === 'pending' ? (
          <span className="block">
            <Badge tone={row.slaBreached ? 'danger' : row.ageHours >= EXCUSE_SLA_HOURS * 0.75 ? 'warning' : 'muted'}>
              <span className="ltr-nums">
                {formatNumber(Math.round(row.ageHours))} {t('cxHoursShort')}
              </span>
            </Badge>
            {row.slaBreached && <span className="mt-1 block text-xs font-bold text-danger">{t('cxSlaBreached')}</span>}
          </span>
        ) : (
          <span className="block text-xs text-muted">
            {row.reviewedByName ?? '—'}
            <span className="block">{formatDateTime(row.reviewedAt, lang)}</span>
          </span>
        ),
    },
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (
        <span className="block">
          <Link to={`/trips/${row.tripId}`} className="ltr-nums block font-bold text-brand hover:underline">
            {row.tripNumber}
          </Link>
          <span className="block text-xs text-muted">{formatDateTime(row.createdAt, lang)}</span>
        </span>
      ),
    },
    {
      key: 'who',
      header: t('actor'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.userName || t('unnamed')}</span>
          <span className="block text-xs text-muted">
            {t(ACTOR_KEY[row.actor] ?? 'actorSystem')} · {t(STAGE_KEY[row.stage] ?? 'cxStageAfterAccept')}
          </span>
        </span>
      ),
    },
    {
      key: 'reason',
      header: t('reason'),
      render: (row) => (
        <span className="block max-w-64">
          <span className="block font-bold">{row.reasonName ?? row.reasonCode}</span>
          {row.note && <span className="line-clamp-2 block whitespace-normal text-xs text-muted">{row.note}</span>}
          {row.reviewNote && (
            <span className="mt-1 block truncate text-xs text-muted">
              {t('reviewNote')}: {row.reviewNote}
            </span>
          )}
        </span>
      ),
    },
    {
      key: 'fee',
      header: t('cxFeeAtStake'),
      className: 'text-end',
      render: (row) => (
        <span className="flex flex-col items-end gap-1">
          <Money value={row.feeAmount} strong />
          {row.feeCharged > 0 && (
            <span className="text-xs text-muted">
              {t('cxCharged')}: <Money value={row.feeCharged} />
            </span>
          )}
          <MetaBadge record={feeStatusMeta} value={row.feeStatus} />
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => <MetaBadge record={excuseStatusMeta} value={row.excuseStatus} />,
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) =>
        row.excuseStatus === 'pending' ? (
          <span className="inline-flex gap-2">
            <Button variant="brand" size="sm" icon="check" onClick={() => setReview({ target: toTarget(row), decision: 'approve' })}>
              {t('approve')}
            </Button>
            <Button variant="danger-outline" size="sm" icon="x" onClick={() => setReview({ target: toTarget(row), decision: 'reject' })}>
              {t('reject')}
            </Button>
          </span>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader title={t('cxExcusesTitle')} description={t('cxExcusesCopy')} />

      <div className="mb-4 flex items-start gap-3 rounded-2xl bg-white p-4 text-sm text-muted shadow-soft">
        <Icon name="clock" className="mt-0.5 size-4 shrink-0 text-brand" />
        <p>
          {t('cxSlaPolicy')} <span className="ltr-nums font-bold text-ink">{formatNumber(EXCUSE_SLA_HOURS)} {t('cxHoursShort')}</span>
        </p>
      </div>

      <Tabs
        className="mb-4"
        value={status}
        onChange={(value) => setFilter('status', value === 'pending' ? '' : value)}
        options={QUEUE_STATUSES.map((value) => ({ value, label: t(excuseStatusMeta[value].key), count: value === status ? (query.data?.total ?? null) : null }))}
      />

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={status === 'pending' ? t('cxNoPendingExcuses') : t('noResults')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <ExcuseReviewModal
        target={review?.target ?? null}
        decision={review?.decision ?? 'approve'}
        onClose={() => setReview(null)}
        onReviewed={() => {
          setReview(null)
          query.reload()
        }}
      />
    </>
  )
}

function toTarget(row: ExcuseQueueItem): ReviewTarget {
  return {
    id: row.id,
    tripNumber: row.tripNumber,
    actor: row.actor,
    userName: row.userName,
    reasonName: row.reasonName,
    reasonCode: row.reasonCode,
    note: row.note,
    stage: row.stage,
    feeAmount: row.feeAmount,
    feeCharged: row.feeCharged,
    penaltyPoints: row.pendingPenaltyPoints ?? (row.penaltyPoints || null),
  }
}
