import { useMemo, useState } from 'react'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { FavoriteRuleFormModal } from '../components/FavoriteRuleFormModal'
import { FavoriteStatsPanel } from '../components/FavoriteStatsPanel'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { favorites, rideCategories, zones } from '../lib/admin'
import { BOOKING_TYPE_KEY } from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { analyzeRules, favoriteRuleInputOf, favoriteRuleStatusOf } from '../lib/favorites'
import { formatDate, formatMoney, formatNumber } from '../lib/format'
import { localName } from '../lib/pricing'
import { promotionStatusMeta } from '../lib/status'
import type { FavoriteDiscountRule } from '../lib/types'

const TABS = ['rules', 'stats'] as const

/** Favorite driver (§F16): discount rules CRUD with the effective-rule analysis, plus the favorites stats (`favorites.manage`). */
export function FavoritesPage() {
  const { t } = useLang()
  const { params, setFilter } = useUrlState()
  const tab = parseEnum(params.get('tab'), TABS) || 'rules'

  return (
    <>
      <PageHeader title={t('fvTitle')} description={t('fvCopy')} />
      <Tabs
        className="mb-4"
        value={tab}
        onChange={(value) => {
          setFilter('tab', value === 'rules' ? '' : value)
        }}
        options={[
          { value: 'rules', label: t('fvTabRules') },
          { value: 'stats', label: t('fvTabStats') },
        ]}
      />
      {tab === 'stats' ? <FavoriteStatsPanel /> : <RulesSection />}
    </>
  )
}

