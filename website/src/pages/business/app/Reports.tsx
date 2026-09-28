import { useState } from 'react'
import { BarChart } from '../../../components/business/BarChart'
import { PageHeader, StatCard, TableWrap, Td, Th } from '../../../components/business/ui'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Field, Input, Select } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { Pagination } from '../../../components/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { corporateApi, downloadFile, type ReportTripsQuery } from '../../../lib/api'
import { describeError } from '../../../lib/errors'
import { formatDate, formatMoney, formatNumber, isoDateOffset, monthStartIso } from '../../../lib/format'
import type { ReportGroupBy } from '../../../lib/types'
import { useDebounced } from '../../../lib/useDebounced'
import { useResource } from '../../../lib/useResource'
import { TripStatusPill } from '../../../components/business/StatusPills'

const GROUPS: ReportGroupBy[] = ['employee', 'department', 'cost_center', 'month', 'category']

export function Reports() {
  const { t, lang } = useI18n()
  const [from, setFrom] = useState(monthStartIso)
  const [to, setTo] = useState(() => isoDateOffset(0))
  const [groupBy, setGroupBy] = useState<ReportGroupBy>('employee')
  const [department, setDepartment] = useState('')
  const [costCenterId, setCostCenterId] = useState('')
  const [page, setPage] = useState(1)
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)
  const debouncedDepartment = useDebounced(department.trim())

  const costCenters = useResource(() => corporateApi.costCenters(), [lang])
  const summary = useResource(() => corporateApi.reportSummary({ from, to, groupBy }), [from, to, groupBy, lang])
  const tripsQuery: ReportTripsQuery = {
    from,
    to,
    department: debouncedDepartment || undefined,
    costCenterId: costCenterId || undefined,
  }
  const trips = useResource(() => corporateApi.reportTrips({ ...tripsQuery, page }), [from, to, debouncedDepartment, costCenterId, page, lang])

  const exportCsv = async () => {
    setExporting(true)
    setExportError(null)
    try {
      await downloadFile(corporateApi.reportTripsCsvPath(tripsQuery), `ata-trips-${from}-${to}.csv`)
    } catch (caught) {
      setExportError(describeError(caught, t))
    } finally {
      setExporting(false)
    }
  }

  const money = (value: number) => formatMoney(value, lang)
  const data = summary.data

  return (
    <>
      <title>{t('biz.nav.reports')} · ATA</title>
      <PageHeader
        title={t('biz.reports.title')}
        subtitle={t('biz.reports.subtitle')}
        actions={
          <Button size="sm" variant="secondary" onClick={() => void exportCsv()} disabled={exporting}>
            <Icon name="download" className="size-4" />
            {exporting ? t('biz.downloading') : t('biz.reports.export')}
          </Button>
        }
      />

      <div className="mb-6 grid gap-3 sm:grid-cols-3">
        <Field label={t('biz.from')} htmlFor="report-from">
          <Input
            id="report-from"
            type="date"
            value={from}
            max={to}
            onChange={(event) => {
              setFrom(event.target.value)
              setPage(1)
            }}
          />
        </Field>
        <Field label={t('biz.to')} htmlFor="report-to">
          <Input
            id="report-to"
            type="date"
            value={to}
            min={from}
            onChange={(event) => {
              setTo(event.target.value)
              setPage(1)
            }}
          />
        </Field>
        <Field label={t('biz.reports.groupBy')} htmlFor="report-group">
          <Select id="report-group" value={groupBy} onChange={(event) => setGroupBy(event.target.value as ReportGroupBy)}>
            {GROUPS.map((group) => (
              <option key={group} value={group}>
                {t(`biz.reports.group.${group}`)}
              </option>
            ))}
          </Select>
        </Field>
      </div>
      {exportError && (
        <Notice tone="error" className="mb-4">
          {exportError}
        </Notice>
      )}

      {summary.loading && !data ? (
        <LoadingState />
      ) : summary.error && !data ? (
        <ErrorState error={summary.error} onRetry={() => summary.reload()} />
      ) : data ? (
        <>
          {summary.error !== null && <ErrorState error={summary.error} className="mb-4" onRetry={() => summary.reload()} />}
          <div className="grid gap-4 sm:grid-cols-3">
            <StatCard icon="car" label={t('biz.reports.trips')} value={formatNumber(data.totals.trips, 0)} />
            <StatCard icon="wallet" label={t('biz.reports.amount')} value={money(data.totals.amount)} />
            <StatCard
              icon="chart"
              label={t('biz.reports.avgFare')}
              value={data.totals.trips > 0 ? money(data.totals.amount / data.totals.trips) : '—'}
            />
          </div>
          {data.rows.length > 0 ? (
            <div className="mt-4 grid gap-4 xl:grid-cols-2">
              <Card>
                <h2 className="mb-1 font-bold">{t('biz.reports.chartTitle', { group: t(`biz.reports.group.${groupBy}`) })}</h2>
                <p className="mb-4 text-xs text-muted">{t('biz.reports.chartHint')}</p>
                <BarChart
                  title={t('biz.reports.chartTitle', { group: t(`biz.reports.group.${groupBy}`) })}
                  data={data.rows.map((row) => ({
                    key: row.key,
                    label: row.label,
                    value: row.amount,
                    display: money(row.amount),
                    details: [`${t('biz.reports.trips')}: ${formatNumber(row.trips, 0)}`, `${t('biz.reports.avgFare')}: ${money(row.avgFare)}`],
                  }))}
                />
              </Card>
              <TableWrap>
                <thead>
                  <tr>
                    <Th>{t(`biz.reports.group.${groupBy}`)}</Th>
                    <Th>{t('biz.reports.trips')}</Th>
                    <Th>{t('biz.reports.amount')}</Th>
                    <Th>{t('biz.reports.avgFare')}</Th>
                  </tr>
                </thead>
                <tbody>
                  {data.rows.map((row) => (
                    <tr key={row.key}>
                      <Td className="font-bold">{row.label}</Td>
                      <Td>{formatNumber(row.trips, 0)}</Td>
                      <Td className="whitespace-nowrap">{money(row.amount)}</Td>
                      <Td className="whitespace-nowrap">{money(row.avgFare)}</Td>
                    </tr>
                  ))}
                </tbody>
              </TableWrap>
            </div>
          ) : (
            <div className="mt-4">
              <EmptyState icon="chart" title={t('biz.reports.empty')} />
            </div>
          )}
        </>
      ) : null}

      <section className="mt-10">
        <h2 className="mb-4 text-xl font-bold">{t('biz.reports.tripsTitle')}</h2>
        <div className="mb-4 grid gap-3 sm:grid-cols-2">
          <Input
            aria-label={t('biz.employee.department')}
            placeholder={t('biz.employee.department')}
            value={department}
            onChange={(event) => {
              setDepartment(event.target.value)
              setPage(1)
            }}
          />
          <Select
            aria-label={t('biz.employee.costCenter')}
            value={costCenterId}
            onChange={(event) => {
              setCostCenterId(event.target.value)
              setPage(1)
            }}
          >
            <option value="">{t('biz.allCostCenters')}</option>
            {(costCenters.data ?? []).map((item) => (
              <option key={item.id} value={item.id}>
                {item.code} · {item.name}
              </option>
            ))}
          </Select>
        </div>
        {trips.loading && !trips.data ? (
          <LoadingState />
        ) : trips.error && !trips.data ? (
          <ErrorState error={trips.error} onRetry={() => trips.reload()} />
        ) : trips.data && trips.data.items.length > 0 ? (
          <>
            <TableWrap>
              <thead>
                <tr>
                  <Th>{t('biz.trip.number')}</Th>
                  <Th>{t('biz.trip.time')}</Th>
                  <Th>{t('biz.trip.rider')}</Th>
                  <Th>{t('biz.employee.department')}</Th>
                  <Th>{t('biz.book.purpose')}</Th>
                  <Th>{t('biz.trip.route')}</Th>
                  <Th>{t('biz.invoice.inclVat')}</Th>
                  <Th>{t('biz.trip.status')}</Th>
                </tr>
              </thead>
              <tbody>
                {trips.data.items.map((row) => (
                  <tr key={row.id}>
                    <Td className="font-bold">
                      <span dir="ltr">{row.tripNumber}</span>
                    </Td>
                    <Td className="whitespace-nowrap text-muted">{formatDate(row.date, lang)}</Td>
                    <Td>{row.guest ? `${row.guest} (${t('biz.trip.guest')})` : (row.employee ?? '—')}</Td>
                    <Td>
                      {row.department ?? '—'}
                      {row.costCenter && <span className="block text-xs text-muted">{row.costCenter}</span>}
                    </Td>
                    <Td className="max-w-40 truncate">{row.purpose ?? '—'}</Td>
                    <Td className="max-w-56">
                      <span className="block truncate">{row.pickup}</span>
                      <span className="block truncate text-xs text-muted">
                        {lang === 'ar' ? '←' : '→'} {row.dropoff}
                      </span>
                    </Td>
                    <Td className="whitespace-nowrap font-bold">{money(row.amountInclVat)}</Td>
                    <Td>
                      <TripStatusPill status={row.status} />
                    </Td>
                  </tr>
                ))}
              </tbody>
            </TableWrap>
            <Pagination className="mt-4" page={trips.data.page} pageSize={trips.data.pageSize} total={trips.data.total} onChange={setPage} />
          </>
        ) : (
          <EmptyState icon="car" title={t('biz.reports.noTrips')} />
        )}
      </section>
    </>
  )
}
