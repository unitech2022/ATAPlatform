import { useState } from 'react'
import { useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { settlements } from '../lib/admin'
import { periodLabel } from '../lib/finance'
import { formatNumber } from '../lib/format'
import { fromLocalInput, toLocalInput } from '../lib/pricing'
import { settlementBatchStatusMeta } from '../lib/status'
import type { SettlementBatch } from '../lib/types'

const PAGE_SIZE = 20

export function SettlementsPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const { page, setPage } = useUrlState()
  const query = useQuery(() => settlements.batches({ page, pageSize: PAGE_SIZE }), `settlement-batches:${page}`)
  const [generateOpen, setGenerateOpen] = useState(false)

  const columns: Column<SettlementBatch>[] = [
    { key: 'number', header: t('batchNumber'), render: (row) => <span className="ltr-nums font-bold">{row.batchNumber}</span> },
    { key: 'period', header: t('period'), render: (row) => <span className="whitespace-nowrap">{periodLabel(row, lang)}</span> },
    { key: 'city', header: t('city'), render: (row) => row.cityName ?? (row.cityId ? <span className="ltr-nums text-xs">{row.cityId}</span> : t('allCities')) },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={settlementBatchStatusMeta} value={row.status} /> },
    { key: 'drivers', header: t('drivers'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.driversCount)}</span> },
    { key: 'trips', header: t('tripsCount'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.totalTrips)}</span> },
    { key: 'commission', header: t('commission'), className: 'text-end', render: (row) => <Money value={row.totalCommission} /> },
    { key: 'net', header: t('netAmount'), className: 'text-end', render: (row) => <Money value={row.totalNet} strong signed /> },
  ]

  return (
    <>
      <PageHeader
        title={t('settlementsTitle')}
        description={t('settlementsCopy')}
        actions={
          <Button icon="plus" onClick={() => setGenerateOpen(true)}>
            {t('generateSettlement')}
          </Button>
        }
      />
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
              onRowClick={(row) => navigate(`/settlements/${row.id}`)}
              emptyTitle={t('noSettlements')}
              emptyDescription={t('noSettlementsCopy')}
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
      <GenerateModal
        open={generateOpen}
        onClose={() => setGenerateOpen(false)}
        onCreated={(batch) => {
          setGenerateOpen(false)
          navigate(`/settlements/${batch.id}`)
        }}
      />
    </>
  )
}

/** Last full week, Sunday → Sunday (docs/08 §F11.4 default period). */
function lastWeek() {
  const today = new Date()
  const end = new Date(today.getFullYear(), today.getMonth(), today.getDate() - today.getDay())
  const start = new Date(end)
  start.setDate(start.getDate() - 7)
  const lastDay = new Date(end)
  lastDay.setDate(lastDay.getDate() - 1)
  return { from: toLocalInput(start.toISOString()).slice(0, 10), to: toLocalInput(lastDay.toISOString()).slice(0, 10) }
}

function GenerateModal({ open, onClose, onCreated }: { open: boolean; onClose: () => void; onCreated: (batch: SettlementBatch) => void }) {
  return open ? <GenerateDialog onClose={onClose} onCreated={onCreated} /> : null
}

function GenerateDialog({ onClose, onCreated }: { onClose: () => void; onCreated: (batch: SettlementBatch) => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const initial = lastWeek()
  const [from, setFrom] = useState(initial.from)
  const [to, setTo] = useState(initial.to)
  const [cityId, setCityId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    if (!from || !to || to < from) {
      setError(t('endAfterStart'))
      return
    }
    // The inclusive "to" day becomes the exclusive end at the next local midnight.
    const endDay = new Date(`${to}T00:00`)
    endDay.setDate(endDay.getDate() + 1)
    const periodStart = fromLocalInput(`${from}T00:00`)
    const periodEnd = endDay.toISOString()
    if (!periodStart) {
      setError(t('endAfterStart'))
      return
    }
    setSubmitting(true)
    try {
      const batch = await settlements.generate({ periodStart, periodEnd, cityId: cityId.trim() || undefined })
      toast.success(t('settlementGenerating'), batch.batchNumber)
      onCreated(batch)
    } catch (caught) {
      setError(describe(caught))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={t('generateSettlement')}
      description={t('generateSettlementCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button icon="plus" onClick={submit} loading={submitting}>
            {t('generate')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4 sm:grid-cols-2">
        <Input id="period-from" type="date" dir="ltr" label={t('fromDate')} value={from} max={to || undefined} onChange={(event) => setFrom(event.target.value)} />
        <Input id="period-to" type="date" dir="ltr" label={t('toDateInclusive')} value={to} min={from || undefined} onChange={(event) => setTo(event.target.value)} />
        <Input
          id="city-id"
          dir="ltr"
          label={t('cityIdOptional')}
          hint={t('cityIdHint')}
          value={cityId}
          onChange={(event) => setCityId(event.target.value)}
          wrapperClassName="sm:col-span-2"
        />
        {error && (
          <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger sm:col-span-2">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>{error}</p>
          </div>
        )}
      </div>
    </Modal>
  )
}
