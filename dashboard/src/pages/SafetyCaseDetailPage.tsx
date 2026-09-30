import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, MetaBadge, TripStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { Select, Textarea, Toggle } from '../components/Field'
import { FilePreviewModal, type FilePreviewTarget } from '../components/FilePreviewModal'
import { Icon, type IconName } from '../components/Icon'
import { MapView } from '../components/MapView'
import { Modal } from '../components/Modal'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { TripMessagesPanel } from '../components/TripMessagesPanel'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useLiveSnapshot } from '../hooks/useLiveSnapshot'
import { useNow } from '../hooks/useNow'
import { useQuery } from '../hooks/useQuery'
import { useSafetyFeed } from '../hooks/useSafetyFeed'
import { safety } from '../lib/admin'
import { formatDateTime, formatNumber } from '../lib/format'
import { driverIcon, escapeHtml, L, pinIcon, routeLineStyle } from '../lib/leaflet'
import {
  ALERT_TYPE_KEY,
  alertMetricsText,
  EMERGENCY_NUMBER,
  ESCALATION_KEY,
  ESCALATION_TARGETS,
  formatDuration,
  isOpenCase,
  mapsUrl,
  NOTE_KIND_KEY,
  plannedRouteOf,
  REPORT_CATEGORY_KEY,
  REPORTER_ROLE_KEY,
  RESOLUTION_CODES,
  RESOLUTION_KEY,
  SAFETY_POLL_INTERVAL_MS,
  SAFETY_SOURCE_KEY,
  SAFETY_TYPE_KEY,
  telHref,
} from '../lib/safety'
import { safetyAlertStatusMeta, safetyCaseStatusMeta, safetyPriorityMeta } from '../lib/status'
import type { GeoPoint, SafetyAlert, SafetyCaseDetail, SafetyCaseNote, SafetyEscalationTarget, SafetyNoteKind, SafetyResolutionCode, TrustedContact } from '../lib/types'

type ActionModal = 'escalate' | 'resolve' | null

