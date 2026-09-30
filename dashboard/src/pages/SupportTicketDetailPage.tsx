import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button, type ButtonVariant } from '../components/Button'
import { Card } from '../components/Card'
import { DisputeResolveModal } from '../components/DisputeResolveModal'
import { Select } from '../components/Field'
import { FilePreviewModal, type FilePreviewTarget } from '../components/FilePreviewModal'
import { Icon, type IconName } from '../components/Icon'
import { Money } from '../components/Money'
import { PermissionError } from '../components/PermissionError'
import { ReasonModal } from '../components/ReasonModal'
import { SlaCountdown } from '../components/SlaCountdown'
import { PageSpinner } from '../components/Spinner'
import { StarRow } from '../components/Stars'
import { TicketComposer } from '../components/TicketComposer'
import { TicketThread } from '../components/TicketThread'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useNow } from '../hooks/useNow'
import { useQuery } from '../hooks/useQuery'
import { useSupportFeed } from '../hooks/useSupportFeed'
import type { TranslationKey } from '../i18n'
import { support } from '../lib/admin'
import { lookupKey } from '../lib/finance'
import { formatDateTime } from '../lib/format'
import {
  DISPUTE_REASON_KEY,
  DISPUTE_RESOLUTION_KEY,
  disputeRefundOf,
  isOpenDispute,
  linkedLostItem,
  linkedSafetyCase,
  NEXT_STATUSES,
  REQUESTER_ROLE_KEY,
  SUPPORT_POLL_INTERVAL_MS,
  TICKET_CHANNEL_KEY,
  TICKET_PRIORITIES,
  TICKET_TYPE_KEY,
  TICKET_TYPES,
} from '../lib/support'
import { disputeStatusMeta, refundStatusMeta, ticketPriorityMeta, ticketStatusMeta, tripStatusMeta } from '../lib/status'
import { PAYMENT_METHOD_KEY } from '../lib/trips'
import type { FareDispute, SupportTicketDetail, TicketAttachment, TicketPriority, TicketStatus, TicketType } from '../lib/types'

type TargetStatus = Exclude<TicketStatus, 'open'>

/** Copy, icon and style of every status action an agent can trigger (§F18.2). */
function statusAction(from: TicketStatus, to: TargetStatus): { label: TranslationKey; icon: IconName; variant: ButtonVariant; asksNote: boolean; title: TranslationKey } {
  switch (to) {
    case 'in_progress':
      if (from === 'resolved') return { label: 'spReopen', icon: 'refresh', variant: 'secondary', asksNote: true, title: 'spReopenTitle' }
      return { label: from === 'pending_user' ? 'spResume' : 'spStartWork', icon: 'play', variant: 'secondary', asksNote: false, title: 'spReopenTitle' }
    case 'pending_user':
      return { label: 'spRequestInfo', icon: 'pause', variant: 'secondary', asksNote: true, title: 'spPendingTitle' }
    case 'resolved':
      return { label: 'spResolve', icon: 'check', variant: 'brand', asksNote: true, title: 'spResolveTitle' }
    case 'closed':
      return { label: 'spClose', icon: 'x', variant: 'danger-outline', asksNote: true, title: 'spCloseTitle' }
  }
}

/** What the optional note of a transition does: a public message to the user for `pending_user` / `resolved`, an internal note otherwise (§F18.3 backend). */
function statusNoteCopy(from: TicketStatus, to: TargetStatus, t: (key: TranslationKey) => string) {
  if (to === 'pending_user') return t('spPendingCopy')
  if (to === 'resolved') return t('spResolveCopy')
  if (to === 'closed') return `${t('spCloseCopy')} ${t('spNoteInternalHint')}`
  return from === 'resolved' ? t('spNoteInternalHint') : undefined
}

const STATUS_TOAST: Record<TargetStatus, TranslationKey> = {
  in_progress: 'spStatusInProgressDone',
  pending_user: 'spStatusPendingDone',
  resolved: 'spStatusResolvedDone',
  closed: 'spStatusClosedDone',
}

