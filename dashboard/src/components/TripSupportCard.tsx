import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { support } from '../lib/admin'
import { lookupKey } from '../lib/finance'
import { formatDateTime } from '../lib/format'
import { DISPUTE_REASON_KEY, DISPUTE_RESOLUTION_KEY, TICKET_TYPE_KEY } from '../lib/support'
import { disputeStatusMeta, ticketPriorityMeta, ticketStatusMeta } from '../lib/status'
import type { FareDispute, TripDetail } from '../lib/types'
import { MetaBadge } from './Badge'
import { Card } from './Card'
import { Money } from './Money'
import { PageSpinner } from './Spinner'

/**
 * "الدعم" section of the trip page: support tickets linked to the trip and its fare dispute (one per trip, §F18.2). Tickets are found with
 * the queue `search` by trip number and narrowed client-side. Without `support.view` the call answers 403 and the section is hidden.
 */
export function TripSupportCard({ trip }: { trip: TripDetail }) {
  const { t, lang } = useLang()
  // The linked tickets come from the queue search by trip number; the dispute (one per trip, on a `payment_issue` ticket) is read from the
  // ticket detail, which embeds it — the disputes list has no per-trip filter.
  const tickets = useQuery(async () => {
    const page = await support.tickets({ search: trip.tripNumber, page: 1, pageSize: 20 })
    const rows = page.items.filter((item) => item.tripNumber === trip.tripNumber)
    const details = await Promise.all(rows.filter((row) => row.type === 'payment_issue').slice(0, 5).map((row) => support.get(row.id).catch(() => null)))
    const dispute = details.map((detail) => detail?.dispute).find((found): found is FareDispute => Boolean(found)) ?? null
    return { rows, dispute }
  }, `trip-tickets:${trip.id}`)

  if (tickets.error?.status === 403) return null
  // Nothing linked: keep the trip page quiet instead of adding an empty section.
  if (!tickets.loading && (tickets.data?.rows ?? []).length === 0 && !tickets.data?.dispute) return null

  const dispute = tickets.data?.dispute ?? null

  return (
    <Card title={t('spTripSection')} className="mb-6">
      {tickets.loading && !tickets.data ? (
        <PageSpinner />
      ) : (
        <div className="grid gap-6 lg:grid-cols-2">
          <section>
            <h3 className="mb-2 text-sm font-bold">{t('spTicketsHeading')}</h3>
            {(tickets.data?.rows ?? []).length === 0 ? (
              <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('spNoTickets')}</p>
            ) : (
              <ul className="space-y-2">
                {(tickets.data?.rows ?? []).map((ticket) => (
                  <li key={ticket.id}>
                    <Link to={`/support/tickets/${ticket.id}`} className="block rounded-2xl border border-line px-4 py-3 transition hover:bg-cloud">
                      <span className="flex flex-wrap items-center justify-between gap-2">
                        <span className="ltr-nums font-bold">{ticket.ticketNumber}</span>
                        <MetaBadge record={ticketStatusMeta} value={ticket.status} />
                      </span>
                      <span className="mt-0.5 block truncate text-sm">{ticket.subject}</span>
                      <span className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted">
                        <MetaBadge record={ticketPriorityMeta} value={ticket.priority} />
                        {t(TICKET_TYPE_KEY[ticket.type] ?? 'spTypeOther')} · {formatDateTime(ticket.createdAt, lang)}
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>
          <section>
            <h3 className="mb-2 text-sm font-bold">{t('spFareDispute')}</h3>
            {dispute ? (
              <Link to={`/support/tickets/${dispute.ticketId}`} className="block rounded-2xl border border-line px-4 py-3 transition hover:bg-cloud">
                <span className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-bold">{t(lookupKey(DISPUTE_REASON_KEY, dispute.reason) ?? 'spReasonOther')}</span>
                  <MetaBadge record={disputeStatusMeta} value={dispute.status} />
                </span>
                <span className="mt-1 block text-xs text-muted">
                  {t('spChargedAmount')}: <Money value={dispute.chargedAmount} />
                  {dispute.resolution && (
                    <>
                      {' · '}
                      {t(DISPUTE_RESOLUTION_KEY[dispute.resolution])}: <Money value={dispute.approvedRefundAmount ?? 0} />
                    </>
                  )}
                </span>
              </Link>
            ) : (
              <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('spNoDispute')}</p>
            )}
          </section>
        </div>
      )}
    </Card>
  )
}
