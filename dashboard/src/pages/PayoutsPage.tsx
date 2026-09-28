import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input } from '../components/Field'
import { Icon } from '../components/Icon'
import { MarkPaidModal } from '../components/MarkPaidModal'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { ReasonModal } from '../components/ReasonModal'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { payoutBatches, payouts } from '../lib/admin'
import { PAYOUT_STATUSES } from '../lib/finance'
import { formatDateTime, formatMoney, formatNumber } from '../lib/format'
import { payoutStatusMeta } from '../lib/status'
import type { Payout, PayoutStatus } from '../lib/types'

const PAGE_SIZE = 20

type Pending =
  | { kind: 'approve'; payout: Payout }
  | { kind: 'reject'; payout: Payout }
  | { kind: 'paid'; payout: Payout }
  | { kind: 'bulk-approve'; ids: string[] }
  | { kind: 'batch'; ids: string[] }
  | { kind: 'batch-all' }
  | null

export function PayoutsPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()

  const rawStatus = params.get('status')
  const status: PayoutStatus | '' = rawStatus === 'all' ? '' : ((PAYOUT_STATUSES as string[]).includes(rawStatus ?? '') ? (rawStatus as PayoutStatus) : 'requested')
  const driverId = params.get('driverId') ?? ''
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))
  const queryKey = `payouts:${status}:${driverId}:${from}:${to}:${page}`

  const query = useQuery(() => payouts.list({ status, driverId, from, to, page, pageSize: PAGE_SIZE }), queryKey)
  const [selection, setSelection] = useState<{ key: string; ids: Set<string> }>({ key: queryKey, ids: new Set() })
  const [pending, setPending] = useState<Pending>(null)

  // Selection belongs to the rows currently on screen; any filter/page change clears it.
  const selected = selection.key === queryKey ? selection.ids : new Set<string>()
  const rows = query.data?.items ?? []
  const selectable = (row: Payout) => row.status === 'requested' || row.status === 'approved'
  const selectedRows = rows.filter((row) => selected.has(row.id))
  const selectedRequested = selectedRows.filter((row) => row.status === 'requested').map((row) => row.id)
  const selectedApproved = selectedRows.filter((row) => row.status === 'approved').map((row) => row.id)
  const selectedTotal = selectedRows.reduce((sum, row) => sum + row.amount, 0)

  const toggle = (id: string) =>
    setSelection(() => {
      const next = new Set(selected)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return { key: queryKey, ids: next }
    })
  const selectableIds = rows.filter(selectable).map((row) => row.id)
  const allSelected = selectableIds.length > 0 && selectableIds.every((id) => selected.has(id))
  const toggleAll = () => setSelection({ key: queryKey, ids: allSelected ? new Set() : new Set(selectableIds) })

  const done = (message: string, description?: string) => {
    toast.success(message, description)
    setPending(null)
    setSelection({ key: queryKey, ids: new Set() })
    query.reload()
  }

  const approve = async (payout: Payout) => {
    try {
      await payouts.approve(payout.id)
      done(t('payoutApproved'), payout.payoutNumber)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const reject = async (payout: Payout, reason: string) => {
    try {
      await payouts.reject(payout.id, reason)
      done(t('payoutRejected'), payout.payoutNumber)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const markPaid = async (payout: Payout, bankReference: string, paidAt?: string) => {
    await payouts.markPaid(payout.id, bankReference, paidAt)
    done(t('payoutPaid'), payout.payoutNumber)
  }

  /** There is no bulk endpoint: approvals run one by one so each keeps its own audit entry. */
  const bulkApprove = async (ids: string[]) => {
    let ok = 0
    const failures: string[] = []
    for (const id of ids) {
      try {
        await payouts.approve(id)
        ok += 1
      } catch (error) {
        const payout = rows.find((row) => row.id === id)
        failures.push(`${payout?.payoutNumber ?? id}: ${describe(error)}`)
      }
    }
    if (failures.length === 0) done(t('bulkApproved'), `${formatNumber(ok)} / ${formatNumber(ids.length)}`)
    else {
      toast.error(`${t('bulkApprovedPartial')} ${formatNumber(ok)} / ${formatNumber(ids.length)}`, failures.slice(0, 3).join(' · '))
      setPending(null)
      setSelection({ key: queryKey, ids: new Set() })
      query.reload()
    }
  }

  const createBatch = async (input: { payoutIds: string[] } | { allApproved: true }) => {
    try {
      const batch = await payoutBatches.create(input)
      done(t('batchCreated'), batch.batchNumber)
      navigate(`/payout-batches/${batch.id}`)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<Payout>[] = [
    {
      key: 'select',
      header: (
        <input type="checkbox" aria-label={t('selectAll')} className="size-4 accent-brand" checked={allSelected} disabled={selectableIds.length === 0} onChange={toggleAll} />
      ),
      render: (row) =>
        selectable(row) ? (
          <input type="checkbox" aria-label={`${t('select')} ${row.payoutNumber}`} className="size-4 accent-brand" checked={selected.has(row.id)} onChange={() => toggle(row.id)} />
        ) : null,
    },
    {
      key: 'number',
      header: t('payoutNumber'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold">{row.payoutNumber}</span>
          <span className="block text-xs text-muted">{formatDateTime(row.requestedAt, lang)}</span>
        </span>
      ),
    },
    {
      key: 'driver',
      header: t('driver'),
      render: (row) => (
        <span className="block min-w-0">
          {row.driverId ? (
            <Link to={`/drivers/${row.driverId}`} className="block truncate font-bold text-brand hover:underline">
              {row.driverName || t('unnamed')}
            </Link>
          ) : (
            <span className="block truncate font-bold">{row.driverName || t('unnamed')}</span>
          )}
          {row.driverPhone && <span className="ltr-nums block text-xs text-muted">{row.driverPhone}</span>}
        </span>
      ),
    },
    { key: 'amount', header: t('amount'), className: 'text-end', render: (row) => <Money value={row.amount} strong /> },
    {
      key: 'iban',
      header: t('iban'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block whitespace-nowrap">{row.ibanMasked ?? '—'}</span>
          {row.accountHolderName && <span className="block text-xs text-muted">{row.accountHolderName}</span>}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={payoutStatusMeta} value={row.status} />
          {row.rejectedReason && <span className="mt-1 block max-w-56 truncate text-xs text-danger">{row.rejectedReason}</span>}
          {row.bankReference && <span className="ltr-nums mt-1 block text-xs text-muted">{row.bankReference}</span>}
        </span>
      ),
    },
    {
      key: 'batch',
      header: t('batch'),
      render: (row) =>
        row.batchId ? (
          <Link to={`/payout-batches/${row.batchId}`} className="ltr-nums text-xs font-bold text-brand hover:underline">
            {row.batchNumber ?? t('viewBatch')}
          </Link>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          {row.status === 'requested' && (
            <>
              <Button variant="brand" size="sm" icon="check" onClick={() => setPending({ kind: 'approve', payout: row })}>
                {t('approve')}
              </Button>
              <Button variant="danger-outline" size="sm" icon="x" onClick={() => setPending({ kind: 'reject', payout: row })}>
                {t('reject')}
              </Button>
            </>
          )}
          {row.status === 'approved' && (
            <Button variant="secondary" size="sm" icon="check" onClick={() => setPending({ kind: 'paid', payout: row })}>
              {t('markPaid')}
            </Button>
          )}
        </span>
      ),
    },
  ]

  const payoutLabel = (payout: Payout) => `${payout.payoutNumber} — ${payout.driverName ?? ''} — ${formatMoney(payout.amount)} ${t('sar')}`

  return (
    <>
      <PageHeader
        title={t('payoutsTitle')}
        description={t('payoutsCopy')}
        actions={
          <>
            <Button variant="secondary" icon="list" onClick={() => navigate('/payout-batches')}>
              {t('navPayoutBatches')}
            </Button>
            {status === 'approved' && (rows.length > 0) && (
              <Button icon="layers" onClick={() => setPending({ kind: 'batch-all' })}>
                {t('batchAllApproved')}
              </Button>
            )}
          </>
        }
      />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status || 'all'}
          onChange={(value) => setFilter('status', value === 'requested' ? '' : value)}
          options={[
            ...PAYOUT_STATUSES.map((value) => ({ value: value as PayoutStatus | 'all', label: t(payoutStatusMeta[value].key), count: value === status ? (query.data?.total ?? null) : null })),
            { value: 'all', label: t('statusAll') },
          ]}
        />
        <div className="grid grid-cols-2 gap-3 lg:w-96">
          <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
          <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
        </div>
      </div>

      {driverId && (
        <div className="mb-4 flex flex-wrap items-center gap-3 rounded-2xl bg-white p-4 text-sm shadow-soft">
          <Icon name="user" className="size-4 text-brand" />
          <span className="text-muted">{t('filteredByDriver')}</span>
          <Button variant="ghost" size="sm" icon="x" onClick={() => setFilter('driverId', '')}>
            {t('clearFilter')}
          </Button>
        </div>
      )}

      {selectedRows.length > 0 && (
        <div className="sticky top-24 z-10 mb-4 flex flex-col gap-3 rounded-2xl bg-ink p-4 text-white shadow-button sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm font-bold">
            {t('selected')}: <span className="ltr-nums">{formatNumber(selectedRows.length)}</span> · <span className="ltr-nums">{formatMoney(selectedTotal)}</span> {t('sar')}
          </p>
          <div className="flex flex-wrap gap-2">
            {selectedRequested.length > 0 && (
              <Button variant="brand" size="sm" icon="check" onClick={() => setPending({ kind: 'bulk-approve', ids: selectedRequested })}>
                {t('approveSelected')} ({formatNumber(selectedRequested.length)})
              </Button>
            )}
            {selectedApproved.length > 0 && (
              <Button variant="secondary" size="sm" icon="layers" onClick={() => setPending({ kind: 'batch', ids: selectedApproved })}>
                {t('createBatch')} ({formatNumber(selectedApproved.length)})
              </Button>
            )}
            <Button variant="ghost" size="sm" className="text-white hover:bg-white/10" onClick={() => setSelection({ key: queryKey, ids: new Set() })}>
              {t('clearSelection')}
            </Button>
          </div>
        </div>
      )}

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('noPayouts')} />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <ConfirmModal
        open={pending?.kind === 'approve'}
        title={t('approvePayoutTitle')}
        description={pending?.kind === 'approve' ? `${payoutLabel(pending.payout)} — ${t('approvePayoutCopy')}` : undefined}
        confirmLabel={t('approve')}
        confirmVariant="brand"
        onClose={() => setPending(null)}
        onConfirm={() => (pending?.kind === 'approve' ? approve(pending.payout) : Promise.resolve())}
      />
      <ReasonModal
        open={pending?.kind === 'reject'}
        title={t('rejectPayoutTitle')}
        description={pending?.kind === 'reject' ? `${payoutLabel(pending.payout)} — ${t('rejectPayoutCopy')}` : undefined}
        confirmLabel={t('reject')}
        onClose={() => setPending(null)}
        onConfirm={(reason) => (pending?.kind === 'reject' ? reject(pending.payout, reason) : Promise.resolve())}
      />
      <MarkPaidModal
        open={pending?.kind === 'paid'}
        title={t('markPaidTitle')}
        description={pending?.kind === 'paid' ? payoutLabel(pending.payout) : undefined}
        withPaidAt
        onClose={() => setPending(null)}
        onSubmit={(reference, paidAt) => (pending?.kind === 'paid' ? markPaid(pending.payout, reference, paidAt) : Promise.resolve())}
      />
      <ConfirmModal
        open={pending?.kind === 'bulk-approve'}
        title={t('approveSelected')}
        description={pending?.kind === 'bulk-approve' ? `${formatNumber(pending.ids.length)} — ${t('bulkApproveCopy')}` : undefined}
        confirmLabel={t('approve')}
        confirmVariant="brand"
        onClose={() => setPending(null)}
        onConfirm={() => (pending?.kind === 'bulk-approve' ? bulkApprove(pending.ids) : Promise.resolve())}
      />
      <ConfirmModal
        open={pending?.kind === 'batch' || pending?.kind === 'batch-all'}
        title={t('createBatch')}
        description={pending?.kind === 'batch' ? `${formatNumber(pending.ids.length)} — ${t('createBatchCopy')}` : t('batchAllApprovedCopy')}
        confirmLabel={t('createBatch')}
        confirmVariant="primary"
        onClose={() => setPending(null)}
        onConfirm={() => (pending?.kind === 'batch' ? createBatch({ payoutIds: pending.ids }) : createBatch({ allApproved: true }))}
      />
    </>
  )
}
