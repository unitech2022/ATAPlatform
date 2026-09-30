import { useState } from 'react'
import { useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { CityField } from '../components/CityField'
import { CorporateAccountFormModal } from '../components/CorporateAccountFormModal'
import { SearchInput } from '../components/Field'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { PermissionError } from '../components/PermissionError'
import { Pagination } from '../components/Pagination'
import { StatCard } from '../components/StatCard'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { UsageBar } from '../components/UsageBar'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { corporateAccounts, corporateInvoices, corporateReceivables } from '../lib/admin'
import { CORPORATE_ACCOUNT_STATUSES, creditShare, creditUsedOf, receivableOf, sumBy, usageTone } from '../lib/corporate'
import { parseEnum } from '../lib/finance'
import { formatDate, formatMoney, formatNumber } from '../lib/format'
import { corporateAccountStatusMeta } from '../lib/status'
import type { CorporateAccountListItem, CorporateAccountStatus } from '../lib/types'

const PAGE_SIZE = 20

/** Corporate accounts (`GET /admin/corporate/accounts`, §F19.7): KPIs, status/city/search filters, credit usage and receivables. */
export function CorporateAccountsPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const status = parseEnum(params.get('status'), CORPORATE_ACCOUNT_STATUSES)
  const cityId = params.get('cityId') ?? ''

  const query = useQuery(
    () => corporateAccounts.list({ status, cityId, search: search.value, page, pageSize: PAGE_SIZE }),
    `corporate-accounts:${status}:${cityId}:${search.value}:${page}`,
  )
  // KPIs: each hides behind "—" on its own error (`payments.view` is needed for receivables).
  const receivables = useQuery(() => corporateReceivables.list(), 'corporate-receivables')
  const activeCount = useQuery(() => corporateAccounts.list({ status: 'active', page: 1, pageSize: 1 }), 'corporate-kpi-active')
  const issuedCount = useQuery(() => corporateInvoices.list({ status: 'issued', page: 1, pageSize: 1 }), 'corporate-kpi-issued')
  const overdueCount = useQuery(() => corporateInvoices.list({ status: 'overdue', page: 1, pageSize: 1 }), 'corporate-kpi-overdue')
  const [creating, setCreating] = useState(false)

  const pendingValue = (state: { loading: boolean; data: unknown }) => state.loading && !state.data
  const spend = receivables.data ? sumBy(receivables.data, (row) => row.unbilled) : null
  const unpaid = receivables.data ? sumBy(receivables.data, (row) => row.unpaidInvoices) : null
  const openInvoices = issuedCount.data && overdueCount.data ? issuedCount.data.total + overdueCount.data.total : null

  const columns: Column<CorporateAccountListItem>[] = [
    {
      key: 'company',
      header: t('coColCompany'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block max-w-64 truncate font-bold">{row.displayName}</span>
          <span className="ltr-nums block text-xs text-muted">{row.accountNumber}</span>
        </span>
      ),
    },
    {
      key: 'cr',
      header: t('coCrNumber'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block">{row.crNumber}</span>
          <span className="block text-xs text-muted">{row.cityName ?? ''}</span>
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={corporateAccountStatusMeta} value={row.status} /> },
    {
      key: 'credit',
      header: t('coColCredit'),
      render: (row) => {
        const receivable = receivableOf(receivables.data, row.id)
        const used = creditUsedOf(receivable)
        const share = creditShare(used, row.creditLimit)
        return (
          <span className="block min-w-36">
            <span className="ltr-nums block font-bold">
              {used === null ? '—' : formatMoney(used)} <span className="text-xs font-normal text-muted">/ {formatMoney(row.creditLimit)} {t('sar')}</span>
            </span>
            <UsageBar share={share} tone={usageTone(share)} className="mt-1" />
          </span>
        )
      },
    },
    {
      key: 'unpaid',
      header: t('coColUnpaid'),
      className: 'text-end',
      render: (row) => {
        const receivable = receivableOf(receivables.data, row.id)
        return receivable ? <Money value={receivable.unpaidInvoices} /> : <span className="text-muted">—</span>
      },
    },
    {
      key: 'overdue',
      header: t('coColOverdue'),
      className: 'text-end',
      render: (row) => {
        const receivable = receivableOf(receivables.data, row.id)
        if (!receivable) return <span className="text-muted">—</span>
        return receivable.overdueAmount > 0 ? <Money value={receivable.overdueAmount} strong className="text-danger" /> : <span className="text-muted">—</span>
      },
    },
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap text-xs text-muted">{formatDate(row.createdAt, lang)}</span> },
  ]

  return (
    <>
      <PageHeader
        title={t('coTitle')}
        description={t('coCopy')}
        actions={
          <Button icon="plus" onClick={() => setCreating(true)}>
            {t('coNew')}
          </Button>
        }
      />

      <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard title={t('coKpiActive')} icon="bank" value={pendingValue(activeCount) ? '…' : activeCount.data ? formatNumber(activeCount.data.total) : '—'} />
        <StatCard title={t('coKpiSpend')} icon="activity" value={pendingValue(receivables) ? '…' : spend === null ? '—' : `${formatMoney(spend)} ${t('sar')}`} meta={t('coKpiSpendMeta')} />
        <StatCard
          title={t('coKpiOpenInvoices')}
          icon="receipt"
          value={pendingValue(issuedCount) || pendingValue(overdueCount) ? '…' : openInvoices === null ? '—' : formatNumber(openInvoices)}
          meta={unpaid === null ? undefined : `${formatMoney(unpaid)} ${t('sar')}`}
        />
        <StatCard
          title={t('coKpiOverdue')}
          icon="alert"
          tone="danger"
          value={pendingValue(overdueCount) ? '…' : overdueCount.data ? formatNumber(overdueCount.data.total) : '—'}
          meta={receivables.data ? `${formatMoney(sumBy(receivables.data, (row) => row.overdueAmount))} ${t('sar')}` : undefined}
        />
      </div>

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status}
          onChange={(value) => setFilter('status', value)}
          options={[{ value: '' as CorporateAccountStatus | '', label: t('statusAll') }, ...CORPORATE_ACCOUNT_STATUSES.map((value) => ({ value, label: t(corporateAccountStatusMeta[value].key) }))]}
        />
        <div className="grid gap-3 sm:grid-cols-2 lg:w-[34rem]">
          <SearchInput className="bg-white shadow-soft" placeholder={t('coSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
          <CityField id="co-filter-city" value={cityId} onChange={(value) => setFilter('cityId', value)} />
        </div>
      </div>

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="corporate.manage" onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.loading} onRowClick={(row) => navigate(`/corporate/${row.id}`)} emptyTitle={t('coEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <CorporateAccountFormModal
        open={creating}
        account={null}
        onClose={() => setCreating(false)}
        onSaved={(saved, adminInvited) => {
          setCreating(false)
          toast.success(t('coSaved'))
          if (!adminInvited) toast.error(t('errorTitle'), t('coInviteFailed'))
          if (saved?.id) navigate(`/corporate/${saved.id}`)
          else query.reload()
        }}
      />
    </>
  )
}
