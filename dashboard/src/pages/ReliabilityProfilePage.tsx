import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { Input, Select, Textarea } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { Money } from '../components/Money'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { reliabilityProfiles } from '../lib/admin'
import { AT_FAULT_KEY, formatRate, RELIABILITY_ACTION_KEY, RELIABILITY_ACTIONS, RELIABILITY_ROLES, ROLE_KEY, STAGE_KEY, THRESHOLD_LEVELS } from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { excuseStatusMeta, restrictionLevelMeta } from '../lib/status'
import type { ReliabilityAction, ReliabilityAdjustment, ReliabilityAdjustInput, ReliabilityEvent, ReliabilityProfileDetail, ReliabilityRole, RestrictionLevel } from '../lib/types'

/** One reliability profile: rolling metrics, 60-day events, adjustments and the manual adjustment form. */
export function ReliabilityProfilePage() {
  const { userId = '' } = useParams()
  const [params] = useSearchParams()
  const role: ReliabilityRole = parseEnum(params.get('role'), RELIABILITY_ROLES) || 'driver'
  const { t, lang } = useLang()
  const query = useQuery(() => reliabilityProfiles.get(userId, role), `reliability-profile:${userId}:${role}`)
  const [adjust, setAdjust] = useState<ReliabilityAction | null>(null)

  if (query.loading && !query.data) return <PageSpinner />
  if (!query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const profile = query.data
  const restricted = profile.level === 'temporarily_restricted' || profile.level === 'suspended'

  return (
    <>
      <Link to={`/reliability${role === 'passenger' ? '?role=passenger' : ''}`} className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('rlBackToProfiles')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="gauge" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-brand">{t(ROLE_KEY[profile.role] ?? 'actorDriver')}</p>
              <h2 className="truncate text-2xl font-bold leading-tight">{profile.name || t('unnamed')}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={restrictionLevelMeta} value={profile.level} />
                {profile.phone && <span className="ltr-nums">{profile.phone}</span>}
                {profile.restrictedUntil && (
                  <Badge tone="danger">
                    {t('rlUntil')} {formatDateTime(profile.restrictedUntil, lang)}
                  </Badge>
                )}
              </div>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="sliders" onClick={() => setAdjust('add_points')}>
              {t('rlAdjust')}
            </Button>
            {(restricted || profile.level !== 'none' || profile.penaltyPoints > 0) && (
              <Button variant="brand" icon="check" onClick={() => setAdjust('clear_restriction')}>
                {t('rlLiftRestriction')}
              </Button>
            )}
          </div>
        </div>
      </Card>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('rlRollingMetrics')} description={`${t('rlWindow')}: ${formatNumber(profile.windowDays)} ${t('rlDays')}`} className="lg:col-span-2">
          <div className="mb-4 grid gap-3 sm:grid-cols-3">
            <Metric label={t('rlCancellationRate')} value={formatRate(profile.cancellationRate)} tone={profile.cancellationRate >= 0.15 ? 'danger' : 'ink'} />
            <Metric label={t('rlReliabilityRate')} value={formatRate(profile.reliabilityRate)} tone="brand" />
            <Metric label={t('rlPenaltyPoints')} value={formatNumber(profile.penaltyPoints)} tone={profile.penaltyPoints > 0 ? 'danger' : 'ink'} />
          </div>
          <DefinitionList
            items={[
              { label: t('rlTripsRequested'), value: formatNumber(profile.tripsRequested), ltr: true },
              { label: t('rlTripsAccepted'), value: formatNumber(profile.tripsAccepted), ltr: true },
              { label: t('rlTripsCompleted'), value: formatNumber(profile.tripsCompleted), ltr: true },
              { label: t('rlCancellationsAtFault'), value: formatNumber(profile.cancellationsAtFault), ltr: true },
              { label: t('rlNoShows'), value: formatNumber(profile.noShowCount), ltr: true },
              ...(profile.role === 'driver'
                ? [
                    { label: t('rlOffers'), value: `${formatNumber(profile.offersAccepted)} / ${formatNumber(profile.offersReceived)}`, ltr: true },
                    { label: t('statAcceptance'), value: formatRate(profile.acceptanceRate), ltr: true },
                  ]
                : []),
              { label: t('rlLevelChangedAt'), value: formatDateTime(profile.levelChangedAt, lang) },
              { label: t('computedAt'), value: formatDateTime(profile.lastComputedAt, lang) },
            ]}
          />
        </Card>

        <Card title={t('rlEffects')}>
          <div className="space-y-3 text-sm">
            {profile.nextLevel ? (
              <div className="rounded-2xl bg-amber-50 px-4 py-3 text-amber-700">
                <p className="text-xs font-bold">{t('rlNextLevel')}</p>
                <p className="mt-1 font-bold">{t(restrictionLevelMeta[profile.nextLevel.level]?.key ?? 'rlLevelWarning')}</p>
                <p className="ltr-nums mt-1 text-xs">
                  {profile.nextLevel.minPenaltyPoints !== null ? `≥ ${formatNumber(profile.nextLevel.minPenaltyPoints)} ${t('points')}` : ''}
                  {profile.nextLevel.minPenaltyPoints !== null && profile.nextLevel.minCancellationRate !== null ? ' · ' : ''}
                  {profile.nextLevel.minCancellationRate !== null ? `≥ ${formatRate(profile.nextLevel.minCancellationRate)}` : ''}
                </p>
              </div>
            ) : (
              <p className="rounded-2xl bg-cloud px-4 py-3 text-muted">{t('rlTopLevel')}</p>
            )}
            {profile.effects && (
              <dl className="space-y-2">
                <div className="flex justify-between gap-3 rounded-2xl bg-cloud px-4 py-3">
                  <dt className="text-muted">{t('rlMatchingFactor')}</dt>
                  <dd className="ltr-nums font-bold">×{profile.effects.matchingFactor.toFixed(2)}</dd>
                </div>
                <div className="flex justify-between gap-3 rounded-2xl bg-cloud px-4 py-3">
                  <dt className="text-muted">{t('rlIncentiveMultiplier')}</dt>
                  <dd className="ltr-nums font-bold">×{profile.effects.incentiveMultiplier.toFixed(2)}</dd>
                </div>
              </dl>
            )}
          </div>
        </Card>
      </div>

      <Card title={t('rlEventsTitle')} description={t('rlEventsCopy')} flush className="mb-6">
        <EventsTable events={profile.events ?? []} />
      </Card>

      <Card title={t('rlAdjustmentsTitle')} flush className="mb-6">
        <AdjustmentsTable adjustments={profile.adjustments ?? []} />
      </Card>

      <AdjustModal
        profile={profile}
        initialAction={adjust}
        onClose={() => setAdjust(null)}
        onSaved={() => {
          setAdjust(null)
          query.reload()
        }}
      />
    </>
  )
}

