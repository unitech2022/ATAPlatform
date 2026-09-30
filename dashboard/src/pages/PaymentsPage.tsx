import { useNavigate } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Card } from '../components/Card'
import { CorporatePaymentsPanel } from '../components/CorporatePaymentsPanel'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput, Select } from '../components/Field'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { payments } from '../lib/admin'
import { lookupKey, parseEnum, PAYMENT_CHANNEL_KEY, PAYMENT_CHANNELS, PAYMENT_PURPOSE_KEY, PAYMENT_PURPOSES, PAYMENT_STATUSES } from '../lib/finance'
import { formatDateTime } from '../lib/format'
import { paymentStatusMeta } from '../lib/status'
import type { PaymentListItem, PaymentStatus } from '../lib/types'

const PAGE_SIZE = 20

const VIEWS = ['cards', 'corporate'] as const

/** Payments: gateway payments (F11) plus a "corporate" view with receivables and collected company invoices (F19). */
export function PaymentsPage() {
  const { t } = useLang()
  const { params, update } = useUrlState()
  const view = parseEnum(params.get('view'), VIEWS) || 'cards'

  return (
    <>
      <PageHeader title={t('paymentsTitle')} description={t('paymentsCopy')} />
      <Tabs
        className="mb-4"
        value={view}
        onChange={(value) =>
          update((next) => {
            for (const key of Array.from(next.keys())) next.delete(key)
            if (value === 'corporate') next.set('view', value)
          })
        }
        options={[
          { value: 'cards' as (typeof VIEWS)[number], label: t('coPayViewCards') },
          { value: 'corporate' as (typeof VIEWS)[number], label: t('coPayViewCorporate') },
        ]}
      />
      {view === 'corporate' ? <CorporatePaymentsPanel /> : <CardPayments />}
    </>
  )
}

function CardPayments() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()

  const status = parseEnum(params.get('status'), PAYMENT_STATUSES)
  const purpose = parseEnum(params.get('purpose'), PAYMENT_PURPOSES)
  const method = parseEnum(params.get('method'), PAYMENT_CHANNELS)
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))

  const query = useQuery(
    () => payments.list({ status, purpose, method, from, to, search: search.value, page, pageSize: PAGE_SIZE }),
    `payments:${status}:${purpose}:${method}:${from}:${to}:${search.value}:${page}`,
  )

  const columns: Column<PaymentListItem>[] = [
    {
      key: 'createdAt',
      header: t('createdAt'),
      render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span>,
    },
    {
      key: 'user',
      header: t('customer'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block truncate font-bold">{row.userName || t('unnamed')}</span>
          {row.userPhone && <span className="ltr-nums block text-xs text-muted">{row.userPhone}</span>}
        </span>
      ),
    },
    {
      key: 'purpose',
      header: t('purpose'),
      render: (row) => (
        <span className="block">
          <span className="block">{t(lookupKey(PAYMENT_PURPOSE_KEY, row.purpose) ?? 'purposeTrip')}</span>
          {row.tripNumber && <span className="ltr-nums block text-xs text-muted">{row.tripNumber}</span>}
        </span>
      ),
    },
    {
      key: 'method',
      header: t('method'),
      render: (row) => (
        <span className="block">
          <span className="block">{t(lookupKey(PAYMENT_CHANNEL_KEY, row.method) ?? 'paymentCard')}</span>
          <span className="ltr-nums block text-xs text-muted">{row.provider}</span>
        </span>
      ),
    },
    { key: 'amount', header: t('amount'), className: 'text-end', render: (row) => <Money value={row.amount} strong /> },
    { key: 'captured', header: t('captured'), className: 'text-end', render: (row) => <Money value={row.capturedAmount} /> },
    {
      key: 'refunded',
      header: t('refunded'),
      className: 'text-end',
      render: (row) => (row.refundedAmount > 0 ? <Money value={row.refundedAmount} /> : <span className="text-muted">—</span>),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={paymentStatusMeta} value={row.status} /> },
    {
      key: 'gateway',
      header: t('gatewayRef'),
      render: (row) => <span className="ltr-nums block max-w-40 truncate text-xs text-muted">{row.gatewayPaymentId ?? '—'}</span>,
    },
  ]

  return (
    <>
      <Tabs
        className="mb-4"
        value={status}
        onChange={(value) => setFilter('status', value)}
        options={[
          { value: '' as PaymentStatus | '', label: t('statusAll') },
          ...PAYMENT_STATUSES.map((value) => ({ value, label: t(paymentStatusMeta[value].key) })),
        ]}
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto_auto_auto]">
        <SearchInput
          placeholder={t('searchPayments')}
          value={search.input}
          onChange={(event) => search.setInput(event.target.value)}
          wrapperClassName="sm:col-span-2 lg:col-span-1"
        />
        <Select id="purpose" aria-label={t('purpose')} value={purpose} onChange={(event) => setFilter('purpose', event.target.value)} wrapperClassName="lg:w-44">
          <option value="">{t('allPurposes')}</option>
          {PAYMENT_PURPOSES.map((value) => (
            <option key={value} value={value}>
              {t(PAYMENT_PURPOSE_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="method" aria-label={t('method')} value={method} onChange={(event) => setFilter('method', event.target.value)} wrapperClassName="lg:w-40">
          <option value="">{t('allMethods')}</option>
          {PAYMENT_CHANNELS.map((value) => (
            <option key={value} value={value}>
              {t(PAYMENT_CHANNEL_KEY[value])}
            </option>
          ))}
        </Select>
        <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} wrapperClassName="lg:w-44" />
        <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} wrapperClassName="lg:w-44" />
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
              onRowClick={(row) => navigate(`/payments/${row.id}`)}
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}
