import { useState } from 'react'
import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useQuery } from '../hooks/useQuery'
import { payments } from '../lib/admin'
import { lookupKey, REFUND_DESTINATION_KEY } from '../lib/finance'
import { formatDateTime, formatMoney } from '../lib/format'
import { paymentStatusMeta, refundStatusMeta } from '../lib/status'
import { PAYMENT_METHOD_KEY } from '../lib/trips'
import type { RefundInput, TripDetail } from '../lib/types'
import { Badge, MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { ErrorState } from './ErrorState'
import { Icon } from './Icon'
import { Money } from './Money'
import { RefundModal } from './RefundModal'
import { PageSpinner } from './Spinner'

/** Trip detail "الدفع" section (docs/08 §F11.10): payment, receipt and refund. */
export function TripPaymentCard({ trip }: { trip: TripDetail }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const [refundOpen, setRefundOpen] = useState(false)
  const hasReceipt = trip.status === 'completed' || trip.status === 'cancelled'
  const receipt = useQuery(() => (hasReceipt ? payments.receipt(trip.id) : Promise.resolve(null)), `trip-receipt:${trip.id}:${trip.status}`)
  const payment = trip.payment ?? null
  const data = receipt.data
  const refundedSoFar = (data?.refunds ?? []).filter((refund) => refund.status !== 'rejected' && refund.status !== 'failed').reduce((sum, refund) => sum + refund.amount, 0)
  const paid = data?.payment?.paidAmount ?? data?.total ?? trip.finalFare ?? null
  const refundable = paid === null ? null : Math.max(0, Math.round((paid - refundedSoFar) * 100) / 100)
  // Corporate trips are billed on the company invoice (§F19.2): there is no payment to refund to a wallet or card.
  const isCorporate = trip.paymentMethod === 'corporate'
  const canRefund = !isCorporate && trip.status === 'completed' && (refundable === null || refundable > 0)

  const submitRefund = async (input: RefundInput) => {
    const refund = payment ? await payments.refund(payment.id, input) : await payments.refundTrip(trip.id, { amount: input.amount, reasonCode: input.reasonCode, reason: input.reason })
    toast.success(refund.status === 'pending_approval' ? t('refundPendingApproval') : t('refundCreated'), refund.refundNumber)
    setRefundOpen(false)
    receipt.reload()
  }

  return (
    <Card
      title={t('paymentSection')}
      className="mb-6"
      action={
        canRefund ? (
          <Button variant="secondary" size="sm" icon="refresh" onClick={() => setRefundOpen(true)}>
            {t('refund')}
          </Button>
        ) : undefined
      }
    >
      <div className="grid gap-6 lg:grid-cols-2">
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone="ink">{t(PAYMENT_METHOD_KEY[trip.paymentMethod] ?? 'paymentCash')}</Badge>
            {payment && <MetaBadge record={paymentStatusMeta} value={payment.status} />}
            {payment?.last4 && (
              <span className="ltr-nums text-sm text-muted">
                {payment.brand ?? ''} •••• {payment.last4}
              </span>
            )}
            {data?.payment?.fallbackToCash && <Badge tone="warning">{t('fallbackToCash')}</Badge>}
          </div>
          {isCorporate && <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('coTcBilledToCompany')}</p>}
          {payment && (
            <dl className="grid grid-cols-2 gap-3 text-sm">
              <div className="rounded-2xl bg-cloud px-4 py-3">
                <dt className="text-xs font-bold text-muted">{t('authorized')}</dt>
                <dd className="mt-1 font-bold">
                  <Money value={payment.authorizedAmount} />
                </dd>
              </div>
              <div className="rounded-2xl bg-cloud px-4 py-3">
                <dt className="text-xs font-bold text-muted">{t('captured')}</dt>
                <dd className="mt-1 font-bold">
                  <Money value={payment.capturedAmount} />
                </dd>
              </div>
            </dl>
          )}
          {payment && (
            <Link to={`/payments/${payment.id}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
              {t('viewPayment')}
              <Icon name="chevron" className="size-4 rtl:rotate-180" />
            </Link>
          )}
          {(data?.refunds.length ?? 0) > 0 && (
            <div>
              <p className="mb-2 text-sm font-bold">{t('refunds')}</p>
              <ul className="space-y-2">
                {data?.refunds.map((refund) => (
                  <li key={refund.id} className="flex flex-wrap items-center justify-between gap-2 rounded-2xl border border-line px-4 py-2 text-sm">
                    <span>
                      <Money value={refund.amount} strong /> · {t(lookupKey(REFUND_DESTINATION_KEY, refund.destination) ?? 'destinationWallet')}
                      <span className="block text-xs text-muted">{formatDateTime(refund.createdAt, lang)}</span>
                    </span>
                    <MetaBadge record={refundStatusMeta} value={refund.status} />
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>

        <div>
          <p className="mb-2 text-sm font-bold">{t('receipt')}</p>
          {!hasReceipt ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('receiptNotReady')}</p>
          ) : receipt.loading && !data ? (
            <PageSpinner />
          ) : receipt.error ? (
            receipt.error.status === 404 || receipt.error.status === 409 ? (
              <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('receiptNotReady')}</p>
            ) : (
              <ErrorState error={receipt.error} onRetry={receipt.reload} />
            )
          ) : data ? (
            <div className="rounded-2xl border border-line">
              <ul className="divide-y divide-line text-sm">
                {data.lines.map((line) => (
                  <li key={`${line.code}-${line.label}`} className="flex items-center justify-between gap-3 px-4 py-2">
                    <span className="min-w-0 truncate">{line.label}</span>
                    <Money value={line.amount} signed={line.amount < 0} />
                  </li>
                ))}
              </ul>
              <dl className="space-y-1 border-t border-line bg-cloud/60 px-4 py-3 text-sm">
                <div className="flex justify-between gap-3 font-bold">
                  <dt>{t('total')}</dt>
                  <dd>
                    <Money value={data.total} strong />
                  </dd>
                </div>
                <div className="flex justify-between gap-3 text-xs text-muted">
                  <dt>
                    {t('vatIncluded')} (<span className="ltr-nums">{data.vatRate}%</span>)
                  </dt>
                  <dd className="ltr-nums">{formatMoney(data.vatIncluded)}</dd>
                </div>
                {data.discountTotal > 0 && (
                  <div className="flex justify-between gap-3 text-xs text-muted">
                    <dt>{t('discount')}</dt>
                    <dd className="ltr-nums">{formatMoney(data.discountTotal)}</dd>
                  </div>
                )}
                <div className="flex justify-between gap-3">
                  <dt>{t('netPaid')}</dt>
                  <dd>
                    <Money value={data.netPaid} strong />
                  </dd>
                </div>
              </dl>
            </div>
          ) : null}
        </div>
      </div>

      <RefundModal
        open={refundOpen}
        description={trip.tripNumber}
        refundable={refundable}
        allowOriginalMethod={payment !== null}
        onClose={() => setRefundOpen(false)}
        onSubmit={submitRefund}
      />
    </Card>
  )
}