function Metric({ label, value, tone }: { label: string; value: string; tone: 'brand' | 'danger' | 'ink' }) {
  return (
    <div className="rounded-2xl bg-cloud px-4 py-3">
      <p className="text-xs font-bold text-muted">{label}</p>
      <p className={`ltr-nums mt-1 text-2xl font-bold ${tone === 'danger' ? 'text-danger' : tone === 'brand' ? 'text-brand' : ''}`}>{value}</p>
    </div>
  )
}

function EventsTable({ events }: { events: ReliabilityEvent[] }) {
  const { t, lang } = useLang()
  const columns: Column<ReliabilityEvent>[] = [
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (
        <span className="block">
          <Link to={`/trips/${row.tripId}`} className="ltr-nums block font-bold text-brand hover:underline">
            {row.tripNumber ?? t('viewTrip')}
          </Link>
          <span className="block text-xs text-muted">{formatDateTime(row.createdAt, lang)}</span>
        </span>
      ),
    },
    { key: 'stage', header: t('cxStage'), render: (row) => t(STAGE_KEY[row.stage] ?? 'cxStageBeforeAccept') },
    {
      key: 'reason',
      header: t('reason'),
      render: (row) => (
        <span className="block max-w-56">
          <span className="block truncate">{row.reasonName ?? row.reasonCode ?? '—'}</span>
          {row.atFault && <span className="block text-xs text-muted">{t('cxAtFault')}: {t(AT_FAULT_KEY[row.atFault] ?? 'cxFaultNone')}</span>}
        </span>
      ),
    },
    { key: 'fee', header: t('cxFee'), className: 'text-end', render: (row) => (row.feeCharged ? <Money value={row.feeCharged} /> : <span className="text-muted">—</span>) },
    { key: 'points', header: t('cxPoints'), className: 'text-center', render: (row) => <span className="ltr-nums font-bold">{formatNumber(row.penaltyPoints)}</span> },
    { key: 'excuse', header: t('cxExcuse'), render: (row) => <MetaBadge record={excuseStatusMeta} value={row.excuseStatus} /> },
    {
      key: 'counts',
      header: t('rlCountsTowardRate'),
      className: 'text-center',
      render: (row) => (row.countsTowardRate === null || row.countsTowardRate === undefined ? <span className="text-muted">—</span> : <Badge tone={row.countsTowardRate ? 'danger' : 'muted'}>{row.countsTowardRate ? t('yes') : t('no')}</Badge>),
    },
  ]
  return <Table columns={columns} rows={events} rowKey={(row) => row.id ?? `${row.tripId}:${row.createdAt}`} emptyTitle={t('rlNoEvents')} emptyDescription="" />
}

