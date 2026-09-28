import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { InvoiceStatusPill } from '../../../components/business/StatusPills'
import { PageHeader, StatCard, TableWrap, Td, Th } from '../../../components/business/ui'
import { Button } from '../../../components/Button'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { Pagination } from '../../../components/Pagination'
import { ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { corporateApi } from '../../../lib/api'
import { formatDate, formatDateTime, formatMoney, formatNumber } from '../../../lib/format'
import { useResource } from '../../../lib/useResource'
import { useInvoiceDownloads } from './useInvoiceDownloads'

export function InvoiceDetail() {
  const { id = '' } = useParams()
  const { t, lang } = useI18n()
  const [page, setPage] = useState(1)
  const invoice = useResource(() => corporateApi.invoice(id, page), [id, page, lang])
  const { busy, error, download } = useInvoiceDownloads()
  const data = invoice.data

  const back = (
    <Link to="/business/app/invoices" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-muted hover:text-ink">
      <Icon name="arrow" className="size-4 ltr:rotate-180" />
      {t('biz.invoices.title')}
    </Link>
  )

  if (invoice.loading && !data) return <LoadingState />
  if (invoice.error && !data) {
    return (
      <>
        {back}
        <ErrorState error={invoice.error} onRetry={() => invoice.reload()} />
      </>
    )
  }
  if (!data) return null

  return (
    <>
      <title>{`${data.invoiceNumber} · ATA`}</title>
      {back}
      <PageHeader
        title={data.invoiceNumber}
        subtitle={t('biz.invoice.periodLabel', { from: formatDate(data.periodStart, lang), to: formatDate(data.periodEnd, lang) })}
        actions={
          <>
            <InvoiceStatusPill status={data.status} />
            <Button variant="secondary" size="sm" disabled={busy !== null} onClick={() => void download(data, 'csv')}>
              <Icon name="download" className="size-4" />
              {busy === `${data.id}:csv` ? t('biz.downloading') : 'CSV'}
            </Button>
            <Button size="sm" disabled={busy !== null || data.status === 'draft'} onClick={() => void download(data, 'pdf')}>
              <Icon name="download" className="size-4" />
              {busy === `${data.id}:pdf` ? t('biz.downloading') : t('biz.invoice.pdf')}
            </Button>
          </>
        }
      />
      {error && (
        <Notice tone="error" className="mb-4">
          {error}
        </Notice>
      )}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard icon="receipt" label={t('biz.invoice.subtotal')} value={formatMoney(data.subtotalExclVat, lang)} />
        <StatCard icon="document" label={t('biz.invoice.vat', { rate: formatNumber(data.vatRate) })} value={formatMoney(data.vatAmount, lang)} />
        <StatCard icon="wallet" label={t('biz.invoice.total')} value={formatMoney(data.totalInclVat, lang)} hint={t('biz.invoice.trips') + ': ' + data.tripsCount} />
        <StatCard
          icon="calendar"
          label={t('biz.invoice.dueDate')}
          value={formatDate(data.dueDate, lang)}
          hint={
            data.paidAt
              ? t('biz.invoice.paidOn', { date: formatDate(data.paidAt, lang), amount: formatMoney(data.paidAmount, lang) })
              : t('biz.invoice.issuedOn', { date: formatDate(data.issueDate, lang) })
          }
        />
      </div>

      <h2 className="mb-4 mt-8 text-xl font-bold">{t('biz.invoice.lines')}</h2>
      <TableWrap>
        <thead>
          <tr>
            <Th>{t('biz.invoice.line')}</Th>
            <Th>{t('biz.trip.time')}</Th>
            <Th>{t('biz.trip.rider')}</Th>
            <Th>{t('biz.employee.department')}</Th>
            <Th>{t('biz.book.purpose')}</Th>
            <Th>{t('biz.invoice.exclVat')}</Th>
            <Th>{t('biz.invoice.vatShort')}</Th>
            <Th>{t('biz.invoice.inclVat')}</Th>
          </tr>
        </thead>
        <tbody>
          {data.lines.items.map((line) => (
            <tr key={line.id}>
              <Td>
                <span className="block font-bold" dir={line.tripNumber ? 'ltr' : undefined}>
                  {line.tripNumber ?? t(`biz.invoice.lineType.${line.lineType}`)}
                </span>
                <span className="block max-w-56 truncate text-xs text-muted">
                  {line.pickupName && line.dropoffName ? `${line.pickupName} ${lang === 'ar' ? '←' : '→'} ${line.dropoffName}` : line.description}
                </span>
              </Td>
              <Td className="whitespace-nowrap text-muted">{line.tripDate ? formatDateTime(line.tripDate, lang) : '—'}</Td>
              <Td>{line.guestName ? `${line.guestName} (${t('biz.trip.guest')})` : (line.employeeName ?? '—')}</Td>
              <Td>
                {line.department ?? '—'}
                {line.costCenterCode && <span className="block text-xs text-muted">{line.costCenterCode}</span>}
              </Td>
              <Td className="max-w-48 truncate">{line.purpose ?? '—'}</Td>
              <Td className="whitespace-nowrap">{formatMoney(line.amountExclVat, lang)}</Td>
              <Td className="whitespace-nowrap">{formatMoney(line.vatAmount, lang)}</Td>
              <Td className="whitespace-nowrap font-bold">{formatMoney(line.amountInclVat, lang)}</Td>
            </tr>
          ))}
        </tbody>
      </TableWrap>
      <Pagination className="mt-4" page={data.lines.page} pageSize={data.lines.pageSize} total={data.lines.total} onChange={setPage} />
    </>
  )
}
