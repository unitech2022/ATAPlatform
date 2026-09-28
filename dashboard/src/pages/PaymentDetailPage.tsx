import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { JsonView } from '../components/JsonView'
import { Money } from '../components/Money'
import { RefundActions } from '../components/RefundActions'
import { RefundModal } from '../components/RefundModal'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { payments } from '../lib/admin'
import { ledgerAccountKey, lookupKey, PAYMENT_CHANNEL_KEY, PAYMENT_PURPOSE_KEY, REFUND_DESTINATION_KEY, REFUND_REASON_KEY, WALLET_TX_KEY } from '../lib/finance'
import { formatDateTime, formatMoney } from '../lib/format'
import { paymentStatusMeta, refundStatusMeta, webhookStatusMeta } from '../lib/status'
import type { LedgerLine, PaymentDetail, PaymentWebhookEvent, Refund, RefundInput } from '../lib/types'

export function PaymentDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const query = useQuery(() => payments.get(id), `payment:${id}`)
  const [refundOpen, setRefundOpen] = useState(false)

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const payment = query.data
  const refunds = payment.refunds ?? []
  const captured = payment.capturedAmount ?? 0
  // Refunds still in flight count against the refundable amount too.
  const inFlight = refunds.filter((refund) => ['pending_approval', 'approved', 'processing'].includes(refund.status)).reduce((sum, refund) => sum + refund.amount, 0)
  const refundable = Math.max(0, Math.round((captured - payment.refundedAmount - inFlight) * 100) / 100)
  const canRefund = (payment.status === 'captured' || payment.status === 'partially_refunded') && refundable > 0
  const awaiting = refunds.filter((refund) => refund.status === 'pending_approval')

  const submitRefund = async (input: RefundInput) => {
    const refund = await payments.refund(payment.id, input)
    toast.success(refund.status === 'pending_approval' ? t('refundPendingApproval') : t('refundCreated'), refund.refundNumber)
    setRefundOpen(false)
    query.reload()
  }

  return (
    <>
      <Link to="/payments" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('backToPayments')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="wallet" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-brand">{t(lookupKey(PAYMENT_PURPOSE_KEY, payment.purpose) ?? 'purposeTrip')}</p>
              <h2 className="text-2xl font-bold leading-tight">
                <Money value={payment.amount} strong />
              </h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={paymentStatusMeta} value={payment.status} />
                <Badge tone="muted">
                  {t(lookupKey(PAYMENT_CHANNEL_KEY, payment.method) ?? 'paymentCard')}
                  {payment.card ? ` · ${payment.card.brand} •••• ${payment.card.last4}` : ''}
                </Badge>
                <span>
                  {payment.userName || t('unnamed')}
                  {payment.userPhone ? <span className="ltr-nums"> · {payment.userPhone}</span> : null}
                </span>
                {payment.tripId && (
                  <Link to={`/trips/${payment.tripId}`} className="ltr-nums font-bold text-brand hover:underline">
                    {payment.tripNumber ?? t('viewTrip')}
                  </Link>
                )}
              </div>
            </div>
          </div>
          {canRefund && (
            <Button icon="refresh" onClick={() => setRefundOpen(true)}>
              {t('refund')}
            </Button>
          )}
        </div>

        {payment.failureCode && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>
              <span className="ltr-nums font-bold">{payment.failureCode}</span>
              {payment.failureMessage ? ` — ${payment.failureMessage}` : ''}
            </p>
          </div>
        )}

        {awaiting.map((refund) => (
          <div key={refund.id} className="mt-5 flex flex-col gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-800 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-start gap-3">
              <Icon name="shield" className="mt-0.5 size-4 shrink-0" />
              <p>
                <span className="font-bold">{t('awaitingSecondApproval')}: </span>
                <span className="ltr-nums">{refund.refundNumber}</span> · <Money value={refund.amount} strong />
                {refund.requestedByName ? ` · ${t('requestedBy')} ${refund.requestedByName}` : ''}
              </p>
            </div>
            <RefundActions refund={refund} onChanged={query.reload} />
          </div>
        ))}
      </Card>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('amounts')} className="lg:col-span-2">
          <DefinitionList
            items={[
              { label: t('amount'), value: `${formatMoney(payment.amount)} ${t('sar')}`, ltr: true },
              { label: t('authorized'), value: `${formatMoney(payment.authorizedAmount)} ${t('sar')}`, ltr: true },
              { label: t('captured'), value: `${formatMoney(payment.capturedAmount)} ${t('sar')}`, ltr: true },
              { label: t('refunded'), value: `${formatMoney(payment.refundedAmount)} ${t('sar')}`, ltr: true },
              { label: t('refundable'), value: `${formatMoney(refundable)} ${t('sar')}`, ltr: true },
              { label: t('captureMode'), value: payment.captureMode === 'manual' ? t('captureManual') : t('captureAuto') },
            ]}
          />
        </Card>
        <Card title={t('timeline')}>
          <PaymentTimeline payment={payment} />
        </Card>
      </div>

      <Card title={t('gatewayRefs')} className="mb-6">
        <DefinitionList
          items={[
            { label: t('provider'), value: payment.provider, ltr: true },
            { label: t('gatewayPaymentId'), value: payment.gatewayPaymentId ?? '—', ltr: true },
            { label: t('gatewayStatus'), value: payment.gatewayStatus ?? '—', ltr: true },
            { label: t('idempotencyKey'), value: payment.idempotencyKey ?? '—', ltr: true },
            { label: t('paymentId'), value: payment.id, ltr: true },
            ...(payment.actionUrl ? [{ label: t('actionUrl'), value: payment.actionUrl, ltr: true }] : []),
          ]}
        />
      </Card>

      <Card title={t('refunds')} flush className="mb-6">
        <RefundsTable refunds={refunds} onChanged={query.reload} />
      </Card>

      <Card title={t('ledgerLines')} description={t('ledgerLinesCopy')} flush className="mb-6">
        <LedgerTable lines={payment.ledger ?? []} />
      </Card>

      <Card title={t('webhookEvents')} flush className="mb-6">
        <WebhookTable events={payment.webhookEvents ?? []} />
      </Card>

      {payment.metadata !== undefined && payment.metadata !== null && (
        <Card title={t('metadata')}>
          <JsonView label={t('metadata')} value={payment.metadata} />
        </Card>
      )}

      <RefundModal
        open={refundOpen}
        description={`${payment.tripNumber ?? payment.id} — ${formatDateTime(payment.createdAt, lang)}`}
        refundable={refundable}
        allowOriginalMethod={payment.method === 'card' || payment.method === 'apple_pay'}
        onClose={() => setRefundOpen(false)}
        onSubmit={submitRefund}
      />
    </>
  )
}