/** Ticket page (`/support/tickets/:id`): thread with internal notes, canned responses, SLA clocks, status/priority/assignment, dispute and links (docs/11 "لوحة الإدارة"). */
export function SupportTicketDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const { user } = useAuth()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const now = useNow(15_000)
  const query = useQuery(() => support.get(id), `support-ticket:${id}`)
  const [busy, setBusy] = useState<string | null>(null)
  const [statusModal, setStatusModal] = useState<TargetStatus | null>(null)
  const [preview, setPreview] = useState<FilePreviewTarget | null>(null)
  const [resolving, setResolving] = useState<FareDispute | null>(null)

  const detail = query.data
  const closed = detail?.status === 'closed'

  const feed = useSupportFeed({
    onEvent: (event) => {
      if (event.kind === 'updated' && event.update.ticketId === id) query.reload()
    },
  })

  // Fallback refresh while the hub is not pushing updates (new user messages, status changes by other agents).
  const { reload } = query
  useEffect(() => {
    if (feed.live || !detail || closed) return
    const timer = window.setInterval(reload, SUPPORT_POLL_INTERVAL_MS)
    return () => window.clearInterval(timer)
  }, [feed.live, detail, closed, reload])

  const run = async (key: string, action: () => Promise<unknown>, success: string) => {
    setBusy(key)
    try {
      await action()
      toast.success(success)
      query.reload()
      return true
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
      return false
    } finally {
      setBusy(null)
    }
  }

  if (query.loading && !detail) return <PageSpinner />
  if (!detail) {
    return (
      <Card>
        <PermissionError error={query.error} permission="support.view" onRetry={query.reload} />
      </Card>
    )
  }

  const mine = Boolean(user?.id) && detail.assignedToUserId === user?.id
  const transitions = NEXT_STATUSES[detail.status]
  const safetyCase = linkedSafetyCase(detail)
  const lostItem = linkedLostItem(detail)
  const openAttachment = (attachment: TicketAttachment) =>
    setPreview({ fileId: attachment.fileId, fileName: attachment.fileName ?? attachment.fileId, title: attachment.fileName ?? t('spAttachment') })

  const changeStatus = (to: TargetStatus, note?: string) =>
    run(`status:${to}`, () => support.setStatus(id, { status: to, note: note || undefined }), t(STATUS_TOAST[to]))

  return (
    <>
      <Link to="/support" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('spBackToQueue')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0">
            <p className="ltr-nums text-sm font-bold text-brand">{detail.ticketNumber}</p>
            <h2 className="mt-0.5 break-words text-2xl font-bold leading-tight">{detail.subject}</h2>
            <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
              <MetaBadge record={ticketStatusMeta} value={detail.status} />
              <MetaBadge record={ticketPriorityMeta} value={detail.priority} />
              <Badge tone="ink">{t(TICKET_TYPE_KEY[detail.type] ?? 'spTypeOther')}</Badge>
              <Badge tone="muted">{t(lookupKey(TICKET_CHANNEL_KEY, detail.channel) ?? 'spChannelApp')}</Badge>
              <Badge tone={feed.live ? 'brand' : 'muted'}>{feed.live ? t('liveViaHub') : t('liveViaPolling')}</Badge>
            </div>
          </div>
          <SlaSummary detail={detail} now={now} />
        </div>

        <div className="mt-5 flex flex-wrap gap-2 border-t border-line pt-5">
          {!closed && (
            <Button
              icon="user"
              variant={mine ? 'secondary' : 'primary'}
              loading={busy === 'assign'}
              disabled={mine || !user?.id}
              onClick={() => run('assign', () => support.assign(id, user?.id ?? null), t('spAssigned'))}
            >
              {mine ? t('spAssignedToYou') : detail.assignedToUserId ? t('spReassignToMe') : t('spAssignToMe')}
            </Button>
          )}
          {!closed && detail.assignedToUserId && (
            <Button variant="secondary" icon="x" loading={busy === 'unassign'} onClick={() => run('unassign', () => support.assign(id, null), t('spUnassignedDone'))}>
              {t('spUnassign')}
            </Button>
          )}
          {transitions.map((to) => {
            const action = statusAction(detail.status, to)
            return (
              <Button
                key={to}
                variant={action.variant}
                icon={action.icon}
                loading={busy === `status:${to}`}
                onClick={() => (action.asksNote ? setStatusModal(to) : void changeStatus(to))}
              >
                {t(action.label)}
              </Button>
            )
          })}
        </div>
        {closed && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-cloud p-4 text-sm text-muted">
            <Icon name="info" className="mt-0.5 size-4 shrink-0" />
            <p>{t('spClosedNotice')}</p>
          </div>
        )}
      </Card>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card title={t('spThread')} description={t('spThreadCopy')}>
            <TicketThread messages={detail.messages} onOpenAttachment={openAttachment} />
            <div className="mt-5">
              <TicketComposer ticket={detail} onSent={query.reload} />
            </div>
          </Card>
        </div>

        <div className="space-y-6">
          <Card title={t('spTicketSettings')}>
            <div className="space-y-3">
              <Select
                id="ticket-priority"
                label={t('priority')}
                hint={t('spPriorityHint')}
                value={detail.priority}
                disabled={closed || busy === 'priority'}
                onChange={(event) => void run('priority', () => support.setPriority(id, event.target.value as TicketPriority), t('spPriorityChanged'))}
              >
                {TICKET_PRIORITIES.map((value) => (
                  <option key={value} value={value}>
                    {t(ticketPriorityMeta[value].key)}
                  </option>
                ))}
              </Select>
              <Select
                id="ticket-type"
                label={t('type')}
                value={detail.type}
                disabled={closed || busy === 'type'}
                onChange={(event) => void run('type', () => support.setType(id, event.target.value as TicketType), t('spTypeChanged'))}
              >
                {TICKET_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {t(TICKET_TYPE_KEY[value])}
                  </option>
                ))}
              </Select>
              <p className="rounded-2xl bg-cloud px-4 py-3 text-sm">
                <span className="block text-xs font-bold text-muted">{t('spAssignedTo')}</span>
                <span className="font-bold">{detail.assignedToName ?? t('spUnassigned')}</span>
                {detail.assignedAt && <span className="block text-xs text-muted">{formatDateTime(detail.assignedAt, lang)}</span>}
              </p>
            </div>
          </Card>

          <Card title={t('spRequester')}>
            <div className="space-y-2">
              <p className="flex flex-wrap items-center gap-2">
                <span className="font-bold">{detail.requester.fullName || t('unnamed')}</span>
                <Badge tone="muted">{t(lookupKey(REQUESTER_ROLE_KEY, detail.requester.role) ?? 'actorPassenger')}</Badge>
              </p>
              {detail.requester.phoneNumber && (
                <a href={`tel:${detail.requester.phoneNumber.replace(/[^\d+]/g, '')}`} className="ltr-nums inline-flex items-center gap-2 text-sm font-bold text-brand hover:underline">
                  <Icon name="phone" className="size-4" />
                  {detail.requester.phoneNumber}
                </a>
              )}
              <div className="flex flex-wrap gap-3 text-sm font-bold text-brand">
                {detail.requester.role === 'driver' && detail.requester.driverId && (
                  <Link to={`/drivers/${detail.requester.driverId}`} className="hover:underline">
                    {t('spOpenDriver')}
                  </Link>
                )}
                {detail.requester.role === 'passenger' && detail.requester.phoneNumber && (
                  <Link to={`/passengers?q=${encodeURIComponent(detail.requester.phoneNumber)}`} className="hover:underline">
                    {t('spOpenPassenger')}
                  </Link>
                )}
              </div>
            </div>
          </Card>

          <Card title={t('spLinkedTrip')}>
            {detail.trip ? (
              <div className="space-y-2 text-sm">
                <Link to={`/trips/${detail.trip.id}`} className="ltr-nums inline-flex items-center gap-1 font-bold text-brand hover:underline">
                  {detail.trip.tripNumber}
                  <Icon name="chevron" className="size-4 rtl:rotate-180" />
                </Link>
                <div className="flex flex-wrap items-center gap-2">
                  <MetaBadge record={tripStatusMeta} value={detail.trip.status} />
                  {detail.trip.paymentMethod && <Badge tone="muted">{t(PAYMENT_METHOD_KEY[detail.trip.paymentMethod] ?? 'paymentCash')}</Badge>}
                </div>
                {(detail.trip.pickupName || detail.trip.dropoffName) && (
                  <p className="text-xs text-muted">
                    {detail.trip.pickupName ?? '—'} → {detail.trip.dropoffName ?? '—'}
                  </p>
                )}
                {detail.trip.driverName && (
                  <p className="text-xs text-muted">
                    {t('driver')}: <span className="font-bold text-ink">{detail.trip.driverName}</span>
                  </p>
                )}
                <dl className="grid grid-cols-2 gap-2">
                  <div className="rounded-xl bg-cloud px-3 py-2">
                    <dt className="text-xs font-bold text-muted">{t('spFare')}</dt>
                    <dd className="font-bold">
                      <Money value={detail.trip.receipt?.total ?? detail.trip.finalFare ?? detail.trip.estimatedFare} />
                    </dd>
                  </div>
                  <div className="rounded-xl bg-cloud px-3 py-2">
                    <dt className="text-xs font-bold text-muted">{t('spCompletedAt')}</dt>
                    <dd className="text-xs font-bold">{formatDateTime(detail.trip.completedAt ?? detail.trip.cancelledAt, lang)}</dd>
                  </div>
                </dl>
              </div>
            ) : (
              <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('spNoTrip')}</p>
            )}
          </Card>

          {detail.dispute && (
            <DisputeCard
              dispute={detail.dispute}
              onResolve={(dispute) => setResolving({ ...dispute, ticketNumber: detail.ticketNumber, tripNumber: detail.trip?.tripNumber ?? null })}
            />
          )}

          {(safetyCase || lostItem) && (
            <Card title={t('spLinkedCases')}>
              <ul className="space-y-2 text-sm">
                {safetyCase && (
                  <li>
                    <Link to={`/safety/cases/${safetyCase.id}`} className="flex items-center justify-between gap-2 rounded-2xl border border-line px-4 py-3 transition hover:bg-cloud">
                      <span className="flex items-center gap-2 font-bold">
                        <Icon name="siren" className="size-4 text-danger" />
                        {t('spSafetyCase')} <span className="ltr-nums">{safetyCase.number ?? ''}</span>
                      </span>
                      <Icon name="chevron" className="size-4 rtl:rotate-180" />
                    </Link>
                  </li>
                )}
                {lostItem && (
                  <li>
                    <Link to={`/lost-items?q=${encodeURIComponent(lostItem.number ?? lostItem.id)}`} className="flex items-center justify-between gap-2 rounded-2xl border border-line px-4 py-3 transition hover:bg-cloud">
                      <span className="flex items-center gap-2 font-bold">
                        <Icon name="box" className="size-4 text-brand" />
                        {t('spLostItemReport')} <span className="ltr-nums">{lostItem.number ?? ''}</span>
                      </span>
                      <Icon name="chevron" className="size-4 rtl:rotate-180" />
                    </Link>
                  </li>
                )}
              </ul>
            </Card>
          )}

          {(detail.status === 'resolved' || detail.status === 'closed' || detail.csatScore !== null) && (
            <Card title={t('spCsat')}>
              {detail.csatScore === null ? (
                <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('spCsatNone')}</p>
              ) : (
                <div className="space-y-2">
                  <p className="flex items-center gap-2">
                    <StarRow value={detail.csatScore} />
                    <span className="ltr-nums font-bold">{detail.csatScore} / 5</span>
                  </p>
                  {detail.csatComment && <p className="whitespace-pre-wrap break-words rounded-2xl bg-cloud px-4 py-3 text-sm">{detail.csatComment}</p>}
                </div>
              )}
            </Card>
          )}

          <Card title={t('spTimeline')}>
            <ul className="space-y-2 text-sm">
              <TimelineRow label={t('spCreatedAt')} value={detail.createdAt} lang={lang} />
              <TimelineRow label={t('spFirstResponseAt')} value={detail.firstResponseAt} lang={lang} />
              <TimelineRow label={t('spLastMessageAt')} value={detail.lastMessageAt} lang={lang} />
              <TimelineRow label={t('spResolvedAt')} value={detail.resolvedAt} lang={lang} />
              <TimelineRow label={t('spClosedAt')} value={detail.closedAt} lang={lang} />
              {detail.slaPausedSeconds > 0 && (
                <li className="flex justify-between gap-3 rounded-xl bg-cloud px-3 py-2">
                  <span className="text-muted">{t('spPausedTotal')}</span>
                  <span className="ltr-nums font-bold">
                    {Math.round(detail.slaPausedSeconds / 60)} {t('min')}
                  </span>
                </li>
              )}
            </ul>
          </Card>
        </div>
      </div>

      <ReasonModal
        open={statusModal !== null}
        title={statusModal ? t(statusAction(detail.status, statusModal).title) : ''}
        description={statusModal ? statusNoteCopy(detail.status, statusModal, t) : undefined}
        confirmLabel={statusModal ? t(statusAction(detail.status, statusModal).label) : t('confirm')}
        confirmVariant={statusModal === 'closed' ? 'danger' : 'brand'}
        required={false}
        label={t('noteOptional')}
        onClose={() => setStatusModal(null)}
        onConfirm={async (note) => {
          if (statusModal && (await changeStatus(statusModal, note))) setStatusModal(null)
        }}
      />
      <DisputeResolveModal dispute={resolving} onClose={() => setResolving(null)} onResolved={() => query.reload()} />
      <FilePreviewModal target={preview} onClose={() => setPreview(null)} />
    </>
  )
}

