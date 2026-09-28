import { useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { SearchInput, Select, Toggle } from '../components/Field'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { wallets } from '../lib/admin'
import { cashDebtOf, lookupKey, MAX_CASH_DEBT, parseEnum, WALLET_KIND_KEY, WALLET_KINDS } from '../lib/finance'
import { walletStatusMeta } from '../lib/status'
import type { WalletListItem } from '../lib/types'

const PAGE_SIZE = 20

export function WalletsPage() {
  const { t } = useLang()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const kind = parseEnum(params.get('kind'), WALLET_KINDS)
  const negativeOnly = params.get('negativeOnly') === 'true'

  const query = useQuery(
    () => wallets.list({ kind, search: search.value, negativeOnly: negativeOnly || '', page, pageSize: PAGE_SIZE }),
    `wallets:${kind}:${search.value}:${negativeOnly}:${page}`,
  )

  const columns: Column<WalletListItem>[] = [
    {
      key: 'user',
      header: t('owner'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block truncate font-bold">{row.userName || t('unnamed')}</span>
          {row.phone && <span className="ltr-nums block text-xs text-muted">{row.phone}</span>}
        </span>
      ),
    },
    { key: 'kind', header: t('walletKind'), render: (row) => t(lookupKey(WALLET_KIND_KEY, row.kind) ?? 'walletKindPassenger') },
    { key: 'balance', header: t('balance'), className: 'text-end', render: (row) => <Money value={row.balance} signed strong /> },
    {
      key: 'debt',
      header: t('debt'),
      className: 'text-end',
      render: (row) => {
        const debt = cashDebtOf(row.balance)
        if (debt === 0) return <span className="text-muted">—</span>
        return <Money value={debt} className={row.kind === 'driver' && debt >= MAX_CASH_DEBT ? 'font-bold text-danger' : 'text-danger'} />
      },
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={walletStatusMeta} value={row.status} /> },
  ]

  return (
    <>
      <PageHeader title={t('walletsTitle')} description={t('walletsCopy')} />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto]">
        <SearchInput
          placeholder={t('searchWallets')}
          value={search.input}
          onChange={(event) => search.setInput(event.target.value)}
          wrapperClassName="sm:col-span-2 lg:col-span-1"
        />
        <Select id="kind" aria-label={t('walletKind')} value={kind} onChange={(event) => setFilter('kind', event.target.value)} wrapperClassName="lg:w-44">
          <option value="">{t('allWallets')}</option>
          {WALLET_KINDS.map((value) => (
            <option key={value} value={value}>
              {t(WALLET_KIND_KEY[value])}
            </option>
          ))}
        </Select>
        <div className="lg:w-64">
          <Toggle checked={negativeOnly} onChange={(value) => setFilter('negativeOnly', value)} label={t('negativeOnly')} description={t('negativeOnlyCopy')} />
        </div>
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={query.data?.items ?? []}
              rowKey={(row) => row.id}
              loading={query.loading}
              onRowClick={(row) => navigate(`/wallets/${row.id}`)}
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}
