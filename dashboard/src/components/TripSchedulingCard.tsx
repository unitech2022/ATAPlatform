import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { scheduledRules, scheduledTrips } from '../lib/admin'
import { formatDateTime, formatNumber } from '../lib/format'
import {
  ACTIVE_RESERVATION_STATUSES,
  buildTimeline,
  commitmentOf,
  formatOffsetLabel,
  pickRule,
  REMINDER_KIND_KEY,
  RELEASE_REASON_KEY,
  RESERVATION_SOURCE_KEY,
  TIMELINE_STEP_KEY,
  type TimelineStep,
} from '../lib/scheduling'
import { reminderStatusMeta, reservationStateMeta } from '../lib/status'
import { isTerminalTripStatus } from '../lib/trips'
import type { ScheduledReminder, ScheduledReservation, TripDetail } from '../lib/types'
import { AssignDriverModal } from './AssignDriverModal'
import { Badge, MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { DefinitionList } from './DefinitionList'
import { Icon } from './Icon'
import { ReasonModal } from './ReasonModal'
import { Table, type Column } from './Table'

/**
 * Scheduled-ride section of the trip detail (§F17.3/§F17.4): scheduled time, the T−60 / T−15 / T−10 confirmation timeline (planned from the
 * applicable rule, merged with what the reservations recorded), reminders, rematch history, the driver's commitment and the admin actions
 * (assign / reassign, release the reservation, cancel with a reason). Reservation and reminder history come from `Trip.scheduling`
 * (assumed admin fields `reservations[]` / `reminders[]`); without them the card falls back to the planned schedule.
 */
export function TripSchedulingCard({ trip, onChanged, onCancel }: { trip: TripDetail; onChanged: () => void; onCancel: () => void }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const rulesQuery = useQuery(() => scheduledRules.list(), 'scheduled-rules')
  const [assigning, setAssigning] = useState(false)
  const [releasing, setReleasing] = useState(false)

  const scheduling = trip.scheduling ?? null
  const reservations = useMemo(() => scheduling?.reservations ?? [], [scheduling])
  const reminders = useMemo(() => scheduling?.reminders ?? [], [scheduling])
  const rule = useMemo(() => pickRule(rulesQuery.data ?? [], { rideCategoryId: trip.rideCategory?.id }), [rulesQuery.data, trip.rideCategory])

  const activeReservation =
    [...reservations].reverse().find((reservation) => ACTIVE_RESERVATION_STATUSES.includes(reservation.status as (typeof ACTIVE_RESERVATION_STATUSES)[number])) ??
    (scheduling?.reservation && ACTIVE_RESERVATION_STATUSES.includes(scheduling.reservation.status as (typeof ACTIVE_RESERVATION_STATUSES)[number]) ? scheduling.reservation : null)
  const commitment = commitmentOf(trip)
  const steps = buildTimeline(trip, rule)
  const openTrip = !isTerminalTripStatus(trip.status)
  const canManage = openTrip && (trip.status === 'scheduled' || trip.status === 'searching')
  const scheduledAtMs = trip.scheduledAt ? new Date(trip.scheduledAt).getTime() : null
  const freeCancelUntil =
    scheduling?.freeCancelUntil ?? (scheduledAtMs !== null && rule ? new Date(scheduledAtMs - rule.freeCancelMinutesBefore * 60_000).toISOString() : null)

  const release = async (reason: string) => {
    try {
      await scheduledTrips.releaseReservation(trip.id, reason)
      toast.success(t('sdReleased'))
      setReleasing(false)
      onChanged()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const reservationColumns: Column<ScheduledReservation>[] = [
    {
      key: 'driver',
      header: t('driver'),
      render: (row) =>
        row.driverId ? (
          <Link to={`/drivers/${row.driverId}`} className="font-bold text-brand hover:underline">
            {row.driverName || t('unnamed')}
          </Link>
        ) : (
          <span className="font-bold">{row.driverName || '—'}</span>
        ),
    },
    { key: 'source', header: t('sdSource'), render: (row) => (row.source ? t(RESERVATION_SOURCE_KEY[row.source]) : '—') },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={reservationStateMeta} value={row.status} /> },
    { key: 'reservedAt', header: t('sdReservedAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.reservedAt, lang)}</span> },
    { key: 'confirmedAt', header: t('sdConfirmedAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.confirmedAt, lang)}</span> },
    { key: 'assignedAt', header: t('sdAssignedAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.assignedAt, lang)}</span> },
    {
      key: 'released',
      header: t('sdReleasedAt'),
      render: (row) =>
        row.releasedAt ? (
          <span className="block whitespace-nowrap text-xs">
            <span className="block">{formatDateTime(row.releasedAt, lang)}</span>
            {row.releaseReason && <span className="block font-bold text-danger">{t(RELEASE_REASON_KEY[row.releaseReason])}</span>}
          </span>
        ) : (
          '—'
        ),
    },
    {
      key: 'penalty',
      header: t('sdPenalty'),
      className: 'text-center',
      render: (row) => (
        <span className="inline-flex flex-col items-center gap-1">
          <span className={`ltr-nums font-bold ${(row.penaltyPoints ?? 0) > 0 ? 'text-danger' : 'text-muted'}`}>{formatNumber(row.penaltyPoints ?? 0)}</span>
          {row.isLateRelease && <Badge tone="warning">{t('sdLateRelease')}</Badge>}
        </span>
      ),
    },
  ]

  const reminderColumns: Column<ScheduledReminder>[] = [
    { key: 'recipient', header: t('sdRecipient'), render: (row) => (row.recipientRole === 'driver' ? t('driver') : t('passenger')) },
    { key: 'kind', header: t('sdKind'), render: (row) => t(REMINDER_KIND_KEY[row.kind]) },
    {
      key: 'offset',
      header: t('sdOffset'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums">{typeof row.offsetMinutes === 'number' ? `T−${formatOffsetLabel(row.offsetMinutes)}` : '—'}</span>,
    },
    { key: 'sendAt', header: t('sdSendAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.sendAt, lang)}</span> },
    { key: 'sentAt', header: t('sdSentAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.sentAt, lang)}</span> },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={reminderStatusMeta} value={row.status} /> },
  ]

  const plannedReminders = useMemo(() => {
    if (!rule || scheduledAtMs === null) return []
    return [
      ...rule.riderReminderOffsets.map((offset) => ({ role: 'passenger' as const, offset })),
      ...rule.driverReminderOffsets.map((offset) => ({ role: 'driver' as const, offset })),
    ].map((entry) => ({ ...entry, at: new Date(scheduledAtMs - entry.offset * 60_000).toISOString() }))
  }, [rule, scheduledAtMs])

  const sentCount = reminders.filter((reminder) => reminder.status === 'sent').length
  const driverName = activeReservation?.driverName ?? (activeReservation && 'driverFirstName' in activeReservation ? activeReservation.driverFirstName : null) ?? null

  return (
    <Card
      className="mb-6"
      title={t('sdTripSection')}
      description={t('sdTripSectionCopy')}
      action={
        openTrip && (
          <div className="flex flex-wrap gap-2">
            {canManage && (
              <Button variant="secondary" size="sm" icon="user" onClick={() => setAssigning(true)}>
                {activeReservation ? t('sdReassign') : t('sdAssign')}
              </Button>
            )}
            {canManage && activeReservation && (
              <Button variant="danger-outline" size="sm" icon="refresh" onClick={() => setReleasing(true)}>
                {t('sdRelease')}
              </Button>
            )}
            <Button variant="danger-outline" size="sm" icon="x" onClick={onCancel}>
              {t('cancelTrip')}
            </Button>
          </div>
        )
      }
    >
      <div className="space-y-6">
        <DefinitionList
          items={[
            { label: t('scheduledAt'), value: formatDateTime(trip.scheduledAt, lang) },
            { label: t('sdFreeCancelUntil'), value: formatDateTime(freeCancelUntil, lang) },
            { label: t('sdSearchStartsAt'), value: formatDateTime(scheduling?.searchStartsAt ?? (scheduledAtMs !== null && rule ? new Date(scheduledAtMs - rule.searchStartMinutesBefore * 60_000).toISOString() : null), lang) },
            { label: t('sdRemindersSent'), value: reminders.length > 0 ? `${formatNumber(sentCount)} / ${formatNumber(reminders.length)}` : '—', ltr: true },
          ]}
        />

        <section>
          <h3 className="mb-3 text-base font-bold">{t('sdConfirmationTimeline')}</h3>
          {rule === null && !rulesQuery.loading && <p className="mb-3 text-xs text-muted">{t('sdNoRuleForPlan')}</p>}
          <ol className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            {steps.map((step) => (
              <TimelineCell key={step.key} step={step} />
            ))}
          </ol>
        </section>

        <section>
          <h3 className="mb-3 text-base font-bold">{t('sdCommitment')}</h3>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            <div className="rounded-2xl bg-cloud px-4 py-3">
              <p className="text-xs font-bold text-muted">{t('sdCurrentDriver')}</p>
              <p className="mt-1 truncate font-bold">{driverName || trip.driver?.fullName || t('sdNoDriverYet')}</p>
              {activeReservation && (
                <div className="mt-1">
                  <MetaBadge record={reservationStateMeta} value={activeReservation.status} />
                </div>
              )}
            </div>
            {[
              { label: t('sdRematches'), value: commitment.rematches, danger: commitment.rematches > 0 },
              { label: t('sdKpiNoShowUnit'), value: commitment.noShows, danger: commitment.noShows > 0 },
              { label: t('sdLateRelease'), value: commitment.lateReleases, danger: commitment.lateReleases > 0 },
              { label: t('sdPenaltyPoints'), value: commitment.penaltyPoints, danger: commitment.penaltyPoints > 0 },
            ].map((item) => (
              <div key={item.label} className="rounded-2xl bg-cloud px-4 py-3">
                <p className="text-xs font-bold text-muted">{item.label}</p>
                <p className={`ltr-nums mt-1 text-xl font-bold ${item.danger ? 'text-danger' : ''}`}>{formatNumber(item.value)}</p>
              </div>
            ))}
          </div>
        </section>

        <section>
          <h3 className="mb-3 text-base font-bold">{t('sdRematchHistory')}</h3>
          <div className="overflow-hidden rounded-2xl border border-line">
            <Table columns={reservationColumns} rows={reservations} rowKey={(row) => row.id ?? `${row.driverId}:${row.reservedAt}:${row.status}`} emptyTitle={t('sdNoReservations')} emptyDescription="" />
          </div>
        </section>

        <section>
          <h3 className="mb-3 text-base font-bold">{t('sdReminders')}</h3>
          {reminders.length > 0 ? (
            <div className="overflow-hidden rounded-2xl border border-line">
              <Table columns={reminderColumns} rows={reminders} rowKey={(row) => row.id ?? `${row.recipientRole}:${row.kind}:${row.sendAt}`} />
            </div>
          ) : plannedReminders.length > 0 ? (
            <div>
              <p className="mb-3 flex items-start gap-2 text-xs text-muted">
                <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
                {t('sdRemindersPlannedOnly')}
              </p>
              <ul className="flex flex-wrap gap-2">
                {plannedReminders.map((reminder) => (
                  <li key={`${reminder.role}:${reminder.offset}`} className="rounded-2xl bg-cloud px-3 py-2 text-xs">
                    <span className="font-bold">{reminder.role === 'driver' ? t('driver') : t('passenger')}</span>{' '}
                    <span className="ltr-nums text-brand">T−{formatOffsetLabel(reminder.offset)}</span>
                    <span className="block text-muted">{formatDateTime(reminder.at, lang)}</span>
                  </li>
                ))}
              </ul>
            </div>
          ) : (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sdNoReminders')}</p>
          )}
        </section>
      </div>

      <AssignDriverModal
        open={assigning}
        tripId={trip.id}
        tripNumber={trip.tripNumber}
        reassign={Boolean(activeReservation)}
        onClose={() => setAssigning(false)}
        onDone={() => {
          setAssigning(false)
          onChanged()
        }}
      />
      <ReasonModal
        open={releasing}
        title={t('sdReleaseTitle')}
        description={`${trip.tripNumber} — ${t('sdReleaseCopy')}`}
        confirmLabel={t('sdRelease')}
        label={t('sdReleaseReason')}
        onClose={() => setReleasing(false)}
        onConfirm={release}
      />
    </Card>
  )
}

function TimelineCell({ step }: { step: TimelineStep }) {
  const { t, lang } = useLang()
  const tone = step.state === 'done' ? 'bg-brand-soft text-brand' : step.state === 'missed' ? 'bg-danger-soft text-danger' : 'bg-cloud text-muted'
  return (
    <li className="rounded-2xl border border-line px-4 py-3">
      <div className="mb-2 flex items-center gap-2">
        <span className={`grid size-6 shrink-0 place-items-center rounded-full ${tone}`}>
          <Icon name={step.state === 'done' ? 'check' : step.state === 'missed' ? 'alert' : 'clock'} className="size-3.5" />
        </span>
        <span className="text-sm font-bold">{t(TIMELINE_STEP_KEY[step.key])}</span>
      </div>
      {step.plannedAt !== null && (
        <p className="text-xs text-muted">
          {t('sdPlanned')}: <span className="ltr-nums text-ink">{formatDateTime(new Date(step.plannedAt).toISOString(), lang)}</span>
        </p>
      )}
      <p className="text-xs text-muted">
        {t('sdActual')}: <span className="ltr-nums text-ink">{step.actualAt !== null ? formatDateTime(new Date(step.actualAt).toISOString(), lang) : t('notYet')}</span>
      </p>
      {step.state === 'missed' && <p className="mt-1 text-xs font-bold text-danger">{t('sdStepMissed')}</p>}
    </li>
  )
}