function TimelineRow({ label, value, lang }: { label: string; value: string | null; lang: 'ar' | 'en' }) {
  return (
    <li className="flex justify-between gap-3 rounded-xl bg-cloud px-3 py-2">
      <span className="text-muted">{label}</span>
      <span className="font-bold">{value ? formatDateTime(value, lang) : '—'}</span>
    </li>
  )
}

/** First-response and resolution clocks: countdown, paused (`pending_user`), or the final outcome once done. */
function SlaSummary({ detail, now }: { detail: SupportTicketDetail; now: number }) {
  const { t, lang } = useLang()
  const resolvedDone = detail.status === 'resolved' || detail.status === 'closed'
  const doneAt = detail.resolvedAt ?? detail.closedAt
  const firstLate = detail.firstResponseAt ? new Date(detail.firstResponseAt).getTime() > new Date(detail.firstResponseDueAt).getTime() : false
  const resolutionLate = doneAt ? new Date(doneAt).getTime() > new Date(detail.resolutionDueAt).getTime() : false

  return (
    <div className="grid gap-3 rounded-2xl bg-cloud p-4 sm:min-w-72" data-testid="sla-summary">
      <div>
        <p className="mb-1 text-xs font-bold text-muted">{t('spSlaFirstResponse')}</p>
        {detail.firstResponseAt ? (
          <Badge tone={firstLate ? 'danger' : 'brand'} className="gap-1.5">
            <Icon name={firstLate ? 'alert' : 'check'} className="size-3.5" />
            {firstLate ? t('spSlaMissed') : t('spSlaMet')} · {formatDateTime(detail.firstResponseAt, lang)}
          </Badge>
        ) : (
          <SlaCountdown dueAt={detail.firstResponseDueAt} now={now} />
        )}
      </div>
      <div>
        <p className="mb-1 text-xs font-bold text-muted">{t('spSlaResolution')}</p>
        {resolvedDone ? (
          <Badge tone={resolutionLate ? 'danger' : 'brand'} className="gap-1.5">
            <Icon name={resolutionLate ? 'alert' : 'check'} className="size-3.5" />
            {resolutionLate ? t('spSlaMissed') : t('spSlaMet')} · {formatDateTime(doneAt, lang)}
          </Badge>
        ) : (
          <SlaCountdown dueAt={detail.resolutionDueAt} now={now} pausedAt={detail.slaPausedAt} />
        )}
        {detail.slaPausedAt && !resolvedDone && <p className="mt-1 text-xs text-muted">{t('spSlaPausedCopy')}</p>}
      </div>
    </div>
  )
}

