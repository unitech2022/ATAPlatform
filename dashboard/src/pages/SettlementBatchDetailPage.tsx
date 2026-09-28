import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { SearchInput } from '../components/Field'
import { Icon } from '../components/Icon'
import { Money } from '../components/Money'
import { Pagination } from '../components/Pagination'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import type { TranslationKey } from '../i18n'
import { settlements } from '../lib/admin'
import { cashDebtOf, parseEnum, periodLabel, saveBlob, SETTLEMENT_DIRECTIONS } from '../lib/finance'
import { formatDateTime, formatMoney, formatNumber } from '../lib/format'
import { settlementBatchStatusMeta, settlementDirectionMeta } from '../lib/status'
import type { Settlement, SettlementBatch, SettlementDirection } from '../lib/types'

const PAGE_SIZE = 25
const POLL_MS = 3000

type Pending = 'finalize' | 'regenerate' | null

export function SettlementBatchDetailPage() {
  const { batchId = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => settlements.batch(batchId), `settlement-batch:${batchId}`)
  const [pending, setPending] = useState<Pending>(null)
  const [exporting, setExporting] = useState(false)
  const { reload } = query
  const generating = query.data?.status === 'generating'

  // Generation runs in the background (202); poll until the batch leaves `generating`.
  useEffect(() => {
    if (!generating) return
    const timer = window.setTimeout(reload, POLL_MS)
    return () => window.clearTimeout(timer)
  }, [generating, reload, query.data])

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
      const { blob, fileName } = await settlements.exportCsv(batch.id)
      saveBlob(blob, fileName ?? `${batch.batchNumber}.csv`)
      toast.success(t('exported'))
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setExporting(false)
    }
  }

  const run = async (action: Exclude<Pending, null>) => {
    try {
      if (action === 'finalize') await settlements.finalize(batch.id)
      else await settlements.regenerate(batch.id)
      toast.success(t(action === 'finalize' ? 'settlementFinalized' : 'settlementGenerating'), batch.batchNumber)
      setPending(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const totals: { label: TranslationKey; value: number; money?: boolean; signed?: boolean }[] = [
    { label: 'drivers', value: batch.driversCount },
    { label: 'tripsCount', value: batch.totalTrips },
    { label: 'grossFares', value: batch.totalGrossFares, money: true },
    { label: 'earnings', value: batch.totalEarnings, money: true },
    { label: 'commission', value: batch.totalCommission, money: true },
    { label: 'cashCollected', value: batch.totalCashCollected, money: true },
    { label: 'incentives', value: batch.totalIncentives, money: true },
    { label: 'compensation', value: batch.totalCompensation, money: true },
    { label: 'adjustmentsTotal', value: batch.totalAdjustments, money: true, signed: true },
    { label: 'netAmount', value: batch.totalNet, money: true, signed: true },
  ]

  return (
    <>
      <Link to="/settlements" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('backToSettlements')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="document" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-brand">{periodLabel(batch, lang)}</p>
              <h2 className="ltr-nums text-2xl font-bold leading-tight">{batch.batchNumber}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={settlementBatchStatusMeta} value={batch.status} />
                <span>{batch.cityName ?? (batch.cityId ? batch.cityId : t('allCities'))}</span>
                {batch.generatedAt && (
                  <span>
                    {t('generatedAt')}: {formatDateTime(batch.generatedAt, lang)}
                    {batch.generatedByName ? ` · ${batch.generatedByName}` : ''}
                  </span>
                )}
              </div>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="download" loading={exporting} disabled={generating} onClick={exportCsv}>
              {t('exportCsv')}
            </Button>
            {(batch.status === 'ready' || batch.status === 'failed') && (
              <Button variant="secondary" icon="refresh" onClick={() => setPending('regenerate')}>
                {t('regenerate')}
              </Button>
            )}
            {batch.status === 'ready' && (
              <Button variant="brand" icon="check" onClick={() => setPending('finalize')}>
                {t('finalize')}
              </Button>
            )}
          </div>
        </div>

        {generating && (
          <div className="mt-5 flex items-center gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
            <Icon name="clock" className="size-4 shrink-0" />
            <p>{t('settlementGeneratingCopy')}</p>
          </div>
        )}
        {batch.status === 'failed' && batch.error && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>{batch.error}</p>
          </div>
        )}
        {batch.status === 'finalized' && (
          <div className="mt-5 flex items-center gap-3 rounded-2xl bg-brand-soft p-4 text-sm text-brand">
            <Icon name="shield" className="size-4 shrink-0" />
            <p>
              {t('settlementLocked')}
              {batch.finalizedAt ? ` — ${formatDateTime(batch.finalizedAt, lang)}` : ''}
              {batch.finalizedByName ? ` · ${batch.finalizedByName}` : ''}
            </p>
          </div>
        )}
      </Card>

      <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        {totals.map((item) => (
          <div key={item.label} className="min-w-0 rounded-2xl bg-white p-4 shadow-soft">
            <p className="truncate text-xs font-bold text-muted">{t(item.label)}</p>
            <p className="mt-1 truncate text-lg font-bold">
              {item.money ? <Money value={item.value} signed={item.signed} /> : <span className="ltr-nums">{formatNumber(item.value)}</span>}
            </p>
          </div>
        ))}
      </div>

      {!generating && <SettlementLines batch={batch} />}

      <ConfirmModal
        open={pending === 'finalize'}
        title={t('finalizeTitle')}
        description={`${batch.batchNumber} — ${t('finalizeCopy')}`}
        confirmLabel={t('finalize')}
        confirmVariant="brand"
        onClose={() => setPending(null)}
        onConfirm={() => run('finalize')}
      />
      <ConfirmModal
        open={pending === 'regenerate'}
        title={t('regenerateTitle')}
        description={`${batch.batchNumber} — ${t('regenerateCopy')}`}
        confirmLabel={t('regenerate')}
        confirmVariant="primary"
        onClose={() => setPending(null)}
        onConfirm={() => run('regenerate')}
      />
    </>
  )
}

function SettlementLines({ batch }: { batch: SettlementBatch }) {
  const { t } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const direction = parseEnum(params.get('direction'), SETTLEMENT_DIRECTIONS)
  const [expanded, setExpanded] = useState<string | null>(null)
  const query = useQuery(
    () => settlements.lines(batch.id, { direction, search: search.value, page, pageSize: PAGE_SIZE }),
    `settlements:${batch.id}:${batch.status}:${direction}:${search.value}:${page}`,
  )

  const columns: Column<Settlement>[] = [
    {
      key: 'driver',
      header: t('driver'),
      render: (row) => (
        <span className="block min-w-0">
          <Link to={`/drivers/${row.driverId}`} className="block truncate font-bold text-brand hover:underline" onClick={(event) => event.stopPropagation()}>
            {row.driverName || t('unnamed')}
          </Link>
          {row.driverPhone && <span className="ltr-nums block text-xs text-muted">{row.driverPhone}</span>}
        </span>
      ),
    },
    { key: 'trips', header: t('tripsCount'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.tripsCount)}</span> },
    { key: 'earnings', header: t('earnings'), className: 'text-end', render: (row) => <Money value={row.earnings} /> },
    { key: 'commission', header: t('commission'), className: 'text-end', render: (row) => <Money value={row.commission} /> },
    { key: 'cash', header: t('cashCollected'), className: 'text-end', render: (row) => <Money value={row.cashCollected} /> },
    { key: 'incentives', header: t('incentives'), className: 'text-end', render: (row) => <Money value={row.incentives} /> },
    { key: 'net', header: t('netAmount'), className: 'text-end', render: (row) => <Money value={row.netAmount} signed strong /> },
    {
      key: 'debt',
      header: t('debt'),
      className: 'text-end',
      render: (row) => {
        const debt = cashDebtOf(row.closingBalance)
        return debt > 0 ? <Money value={debt} className="text-danger" strong /> : <span className="text-muted">—</span>
      },
    },
    { key: 'direction', header: t('direction'), render: (row) => <MetaBadge record={settlementDirectionMeta} value={row.direction} /> },
  ]

  return (
    <Card
      title={t('settlementLines')}
      flush
      action={
        <Tabs
          value={direction}
          onChange={(value) => setFilter('direction', value)}
          options={[
            { value: '' as SettlementDirection | '', label: t('statusAll') },
            ...SETTLEMENT_DIRECTIONS.map((value) => ({ value, label: t(settlementDirectionMeta[value].key) })),
          ]}
        />
      }
    >
      <div className="px-5 pb-4 sm:px-6">
        <SearchInput placeholder={t('searchDrivers')} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
      </div>
      {query.error ? (
        <ErrorState error={query.error} onRetry={query.reload} />
      ) : (
        <>
          <Table
            columns={columns}
            rows={query.data?.items ?? []}
            rowKey={(row) => row.id}
            loading={query.loading}
            onRowClick={(row) => setExpanded((current) => (current === row.id ? null : row.id))}
            renderExpanded={(row) => (expanded === row.id ? <SettlementBreakdown line={row} /> : null)}
            emptyTitle={t('noSettlementLines')}
          />
          {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
        </>
      )}
    </Card>
  )
}

/** Full statement of one driver, with the invariant from §F11.4 shown as a check. */
function SettlementBreakdown({ line }: { line: Settlement }) {
  const { t } = useLang()
  const expected = line.openingBalance + line.netAmount + line.topups - line.fees - line.payoutsInPeriod
  const consistent = Math.abs(expected - line.closingBalance) < 0.01
  const money = (value: number) => `${formatMoney(value)} ${t('sar')}`
  return (
    <div className="space-y-3">
      <DefinitionList
        items={[
          { label: t('openingBalance'), value: money(line.openingBalance), ltr: true },
          { label: t('grossFares'), value: money(line.grossFares), ltr: true },
          { label: t('earnings'), value: money(line.earnings), ltr: true },
          { label: t('commission'), value: money(line.commission), ltr: true },
          { label: t('cashCollected'), value: money(line.cashCollected), ltr: true },
          { label: t('incentives'), value: money(line.incentives), ltr: true },
          { label: t('compensation'), value: money(line.cancellationCompensation), ltr: true },
          { label: t('adjustmentsTotal'), value: money(line.adjustments), ltr: true },
          { label: t('feesCharged'), value: money(line.fees), ltr: true },
          { label: t('topups'), value: money(line.topups), ltr: true },
          { label: t('payoutsInPeriod'), value: money(line.payoutsInPeriod), ltr: true },
          { label: t('netAmount'), value: money(line.netAmount), ltr: true },
          { label: t('closingBalance'), value: money(line.closingBalance), ltr: true },
        ]}
      />
      <Badge tone={consistent ? 'brand' : 'danger'}>{consistent ? t('settlementConsistent') : t('settlementInconsistent')}</Badge>
    </div>
  )
}
