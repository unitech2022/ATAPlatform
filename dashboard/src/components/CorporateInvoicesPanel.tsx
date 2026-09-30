import { useState } from 'react'
import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { corporateAccounts, corporateInvoices } from '../lib/admin'
import { CORPORATE_INVOICE_STATUSES, dateOnly, invoiceActions, invoiceRemaining, sumInvoices } from '../lib/corporate'
import { parseEnum, saveBlob } from '../lib/finance'
import { formatDate, formatNumber } from '../lib/format'
import { corporateInvoiceStatusMeta } from '../lib/status'
import type { CorporateInvoice, CorporateInvoiceStatus } from '../lib/types'
import { MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { ConfirmModal } from './ConfirmModal'
import { CorporateInvoiceGenerateModal } from './CorporateInvoiceGenerateModal'
import { CorporateMarkPaidModal } from './CorporateMarkPaidModal'
import { Input, Select } from './Field'
import { Money } from './Money'
import { PermissionError } from './PermissionError'
import { Pagination } from './Pagination'
import { ReasonModal } from './ReasonModal'
import { Table, type Column } from './Table'
import { Tabs } from './Tabs'

const PAGE_SIZE = 20

/**
 * Corporate invoices (`GET /admin/corporate/invoices`, §F19.4): status tabs, filters, page totals incl. VAT and the
 * per-invoice actions (issue, record payment, void, authenticated PDF download) plus manual generation.
 * With `accountId` it is the invoices tab of a company; without it the cross-company overview.
 */
export function CorporateInvoicesPanel({ accountId }: { accountId?: string }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, page, setPage } = useUrlState()
  const status = parseEnum(params.get('status'), CORPORATE_INVOICE_STATUSES)
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))
  const companyParam = params.get('accountId') ?? ''
  const company = accountId ?? companyParam

  const query = useQuery(
    () => corporateInvoices.list({ accountId: company, status, from, to, page, pageSize: PAGE_SIZE }),
    `corporate-invoices:${company}:${status}:${from}:${to}:${page}`,
  )
  const accounts = useQuery(() => (accountId ? Promise.resolve(null) : corporateAccounts.list({ page: 1, pageSize: 200 })), `corporate-accounts-filter:${accountId ?? ''}`)

  const [generating, setGenerating] = useState(false)
  const [issuing, setIssuing] = useState<CorporateInvoice | null>(null)
  const [paying, setPaying] = useState<CorporateInvoice | null>(null)
  const [voiding, setVoiding] = useState<CorporateInvoice | null>(null)
  const [pdfBusy, setPdfBusy] = useState<string | null>(null)

  const rows = query.data?.items ?? []
  const totals = sumInvoices(rows)

  const issue = async () => {
    if (!issuing) return
    try {
      await corporateInvoices.issue(issuing.id)
      toast.success(t('coInvIssuedDone'))
      setIssuing(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const voidInvoice = async (reason: string) => {
    if (!voiding) return
    try {
      await corporateInvoices.void(voiding.id, reason)
      toast.success(t('coInvVoided'))
      setVoiding(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const downloadPdf = async (invoice: CorporateInvoice) => {
    setPdfBusy(invoice.id)
    try {
      const { blob, fileName } = await corporateInvoices.pdf(invoice.id)
      saveBlob(blob, fileName ?? `${invoice.invoiceNumber}.pdf`)
      toast.success(t('exported'))
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setPdfBusy(null)
    }
  }

  const columns: Column<CorporateInvoice>[] = [
    {
      key: 'number',
      header: t('coInvNumber'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="ltr-nums block font-bold">{row.invoiceNumber}</span>
          {!accountId && (
            <Link to={`/corporate/${row.accountId}`} className="block max-w-56 truncate text-xs font-bold text-brand hover:underline">
              {row.accountName ?? row.accountNumber ?? row.accountId}
            </Link>
          )}
        </span>
      ),
    },
    {
      key: 'period',
      header: t('coInvPeriod'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDate(dateOnly(row.periodStart), lang)}</span>
          <span className="block text-muted">← {formatDate(dateOnly(row.periodEnd), lang)}</span>
        </span>
      ),
    },
    {
      key: 'dates',
      header: t('coInvIssueDue'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">{formatDate(dateOnly(row.issueDate), lang)}</span>
          <span className={`block ${row.status === 'overdue' ? 'font-bold text-danger' : 'text-muted'}`}>{formatDate(dateOnly(row.dueDate), lang)}</span>
        </span>
      ),
    },
    { key: 'trips', header: t('coInvTrips'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatNumber(row.tripsCount)}</span> },
    { key: 'subtotal', header: t('coInvSubtotal'), className: 'text-end', render: (row) => <Money value={row.subtotalExclVat} /> },
    {
      key: 'vat',
      header: t('coInvVat'),
      className: 'text-end',
      render: (row) => (
        <span className="block">
          <Money value={row.vatAmount} />
          <span className="ltr-nums block text-xs text-muted">{row.vatRate}%</span>
        </span>
      ),
    },
    { key: 'total', header: t('coInvTotal'), className: 'text-end', render: (row) => <Money value={row.totalInclVat} strong /> },
    {
      key: 'paid',
      header: t('coInvPaidAmount'),
      className: 'text-end',
      render: (row) =>
        row.paidAmount ? (
          <span className="block">
            <Money value={row.paidAmount} />
            {row.paymentReference && <span className="ltr-nums block max-w-32 truncate text-xs text-muted">{row.paymentReference}</span>}
            {row.status !== 'paid' && <span className="block text-xs font-bold text-danger">{t('coInvRemaining')}: <Money value={invoiceRemaining(row)} /></span>}
          </span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={corporateInvoiceStatusMeta} value={row.status} />
          {row.status === 'void' && row.voidReason && <span className="mt-1 block max-w-40 truncate text-xs text-muted">{row.voidReason}</span>}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => {
        const allowed = invoiceActions(row.status)
        return (
          <span className="inline-flex flex-wrap justify-end gap-2">
            {allowed.issue && (
              <Button variant="brand" size="sm" icon="send" onClick={() => setIssuing(row)}>
                {t('coInvIssue')}
              </Button>
            )}
            {allowed.markPaid && (
              <Button variant="brand" size="sm" icon="check" onClick={() => setPaying(row)}>
                {t('coInvMarkPaid')}
              </Button>
            )}
            {allowed.pdf && (
              <Button variant="secondary" size="sm" icon="download" loading={pdfBusy === row.id} onClick={() => downloadPdf(row)}>
                {t('coInvPdf')}
              </Button>
            )}
            {allowed.void && (
              <Button variant="danger-outline" size="sm" icon="x" onClick={() => setVoiding(row)}>
                {t('coInvVoidAction')}
              </Button>
            )}
          </span>
        )
      },
    },
  ]

  return (
    <>
      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status}
          onChange={(value) => setFilter('status', value)}
          options={[{ value: '' as CorporateInvoiceStatus | '', label: t('statusAll') }, ...CORPORATE_INVOICE_STATUSES.map((value) => ({ value, label: t(corporateInvoiceStatusMeta[value].key) }))]}
        />
        <Button icon="receipt" onClick={() => setGenerating(true)}>
          {t('coInvGenerate')}
        </Button>
      </div>

      <div className={`mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 ${accountId ? 'lg:grid-cols-2' : 'lg:grid-cols-3'}`}>
        {!accountId && (
          <Select id="co-inv-company" aria-label={t('coColCompany')} value={companyParam} onChange={(event) => setFilter('accountId', event.target.value)}>
            <option value="">{t('coAllCompanies')}</option>
            {(accounts.data?.items ?? []).map((account) => (
              <option key={account.id} value={account.id}>
                {account.displayName}
              </option>
            ))}
          </Select>
        )}
        <Input id="co-inv-from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="co-inv-to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
      </div>

      {rows.length > 0 && (
        <div className="mb-4 grid gap-3 sm:grid-cols-3" data-testid="corporate-invoice-totals">
          {[
            { key: 'coInvSubtotal' as const, value: totals.subtotalExclVat },
            { key: 'coInvVat' as const, value: totals.vatAmount },
            { key: 'coInvTotal' as const, value: totals.totalInclVat },
          ].map((item) => (
            <div key={item.key} className="rounded-2xl bg-white px-4 py-3 shadow-soft">
              <p className="text-xs font-bold text-muted">
                {t(item.key)} · {t('coInvPageTotals')}
              </p>
              <p className="mt-1 text-lg font-bold">
                <Money value={item.value} strong />
              </p>
            </div>
          ))}
        </div>
      )}

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="corporate.manage" onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('coInvEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <CorporateInvoiceGenerateModal
        open={generating}
        accountId={accountId}
        onClose={() => setGenerating(false)}
        onGenerated={() => {
          setGenerating(false)
          toast.success(t('coInvGenerated'))
          query.reload()
        }}
      />
      <CorporateMarkPaidModal
        invoice={paying}
        onClose={() => setPaying(null)}
        onSaved={() => {
          setPaying(null)
          toast.success(t('coInvPayRecorded'))
          query.reload()
        }}
      />
      <ConfirmModal
        open={issuing !== null}
        title={t('coInvIssueTitle')}
        description={issuing ? `${issuing.invoiceNumber} — ${t('coInvIssueCopy')}` : undefined}
        confirmLabel={t('coInvIssue')}
        confirmVariant="brand"
        onClose={() => setIssuing(null)}
        onConfirm={issue}
      />
      <ReasonModal
        open={voiding !== null}
        title={t('coInvVoidTitle')}
        description={voiding ? `${voiding.invoiceNumber} — ${t('coInvVoidCopy')}` : undefined}
        confirmLabel={t('coInvVoidAction')}
        onClose={() => setVoiding(null)}
        onConfirm={voidInvoice}
      />
    </>
  )
}
