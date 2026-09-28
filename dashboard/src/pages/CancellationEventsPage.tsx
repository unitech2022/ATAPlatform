import { Link } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { CancellationKpis } from '../components/CancellationKpis'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput, Select } from '../components/Field'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { cancellations } from '../lib/admin'
import { ACTOR_KEY, AT_FAULT_KEY, AT_FAULTS, CANCELLATION_STAGES, EVENT_ACTORS, EXCUSE_STATUSES, FEE_STATUSES, STAGE_KEY } from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { excuseStatusMeta, feeStatusMeta } from '../lib/status'
import type { CancellationEvent } from '../lib/types'

const PAGE_SIZE = 20

/** Cancellation log (`/admin/cancellations`) with the §F14.7 KPIs for the same date range. */
export function CancellationEventsPage() {
  const { t, lang } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const actor = parseEnum(params.get('actor'), EVENT_ACTORS)
  const stage = parseEnum(params.get('stage'), CANCELLATION_STAGES)
  const atFault = parseEnum(params.get('atFault'), AT_FAULTS)
  const feeStatus = parseEnum(params.get('feeStatus'), FEE_STATUSES)
  const excuseStatus = parseEnum(params.get('excuseStatus'), EXCUSE_STATUSES)
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))

  const stats = useQuery(() => cancellations.stats({ from, to }), `cancellation-stats:${from}:${to}`)
  const query = useQuery(
    () => cancellations.list({ actor, stage, atFault, feeStatus, excuseStatus, from, to, search: search.value, page, pageSize: PAGE_SIZE }),
    `cancellations:${actor}:${stage}:${atFault}:${feeStatus}:${excuseStatus}:${from}:${to}:${search.value}:${page}`,
  )

  const columns: Column<CancellationEvent>[] = [
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
      key: 'actor',
      header: t('actor'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.userName || t(ACTOR_KEY[row.actor] ?? 'actorSystem')}</span>
          <span className="block text-xs text-muted">
            {t(ACTOR_KEY[row.actor] ?? 'actorSystem')} · {t('cxAtFault')}: {t(AT_FAULT_KEY[row.atFault] ?? 'cxFaultNone')}
          </span>
        </span>
      ),
    },
    { key: 'stage', header: t('cxStage'), render: (row) => t(STAGE_KEY[row.stage] ?? 'cxStageBeforeAccept') },
    { key: 'reason', header: t('reason'), render: (row) => <span className="block max-w-56 truncate">{row.reasonName ?? row.reasonCode}</span> },
    {
      key: 'fee',
      header: t('cxFee'),
      className: 'text-end',
      render: (row) => (
        <span className="flex flex-col items-end gap-1">
          <Money value={row.feeCharged || row.feeAmount} strong />
          <MetaBadge record={feeStatusMeta} value={row.feeStatus} />
        </span>
      ),
    },
    { key: 'compensation', header: t('cxCompensation'), className: 'text-end', render: (row) => (row.compensationAmount > 0 ? <Money value={row.compensationAmount} /> : <span className="text-muted">—</span>) },
    { key: 'points', header: t('cxPoints'), className: 'text-center', render: (row) => <span className="ltr-nums font-bold">{formatNumber(row.penaltyPoints)}</span> },
    { key: 'excuse', header: t('cxExcuse'), render: (row) => <MetaBadge record={excuseStatusMeta} value={row.excuseStatus} /> },
  ]

  return (
    <>
      <PageHeader title={t('cxEventsTitle')} description={t('cxEventsCopy')} />

      <div className="mb-4 grid grid-cols-2 gap-3 sm:w-96">
        <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
      </div>

      <div className="mb-6">{stats.error ? <Card><ErrorState error={stats.error} onRetry={stats.reload} /></Card> : <CancellationKpis stats={stats.data} loading={stats.loading} />}</div>

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-[1fr_auto_auto_auto_auto_auto]">
        <SearchInput placeholder={t('searchTrips')} value={search.input} onChange={(event) => search.setInput(event.target.value)} wrapperClassName="sm:col-span-2 lg:col-span-3 xl:col-span-1" />
        <Select id="ev-actor" aria-label={t('actor')} value={actor} onChange={(event) => setFilter('actor', event.target.value)} wrapperClassName="xl:w-36">
          <option value="">{t('cxAllActors')}</option>
          {EVENT_ACTORS.map((value) => (
            <option key={value} value={value}>
              {t(ACTOR_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="ev-stage" aria-label={t('cxStage')} value={stage} onChange={(event) => setFilter('stage', event.target.value)} wrapperClassName="xl:w-40">
          <option value="">{t('cxAllStages')}</option>
          {CANCELLATION_STAGES.map((value) => (
            <option key={value} value={value}>
              {t(STAGE_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="ev-fault" aria-label={t('cxAtFault')} value={atFault} onChange={(event) => setFilter('atFault', event.target.value)} wrapperClassName="xl:w-36">
          <option value="">{t('cxAnyFault')}</option>
          {AT_FAULTS.map((value) => (
            <option key={value} value={value}>
              {t(AT_FAULT_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="ev-fee" aria-label={t('cxFeeStatus')} value={feeStatus} onChange={(event) => setFilter('feeStatus', event.target.value)} wrapperClassName="xl:w-40">
          <option value="">{t('cxAnyFeeStatus')}</option>
          {FEE_STATUSES.map((value) => (
            <option key={value} value={value}>
              {t(feeStatusMeta[value].key)}
            </option>
          ))}
        </Select>
        <Select id="ev-excuse" aria-label={t('cxExcuse')} value={excuseStatus} onChange={(event) => setFilter('excuseStatus', event.target.value)} wrapperClassName="xl:w-40">
          <option value="">{t('cxAnyExcuse')}</option>
          {EXCUSE_STATUSES.map((value) => (
            <option key={value} value={value}>
              {t(excuseStatusMeta[value].key)}
            </option>
          ))}
        </Select>
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('cxNoEvents')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}
