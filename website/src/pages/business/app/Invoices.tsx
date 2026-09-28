import { useState } from 'react'
import { Link } from 'react-router'
import { InvoiceStatusPill } from '../../../components/business/StatusPills'
import { PageHeader, TableWrap, Td, Th } from '../../../components/business/ui'
import { Action } from '../../../components/Button'
import { Select } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { Pagination } from '../../../components/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { corporateApi } from '../../../lib/api'
import { formatDate, formatMoney } from '../../../lib/format'
import { useResource } from '../../../lib/useResource'
import { useInvoiceDownloads } from './useInvoiceDownloads'

const STATUSES = ['issued', 'overdue', 'paid', 'void'] as const

export function Invoices() {
  const { t, lang } = useI18n()
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const invoices = useResource(() => corporateApi.invoices({ status: status || undefined, page }), [status, page, lang])
  const { busy, error, download } = useInvoiceDownloads()

  return (
    <>
      <title>{t('biz.nav.invoices')} · ATA</title>
      <PageHeader title={t('biz.invoices.title')} subtitle={t('biz.invoices.subtitle')} />
      <div className="mb-4 max-w-xs">
        <Select
          aria-label={t('biz.trip.status')}
          value={status}
          onChange={(event) => {
            setStatus(event.target.value)
            setPage(1)
          }}
        >
          <option value="">{t('biz.allStatuses')}</option>
          {STATUSES.map((value) => (
            <option key={value} value={value}>
              {t(`invoice.status.${value}`)}
            </option>
          ))}
        </Select>
      </div>
      {error && (
        <Notice tone="error" className="mb-4">
          {error}
        </Notice>
      )}
      {invoices.loading && !invoices.data ? (
        <LoadingState />
      ) : invoices.error && !invoices.data ? (
        <ErrorState error={invoices.error} onRetry={() => invoices.reload()} />
      ) : invoices.data && invoices.data.items.length > 0 ? (
        <>
          <TableWrap>
            <thead>
              <tr>
                <Th>{t('biz.invoice.number')}</Th>
                <Th>{t('biz.invoice.period')}</Th>
                <Th>{t('biz.invoice.dueDate')}</Th>
                <Th>{t('biz.invoice.trips')}</Th>
                <Th>{t('biz.invoice.total')}</Th>
                <Th>{t('biz.trip.status')}</Th>
                <Th>
                  <span className="sr-only">{t('biz.actions')}</span>
                </Th>
              </tr>
            </thead>
            <tbody>
              {invoices.data.items.map((invoice) => (
                <tr key={invoice.id} className="hover:bg-cloud">
                  <Td>
                    <Link to={`/business/app/invoices/${invoice.id}`} className="font-bold text-brand" dir="ltr">
                      {invoice.invoiceNumber}
                    </Link>
                  </Td>
                  <Td className="whitespace-nowrap">
                    {formatDate(invoice.periodStart, lang)} – {formatDate(invoice.periodEnd, lang)}
                  </Td>
                  <Td className="whitespace-nowrap">{formatDate(invoice.dueDate, lang)}</Td>
                  <Td>{invoice.tripsCount}</Td>
                  <Td className="whitespace-nowrap font-bold">{formatMoney(invoice.totalInclVat, lang)}</Td>
                  <Td>
                    <InvoiceStatusPill status={invoice.status} />
                  </Td>
                  <Td>
                    <Action
                      onClick={() => void download(invoice, 'pdf')}
                      disabled={busy !== null || invoice.status === 'draft'}
                      className="flex items-center gap-1.5 whitespace-nowrap rounded-xl px-3 py-2 text-xs font-bold text-brand hover:bg-brand-soft disabled:text-muted"
                    >
                      <Icon name="download" className="size-4" />
                      {busy === `${invoice.id}:pdf` ? t('biz.downloading') : 'PDF'}
                    </Action>
                  </Td>
                </tr>
              ))}
            </tbody>
          </TableWrap>
          <Pagination className="mt-4" page={invoices.data.page} pageSize={invoices.data.pageSize} total={invoices.data.total} onChange={setPage} />
        </>
      ) : (
        <EmptyState icon="receipt" title={t('biz.invoices.empty')}>
          {t('biz.invoices.emptyHint')}
        </EmptyState>
      )}
    </>
  )
}