function PaymentTimeline({ payment }: { payment: PaymentDetail }) {
  const { t, lang } = useLang()
  const steps: { key: string; label: TranslationKey; at: string | null; tone: 'brand' | 'danger' | 'muted' }[] = [
    { key: 'created', label: 'tlPaymentCreated', at: payment.createdAt, tone: 'brand' },
    { key: 'authorized', label: 'tlAuthorized', at: payment.authorizedAt, tone: 'brand' },
    { key: 'captured', label: 'tlCaptured', at: payment.capturedAt, tone: 'brand' },
  ]
  if (payment.failedAt) steps.push({ key: 'failed', label: 'tlFailed', at: payment.failedAt, tone: 'danger' })
  if (payment.voidedAt) steps.push({ key: 'voided', label: 'tlVoided', at: payment.voidedAt, tone: 'muted' })
  for (const refund of payment.refunds ?? []) {
    if (refund.status === 'succeeded') steps.push({ key: `refund-${refund.id}`, label: 'tlRefunded', at: refund.processedAt ?? refund.createdAt, tone: 'brand' })
  }
  const rows = steps.filter((step) => step.at !== null || step.key === 'authorized' || step.key === 'captured')
  rows.sort((a, b) => (a.at && b.at ? a.at.localeCompare(b.at) : a.at ? -1 : b.at ? 1 : 0))

  return (
    <ol className="relative space-y-5 border-s-2 border-line ps-6">
      {rows.map((step) => {
        const done = step.at !== null
        const ring = !done ? 'bg-cloud text-muted' : step.tone === 'danger' ? 'bg-danger-soft text-danger' : step.tone === 'muted' ? 'bg-cloud text-ink' : 'bg-brand-soft text-brand'
        return (
          <li key={step.key} className="relative">
            <span className={`absolute -start-[31px] top-1 grid size-5 place-items-center rounded-full ring-4 ring-white ${ring}`}>
              <Icon name={step.tone === 'danger' ? 'x' : done ? 'check' : 'clock'} className="size-3" />
            </span>
            <p className={`font-bold ${done ? '' : 'text-muted'}`}>{t(step.label)}</p>
            <p className="mt-0.5 text-xs text-muted">{done ? formatDateTime(step.at, lang) : t('notYet')}</p>
          </li>
        )
      })}
    </ol>
  )
}

