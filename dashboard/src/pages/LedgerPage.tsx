import { Badge } from '../components/Badge'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input } from '../components/Field'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { ledger } from '../lib/admin'
import { ledgerAccountKey } from '../lib/finance'
import { daysAgoIso, todayIso } from '../lib/pricing'
import type { LedgerBalance } from '../lib/types'

export function LedgerPage() {
  const { t } = useLang()
  const { params, setFilter } = useUrlState()
  const from = parseIsoDate(params.get('from')) || daysAgoIso(30)
  const to = parseIsoDate(params.get('to')) || todayIso()
  const query = useQuery(() => ledger.balances({ from, to }), `ledger:${from}:${to}`)
  const rows = query.data ?? []
  const totalDebit = rows.reduce((sum, row) => sum + row.debit, 0)
  const totalCredit = rows.reduce((sum, row) => sum + row.credit, 0)
  const balanced = Math.abs(totalDebit - totalCredit) < 0.005

  const columns: Column<LedgerBalance>[] = [
    {
      key: 'account',
      header: t('account'),
      render: (row) => {
        const key = ledgerAccountKey(row.account)
        return (
          <span className="block">
            <span className="block font-bold">{key ? t(key) : row.account}</span>
            <span className="ltr-nums block text-xs text-muted">{row.account}</span>
          </span>
        )
      },
    },
    { key: 'debit', header: t('debit'), className: 'text-end', render: (row) => <Money value={row.debit} /> },
    { key: 'credit', header: t('credit'), className: 'text-end', render: (row) => <Money value={row.credit} /> },
    { key: 'balance', header: t('balance'), className: 'text-end', render: (row) => <Money value={row.balance} signed strong /> },
  ]

  return (
    <>
      <PageHeader title={t('ledgerTitle')} description={t('ledgerCopy')} />
      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:max-w-xl">
        <Input id="from" type="date" dir="ltr" label={t('fromDate')} value={from} max={to} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="to" type="date" dir="ltr" label={t('toDate')} value={to} min={from} onChange={(event) => setFilter('to', event.target.value)} />
      </div>
      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.account} loading={query.loading} emptyTitle={t('noLedgerLines')} />
            {rows.length > 0 && (
              <div className="flex flex-wrap items-center justify-between gap-3 border-t border-line px-5 py-4 text-sm sm:px-6">
                <Badge tone={balanced ? 'brand' : 'danger'}>{balanced ? t('ledgerBalanced') : t('ledgerUnbalanced')}</Badge>
                <span className="flex flex-wrap gap-4 text-muted">
                  <span>
                    {t('debit')}: <Money value={totalDebit} strong />
                  </span>
                  <span>
                    {t('credit')}: <Money value={totalCredit} strong />
                  </span>
                </span>
              </div>
            )}
          </>
        )}
      </Card>
    </>
  )
}