function AdjustmentsTable({ adjustments }: { adjustments: ReliabilityAdjustment[] }) {
  const { t, lang } = useLang()
  const columns: Column<ReliabilityAdjustment>[] = [
    { key: 'at', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.createdAt, lang)}</span> },
    { key: 'action', header: t('action'), render: (row) => <span className="font-bold">{t(RELIABILITY_ACTION_KEY[row.action] ?? 'rlActionAddPoints')}</span> },
    {
      key: 'value',
      header: t('details'),
      render: (row) => (
        <span className="block text-xs">
          {row.points !== null && <span className="ltr-nums block font-bold">{row.points > 0 ? `+${row.points}` : row.points}</span>}
          {row.level && <MetaBadge record={restrictionLevelMeta} value={row.level} />}
          {row.until && (
            <span className="block text-muted">
              {t('rlUntil')} {formatDateTime(row.until, lang)}
            </span>
          )}
        </span>
      ),
    },
    { key: 'reason', header: t('reason'), render: (row) => <span className="block max-w-72 whitespace-normal text-sm">{row.reason}</span> },
    { key: 'by', header: t('createdBy'), render: (row) => row.createdByName ?? '—' },
  ]
  return <Table columns={columns} rows={adjustments} rowKey={(row) => row.id} emptyTitle={t('rlNoAdjustments')} emptyDescription="" />
}

function AdjustModal({ profile, initialAction, onClose, onSaved }: { profile: ReliabilityProfileDetail; initialAction: ReliabilityAction | null; onClose: () => void; onSaved: () => void }) {
  return initialAction ? <AdjustDialog key={initialAction} profile={profile} initialAction={initialAction} onClose={onClose} onSaved={onSaved} /> : null
}

function AdjustDialog({ profile, initialAction, onClose, onSaved }: { profile: ReliabilityProfileDetail; initialAction: ReliabilityAction; onClose: () => void; onSaved: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const levels = THRESHOLD_LEVELS[profile.role] ?? THRESHOLD_LEVELS.driver
  const [action, setAction] = useState<ReliabilityAction>(initialAction)
  const [points, setPoints] = useState('1')
  const [level, setLevel] = useState<Exclude<RestrictionLevel, 'none'>>(levels[0])
  const [until, setUntil] = useState('')
  const [reason, setReason] = useState('')
  const [errors, setErrors] = useState<{ points?: string; reason?: string; until?: string }>({})
  const [saving, setSaving] = useState(false)

  const submit = async () => {
    const next: typeof errors = {}
    const amount = Number(points)
    if ((action === 'add_points' || action === 'remove_points') && (!Number.isInteger(amount) || amount <= 0)) next.points = t('invalidNumber')
    if (!reason.trim()) next.reason = t('reasonRequired')
    const untilIso = until ? new Date(until).toISOString() : undefined
    if (action === 'set_level' && until && new Date(until).getTime() <= Date.now()) next.until = t('scheduleInFuture')
    setErrors(next)
    if (Object.keys(next).length > 0) return

    const input: ReliabilityAdjustInput = { role: profile.role, action, reason: reason.trim() }
    if (action === 'add_points') input.points = amount
    if (action === 'remove_points') input.points = -amount
    if (action === 'set_level') {
      input.level = level
      input.until = untilIso
    }
    setSaving(true)
    try {
      await reliabilityProfiles.adjust(profile.userId, input)
      toast.success(t('rlAdjusted'))
      onSaved()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={action === 'clear_restriction' ? t('rlLiftRestriction') : t('rlAdjust')}
      description={`${profile.name || t('unnamed')} · ${t(ROLE_KEY[profile.role])} — ${t('rlAdjustCopy')}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button onClick={submit} loading={saving} variant={action === 'clear_restriction' ? 'brand' : 'primary'}>
            {t('confirm')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4">
        <Select id="adjust-action" label={t('action')} value={action} onChange={(event) => setAction(event.target.value as ReliabilityAction)}>
          {RELIABILITY_ACTIONS.map((value) => (
            <option key={value} value={value}>
              {t(RELIABILITY_ACTION_KEY[value])}
            </option>
          ))}
        </Select>
        {(action === 'add_points' || action === 'remove_points') && (
          <Input id="adjust-points" type="number" min={1} step={1} dir="ltr" label={t('rlPoints')} value={points} error={errors.points} onChange={(event) => setPoints(event.target.value)} />
        )}
        {action === 'set_level' && (
          <>
            <Select id="adjust-level" label={t('rlLevel')} value={level} onChange={(event) => setLevel(event.target.value as Exclude<RestrictionLevel, 'none'>)}>
              {levels.map((value) => (
                <option key={value} value={value}>
                  {t(restrictionLevelMeta[value].key)}
                </option>
              ))}
            </Select>
            <Input id="adjust-until" type="datetime-local" dir="ltr" label={t('rlUntilOptional')} hint={t('rlUntilHint')} value={until} error={errors.until} onChange={(event) => setUntil(event.target.value)} />
          </>
        )}
        {action === 'clear_restriction' && <p className="rounded-2xl bg-brand-soft px-4 py-3 text-sm">{t('rlClearCopy')}</p>}
        <Textarea id="adjust-reason" label={t('reason')} maxLength={500} value={reason} error={errors.reason} onChange={(event) => setReason(event.target.value)} />
      </div>
    </Modal>
  )
}