function RulesSection() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => favorites.rules(), 'favorite-rules')
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const [editing, setEditing] = useState<FavoriteDiscountRule | 'new' | null>(null)
  const [deleting, setDeleting] = useState<FavoriteDiscountRule | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const rows = useMemo(() => [...(query.data ?? [])].sort((a, b) => b.priority - a.priority || a.name.localeCompare(b.name)), [query.data])
  const analysis = useMemo(() => analyzeRules(rows), [rows])
  const categoryNames = useMemo(() => new Map((categoriesQuery.data ?? []).map((category) => [category.id, localName(category, lang)])), [categoriesQuery.data, lang])
  const zoneNames = useMemo(() => new Map((zonesQuery.data ?? []).map((zone) => [zone.id, localName(zone, lang)])), [zonesQuery.data, lang])
  const nameOf = (id: string) => rows.find((row) => row.id === id)?.name ?? id
  const topEffective = rows.find((row) => analysis.effective.has(row.id))
  const hasActive = rows.some((row) => favoriteRuleStatusOf(row) === 'active')

  const setActive = async (row: FavoriteDiscountRule, isActive: boolean) => {
    setBusyId(row.id)
    try {
      await favorites.update(row.id, favoriteRuleInputOf(row, isActive))
      toast.success(t(isActive ? 'fvActivated' : 'fvDeactivated'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyId(null)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await favorites.remove(deleting.id)
      toast.success(t('fvDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const scopeText = (ids: string[] | null | undefined, names: Map<string, string>, all: string) => {
    if (!ids || ids.length === 0) return all
    const labels = ids.map((id) => names.get(id) ?? id)
    return labels.length > 2 ? `${labels.slice(0, 2).join(' · ')} +${labels.length - 2}` : labels.join(' · ')
  }

  const columns: Column<FavoriteDiscountRule>[] = [
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block min-w-40">
          <span className="block font-bold">{row.name}</span>
          <span className="block text-xs text-muted">
            {t('priority')}: <span className="ltr-nums font-bold text-ink">{formatNumber(row.priority)}</span>
          </span>
        </span>
      ),
    },
    {
      key: 'discount',
      header: t('fvDiscount'),
      render: (row) => (
        <span className="block whitespace-nowrap">
          <span className="ltr-nums block font-bold">{formatNumber(row.discountPercent)}%</span>
          <span className="block text-xs text-muted">
            {t('fvMaxAmount')}: <span className="ltr-nums">{formatMoney(row.maxDiscountAmount)}</span> {t('sar')}
          </span>
          {typeof row.minFare === 'number' && (
            <span className="block text-xs text-muted">
              {t('minFare')}: <span className="ltr-nums">{formatMoney(row.minFare)}</span> {t('sar')}
            </span>
          )}
        </span>
      ),
    },
    { key: 'stackable', header: t('fvStackable'), render: (row) => <Badge tone={row.stackableWithPromotions ? 'brand' : 'muted'}>{row.stackableWithPromotions ? t('yes') : t('no')}</Badge> },
    {
      key: 'window',
      header: t('validity'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDate(row.validFrom, lang)}</span>
          <span className="block text-muted">← {row.validTo ? formatDate(row.validTo, lang) : t('fvOpenEnded')}</span>
        </span>
      ),
    },
    {
      key: 'scope',
      header: t('fvScope'),
      render: (row) => (
        <span className="block min-w-40 text-xs text-muted">
          <span className="block">
            <Icon name="layers" className="me-1 inline size-3.5" />
            {scopeText(row.rideCategoryIds, categoryNames, t('allCategories'))}
          </span>
          <span className="block">
            <Icon name="polygon" className="me-1 inline size-3.5" />
            {scopeText(row.zoneIds, zoneNames, t('allZones'))}
          </span>
          <span className="block">
            <Icon name="clock" className="me-1 inline size-3.5" />
            {row.bookingTypes && row.bookingTypes.length > 0 ? row.bookingTypes.map((type) => t(BOOKING_TYPE_KEY[type])).join(' · ') : t('cxAnyBooking')}
          </span>
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => {
        const shadowedBy = analysis.shadowed.get(row.id)
        const ties = analysis.ties.get(row.id)
        return (
          <span className="flex min-w-32 flex-col items-start gap-1">
            <MetaBadge record={promotionStatusMeta} value={favoriteRuleStatusOf(row)} />
            {analysis.effective.has(row.id) && <Badge tone="brand">{t('fvEffective')}</Badge>}
            {shadowedBy && <Badge tone="warning">{`${t('fvShadowed')}: ${nameOf(shadowedBy)}`}</Badge>}
            {ties && <Badge tone="danger">{`${t('fvTie')}: ${ties.map(nameOf).join(' · ')}`}</Badge>}
          </span>
        )
      },
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => setEditing(row)}>
            {t('edit')}
          </Button>
          {row.isActive ? (
            <Button variant="danger-outline" size="sm" icon="pause" loading={busyId === row.id} onClick={() => setActive(row, false)}>
              {t('prDeactivate')}
            </Button>
          ) : (
            <Button variant="brand" size="sm" icon="play" loading={busyId === row.id} onClick={() => setActive(row, true)}>
              {t('prActivate')}
            </Button>
          )}
          <Button variant="danger-outline" size="sm" icon="trash" aria-label={t('delete')} onClick={() => setDeleting(row)}>
            {t('delete')}
          </Button>
        </span>
      ),
    },
  ]

  return (
    <>
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <p className="max-w-2xl text-sm text-muted">{t('fvRulesCopy')}</p>
        <Button icon="plus" onClick={() => setEditing('new')}>
          {t('fvNew')}
        </Button>
      </div>

      {query.data && (
        <div className="mb-4 space-y-2">
          {topEffective ? (
            <p className="flex items-start gap-2 rounded-2xl bg-brand-soft px-4 py-3 text-sm font-bold text-brand">
              <Icon name="check" className="mt-0.5 size-4 shrink-0" />
              <span>
                {t('fvEffectiveNow')}: {topEffective.name} ({formatNumber(topEffective.discountPercent)}%) · {t('priority')} {formatNumber(topEffective.priority)}
              </span>
            </p>
          ) : (
            <p className="flex items-start gap-2 rounded-2xl bg-amber-50 px-4 py-3 text-sm font-bold text-amber-800">
              <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
              {hasActive ? t('fvNoEffective') : t('fvNoActiveRule')}
            </p>
          )}
          {analysis.ties.size > 0 && (
            <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
              <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
              {t('fvTieBanner')}
            </p>
          )}
        </div>
      )}

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading && !query.data} emptyTitle={t('fvEmpty')} emptyDescription="" />
        )}
      </Card>

      <FavoriteRuleFormModal
        open={editing !== null}
        rule={editing === 'new' ? null : editing}
        rules={rows}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          toast.success(t('fvSaved'))
          query.reload()
        }}
      />

      <ConfirmModal
        open={deleting !== null}
        title={t('fvDeleteTitle')}
        description={deleting ? `${deleting.name} — ${t('fvDeleteCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