function RefundsTable({ refunds, onChanged }: { refunds: Refund[]; onChanged: () => void }) {
  const { t, lang } = useLang()
  const columns: Column<Refund>[] = [
    { key: 'number', header: t('refundNumber'), render: (row) => <span className="ltr-nums font-bold">{row.refundNumber}</span> },
    { key: 'amount', header: t('amount'), className: 'text-end', render: (row) => <Money value={row.amount} strong /> },
    { key: 'destination', header: t('destination'), render: (row) => t(lookupKey(REFUND_DESTINATION_KEY, row.destination) ?? 'destinationWallet') },
    {
      key: 'reason',
      header: t('reason'),
      render: (row) => (
        <span className="block max-w-64">
          <span className="block font-bold">{t(lookupKey(REFUND_REASON_KEY, row.reasonCode) ?? 'reasonOther')}</span>
          {row.reason && <span className="block truncate text-xs text-muted">{row.reason}</span>}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={refundStatusMeta} value={row.status} />
          {(row.rejectedReason || row.failureMessage) && <span className="mt-1 block max-w-56 truncate text-xs text-danger">{row.rejectedReason ?? row.failureMessage}</span>}
        </span>
      ),
    },
    {
      key: 'people',
      header: t('requestedBy'),
      render: (row) => (
        <span className="block text-xs">
          <span className="block font-bold">{row.requestedByName ?? '—'}</span>
          {row.approvedByName && (
            <span className="block text-muted">
              {t('approvedBy')}: {row.approvedByName}
            </span>
          )}
        </span>
      ),
    },
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span> },
    { key: 'actions', header: t('actions'), className: 'text-end', render: (row) => <RefundActions refund={row} onChanged={onChanged} /> },
  ]
  return <Table columns={columns} rows={refunds} rowKey={(row) => row.id} emptyTitle={t('noRefunds')} emptyDescription="" />
}

function LedgerTable({ lines }: { lines: LedgerLine[] }) {
  const { t, lang } = useLang()
  const totalDebit = lines.reduce((sum, line) => sum + line.debit, 0)
  const totalCredit = lines.reduce((sum, line) => sum + line.credit, 0)
  const balanced = Math.abs(totalDebit - totalCredit) < 0.005

  const columns: Column<LedgerLine>[] = [
    {
      key: 'account',
      header: t('account'),
      render: (row) => {
        const key = ledgerAccountKey(row.account)
        return (
          <span className="block">
            {key && <span className="block font-bold">{t(key)}</span>}
            <span className="ltr-nums block max-w-64 truncate text-xs text-muted">{row.account}</span>
          </span>
        )
      },
    },
    {
      key: 'type',
      header: t('type'),
      render: (row) => {
        const key = lookupKey(WALLET_TX_KEY, row.type)
        return key ? t(key) : <span className="ltr-nums text-xs text-muted">{row.type ?? '—'}</span>
      },
    },
    { key: 'debit', header: t('debit'), className: 'text-end', render: (row) => (row.debit ? <Money value={row.debit} /> : <span className="text-muted">—</span>) },
    { key: 'credit', header: t('credit'), className: 'text-end', render: (row) => (row.credit ? <Money value={row.credit} /> : <span className="text-muted">—</span>) },
    { key: 'description', header: t('description'), render: (row) => <span className="block max-w-64 truncate text-muted">{row.description ?? '—'}</span> },
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span> },
  ]

  return (
    <>
      <Table columns={columns} rows={lines} rowKey={(row) => row.id} emptyTitle={t('noLedgerLines')} emptyDescription="" />
      {lines.length > 0 && (
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
  )
}

function WebhookTable({ events }: { events: PaymentWebhookEvent[] }) {
  const { t, lang } = useLang()
  const [expanded, setExpanded] = useState<string | null>(null)
  const columns: Column<PaymentWebhookEvent>[] = [
    {
      key: 'type',
      header: t('eventType'),
      render: (row) => (
        <span className="block">
          <Badge tone="ink">{row.eventType}</Badge>
          <span className="ltr-nums mt-1 block max-w-48 truncate text-xs text-muted">{row.eventId}</span>
        </span>
      ),
    },
    {
      key: 'signature',
      header: t('signature'),
      render: (row) => <Badge tone={row.signatureValid ? 'brand' : 'danger'}>{row.signatureValid ? t('signatureValid') : t('signatureInvalid')}</Badge>,
    },
    {
      key: 'processing',
      header: t('processing'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={webhookStatusMeta} value={row.processingStatus} />
          {row.error && <span className="mt-1 block max-w-56 truncate text-xs text-danger">{row.error}</span>}
        </span>
      ),
    },
    { key: 'receivedAt', header: t('receivedAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.receivedAt, lang)}</span> },
    {
      key: 'details',
      header: t('details'),
      className: 'text-end',
      render: (row) =>
        row.payload !== undefined && row.payload !== null ? (
          <Button variant="secondary" size="sm" icon={expanded === row.id ? 'x' : 'eye'} onClick={() => setExpanded((current) => (current === row.id ? null : row.id))}>
            {expanded === row.id ? t('hideDetails') : t('showDetails')}
          </Button>
        ) : null,
    },
  ]
  return (
    <Table
      columns={columns}
      rows={events}
      rowKey={(row) => row.id}
      emptyTitle={t('noWebhookEvents')}
      emptyDescription=""
      renderExpanded={(row) => (expanded === row.id ? <JsonView label={t('payload')} value={row.payload} /> : null)}
    />
  )
}
