import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { support } from '../lib/admin'
import { formatDateTime } from '../lib/format'
import { ticketsOfUser, TICKET_TYPE_KEY } from '../lib/support'
import { ticketPriorityMeta, ticketStatusMeta } from '../lib/status'
import { MetaBadge } from './Badge'
import { Card } from './Card'
import { ErrorState } from './ErrorState'
import { Icon } from './Icon'
import { PageSpinner } from './Spinner'

const LIMIT = 5

/**
 * Recent support tickets of a driver or passenger (docs/11 dashboard brief). The admin list has no documented per-requester filter,
 * so it asks with an assumed `requesterUserId` + the user's name/phone as `search` and keeps only rows that belong to the user.
 * A 403 (no `support.view`) hides the card.
 */
export function UserTicketsCard({ userId, name, phone, bare = false }: { userId: string; name?: string | null; phone?: string | null; bare?: boolean }) {
  const { t, lang } = useLang()
  const query = useQuery(async () => {
    const page = await support.tickets({ requesterUserId: userId, search: phone || name || undefined, page: 1, pageSize: 50 })
    return ticketsOfUser(page.items, { userId, name, phone })
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
      .slice(0, LIMIT)
  }, `user-tickets:${userId}`)
  if (query.error?.status === 403) return null

  const link = (
    <Link to={`/support?q=${encodeURIComponent(phone || name || '')}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
      {t('spAllTickets')}
      <Icon name="chevron" className="size-4 rtl:rotate-180" />
    </Link>
  )

  const body =
    query.loading && !query.data ? (
      <PageSpinner />
    ) : query.error ? (
      <ErrorState error={query.error} onRetry={query.reload} />
    ) : (query.data ?? []).length === 0 ? (
      <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('spNoUserTickets')}</p>
    ) : (
      <ul className="space-y-2">
        {(query.data ?? []).map((ticket) => (
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
    )

  if (bare) {
    return (
      <div>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm font-bold">{t('spRecentTickets')}</p>
          {link}
        </div>
        {body}
      </div>
    )
  }

  return (
    <Card title={t('spRecentTickets')} className="mb-6" action={link}>
      {body}
    </Card>
  )
}