export function SafetyCaseDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const now = useNow()
  // `receivedAt` anchors the server-computed age so the timer keeps ticking between refreshes.
  const query = useQuery(async () => ({ detail: await safety.get(id), receivedAt: Date.now() }), `safety-case:${id}`)
  const [modal, setModal] = useState<ActionModal>(null)
  const [busy, setBusy] = useState<string | null>(null)
  const [preview, setPreview] = useState<FilePreviewTarget | null>(null)
  const [notePreset, setNotePreset] = useState<{ kind: 'note' | 'contact_attempt'; body: string; nonce: number } | null>(null)
  const noteRef = useRef<HTMLDivElement>(null)

  const detail = query.data?.detail ?? null
  const open = detail ? isOpenCase(detail.status) : false

  const feed = useSafetyFeed({
    onEvent: (event) => {
      if ((event.kind === 'case_updated' || event.kind === 'case_opened') && event.item.id === id) query.reload()
    },
  })

  // Fallback refresh while the case is open and the hub is not pushing updates (last location, notes).
  const { reload } = query
  useEffect(() => {
    if (!open || feed.live) return
    const timer = window.setInterval(reload, SAFETY_POLL_INTERVAL_MS)
    return () => window.clearInterval(timer)
  }, [open, feed.live, reload])

  const fetchedAt = query.data?.receivedAt ?? now

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

  const logContact = (who: string) => {
    setNotePreset({ kind: 'contact_attempt', body: `${who}: `, nonce: Date.now() })
    noteRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })
  }

  if (query.loading && !detail) return <PageSpinner />
  if (!detail) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const age = open ? detail.ageSeconds + Math.max(0, (now - fetchedAt) / 1000) : null
  const trip = detail.trip
  const notifiedContacts = detail.trustedContactsNotified
  const notifiedCount = Array.isArray(notifiedContacts) ? notifiedContacts.length : (notifiedContacts ?? detail.contactsNotified)

  return (
    <>
      <Link to="/safety" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('sfBackToCases')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className={`grid size-14 shrink-0 place-items-center rounded-2xl ${detail.priority === 'critical' && open ? 'bg-danger text-white' : 'bg-danger-soft text-danger'}`}>
              <Icon name={detail.type === 'sos' ? 'siren' : 'shield'} className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-danger">{t(SAFETY_TYPE_KEY[detail.type] ?? 'sfTypeSafetyReport')}</p>
              <h2 className="ltr-nums text-2xl font-bold leading-tight">{detail.caseNumber}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={safetyPriorityMeta} value={detail.priority} />
                <MetaBadge record={safetyCaseStatusMeta} value={detail.status} />
                <Badge tone="muted">{t(SAFETY_SOURCE_KEY[detail.source] ?? 'sfSourceAdmin')}</Badge>
                {detail.escalatedTo && <Badge tone="ink">{t(ESCALATION_KEY[detail.escalatedTo] ?? 'sfEscOther')}</Badge>}
                <Badge tone={feed.live ? 'brand' : 'muted'}>{feed.live ? t('liveViaHub') : t('liveViaPolling')}</Badge>
              </div>
            </div>
          </div>
          {age !== null && (
            <div className="rounded-2xl bg-cloud px-4 py-3 text-center">
              <p className="text-xs font-bold text-muted">{t('sfAge')}</p>
              <p className={`ltr-nums text-2xl font-bold ${detail.priority === 'critical' ? 'text-danger' : ''}`}>{formatDuration(age)}</p>
            </div>
          )}
        </div>

        {detail.reporterCancelledAt && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
            <Icon name="info" className="mt-0.5 size-4 shrink-0" />
            <p>
              <span className="font-bold">{t('sfReporterCancelled')}</span> · {formatDateTime(detail.reporterCancelledAt, lang)}
            </p>
          </div>
        )}

        {detail.status === 'resolved' && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-brand-soft p-4 text-sm text-ink">
            <Icon name="check" className="mt-0.5 size-4 shrink-0 text-brand" />
            <p>
              <span className="font-bold">{detail.resolutionCode ? t(RESOLUTION_KEY[detail.resolutionCode] ?? 'sfResOther') : t('sfStatusResolved')}</span>
              {detail.resolution ? ` — ${detail.resolution}` : ''} · {formatDateTime(detail.resolvedAt, lang)}
            </p>
          </div>
        )}

        {open && (
          <div className="mt-5 flex flex-wrap gap-2 border-t border-line pt-5">
            <Button icon="user" loading={busy === 'assign'} onClick={() => run('assign', () => safety.assign(id, null), t('sfAssigned'))}>
              {detail.assignedToUserId ? t('sfReassignToMe') : t('sfAssignToMe')}
            </Button>
            {detail.status === 'open' && (
              <Button
                variant="secondary"
                icon="play"
                loading={busy === 'progress'}
                onClick={() => run('progress', () => safety.setStatus(id, { status: 'in_progress' }), t('sfStatusChanged'))}
              >
                {t('sfMarkInProgress')}
              </Button>
            )}
            <Button variant="danger-outline" icon="alert" onClick={() => setModal('escalate')}>
              {t('sfEscalate')}
            </Button>
            <Button variant="brand" icon="check" onClick={() => setModal('resolve')}>
              {t('sfResolve')}
            </Button>
            <a
              href={telHref(EMERGENCY_NUMBER)}
              className="inline-flex h-11 items-center gap-2 rounded-2xl bg-danger px-4 text-sm font-bold text-white transition hover:brightness-95"
            >
              <Icon name="phone" className="size-4" />
              {t('sfCallEmergency')} <span className="ltr-nums">{EMERGENCY_NUMBER}</span>
            </a>
          </div>
        )}
      </Card>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('sfLiveMap')} className="lg:col-span-2" flush>
          <div className="px-5 pb-5 sm:px-6 sm:pb-6">
            {open && trip?.driver ? <LiveCaseMap detail={detail} /> : <CaseMap detail={detail} driverPoint={detail.liveLocation} />}
            <MapLegend detail={detail} />
          </div>
        </Card>

        <Card title={t('details')}>
          <DefinitionList
            columns={1}
            items={[
              { label: t('sfReporter'), value: `${detail.reporterName || t('unnamed')} · ${t(REPORTER_ROLE_KEY[detail.reporterRole] ?? 'actorSystem')}` },
              ...(detail.subjectName ? [{ label: t('sfSubject'), value: detail.subjectName }] : []),
              ...(detail.reportCategory ? [{ label: t('category'), value: t(REPORT_CATEGORY_KEY[detail.reportCategory] ?? 'sfCatOther') }] : []),
              { label: t('sfAssignedTo'), value: detail.assignedToName ?? t('sfUnassigned') },
              { label: t('openedAt'), value: formatDateTime(detail.openedAt, lang) },
              { label: t('sfFirstResponse'), value: detail.firstResponseAt ? formatDateTime(detail.firstResponseAt, lang) : t('notYet') },
              { label: t('sfLastLocationAt'), value: formatDateTime(detail.lastLocationAt, lang) },
              { label: t('sfContactsNotified'), value: formatNumber(notifiedCount ?? 0), ltr: true },
              { label: t('sfSharesCount'), value: formatNumber(detail.sharesCount ?? 0), ltr: true },
            ]}
          />
          {detail.supportTicketId && (
            <Link
              to={`/support/tickets/${detail.supportTicketId}`}
              className="mt-3 flex items-center justify-between gap-2 rounded-2xl border border-line px-4 py-3 text-sm font-bold text-brand transition hover:bg-cloud"
            >
              <span className="flex items-center gap-2">
                <Icon name="chat" className="size-4" />
                {t('sfSupportTicket')}
              </span>
              <Icon name="chevron" className="size-4 rtl:rotate-180" />
            </Link>
          )}
          {detail.description && <p className="mt-4 whitespace-pre-wrap break-words rounded-2xl bg-cloud px-4 py-3 text-sm">{detail.description}</p>}
          {Array.isArray(notifiedContacts) && notifiedContacts.length > 0 && (
            <ul className="mt-4 space-y-1 text-sm">
              {notifiedContacts.map((contact) => (
                <li key={`${contact.name}-${contact.phoneNumber ?? ''}`} className="flex justify-between gap-3 rounded-xl bg-cloud px-3 py-2">
                  <span className="truncate font-bold">{contact.name}</span>
                  {contact.phoneNumber && <span className="ltr-nums text-muted">{contact.phoneNumber}</span>}
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-2">
        <Card
          title={t('sfTripAndParties')}
          action={
            trip ? (
              <Link to={`/trips/${trip.id}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
                <span className="ltr-nums">{trip.tripNumber}</span>
                <Icon name="chevron" className="size-4 rtl:rotate-180" />
              </Link>
            ) : undefined
          }
        >
          {trip ? (
            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <TripStatusBadge status={trip.status} />
                {trip.rideCategory && <Badge tone="ink">{trip.rideCategory.name}</Badge>}
                {trip.vehicle && (
                  <span className="text-muted">
                    {trip.vehicle.make} {trip.vehicle.model} · {trip.vehicle.color} · <span className="ltr-nums font-bold text-ink">{trip.vehicle.plateNumber}</span>
                  </span>
                )}
              </div>
              <PartyRow icon="user" role={t('passenger')} name={trip.passenger?.fullName} phone={trip.passenger?.phoneNumber} onLog={open ? logContact : undefined} />
              <PartyRow icon="car" role={t('driver')} name={trip.driver?.fullName} phone={trip.driver?.phoneNumber} onLog={open ? logContact : undefined} />
              <p className="text-xs text-muted">
                {t('pickup')}: {trip.pickup.name ?? trip.pickup.address ?? '—'} · {t('dropoff')}: {trip.dropoff.name ?? trip.dropoff.address ?? '—'}
              </p>
            </div>
          ) : (
            <EmptyState icon="route" title={t('sfNoTrip')} />
          )}
          {open && detail.reporterUserId && <TrustedContactsReveal userId={detail.reporterUserId} onLog={logContact} />}
        </Card>

        <Card title={t('sfCaseAlerts')} flush>
          <AlertsTable alerts={detail.alerts ?? []} />
        </Card>
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('sfTimeline')} className="lg:col-span-2">
          <div ref={noteRef}>{open && <NoteForm key={notePreset?.nonce ?? 0} caseId={id} preset={notePreset} onSaved={query.reload} />}</div>
          <NotesTimeline notes={detail.notes ?? []} />
        </Card>
        <Card title={t('sfAttachments')}>
          {(detail.attachments ?? []).length === 0 ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfNoAttachments')}</p>
          ) : (
            <ul className="space-y-2">
              {detail.attachments.map((file) => (
                <li key={file.id}>
                  <button
                    type="button"
                    onClick={() => setPreview({ fileId: file.fileId, fileName: file.fileName ?? file.fileId, title: file.fileName ?? t('sfAttachment') })}
                    className="flex w-full items-center gap-3 rounded-2xl border border-line px-4 py-3 text-start transition hover:bg-cloud"
                  >
                    <Icon name="document" className="size-5 shrink-0 text-brand" />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-bold">{file.fileName ?? t('sfAttachment')}</span>
                      <span className="block text-xs text-muted">
                        {file.uploadedByName ? `${file.uploadedByName} · ` : ''}
                        {formatDateTime(file.createdAt, lang)}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {trip && (
        <Card title={t('sfTripChat')} flush className="mb-6">
          <TripMessagesPanel tripId={trip.id} />
        </Card>
      )}

      <EscalateModal
        open={modal === 'escalate'}
        onClose={() => setModal(null)}
        onConfirm={async (escalatedTo, note) => {
          if (await run('escalate', () => safety.setStatus(id, { status: 'escalated', escalatedTo, note: note || undefined }), t('sfEscalated'))) setModal(null)
        }}
      />
      <ResolveModal
        open={modal === 'resolve'}
        onClose={() => setModal(null)}
        onConfirm={async (resolutionCode, resolution) => {
          if (await run('resolve', () => safety.resolve(id, { resolutionCode, resolution }), t('sfResolved'))) setModal(null)
        }}
      />
      <FilePreviewModal target={preview} onClose={() => setPreview(null)} />
    </>
  )
}

function PartyRow({ icon, role, name, phone, onLog }: { icon: IconName; role: string; name?: string | null; phone?: string | null; onLog?: (who: string) => void }) {
  const { t } = useLang()
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-cloud px-4 py-3">
      <div className="flex min-w-0 items-center gap-3">
        <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-white text-ink">
          <Icon name={icon} className="size-5" />
        </span>
        <span className="min-w-0">
          <span className="block text-xs font-bold text-muted">{role}</span>
          <span className="block truncate font-bold">{name || t('unnamed')}</span>
          {phone && <span className="ltr-nums block text-xs text-muted">{phone}</span>}
        </span>
      </div>
      {phone && (
        <div className="flex gap-2">
          <a href={telHref(phone)} className="inline-flex h-9 items-center gap-1.5 rounded-xl bg-brand px-3 text-xs font-bold text-white transition hover:brightness-95">
            <Icon name="phone" className="size-4" />
            {t('sfCall')}
          </a>
          {onLog && (
            <Button variant="secondary" size="sm" icon="edit" onClick={() => onLog(`${role} (${phone})`)}>
              {t('sfLogContact')}
            </Button>
          )}
        </div>
      )}
    </div>
  )
}

/** Trusted contacts of the reporter — audited read, available only while the case is open (§F12.7). */
function TrustedContactsReveal({ userId, onLog }: { userId: string; onLog: (who: string) => void }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [state, setState] = useState<{ kind: 'idle' | 'loading' } | { kind: 'error'; message: string } | { kind: 'ready'; contacts: TrustedContact[] }>({ kind: 'idle' })

  const load = async () => {
    setState({ kind: 'loading' })
    try {
      setState({ kind: 'ready', contacts: await safety.trustedContacts(userId) })
    } catch (error) {
      setState({ kind: 'error', message: describe(error) })
    }
  }

  return (
    <div className="mt-5 border-t border-line pt-4">
      <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-bold">{t('sfTrustedContacts')}</p>
        {state.kind !== 'ready' && (
          <Button variant="secondary" size="sm" icon="eye" loading={state.kind === 'loading'} onClick={load}>
            {t('sfShowTrustedContacts')}
          </Button>
        )}
      </div>
      {state.kind === 'idle' && <p className="text-xs text-muted">{t('sfTrustedContactsAudit')}</p>}
      {state.kind === 'error' && <p className="text-xs font-bold text-danger">{state.message}</p>}
      {state.kind === 'ready' &&
        (state.contacts.length === 0 ? (
          <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfNoTrustedContacts')}</p>
        ) : (
          <ul className="space-y-2">
            {state.contacts.map((contact) => (
              <li key={contact.id} className="flex flex-wrap items-center justify-between gap-2 rounded-2xl bg-cloud px-4 py-2.5 text-sm">
                <span className="min-w-0">
                  <span className="block font-bold">
                    {contact.name}
                    {contact.relationship ? <span className="font-normal text-muted"> · {contact.relationship}</span> : null}
                  </span>
                  <span className="ltr-nums block text-xs text-muted">{contact.phoneNumber}</span>
                </span>
                <span className="flex gap-2">
                  <a href={telHref(contact.phoneNumber)} className="inline-flex h-9 items-center gap-1.5 rounded-xl bg-brand px-3 text-xs font-bold text-white">
                    <Icon name="phone" className="size-4" />
                    {t('sfCall')}
                  </a>
                  <Button variant="secondary" size="sm" icon="edit" onClick={() => onLog(`${contact.name} (${contact.phoneNumber})`)}>
                    {t('sfLogContact')}
                  </Button>
                </span>
              </li>
            ))}
          </ul>
        ))}
    </div>
  )
}

function AlertsTable({ alerts }: { alerts: SafetyAlert[] }) {
  const { t, lang } = useLang()
  const columns: Column<SafetyAlert>[] = [
    { key: 'type', header: t('type'), render: (row) => <span className="font-bold">{t(ALERT_TYPE_KEY[row.type] ?? 'sfTypeUnexpectedStop')}</span> },
    { key: 'metrics', header: t('details'), render: (row) => <span className="ltr-nums text-xs text-muted">{alertMetricsText(row, t)}</span> },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={safetyAlertStatusMeta} value={row.status} /> },
    { key: 'detected', header: t('sfDetectedAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.detectedAt, lang)}</span> },
  ]
  return <Table columns={columns} rows={alerts} rowKey={(row) => row.id} emptyTitle={t('sfNoAlerts')} emptyDescription="" />
}

const NOTE_ICON: Record<SafetyNoteKind, IconName> = {
  note: 'edit',
  status_change: 'refresh',
  assignment: 'user',
  contact_attempt: 'phone',
  system: 'info',
}

function NotesTimeline({ notes }: { notes: SafetyCaseNote[] }) {
  const { t, lang } = useLang()
  const sorted = [...notes].sort((a, b) => b.createdAt.localeCompare(a.createdAt))
  if (sorted.length === 0) return <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfNoNotes')}</p>
  return (
    <ol className="relative space-y-5 border-s-2 border-line ps-6">
      {sorted.map((note) => (
        <li key={note.id} className="relative">
          <span
            className={`absolute -start-[33px] top-0.5 grid size-6 place-items-center rounded-full ring-4 ring-white ${note.kind === 'system' ? 'bg-cloud text-muted' : note.kind === 'contact_attempt' ? 'bg-brand-soft text-brand' : 'bg-ink text-white'}`}
          >
            <Icon name={NOTE_ICON[note.kind] ?? 'info'} className="size-3" />
          </span>
          <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
            <span className="font-bold text-ink">{note.authorUserId ? note.authorName || t('admin') : t('actorSystem')}</span>
            <span>·</span>
            <span>{t(NOTE_KIND_KEY[note.kind] ?? 'sfNoteKindNote')}</span>
            <span>·</span>
            <span>{formatDateTime(note.createdAt, lang)}</span>
            {!note.isInternal && <Badge tone="warning">{t('sfVisibleToReporter')}</Badge>}
          </div>
          <p className="mt-1 whitespace-pre-wrap break-words text-sm">{note.body}</p>
        </li>
      ))}
    </ol>
  )
}

function NoteForm({ caseId, preset, onSaved }: { caseId: string; preset: { kind: 'note' | 'contact_attempt'; body: string } | null; onSaved: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [kind, setKind] = useState<'note' | 'contact_attempt'>(preset?.kind ?? 'note')
  const [body, setBody] = useState(preset?.body ?? '')
  const [isInternal, setIsInternal] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!body.trim()) {
      setError(t('fieldRequired'))
      return
    }
    setSaving(true)
    try {
      await safety.addNote(caseId, { body: body.trim(), kind, isInternal })
      toast.success(t('sfNoteAdded'))
      setBody('')
      setKind('note')
      setIsInternal(true)
      onSaved()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={submit} noValidate className="mb-6 space-y-3 rounded-2xl border border-line p-4">
      <div className="flex flex-wrap gap-2">
        {(['note', 'contact_attempt'] as const).map((value) => (
          <button
            key={value}
            type="button"
            aria-pressed={kind === value}
            onClick={() => setKind(value)}
            className={`rounded-xl px-3 py-1.5 text-xs font-bold transition ${kind === value ? 'bg-ink text-white' : 'bg-cloud text-muted hover:bg-line'}`}
          >
            {t(NOTE_KIND_KEY[value])}
          </button>
        ))}
      </div>
      <Textarea
        id="note-body"
        aria-label={t('note')}
        placeholder={t('sfNotePlaceholder')}
        className="min-h-20"
        maxLength={2000}
        value={body}
        error={error}
        autoFocus={Boolean(preset)}
        onChange={(event) => {
          setBody(event.target.value)
          setError(null)
        }}
      />
      <Toggle checked={!isInternal} onChange={(value) => setIsInternal(!value)} label={t('sfShareWithReporter')} description={t('sfShareWithReporterCopy')} />
      <div className="flex justify-end">
        <Button type="submit" icon="plus" loading={saving}>
          {t('sfAddNote')}
        </Button>
      </div>
    </form>
  )
}

function EscalateModal({ open, onClose, onConfirm }: { open: boolean; onClose: () => void; onConfirm: (target: SafetyEscalationTarget, note: string) => Promise<void> }) {
  return open ? <EscalateDialog onClose={onClose} onConfirm={onConfirm} /> : null
}

function EscalateDialog({ onClose, onConfirm }: { onClose: () => void; onConfirm: (target: SafetyEscalationTarget, note: string) => Promise<void> }) {
  const { t } = useLang()
  const [target, setTarget] = useState<SafetyEscalationTarget>('police')
  const [note, setNote] = useState('')
  const [saving, setSaving] = useState(false)
  const submit = async () => {
    setSaving(true)
    try {
      await onConfirm(target, note.trim())
    } finally {
      setSaving(false)
    }
  }
  return (
    <Modal
      open
      title={t('sfEscalateTitle')}
      description={t('sfEscalateCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button variant="danger" onClick={submit} loading={saving}>
            {t('sfEscalate')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4">
        <Select id="escalate-target" label={t('sfEscalatedTo')} value={target} onChange={(event) => setTarget(event.target.value as SafetyEscalationTarget)}>
          {ESCALATION_TARGETS.map((value) => (
            <option key={value} value={value}>
              {t(ESCALATION_KEY[value])}
            </option>
          ))}
        </Select>
        <Textarea id="escalate-note" label={t('noteOptional')} value={note} onChange={(event) => setNote(event.target.value)} />
      </div>
    </Modal>
  )
}

function ResolveModal({ open, onClose, onConfirm }: { open: boolean; onClose: () => void; onConfirm: (code: SafetyResolutionCode, resolution: string) => Promise<void> }) {
  return open ? <ResolveDialog onClose={onClose} onConfirm={onConfirm} /> : null
}

function ResolveDialog({ onClose, onConfirm }: { onClose: () => void; onConfirm: (code: SafetyResolutionCode, resolution: string) => Promise<void> }) {
  const { t } = useLang()
  const [code, setCode] = useState<SafetyResolutionCode>('resolved_contacted')
  const [resolution, setResolution] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const submit = async () => {
    if (!resolution.trim()) {
      setError(t('fieldRequired'))
      return
    }
    setSaving(true)
    try {
      await onConfirm(code, resolution.trim())
    } finally {
      setSaving(false)
    }
  }
  return (
    <Modal
      open
      title={t('sfResolveTitle')}
      description={t('sfResolveCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button variant="brand" onClick={submit} loading={saving}>
            {t('sfResolve')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4">
        <Select id="resolution-code" label={t('sfResolutionCode')} value={code} onChange={(event) => setCode(event.target.value as SafetyResolutionCode)}>
          {RESOLUTION_CODES.map((value) => (
            <option key={value} value={value}>
              {t(RESOLUTION_KEY[value])}
            </option>
          ))}
        </Select>
        <Textarea
          id="resolution"
          label={t('sfResolution')}
          maxLength={2000}
          value={resolution}
          error={error}
          onChange={(event) => {
            setResolution(event.target.value)
            setError(null)
          }}
        />
      </div>
    </Modal>
  )
}

/** Driver position from the live snapshot (5 s poll / `LiveSnapshot` push), mounted only for open cases. */
function LiveCaseMap({ detail }: { detail: SafetyCaseDetail }) {
  const { snapshot } = useLiveSnapshot()
  const driverId = detail.trip?.driver?.id
  const live = snapshot?.drivers.find((driver) => driver.driverId === driverId || (detail.trip && driver.currentTripId === detail.trip.id))
  return <CaseMap detail={detail} driverPoint={live ? { lat: live.lat, lng: live.lng } : detail.liveLocation} />
}

function CaseMap({ detail, driverPoint }: { detail: SafetyCaseDetail; driverPoint: GeoPoint | null }) {
  const { t, lang } = useLang()
  const [map, setMap] = useState<L.Map | null>(null)
  const fitted = useRef(false)

  // Static layers: planned route, pickup / dropoff, SOS origin, last reported location.
  useEffect(() => {
    if (!map) return
    const group = L.layerGroup().addTo(map)
    const bounds: L.LatLngExpression[] = []
    const trip = detail.trip
    if (trip) {
      const route = plannedRouteOf(trip)
      L.polyline(route, routeLineStyle).addTo(group)
      bounds.push(...route)
      L.marker([trip.pickup.lat, trip.pickup.lng], { icon: pinIcon('brand') })
        .bindPopup(`<strong>${escapeHtml(t('pickup'))}</strong><br>${escapeHtml(trip.pickup.name ?? trip.pickup.address)}`)
        .addTo(group)
      L.marker([trip.dropoff.lat, trip.dropoff.lng], { icon: pinIcon('ink') })
        .bindPopup(`<strong>${escapeHtml(t('dropoff'))}</strong><br>${escapeHtml(trip.dropoff.name ?? trip.dropoff.address)}`)
        .addTo(group)
    }
    if (detail.lat !== null && detail.lng !== null) {
      L.marker([detail.lat, detail.lng], { icon: pinIcon('danger', '!') })
        .bindPopup(
          `<strong>${escapeHtml(t('sfSosLocation'))}</strong><br>${escapeHtml(formatDateTime(detail.openedAt, lang))}<br><a href="${mapsUrl(detail.lat, detail.lng)}" target="_blank" rel="noreferrer">Google Maps</a>`,
        )
        .addTo(group)
      bounds.push([detail.lat, detail.lng])
    }
    if (detail.lastLat !== null && detail.lastLng !== null) {
      L.marker([detail.lastLat, detail.lastLng], { icon: driverIcon('danger', true), zIndexOffset: 500 })
        .bindPopup(`<strong>${escapeHtml(t('sfLastLocation'))}</strong><br>${escapeHtml(formatDateTime(detail.lastLocationAt, lang))}`)
        .addTo(group)
      bounds.push([detail.lastLat, detail.lastLng])
    }
    if (!fitted.current && bounds.length > 0) {
      fitted.current = true
      if (bounds.length === 1) map.setView(bounds[0], 15)
      else map.fitBounds(L.latLngBounds(bounds), { padding: [32, 32], maxZoom: 16 })
    }
    return () => {
      group.remove()
    }
  }, [map, detail, t, lang])

  // Driver marker moves on every snapshot without refitting the view.
  const driverLat = driverPoint?.lat ?? null
  const driverLng = driverPoint?.lng ?? null
  useEffect(() => {
    if (!map || driverLat === null || driverLng === null) return
    const marker = L.marker([driverLat, driverLng], { icon: driverIcon('ink', true), zIndexOffset: 400 })
      .bindTooltip(escapeHtml(t('sfDriverLocation')))
      .addTo(map)
    return () => {
      marker.remove()
    }
  }, [map, driverLat, driverLng, t])

  return <MapView className="h-80 w-full lg:h-[26rem]" onReady={setMap} onDispose={() => setMap(null)} />
}

function MapLegend({ detail }: { detail: SafetyCaseDetail }) {
  const { t } = useLang()
  const items: { color: string; label: string; show: boolean }[] = [
    { color: 'bg-danger', label: t('sfSosLocation'), show: detail.lat !== null },
    { color: 'bg-danger/60', label: t('sfLastLocation'), show: detail.lastLat !== null },
    { color: 'bg-ink', label: t('sfDriverLocation'), show: Boolean(detail.trip?.driver) },
    { color: 'bg-brand', label: t('pickup'), show: Boolean(detail.trip) },
  ]
  return (
    <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted">
      {items
        .filter((item) => item.show)
        .map((item) => (
          <span key={item.label} className="inline-flex items-center gap-1.5">
            <span className={`size-2.5 rounded-full ${item.color}`} />
            {item.label}
          </span>
        ))}
      {detail.lat !== null && detail.lng !== null && (
        <a href={mapsUrl(detail.lat, detail.lng)} target="_blank" rel="noreferrer" className="font-bold text-brand hover:underline">
          Google Maps ↗
        </a>
      )}
    </div>
  )
}
