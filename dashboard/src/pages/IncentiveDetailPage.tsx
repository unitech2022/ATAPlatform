import { useCallback, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { IncentiveFormModal } from '../components/IncentiveFormModal'
import { Money } from '../components/Money'
import { Pagination } from '../components/Pagination'
import { ReasonModal } from '../components/ReasonModal'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { UsageBar } from '../components/UsageBar'
import { ZonesMap } from '../components/ZonesMap'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { catalog, incentives, rideCategories, zones } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDate, formatDateTime, formatMoney, formatNumber } from '../lib/format'
import { localName, weekdayKey } from '../lib/pricing'
import { canVoidProgress, INCENTIVE_TYPE_KEY, incentiveStatusOf, PAYOUT_DELAY_HOURS, PROGRESS_STATUSES, usageShare } from '../lib/rewards'
import { driverTierMeta, incentiveProgressStatusMeta, incentiveStatusMeta } from '../lib/status'
import type { Incentive, IncentiveProgress, Zone } from '../lib/types'

const PAGE_SIZE = 20

/** Incentive detail (`/incentives/:id`): definition, zones map, driver progress with payout status and void. */
export function IncentiveDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => incentives.get(id), `incentive:${id}`)
  const [editing, setEditing] = useState(false)
  const [deactivating, setDeactivating] = useState(false)

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }
  const incentive = query.data

  const deactivate = async () => {
    try {
      await incentives.deactivate(incentive.id)
      toast.success(t('icDeactivated'))
      setDeactivating(false)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  return (
    <>
      <Link to="/incentives" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('icBack')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="target" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-brand">{t(INCENTIVE_TYPE_KEY[incentive.type] ?? 'icTypeOneTime')}</p>
              <h2 className="text-2xl font-bold leading-tight">{lang === 'en' && incentive.nameEn ? incentive.nameEn : incentive.nameAr}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={incentiveStatusMeta} value={incentiveStatusOf(incentive)} />
                <Badge tone="ink">
                  <span className="ltr-nums">
                    {formatNumber(incentive.targetTrips)} {t('icTrips')} → {formatMoney(incentive.rewardAmount)} {t('sar')}
                  </span>
                </Badge>
              </div>
              {(lang === 'en' ? incentive.descriptionEn : incentive.descriptionAr) && <p className="mt-3 max-w-2xl text-sm text-muted">{lang === 'en' ? incentive.descriptionEn : incentive.descriptionAr}</p>}
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="edit" onClick={() => setEditing(true)}>
              {t('edit')}
            </Button>
            {incentive.isActive && (
              <Button variant="danger-outline" icon="pause" onClick={() => setDeactivating(true)}>
                {t('prDeactivate')}
              </Button>
            )}
          </div>
        </div>
        <div className="mt-5 grid gap-4 sm:grid-cols-3">
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <p className="text-xs font-bold text-muted">{t('prBudget')}</p>
            <p className="mt-1 font-bold">
              <Money value={incentive.spentAmount} strong /> <span className="text-sm text-muted">/ {incentive.budgetAmount === null ? t('prNoBudget') : `${formatMoney(incentive.budgetAmount)} ${t('sar')}`}</span>
            </p>
            <UsageBar share={usageShare(incentive.spentAmount, incentive.budgetAmount)} className="mt-2" />
          </div>
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <p className="text-xs font-bold text-muted">{t('icParticipants')}</p>
            <p className="ltr-nums mt-1 text-lg font-bold">
              {typeof incentive.participantsCount === 'number' ? formatNumber(incentive.participantsCount) : '—'}
              {incentive.maxParticipants !== null ? ` / ${formatNumber(incentive.maxParticipants)}` : ''}
            </p>
          </div>
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <p className="text-xs font-bold text-muted">
              {t('icProgAchieved')} / {t('icProgPaid')}
            </p>
            <p className="ltr-nums mt-1 text-lg font-bold">
              {typeof incentive.achievedCount === 'number' ? formatNumber(incentive.achievedCount) : '—'} / {typeof incentive.paidCount === 'number' ? formatNumber(incentive.paidCount) : '—'}
            </p>
          </div>
        </div>
      </Card>

      <DefinitionCard incentive={incentive} />

      <ProgressCard incentiveId={incentive.id} targetTrips={incentive.targetTrips} />

      <IncentiveFormModal
        open={editing}
        incentive={incentive}
        onClose={() => setEditing(false)}
        onSaved={() => {
          setEditing(false)
          toast.success(t('icSaved'))
          query.reload()
        }}
      />
      <ConfirmModal open={deactivating} title={t('icDeactivateTitle')} description={t('icDeactivateCopy')} confirmLabel={t('prDeactivate')} onClose={() => setDeactivating(false)} onConfirm={deactivate} />
    </>
  )
}

function DefinitionCard({ incentive }: { incentive: Incentive }) {
  const { t, lang } = useLang()
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const citiesQuery = useQuery(() => catalog.cities(), 'catalog-cities')
  const cityName = incentive.cityId ? (citiesQuery.data?.find((city) => city.id === incentive.cityId)?.name ?? incentive.cityId) : t('allCities')
  const selectedZones = useMemo(() => (zonesQuery.data ?? []).filter((zone) => incentive.zoneIds?.includes(zone.id)), [zonesQuery.data, incentive.zoneIds])
  const categoryNames = (incentive.rideCategoryIds ?? []).map((id) => localName(categoriesQuery.data?.find((item) => item.id === id), lang))
  const colorFor = useCallback(() => '#123650', [])
  const days = incentive.daysOfWeek && incentive.daysOfWeek.length > 0 ? incentive.daysOfWeek.map((day) => t(weekdayKey(day))).join(lang === 'ar' ? '، ' : ', ') : t('everyDay')
  const hours = incentive.dailyFrom && incentive.dailyTo ? `${incentive.dailyFrom.slice(0, 5)}–${incentive.dailyTo.slice(0, 5)}` : t('icAllDay')

  return (
    <div className="mb-6 grid gap-6 lg:grid-cols-5">
      <Card title={t('icDefinition')} className="lg:col-span-3">
        <DefinitionList
          items={[
            { label: t('city'), value: cityName },
            { label: t('startsAt'), value: formatDateTime(incentive.startsAt, lang) },
            { label: t('endsAt'), value: formatDateTime(incentive.endsAt, lang) },
            { label: t('dayOfWeek'), value: days },
            { label: t('icHours'), value: hours, ltr: true },
            { label: t('icMinTripFare'), value: incentive.minTripFare === null ? '—' : `${formatMoney(incentive.minTripFare)} ${t('sar')}`, ltr: true },
            { label: t('rideCategory'), value: categoryNames.length > 0 ? categoryNames.join(lang === 'ar' ? '، ' : ', ') : t('allCategories') },
            { label: t('icMinTier'), value: incentive.minTier ? t(driverTierMeta[incentive.minTier]?.key ?? 'tierBronze') : t('icAnyTier') },
            { label: t('icMinRating'), value: incentive.minRating === null ? '—' : String(incentive.minRating), ltr: true },
            { label: t('icRequiresOptIn'), value: incentive.requiresOptIn ? t('yes') : t('no') },
            { label: t('icNotifyOnPublish'), value: incentive.notifyOnPublish ? t('yes') : t('no') },
          ]}
        />
        <p className="mt-4 flex items-start gap-2 text-xs text-muted">
          <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
          {t('icPayoutNote').replace('{hours}', String(PAYOUT_DELAY_HOURS))}
        </p>
      </Card>
      <Card title={t('pickupZone')} flush className="lg:col-span-2">
        {selectedZones.length > 0 ? (
          <>
            <div className="flex flex-wrap gap-1.5 px-5 pb-4 sm:px-6">
              {selectedZones.map((zone: Zone) => (
                <Badge key={zone.id} tone="ink">
                  {localName(zone, lang)}
                </Badge>
              ))}
            </div>
            <ZonesMap zones={selectedZones} colorFor={colorFor} labelFor={(zone) => localName(zone, lang)} className="h-64 w-full" />
          </>
        ) : (
          <p className="px-5 pb-5 text-sm text-muted sm:px-6">{incentive.zoneIds && incentive.zoneIds.length > 0 ? t('loading') : t('icWholeCity')}</p>
        )}
      </Card>
    </div>
  )
}

function ProgressCard({ incentiveId, targetTrips }: { incentiveId: string; targetTrips: number }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, page, setPage } = useUrlState()
  const status = parseEnum(params.get('status'), PROGRESS_STATUSES)
  const query = useQuery(() => incentives.progress(incentiveId, { status, page, pageSize: PAGE_SIZE }), `incentive-progress:${incentiveId}:${status}:${page}`)
  const [voiding, setVoiding] = useState<IncentiveProgress | null>(null)
  const rows = (query.data?.items ?? []).filter((row) => !status || row.status === status)

  const voidProgress = async (reason: string) => {
    if (!voiding) return
    try {
      await incentives.voidProgress(voiding.id, reason)
      toast.success(t('icVoided'))
      setVoiding(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<IncentiveProgress>[] = [
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
    {
      key: 'period',
      header: t('period'),
      render: (row) => (
        <span className="whitespace-nowrap text-xs">
          {formatDate(row.periodStart, lang)}
          {row.periodEnd ? ` – ${formatDate(row.periodEnd, lang)}` : ''}
        </span>
      ),
    },
    {
      key: 'progress',
      header: t('progress'),
      render: (row) => (
        <span className="block min-w-28">
          <span className="ltr-nums block font-bold">
            {formatNumber(row.completedTrips)} / {formatNumber(targetTrips)}
          </span>
          <UsageBar share={usageShare(row.completedTrips, targetTrips)} tone={row.completedTrips >= targetTrips ? 'brand' : 'warning'} className="mt-1" />
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={incentiveProgressStatusMeta} value={row.status} />
          {row.voidedReason && <span className="mt-1 block max-w-48 truncate text-xs text-muted">{row.voidedReason}</span>}
        </span>
      ),
    },
    {
      key: 'multiplier',
      header: t('rlIncentiveMultiplier'),
      className: 'text-center',
      render: (row) =>
        row.incentiveMultiplier === null ? (
          <span className="text-muted">—</span>
        ) : (
          <span className="inline-flex flex-col items-center gap-1">
            <span className={`ltr-nums font-bold ${row.incentiveMultiplier < 1 ? 'text-danger' : ''}`}>×{row.incentiveMultiplier.toFixed(2)}</span>
            {row.incentiveMultiplier < 1 && <Badge tone="danger">{t('icReduced')}</Badge>}
          </span>
        ),
    },
    {
      key: 'reward',
      header: t('icPaidReward'),
      className: 'text-end',
      render: (row) => (
        <span className="block">
          {row.rewardAmount === null ? <span className="text-muted">—</span> : <Money value={row.rewardAmount} strong />}
          {row.paidAt && <span className="block whitespace-nowrap text-xs text-muted">{formatDateTime(row.paidAt, lang)}</span>}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) =>
        canVoidProgress(row.status) ? (
          <Button variant="danger-outline" size="sm" icon="x" onClick={() => setVoiding(row)}>
            {t('icVoid')}
          </Button>
        ) : null,
    },
  ]

  return (
    <Card title={t('icProgressTitle')} description={t('icProgressCopy')} flush>
      <div className="px-5 pb-4 sm:px-6">
        <Tabs value={status} onChange={(value) => setFilter('status', value)} options={[{ value: '' as const, label: t('statusAll') }, ...PROGRESS_STATUSES.map((value) => ({ value, label: t(incentiveProgressStatusMeta[value].key) }))]} />
      </div>
      {query.error ? (
        <ErrorState error={query.error} onRetry={query.reload} />
      ) : (
        <>
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('icNoProgress')} emptyDescription="" />
          {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
        </>
      )}
      <ReasonModal open={voiding !== null} title={t('icVoidTitle')} description={voiding ? `${voiding.driverName || t('unnamed')} — ${t('icVoidCopy')}` : undefined} confirmLabel={t('icVoid')} onClose={() => setVoiding(null)} onConfirm={voidProgress} />
    </Card>
  )
}
