import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { Money } from '../components/Money'
import { Pagination } from '../components/Pagination'
import { PromotionFormModal } from '../components/PromotionFormModal'
import { PageSpinner } from '../components/Spinner'
import { StatCard } from '../components/StatCard'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { UsageBar } from '../components/UsageBar'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { catalog, promotions, rideCategories, zones } from '../lib/admin'
import { BOOKING_TYPE_KEY } from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatMoney, formatNumber } from '../lib/format'
import { localName } from '../lib/pricing'
import {
  formatPromoValue,
  formatRatio,
  PROMOTION_TYPE_KEY,
  promotionInputOf,
  promotionStatusOf,
  REDEMPTION_STATUSES,
  RELEASE_REASON_KEY,
  usageShare,
} from '../lib/rewards'
import { promotionStatusMeta, redemptionStatusMeta } from '../lib/status'
import { PAYMENT_METHOD_KEY } from '../lib/trips'
import type { Promotion, PromotionRedemption, PromotionStats } from '../lib/types'

const PAGE_SIZE = 20

/** Promotion detail (`/promotions/:id`): definition, restrictions, stats and the redemptions table. */
export function PromotionDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => promotions.get(id), `promotion:${id}`)
  const stats = useQuery(() => promotions.stats(id), `promotion-stats:${id}`)
  const [editing, setEditing] = useState(false)
  const [confirming, setConfirming] = useState<'deactivate' | 'activate' | null>(null)

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }
  const promotion = query.data
  const status = promotionStatusOf(promotion)

  const toggle = async () => {
    try {
      if (confirming === 'deactivate') await promotions.deactivate(promotion.id)
      else await promotions.update(promotion.id, promotionInputOf(promotion, true))
      toast.success(confirming === 'deactivate' ? t('prDeactivated') : t('prActivated'))
      setConfirming(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  return (
    <>
      <Link to="/promotions" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('prBack')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="gift" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="ltr-nums text-2xl font-bold tracking-widest">{promotion.code}</p>
              <h2 className="mt-1 text-lg font-bold">{lang === 'en' && promotion.nameEn ? promotion.nameEn : promotion.nameAr}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={promotionStatusMeta} value={status} />
                <Badge tone="ink">
                  <span className="ltr-nums">{formatPromoValue(promotion.type, promotion.value, t)}</span>
                </Badge>
                {promotion.isPublic && <Badge tone="brand">{t('prPublic')}</Badge>}
                {promotion.isStackable && <Badge tone="brand">{t('prStackable')}</Badge>}
              </div>
              {(lang === 'en' ? promotion.descriptionEn : promotion.descriptionAr) && (
                <p className="mt-3 max-w-2xl text-sm text-muted">{lang === 'en' ? promotion.descriptionEn : promotion.descriptionAr}</p>
              )}
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="edit" onClick={() => setEditing(true)}>
              {t('edit')}
            </Button>
            {promotion.isActive ? (
              <Button variant="danger-outline" icon="pause" onClick={() => setConfirming('deactivate')}>
                {t('prDeactivate')}
              </Button>
            ) : (
              <Button variant="brand" icon="play" onClick={() => setConfirming('activate')}>
                {t('prActivate')}
              </Button>
            )}
          </div>
        </div>
        <div className="mt-5 grid gap-4 sm:grid-cols-2">
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <p className="text-xs font-bold text-muted">{t('prUsage')}</p>
            <p className="ltr-nums mt-1 text-lg font-bold">
              {formatNumber(promotion.usageCount)} / {promotion.totalUsageLimit === null ? '∞' : formatNumber(promotion.totalUsageLimit)}
            </p>
            <UsageBar share={usageShare(promotion.usageCount, promotion.totalUsageLimit)} className="mt-2" />
            <p className="mt-1 text-xs text-muted">{t('prUsageHint')}</p>
          </div>
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <p className="text-xs font-bold text-muted">{t('prBudget')}</p>
            <p className="mt-1 text-lg font-bold">
              <Money value={promotion.spentAmount} strong /> <span className="text-sm text-muted">/ {promotion.budgetAmount === null ? t('prNoBudget') : `${formatMoney(promotion.budgetAmount)} ${t('sar')}`}</span>
            </p>
            <UsageBar share={usageShare(promotion.spentAmount, promotion.budgetAmount)} className="mt-2" />
          </div>
        </div>
      </Card>

      <PromotionStatsGrid stats={stats.data} loading={stats.loading} hidden={Boolean(stats.error)} />

      <RestrictionsCard promotion={promotion} />

      <RedemptionsCard promotionId={promotion.id} stats={stats.data} />

      <PromotionFormModal
        open={editing}
        promotion={promotion}
        onClose={() => setEditing(false)}
        onSaved={() => {
          setEditing(false)
          toast.success(t('prSaved'))
          query.reload()
        }}
      />
      <ConfirmModal
        open={confirming !== null}
        title={confirming === 'deactivate' ? t('prDeactivateTitle') : t('prActivateTitle')}
        description={confirming === 'deactivate' ? t('prDeactivateCopy') : t('prActivateCopy')}
        confirmLabel={confirming === 'deactivate' ? t('prDeactivate') : t('prActivate')}
        confirmVariant={confirming === 'deactivate' ? 'danger' : 'primary'}
        onClose={() => setConfirming(null)}
        onConfirm={toggle}
      />
    </>
  )
}

function PromotionStatsGrid({ stats, loading, hidden }: { stats: PromotionStats | null; loading: boolean; hidden: boolean }) {
  const { t } = useLang()
  if (hidden) return null
  const value = (count: number | undefined, money = false) => (loading && !stats ? '…' : money ? `${formatMoney(count)} ${t('sar')}` : formatNumber(count))
  const conversion = stats && stats.applied > 0 ? stats.firstTripConversions / stats.applied : null
  return (
    <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      <StatCard title={t('prStatApplied')} icon="check" value={value(stats?.applied)} meta={`${t('prRedReserved')}: ${value(stats?.reserved)}`} />
      <StatCard title={t('prStatReleased')} icon="refresh" tone="danger" value={value(stats?.released)} />
      <StatCard title={t('prStatTotalDiscount')} icon="wallet" value={value(stats?.totalDiscount, true)} />
      <StatCard title={t('prStatUniqueUsers')} icon="users" value={value(stats?.uniqueUsers)} />
      <StatCard title={t('prStatFirstTrip')} icon="star" value={value(stats?.firstTripConversions)} meta={conversion !== null ? formatRatio(conversion) : undefined} />
    </div>
  )
}

function RestrictionsCard({ promotion }: { promotion: Promotion }) {
  const { t, lang } = useLang()
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const citiesQuery = useQuery(() => catalog.cities(), 'catalog-cities')
  const cityName = promotion.cityId ? (citiesQuery.data?.find((city) => city.id === promotion.cityId)?.name ?? promotion.cityId) : null
  const categoryNames = useMemo(
    () => (promotion.rideCategoryIds ?? []).map((id) => localName(categoriesQuery.data?.find((item) => item.id === id), lang)),
    [promotion.rideCategoryIds, categoriesQuery.data, lang],
  )
  const zoneNames = useMemo(() => (promotion.zoneIds ?? []).map((id) => localName(zonesQuery.data?.find((item) => item.id === id), lang)), [promotion.zoneIds, zonesQuery.data, lang])

  const chips = (values: string[], empty: string) =>
    values.length === 0 ? (
      <span className="text-sm text-muted">{empty}</span>
    ) : (
      <span className="flex flex-wrap gap-1.5">
        {values.map((value, index) => (
          <Badge key={`${value}-${index}`} tone="ink">
            {value}
          </Badge>
        ))}
      </span>
    )

  return (
    <Card title={t('prRestrictionsTitle')} className="mb-6">
      <DefinitionList
        items={[
          { label: t('type'), value: t(PROMOTION_TYPE_KEY[promotion.type] ?? 'prTypeFixed') },
          { label: t('prMaxDiscount'), value: promotion.maxDiscount === null ? '—' : `${formatMoney(promotion.maxDiscount)} ${t('sar')}`, ltr: true },
          { label: t('minFare'), value: promotion.minFare === null ? '—' : `${formatMoney(promotion.minFare)} ${t('sar')}`, ltr: true },
          { label: t('prPerUserLimit'), value: formatNumber(promotion.perUserLimit), ltr: true },
          { label: t('prValidFrom'), value: formatDateTime(promotion.validFrom, lang) },
          { label: t('prValidTo'), value: formatDateTime(promotion.validTo, lang) },
          { label: t('prFirstTripOnly'), value: promotion.firstTripOnly ? t('yes') : t('no') },
          { label: t('prNewUsersOnly'), value: promotion.newUsersOnly ? `${t('yes')} · ${formatNumber(promotion.newUserDays)} ${t('rlDays')}` : t('no') },
          { label: t('city'), value: cityName ?? t('allCities') },
          { label: t('prCreated'), value: `${formatDateTime(promotion.createdAt, lang)}${promotion.createdByName ? ` · ${promotion.createdByName}` : ''}` },
        ]}
      />
      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <div>
          <p className="mb-2 text-xs font-bold text-muted">{t('rideCategory')}</p>
          {chips(categoryNames, t('allCategories'))}
        </div>
        <div>
          <p className="mb-2 text-xs font-bold text-muted">{t('pickupZone')}</p>
          {chips(zoneNames, t('allZones'))}
        </div>
        <div>
          <p className="mb-2 text-xs font-bold text-muted">{t('paymentMethod')}</p>
          {chips((promotion.paymentMethods ?? []).map((method) => t(PAYMENT_METHOD_KEY[method] ?? 'paymentCash')), t('prAllMethods'))}
        </div>
        <div>
          <p className="mb-2 text-xs font-bold text-muted">{t('bookingType')}</p>
          {chips((promotion.bookingTypes ?? []).map((type) => t(BOOKING_TYPE_KEY[type] ?? 'bookingNow')), t('cxAnyBooking'))}
        </div>
      </div>
    </Card>
  )
}

function RedemptionsCard({ promotionId, stats }: { promotionId: string; stats: PromotionStats | null }) {
  const { t, lang } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()
  const status = parseEnum(params.get('status'), REDEMPTION_STATUSES)
  const query = useQuery(() => promotions.redemptions(promotionId, { status, page, pageSize: PAGE_SIZE }), `promotion-redemptions:${promotionId}:${status}:${page}`)
  const rows = (query.data?.items ?? []).filter((row) => !status || row.status === status)
  const pageReserved = rows.reduce((sum, row) => sum + (row.reservedAmount ?? 0), 0)
  const pageDiscount = rows.reduce((sum, row) => sum + (row.status === 'applied' ? (row.discountAmount ?? 0) : 0), 0)
  const counts: Record<string, number | undefined> = { reserved: stats?.reserved, applied: stats?.applied, released: stats?.released }

  const columns: Column<PromotionRedemption>[] = [
    {
      key: 'passenger',
      header: t('passenger'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.passengerName || t('unnamed')}</span>
          {row.phoneMasked && <span className="ltr-nums block text-xs text-muted">{row.phoneMasked}</span>}
        </span>
      ),
    },
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) =>
        row.tripId ? (
          <Link to={`/trips/${row.tripId}`} className="ltr-nums font-bold text-brand hover:underline">
            {row.tripNumber}
          </Link>
        ) : (
          <span className="ltr-nums font-bold">{row.tripNumber}</span>
        ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={redemptionStatusMeta} value={row.status} />
          {row.releaseReason && <span className="mt-1 block text-xs text-muted">{t(RELEASE_REASON_KEY[row.releaseReason] ?? 'prRelAdmin')}</span>}
        </span>
      ),
    },
    { key: 'reserved', header: t('prReservedAmount'), className: 'text-end', render: (row) => <Money value={row.reservedAmount} /> },
    { key: 'discount', header: t('prAppliedAmount'), className: 'text-end', render: (row) => (row.discountAmount === null ? <span className="text-muted">—</span> : <Money value={row.discountAmount} strong />) },
    {
      key: 'dates',
      header: t('prReservedAt'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDateTime(row.reservedAt, lang)}</span>
          {row.appliedAt && (
            <span className="block text-muted">
              {t('prRedApplied')}: {formatDateTime(row.appliedAt, lang)}
            </span>
          )}
          {row.releasedAt && (
            <span className="block text-muted">
              {t('prRedReleased')}: {formatDateTime(row.releasedAt, lang)}
            </span>
          )}
        </span>
      ),
    },
  ]

  return (
    <Card title={t('prRedemptions')} description={t('prRedemptionsCopy')} flush>
      <div className="px-5 pb-4 sm:px-6">
        <Tabs
          value={status}
          onChange={(value) => setFilter('status', value)}
          options={[
            { value: '' as const, label: t('statusAll'), count: stats ? stats.reserved + stats.applied + stats.released : null },
            ...REDEMPTION_STATUSES.map((value) => ({ value, label: t(redemptionStatusMeta[value].key), count: counts[value] ?? null })),
          ]}
        />
      </div>
      {query.error ? (
        <ErrorState error={query.error} onRetry={query.reload} />
      ) : (
        <>
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('prNoRedemptions')} emptyDescription="" />
          {rows.length > 0 && (
            <div className="flex flex-wrap items-center justify-end gap-x-6 gap-y-2 border-t border-line bg-cloud/60 px-5 py-3 text-sm sm:px-6">
              <span className="text-muted">
                {t('prPageTotals')}: {t('prReservedAmount')} <Money value={pageReserved} strong />
              </span>
              <span className="text-muted">
                {t('prAppliedAmount')} <Money value={pageDiscount} strong />
              </span>
              {stats && (
                <span className="text-muted">
                  {t('prStatTotalDiscount')} <Money value={stats.totalDiscount} strong />
                </span>
              )}
            </div>
          )}
          {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
        </>
      )}
    </Card>
  )
}
