import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { SearchInput } from '../components/Field'
import { IncentiveFormModal } from '../components/IncentiveFormModal'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { UsageBar } from '../components/UsageBar'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { incentives } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatMoney, formatNumber } from '../lib/format'
import { weekdayKey } from '../lib/pricing'
import { INCENTIVE_STATUSES, INCENTIVE_TYPE_KEY, incentiveStatusOf, usageShare } from '../lib/rewards'
import { driverTierMeta, incentiveStatusMeta } from '../lib/status'
import type { Incentive } from '../lib/types'

/** Driver incentives / quests (`GET /admin/incentives`, §F15.10): status tabs, create/edit, deactivate. */
export function IncentivesPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const { params, setFilter } = useUrlState()
  const search = useUrlSearch()
  const status = parseEnum(params.get('status'), INCENTIVE_STATUSES)
  const query = useQuery(() => incentives.list(), 'incentives')
  const [creating, setCreating] = useState(false)
  const [deactivating, setDeactivating] = useState<Incentive | null>(null)

  const all = query.data ?? []
  const counts = Object.fromEntries(INCENTIVE_STATUSES.map((value) => [value, all.filter((row) => incentiveStatusOf(row) === value).length]))
  const needle = search.value.toLowerCase()
  const rows = all
    .filter((row) => (!status || incentiveStatusOf(row) === status) && (!needle || `${row.nameAr} ${row.nameEn}`.toLowerCase().includes(needle)))
    .sort((a, b) => new Date(b.startsAt).getTime() - new Date(a.startsAt).getTime())

  const deactivate = async () => {
    if (!deactivating) return
    try {
      await incentives.deactivate(deactivating.id)
      toast.success(t('icDeactivated'))
      setDeactivating(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const windowText = (row: Incentive) => {
    const days = row.daysOfWeek && row.daysOfWeek.length > 0 ? row.daysOfWeek.map((day) => t(weekdayKey(day))).join(lang === 'ar' ? '، ' : ', ') : ''
    const hours = row.dailyFrom && row.dailyTo ? `${row.dailyFrom.slice(0, 5)}–${row.dailyTo.slice(0, 5)}` : ''
    return [days, hours].filter(Boolean).join(' · ')
  }

  const columns: Column<Incentive>[] = [
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{lang === 'en' && row.nameEn ? row.nameEn : row.nameAr}</span>
          <span className="block text-xs text-muted">{t(INCENTIVE_TYPE_KEY[row.type] ?? 'icTypeOneTime')}</span>
        </span>
      ),
    },
    {
      key: 'period',
      header: t('period'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDateTime(row.startsAt, lang)}</span>
          <span className="block text-muted">← {formatDateTime(row.endsAt, lang)}</span>
          {windowText(row) && <span className="ltr-nums mt-0.5 block text-brand">{windowText(row)}</span>}
        </span>
      ),
    },
    {
      key: 'goal',
      header: t('icGoal'),
      render: (row) => (
        <span className="block whitespace-nowrap">
          <span className="ltr-nums block font-bold">
            {formatNumber(row.targetTrips)} {t('icTrips')}
          </span>
          <Money value={row.rewardAmount} className="text-xs" />
        </span>
      ),
    },
    {
      key: 'eligibility',
      header: t('icEligibility'),
      render: (row) => (
        <span className="flex max-w-56 flex-wrap gap-1">
          {row.minTier && <MetaBadge record={driverTierMeta} value={row.minTier} />}
          {row.minRating !== null && <Badge tone="muted">★ ≥ {row.minRating}</Badge>}
          {row.requiresOptIn && <Badge tone="ink">{t('icOptIn')}</Badge>}
          {row.zoneIds && row.zoneIds.length > 0 && (
            <Badge tone="muted">
              {t('zone')}: {formatNumber(row.zoneIds.length)}
            </Badge>
          )}
          {!row.minTier && row.minRating === null && !row.requiresOptIn && !(row.zoneIds && row.zoneIds.length > 0) && <span className="text-xs text-muted">{t('icEveryone')}</span>}
        </span>
      ),
    },
    {
      key: 'budget',
      header: t('prBudget'),
      render: (row) => (
        <span className="block min-w-28">
          <Money value={row.spentAmount} strong />
          <span className="block text-xs text-muted">{row.budgetAmount === null ? t('prNoBudget') : `/ ${formatMoney(row.budgetAmount)} ${t('sar')}`}</span>
          <UsageBar share={usageShare(row.spentAmount, row.budgetAmount)} className="mt-1" />
        </span>
      ),
    },
    {
      key: 'participants',
      header: t('icParticipants'),
      className: 'text-center',
      render: (row) => (
        <span className="ltr-nums">
          {typeof row.participantsCount === 'number' ? formatNumber(row.participantsCount) : '—'}
          {row.maxParticipants !== null ? ` / ${formatNumber(row.maxParticipants)}` : ''}
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={incentiveStatusMeta} value={incentiveStatusOf(row)} /> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) =>
        row.isActive ? (
          <span onClick={(event) => event.stopPropagation()}>
            <Button variant="danger-outline" size="sm" icon="pause" onClick={() => setDeactivating(row)}>
              {t('prDeactivate')}
            </Button>
          </span>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader
        title={t('icTitle')}
        description={t('icCopy')}
        actions={
          <Button icon="plus" onClick={() => setCreating(true)}>
            {t('icNew')}
          </Button>
        }
      />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status}
          onChange={(value) => setFilter('status', value)}
          options={[
            { value: '' as const, label: t('statusAll'), count: query.data ? all.length : null },
            ...INCENTIVE_STATUSES.map((value) => ({ value, label: t(incentiveStatusMeta[value].key), count: query.data ? counts[value] : null })),
          ]}
        />
        <SearchInput wrapperClassName="lg:w-80" className="bg-white shadow-soft" placeholder={t('icSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} onRowClick={(row) => navigate(`/incentives/${row.id}`)} emptyTitle={t('icEmpty')} emptyDescription="" />
        )}
      </Card>

      <IncentiveFormModal
        open={creating}
        incentive={null}
        onClose={() => setCreating(false)}
        onSaved={(saved) => {
          setCreating(false)
          toast.success(t('icSaved'))
          if (saved?.id) navigate(`/incentives/${saved.id}`)
          else query.reload()
        }}
      />

      <ConfirmModal
        open={deactivating !== null}
        title={t('icDeactivateTitle')}
        description={deactivating ? `${lang === 'en' && deactivating.nameEn ? deactivating.nameEn : deactivating.nameAr} — ${t('icDeactivateCopy')}` : undefined}
        confirmLabel={t('prDeactivate')}
        onClose={() => setDeactivating(null)}
        onConfirm={deactivate}
      />
    </>
  )
}
