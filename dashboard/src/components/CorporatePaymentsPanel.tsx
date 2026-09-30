import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { corporateInvoices, corporateReceivables } from '../lib/admin'
import { creditShare, dateOnly, usageTone } from '../lib/corporate'
import { formatDate, formatDateTime, formatMoney } from '../lib/format'
import type { CorporateInvoice, CorporateReceivable } from '../lib/types'
import { Card } from './Card'
import { Input } from './Field'
import { Money } from './Money'
import { PermissionError } from './PermissionError'
import { Pagination } from './Pagination'
import { Table, type Column } from './Table'
import { UsageBar } from './UsageBar'

const PAGE_SIZE = 20

/**
 * Corporate side of the payments page: what each company owes (`GET /admin/corporate/receivables`, needs `payments.view` too)
 * and the invoice payments collected (`GET /admin/corporate/invoices?status=paid`, recorded by `mark-paid`, §F19.2).
 */
export function CorporatePaymentsPanel() {
  const { t, lang } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))
  const receivables = useQuery(() => corporateReceivables.list(), 'corporate-receivables')
  const collected = useQuery(() => corporateInvoices.list({ status: 'paid', from, to, page, pageSize: PAGE_SIZE }), `corporate-collected:${from}:${to}:${page}`)

  const receivableColumns: Column<CorporateReceivable>[] = [
    {
      key: 'company',
      header: t('coColCompany'),
      render: (row) => (
        <Link to={`/corporate/${row.accountId}`} className="block max-w-64 truncate font-bold text-brand hover:underline">
          {row.name}
        </Link>
      ),
    },
    {
      key: 'credit',
      header: t('coColCredit'),
      render: (row) => {
        const used = row.unbilled + row.unpaidInvoices
        const share = creditShare(used, row.creditLimit)
        return (
          <span className="block min-w-36">
            <span className="ltr-nums block font-bold">
              {formatMoney(used)} <span className="text-xs font-normal text-muted">/ {formatMoney(row.creditLimit)} {t('sar')}</span>
            </span>
            <UsageBar share={share} tone={usageTone(share)} className="mt-1" />
          </span>
        )
      },
    },
    { key: 'unbilled', header: t('coUnbilled'), className: 'text-end', render: (row) => <Money value={row.unbilled} /> },
    { key: 'unpaid', header: t('coUnpaidInvoices'), className: 'text-end', render: (row) => <Money value={row.unpaidInvoices} /> },
    {
      key: 'overdue',
      header: t('coOverdueAmount'),
      className: 'text-end',
      render: (row) => (row.overdueAmount > 0 ? <Money value={row.overdueAmount} strong className="text-danger" /> : <span className="text-muted">—</span>),
    },
  ]

  const collectedColumns: Column<CorporateInvoice>[] = [
    {
      key: 'paidAt',
      header: t('coPayColPaidAt'),
      render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.paidAt, lang)}</span>,
    },
    {
      key: 'invoice',
      header: t('coInvNumber'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="ltr-nums block font-bold">{row.invoiceNumber}</span>
          <Link to={`/corporate/${row.accountId}?tab=invoices`} className="block max-w-56 truncate text-xs font-bold text-brand hover:underline">
            {row.accountName ?? row.accountId}
          </Link>
        </span>
      ),
    },
    {
      key: 'period',
      header: t('coInvPeriod'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          {formatDate(dateOnly(row.periodStart), lang)} ← {formatDate(dateOnly(row.periodEnd), lang)}
        </span>
      ),
    },
    { key: 'total', header: t('coInvTotal'), className: 'text-end', render: (row) => <Money value={row.totalInclVat} /> },
    { key: 'paid', header: t('coInvPaidAmount'), className: 'text-end', render: (row) => <Money value={row.paidAmount} strong /> },
    { key: 'reference', header: t('coInvPayReference'), render: (row) => <span className="ltr-nums block max-w-40 truncate text-xs text-muted">{row.paymentReference ?? '—'}</span> },
  ]

  return (
    <div className="space-y-6">
      <Card title={t('coPayReceivablesTitle')} description={t('coPayReceivablesCopy')} flush>
        {receivables.error ? (
          <PermissionError error={receivables.error} permission="corporate.manage + payments.view" onRetry={receivables.reload} />
        ) : (
          <Table columns={receivableColumns} rows={receivables.data ?? []} rowKey={(row) => row.accountId} loading={receivables.loading} emptyTitle={t('coEmpty')} emptyDescription="" />
        )}
      </Card>

      <Card title={t('coPayCollectedTitle')} description={t('coPayCollectedCopy')} flush>
        <div className="grid gap-3 border-b border-line p-4 sm:grid-cols-2 sm:px-6">
          <Input id="co-pay-from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
          <Input id="co-pay-to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
        </div>
        {collected.error ? (
          <PermissionError error={collected.error} permission="corporate.manage" onRetry={collected.reload} />
        ) : (
          <>
            <Table columns={collectedColumns} rows={collected.data?.items ?? []} rowKey={(row) => row.id} loading={collected.loading} emptyTitle={t('coPayCollectedEmpty')} emptyDescription="" />
            {collected.data && <Pagination page={collected.data.page} pageSize={collected.data.pageSize} total={collected.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </div>
  )
}