function DisputeCard({ dispute, onResolve }: { dispute: FareDispute; onResolve: (dispute: FareDispute) => void }) {
  const { t, lang } = useLang()
  const refund = disputeRefundOf(dispute)
  const open = isOpenDispute(dispute.status)
  return (
    <Card title={t('spFareDispute')} action={<MetaBadge record={disputeStatusMeta} value={dispute.status} />}>
      <dl className="grid grid-cols-2 gap-2 text-sm">
        <div className="col-span-2 rounded-xl bg-cloud px-3 py-2">
          <dt className="text-xs font-bold text-muted">{t('spDisputeReason')}</dt>
          <dd className="font-bold">{t(lookupKey(DISPUTE_REASON_KEY, dispute.reason) ?? 'spReasonOther')}</dd>
        </div>
        <div className="rounded-xl bg-cloud px-3 py-2">
          <dt className="text-xs font-bold text-muted">{t('spChargedAmount')}</dt>
          <dd className="font-bold">
            <Money value={dispute.chargedAmount} />
          </dd>
        </div>
        <div className="rounded-xl bg-cloud px-3 py-2">
          <dt className="text-xs font-bold text-muted">{t('spRequestedAmount')}</dt>
          <dd className="font-bold">{dispute.requestedRefundAmount === null ? '—' : <Money value={dispute.requestedRefundAmount} />}</dd>
        </div>
        {dispute.resolution && (
          <>
            <div className="rounded-xl bg-cloud px-3 py-2">
              <dt className="text-xs font-bold text-muted">{t('spResolution')}</dt>
              <dd className="font-bold">{t(DISPUTE_RESOLUTION_KEY[dispute.resolution])}</dd>
            </div>
            <div className="rounded-xl bg-cloud px-3 py-2">
              <dt className="text-xs font-bold text-muted">{t('spApprovedAmount')}</dt>
              <dd className="font-bold">
                <Money value={dispute.approvedRefundAmount ?? 0} />
              </dd>
            </div>
          </>
        )}
      </dl>
      {dispute.resolutionNote && <p className="mt-3 whitespace-pre-wrap break-words rounded-2xl bg-cloud px-4 py-3 text-sm">{dispute.resolutionNote}</p>}
      {dispute.resolvedAt && <p className="mt-2 text-xs text-muted">{formatDateTime(dispute.resolvedAt, lang)}</p>}
      {refund && (
        <div className="mt-3 rounded-2xl border border-line p-3 text-sm">
          <p className="flex flex-wrap items-center gap-2">
            <span className="text-xs font-bold text-muted">{t('spRefund')}</span>
            {refund.refundNumber && <span className="ltr-nums font-bold">{refund.refundNumber}</span>}
            <MetaBadge record={refundStatusMeta} value={refund.status} />
          </p>
          {refund.status === 'pending_approval' && <p className="mt-1 text-xs text-amber-700">{t('spRefundPendingApproval')}</p>}
          <Link to={refund.status === 'pending_approval' ? '/refunds?status=pending_approval' : '/refunds?status=all'} className="mt-1 inline-flex items-center gap-1 text-xs font-bold text-brand">
            {t('spOpenRefunds')}
            <Icon name="chevron" className="size-3.5 rtl:rotate-180" />
          </Link>
        </div>
      )}
      {open && (
        <Button className="mt-4 w-full" variant="brand" icon="check" onClick={() => onResolve(dispute)}>
          {t('spResolveDispute')}
        </Button>
      )}
    </Card>
  )
}
