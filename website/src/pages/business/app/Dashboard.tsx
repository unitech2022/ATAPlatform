import { Link, useNavigate } from 'react-router'
import { TripStatusPill } from '../../../components/business/StatusPills'
import { PageHeader, StatCard, TableWrap, Td, Th } from '../../../components/business/ui'
import { Icon } from '../../../components/Icon'
import { EmptyState, ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { corporateApi } from '../../../lib/api'
import { formatDateTime, formatMoney, formatNumber } from '../../../lib/format'
import type { CorporateTripSummary } from '../../../lib/types'
import { useResource } from '../../../lib/useResource'

export function TripsTable({ trips }: { trips: CorporateTripSummary[] }) {
  const { t, lang } = useI18n()
  const navigate = useNavigate()
  return (
    <TableWrap>
      <thead>
        <tr>
          <Th>{t('biz.trip.number')}</Th>
          <Th>{t('biz.trip.rider')}</Th>
          <Th>{t('biz.trip.route')}</Th>
          <Th>{t('biz.trip.time')}</Th>
          <Th>{t('biz.trip.amount')}</Th>
          <Th>{t('biz.trip.status')}</Th>
        </tr>
      </thead>
      <tbody>
        {trips.map((trip) => {
          const time = trip.completedAt ?? trip.scheduledAt ?? trip.requestedAt
          return (
            <tr
              key={trip.id}
              className="cursor-pointer hover:bg-cloud"
              onClick={() => navigate(`/business/app/bookings/${trip.id}`)}
            >
              <Td>
                <Link to={`/business/app/bookings/${trip.id}`} className="font-bold text-brand" dir="ltr" onClick={(event) => event.stopPropagation()}>
                  {trip.tripNumber}
                </Link>
              </Td>
              <Td>
                <span className="font-bold">{trip.isGuest ? trip.guestName : trip.employeeName}</span>
                {trip.isGuest && <span className="ms-2 rounded-full bg-cloud px-2 py-0.5 text-xs font-bold text-muted">{t('biz.trip.guest')}</span>}
              </Td>
              <Td className="max-w-64">
                <span className="block truncate">{trip.pickupName}</span>
                <span className="block truncate text-xs text-muted">{lang === 'ar' ? '←' : '→'} {trip.dropoffName}</span>
              </Td>
              <Td className="whitespace-nowrap text-muted">{time ? formatDateTime(time, lang) : '—'}</Td>
              <Td className="whitespace-nowrap font-bold">{formatMoney(trip.amount, lang)}</Td>
              <Td>
                <TripStatusPill status={trip.status} />
              </Td>
            </tr>
          )
        })}
      </tbody>
    </TableWrap>
  )
}

export function Dashboard() {
  const { t, lang } = useI18n()
  const dashboard = useResource(() => corporateApi.dashboard(), [lang])
  const data = dashboard.data

  return (
    <>
      <title>{t('biz.nav.dashboard')} · ATA</title>
      <PageHeader
        title={t('biz.dashboard.title')}
        subtitle={t('biz.dashboard.subtitle')}
        actions={
          <>
            <Link
              to="/business/app/employees?invite=1"
              className="inline-flex items-center gap-2 rounded-2xl border border-line bg-white px-4 py-2.5 text-sm font-bold hover:bg-cloud"
            >
              <Icon name="users" className="size-4" />
              {t('biz.employees.invite')}
            </Link>
            <Link
              to="/business/app/bookings/new"
              className="inline-flex items-center gap-2 rounded-2xl bg-ink px-4 py-2.5 text-sm font-bold text-white shadow-button hover:bg-ink-soft"
            >
              <Icon name="plus" className="size-4" />
              {t('biz.bookings.new')}
            </Link>
          </>
        }
      />
      {dashboard.loading && !data ? (
        <LoadingState />
      ) : dashboard.error && !data ? (
        <ErrorState error={dashboard.error} onRetry={() => dashboard.reload()} />
      ) : data ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <StatCard icon="car" label={t('biz.dashboard.mtdTrips')} value={formatNumber(data.monthToDate.trips, 0)} />
            <StatCard icon="wallet" label={t('biz.dashboard.mtdSpend')} value={formatMoney(data.monthToDate.spend, lang)} />
            <StatCard
              icon="users"
              label={t('biz.dashboard.activeEmployees')}
              value={formatNumber(data.activeEmployees, 0)}
              hint={t('biz.dashboard.invited', { n: data.invitedEmployees })}
            />
            <StatCard
              icon="receipt"
              label={t('biz.dashboard.openInvoices')}
              value={formatMoney(data.openInvoices.amount, lang)}
              hint={t('biz.dashboard.openInvoicesCount', { n: data.openInvoices.count })}
            />
          </div>
          <div className="mt-4 grid gap-4 lg:grid-cols-2">
            <StatCard
              icon="shield"
              label={t('biz.dashboard.credit')}
              value={`${formatMoney(data.creditUsed, lang)} / ${formatMoney(data.creditLimit, lang)}`}
            >
              <Meter value={data.creditLimit > 0 ? (data.creditUsed / data.creditLimit) * 100 : 0} label={t('biz.dashboard.credit')} />
            </StatCard>
            <StatCard
              icon="chart"
              label={t('biz.dashboard.budget')}
              value={data.budgetUtilizationPercent === null ? '—' : `${formatNumber(data.budgetUtilizationPercent, 1)}%`}
              hint={t('biz.dashboard.budgetHint')}
            >
              {data.budgetUtilizationPercent !== null && <Meter value={data.budgetUtilizationPercent} label={t('biz.dashboard.budget')} />}
            </StatCard>
          </div>
          <section className="mt-8">
            <div className="mb-4 flex items-center justify-between gap-3">
              <h2 className="text-xl font-bold">{t('biz.dashboard.recent')}</h2>
              <Link to="/business/app/bookings" className="text-sm font-bold text-brand">
                {t('biz.viewAll')}
              </Link>
            </div>
            {data.recentTrips.length > 0 ? <TripsTable trips={data.recentTrips} /> : <EmptyState icon="car" title={t('biz.bookings.empty')} />}
          </section>
        </>
      ) : null}
    </>
  )
}

function Meter({ value, label }: { value: number; label: string }) {
  const clamped = Math.max(0, Math.min(100, value))
  const tone = value >= 90 ? 'bg-danger' : value >= 75 ? 'bg-amber-500' : 'bg-brand'
  return (
    <div
      className="mt-4 h-2.5 overflow-hidden rounded-full bg-cloud"
      role="meter"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(clamped)}
    >
      <div className={`h-full rounded-full ${tone}`} style={{ width: `${clamped}%` }} />
    </div>
  )
}
