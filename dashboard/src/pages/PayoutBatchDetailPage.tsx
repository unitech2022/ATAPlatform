import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { MarkPaidModal } from '../components/MarkPaidModal'
import { Money } from '../components/Money'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { usePermission } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { payoutBatches } from '../lib/admin'
import { saveBlob } from '../lib/finance'
import { formatDateTime, formatMoney, formatNumber } from '../lib/format'
import { payoutBatchStatusMeta, payoutStatusMeta } from '../lib/status'
import type { Payout } from '../lib/types'

export function PayoutBatchDetailPage() {
  // F20: confirming a batch payment needs `payouts.approve`.
  const canApprove = usePermission('payouts.approve')
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => payoutBatches.get(id), `payout-batch:${id}`)
  const [exporting, setExporting] = useState(false)
  const [paidOpen, setPaidOpen] = useState(false)

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const batch = query.data

  const exportCsv = async () => {
    setExporting(true)
    try {
      const { blob, fileName } = await payoutBatches.exportCsv(batch.id)
      saveBlob(blob, fileName ?? `${batch.batchNumber}.csv`)
      toast.success(t('exported'))
      // Exporting moves an open batch to `exported` (audited server-side).
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setExporting(false)
    }
  }

  const markPaid = async (reference: string) => {
    await payoutBatches.markPaid(batch.id, reference)
    toast.success(t('batchPaid'), batch.batchNumber)
    setPaidOpen(false)
    query.reload()
  }

  const columns: Column<Payout>[] = [
    { key: 'number', header: t('payoutNumber'), render: (row) => <span className="ltr-nums font-bold">{row.payoutNumber}</span> },
    {
      key: 'driver',
      header: t('driver'),
      render: (row) =>
        row.driverId ? (
          <Link to={`/drivers/${row.driverId}`} className="font-bold text-brand hover:underline">
            {row.driverName || t('unnamed')}
          </Link>
        ) : (
          <span className="font-bold">{row.driverName || t('unnamed')}</span>
        ),
    },
    { key: 'iban', header: t('iban'), render: (row) => <span className="ltr-nums whitespace-nowrap">{row.ibanMasked ?? '—'}</span> },
    { key: 'amount', header: t('amount'), className: 'text-end', render: (row) => <Money value={row.amount} strong /> },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={payoutStatusMeta} value={row.status} /> },
  ]

  return (
    <>
      <Link to="/payout-batches" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('backToBatches')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="layers" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-brand">{t('batchNumber')}</p>
              <h2 className="ltr-nums text-2xl font-bold leading-tight">{batch.batchNumber}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={payoutBatchStatusMeta} value={batch.status} />
                <span>
                  <span className="ltr-nums">{formatNumber(batch.payoutsCount)}</span> {t('payoutsCount')} · <Money value={batch.totalAmount} strong />
                </span>
              </div>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="download" loading={exporting} onClick={exportCsv}>
              {t('exportCsv')}
            </Button>
            {canApprove && batch.status !== 'paid' && (
              <Button variant="brand" icon="check" onClick={() => setPaidOpen(true)}>
                {t('markBatchPaid')}
              </Button>
            )}
          </div>
        </div>
      </Card>

      <Card title={t('details')} className="mb-6">
        <DefinitionList
          items={[
            { label: t('createdAt'), value: `${formatDateTime(batch.createdAt, lang)}${batch.createdByName ? ` · ${batch.createdByName}` : ''}` },
            { label: t('exportedAt'), value: `${formatDateTime(batch.exportedAt, lang)}${batch.exportedByName ? ` · ${batch.exportedByName}` : ''}` },
            { label: t('paidAt'), value: `${formatDateTime(batch.paidAt, lang)}${batch.paidByName ? ` · ${batch.paidByName}` : ''}` },
            { label: t('bankReference'), value: batch.bankReference ?? '—', ltr: true },
          ]}
        />
      </Card>

      <Card title={t('payoutsTitle')} flush>
        <Table columns={columns} rows={batch.payouts ?? []} rowKey={(row) => row.id} emptyTitle={t('noPayouts')} emptyDescription="" />
      </Card>

      <MarkPaidModal
        open={paidOpen}
        title={t('markBatchPaid')}
        description={`${batch.batchNumber} — ${formatMoney(batch.totalAmount)} ${t('sar')} — ${t('markBatchPaidCopy')}`}
        onClose={() => setPaidOpen(false)}
        onSubmit={markPaid}
      />
    </>
  )
}
