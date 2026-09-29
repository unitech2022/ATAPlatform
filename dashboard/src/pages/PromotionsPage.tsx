import { useState } from 'react'
import { useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { SearchInput } from '../components/Field'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { PromotionFormModal } from '../components/PromotionFormModal'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { UsageBar } from '../components/UsageBar'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { promotions } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDate, formatNumber } from '../lib/format'
import { formatPromoValue, PROMOTION_STATUSES, PROMOTION_TYPE_KEY, promotionInputOf, promotionStatusOf, usageShare } from '../lib/rewards'
import { promotionStatusMeta } from '../lib/status'
import type { PromotionListItem } from '../lib/types'

const PAGE_SIZE = 20

/** Promotions (`GET /admin/promotions`, §F15.6): status tabs, search, usage/budget, create, activate/deactivate. */
export function PromotionsPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const status = parseEnum(params.get('status'), PROMOTION_STATUSES)
  const query = useQuery(() => promotions.list({ status, search: search.value, page, pageSize: PAGE_SIZE }), `promotions:${status}:${search.value}:${page}`)

  const [creating, setCreating] = useState(false)
  const [deactivating, setDeactivating] = useState<PromotionListItem | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  // The status filter is also applied client-side in case the server ignores it.
  const rows = (query.data?.items ?? []).filter((row) => !status || promotionStatusOf(row) === status)

  const deactivate = async () => {
    if (!deactivating) return
    try {
      await promotions.deactivate(deactivating.id)
      toast.success(t('prDeactivated'))
      setDeactivating(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  /** §F15.6 has no activate endpoint: re-save the full promotion with `isActive = true`. */
  const activate = async (row: PromotionListItem) => {
    setBusyId(row.id)
    try {
      const full = await promotions.get(row.id)
      await promotions.update(row.id, promotionInputOf(full, true))
      toast.success(t('prActivated'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyId(null)
    }
  }

  const columns: Column<PromotionListItem>[] = [
    {
      key: 'code',
      header: t('prCode'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold tracking-wider">{row.code}</span>
          <span className="block max-w-56 truncate text-xs text-muted">{lang === 'en' && row.nameEn ? row.nameEn : row.nameAr}</span>
        </span>
      ),
    },
    {
      key: 'value',
      header: t('prDiscount'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold">{formatPromoValue(row.type, row.value, t)}</span>
          <span className="block text-xs text-muted">{t(PROMOTION_TYPE_KEY[row.type] ?? 'prTypeFixed')}</span>
        </span>
      ),
    },
    {
      key: 'validity',
      header: t('validity'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDate(row.validFrom, lang)}</span>
          <span className="block text-muted">← {formatDate(row.validTo, lang)}</span>
        </span>
      ),
    },
    {
      key: 'usage',
      header: t('prUsage'),
      render: (row) => (
        <span className="block min-w-28">
          <span className="ltr-nums block font-bold">
            {formatNumber(row.usageCount)} / {row.totalUsageLimit === null ? '∞' : formatNumber(row.totalUsageLimit)}
          </span>
          <UsageBar share={usageShare(row.usageCount, row.totalUsageLimit)} className="mt-1" />
        </span>
      ),
    },
    {
      key: 'budget',
      header: t('prBudget'),
      render: (row) => (
        <span className="block min-w-32">
          <Money value={row.spentAmount} strong />
          <span className="block text-xs text-muted">
            {row.budgetAmount === null ? t('prNoBudget') : <>/ {formatNumber(row.budgetAmount)} {t('sar')}</>}
          </span>
          <UsageBar share={usageShare(row.spentAmount, row.budgetAmount)} tone={row.budgetAmount !== null && row.spentAmount >= row.budgetAmount * 0.9 ? 'danger' : 'brand'} className="mt-1" />
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={promotionStatusMeta} value={promotionStatusOf(row)} /> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2" onClick={(event) => event.stopPropagation()}>
          {row.isActive ? (
            <Button variant="danger-outline" size="sm" icon="pause" onClick={() => setDeactivating(row)}>
              {t('prDeactivate')}
            </Button>
          ) : (
            <Button variant="brand" size="sm" icon="play" loading={busyId === row.id} onClick={() => activate(row)}>
              {t('prActivate')}
            </Button>
          )}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('prTitle')}
        description={t('prCopy')}
        actions={
          <Button icon="plus" onClick={() => setCreating(true)}>
            {t('prNew')}
          </Button>
        }
      />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status}
          onChange={(value) => setFilter('status', value)}
          options={[{ value: '' as const, label: t('statusAll') }, ...PROMOTION_STATUSES.map((value) => ({ value, label: t(promotionStatusMeta[value].key) }))]}
        />
        <SearchInput wrapperClassName="lg:w-80" className="bg-white shadow-soft" placeholder={t('prSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} onRowClick={(row) => navigate(`/promotions/${row.id}`)} emptyTitle={t('prEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <PromotionFormModal
        open={creating}
        promotion={null}
        onClose={() => setCreating(false)}
        onSaved={(saved) => {
          setCreating(false)
          toast.success(t('prSaved'))
          if (saved?.id) navigate(`/promotions/${saved.id}`)
          else query.reload()
        }}
      />

      <ConfirmModal
        open={deactivating !== null}
        title={t('prDeactivateTitle')}
        description={deactivating ? `${deactivating.code} — ${t('prDeactivateCopy')}` : undefined}
        confirmLabel={t('prDeactivate')}
        onClose={() => setDeactivating(null)}
        onConfirm={deactivate}
      />
    </>
  )
}
