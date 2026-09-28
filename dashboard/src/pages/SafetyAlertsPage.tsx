import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input, Select } from '../components/Field'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { ReasonModal } from '../components/ReasonModal'
import { SafetyCaseCreateModal, type SafetyCaseCreatePreset } from '../components/SafetyCaseCreateModal'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useNow } from '../hooks/useNow'
import { useQuery } from '../hooks/useQuery'
import { useSafetyFeed } from '../hooks/useSafetyFeed'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { safety } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDateTime } from '../lib/format'
import { ALERT_TYPE_KEY, alertMetricsText, formatDuration, mapsUrl, SAFETY_ALERT_STATUSES, SAFETY_ALERT_TYPES } from '../lib/safety'
import { safetyAlertStatusMeta } from '../lib/status'
import type { SafetyAlert, SafetyAlertStatus } from '../lib/types'

const PAGE_SIZE = 20

/** Automatic alerts (unexpected stop / route deviation / overrun) — acknowledge or convert to a case. */
export function SafetyAlertsPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const now = useNow()
  const { params, setFilter, page, setPage } = useUrlState()
  const [dismissing, setDismissing] = useState<SafetyAlert | null>(null)
  const [converting, setConverting] = useState<SafetyCaseCreatePreset | null>(null)
  const [fresh, setFresh] = useState<Set<string>>(() => new Set())

  const status = parseEnum(params.get('status'), SAFETY_ALERT_STATUSES)
  const type = parseEnum(params.get('type'), SAFETY_ALERT_TYPES)
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))

  const query = useQuery(() => safety.alerts({ status, type, from, to, page, pageSize: PAGE_SIZE }), `safety-alerts:${status}:${type}:${from}:${to}:${page}`)

  const feed = useSafetyFeed({
    watchAlerts: true,
    onEvent: (event) => {
      if (event.kind === 'alert_raised') setFresh((current) => new Set(current).add(event.alert.id))
      if (event.kind === 'alert_raised' || event.kind === 'changed') query.reload()
    },
  })

  const dismiss = async (note: string) => {
    if (!dismissing) return
    try {
      await safety.dismissAlert(dismissing.id, note)
      toast.success(t('sfAlertDismissedToast'))
      setDismissing(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const convert = (alert: SafetyAlert) =>
    setConverting({
      tripId: alert.tripId,
      tripLabel: alert.tripNumber ?? alert.tripId,
      priority: 'high',
      description: `${t(ALERT_TYPE_KEY[alert.type] ?? 'sfTypeUnexpectedStop')} — ${alertMetricsText(alert, t)} · ${formatDateTime(alert.detectedAt, lang)}`,
    })

  const columns: Column<SafetyAlert>[] = [
    {
      key: 'type',
      header: t('type'),
      render: (row) => (
        <span className="flex items-center gap-2">
          {fresh.has(row.id) && <Badge tone="danger">{t('sfNew')}</Badge>}
          <span className="block">
            <span className="block font-bold">{t(ALERT_TYPE_KEY[row.type] ?? 'sfTypeUnexpectedStop')}</span>
            <span className="ltr-nums block text-xs text-muted">{alertMetricsText(row, t)}</span>
          </span>
        </span>
      ),
    },
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (
        <Link to={`/trips/${row.tripId}`} className="ltr-nums font-bold text-brand hover:underline">
          {row.tripNumber ?? t('viewTrip')}
        </Link>
      ),
    },
    { key: 'detected', header: t('sfDetectedAt'), render: (row) => <span className="whitespace-nowrap text-xs">{formatDateTime(row.detectedAt, lang)}</span> },
    {
      key: 'status',
      header: t('status'),
      render: (row) => {
        const remaining = row.status === 'pending_rider' && row.respondBy ? (new Date(row.respondBy).getTime() - now) / 1000 : null
        return (
          <span className="block">
            <MetaBadge record={safetyAlertStatusMeta} value={row.status} />
            {remaining !== null && (
              <span className={`ltr-nums mt-1 block text-xs font-bold ${remaining <= 0 ? 'text-danger' : 'text-amber-700'}`}>
                {remaining > 0 ? `${t('sfRespondWithin')} ${formatDuration(remaining)}` : t('sfResponseOverdue')}
              </span>
            )}
            {row.response && <span className="mt-1 block text-xs text-muted">{row.response === 'ok' ? t('sfRiderOk') : t('sfRiderNeedsHelp')}</span>}
            {row.dismissedByName && <span className="mt-1 block text-xs text-muted">{row.dismissedByName}</span>}
          </span>
        )
      },
    },
    {
      key: 'location',
      header: t('sfLocation'),
      render: (row) =>
        row.lat !== null && row.lng !== null ? (
          <a href={mapsUrl(row.lat, row.lng)} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-xs font-bold text-brand hover:underline">
            <Icon name="pin" className="size-3.5" />
            <span className="ltr-nums">
              {row.lat.toFixed(4)}, {row.lng.toFixed(4)}
            </span>
          </a>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) =>
        row.safetyCaseId ? (
          <Link to={`/safety/cases/${row.safetyCaseId}`} className="inline-flex items-center gap-1 text-xs font-bold text-brand hover:underline">
            {row.safetyCaseNumber ? <span className="ltr-nums">{row.safetyCaseNumber}</span> : t('sfOpenCase')}
            <Icon name="chevron" className="size-3.5 rtl:rotate-180" />
          </Link>
        ) : row.status === 'dismissed' || row.status === 'resolved_ok' ? null : (
          <span className="inline-flex gap-2">
            <Button variant="secondary" size="sm" icon="check" onClick={() => setDismissing(row)}>
              {t('sfAcknowledge')}
            </Button>
            <Button variant="danger-outline" size="sm" icon="shield" onClick={() => convert(row)}>
              {t('sfConvertToCase')}
            </Button>
          </span>
        ),
    },
  ]

  return (
    <>
      <PageHeader title={t('sfAlertsTitle')} description={t('sfAlertsCopy')} actions={<Badge tone={feed.live ? 'brand' : 'muted'}>{feed.live ? t('liveViaHub') : t('liveViaPolling')}</Badge>} />

      <Tabs
        className="mb-4"
        value={status}
        onChange={(value) => setFilter('status', value)}
        options={[
          { value: '' as SafetyAlertStatus | '', label: t('statusAll') },
          ...SAFETY_ALERT_STATUSES.map((value) => ({ value, label: t(safetyAlertStatusMeta[value].key) })),
        ]}
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-3">
        <Select id="alert-type" aria-label={t('type')} value={type} onChange={(event) => setFilter('type', event.target.value)}>
          <option value="">{t('sfAllTypes')}</option>
          {SAFETY_ALERT_TYPES.map((value) => (
            <option key={value} value={value}>
              {t(ALERT_TYPE_KEY[value])}
            </option>
          ))}
        </Select>
        <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('sfNoAlerts')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <ReasonModal
        open={dismissing !== null}
        title={t('sfAcknowledgeTitle')}
        description={dismissing ? `${t(ALERT_TYPE_KEY[dismissing.type] ?? 'sfTypeUnexpectedStop')} · ${dismissing.tripNumber ?? ''} — ${t('sfAcknowledgeCopy')}` : undefined}
        confirmLabel={t('sfAcknowledge')}
        confirmVariant="primary"
        label={t('note')}
        onClose={() => setDismissing(null)}
        onConfirm={dismiss}
      />

      <SafetyCaseCreateModal
        open={converting !== null}
        preset={converting ?? undefined}
        onClose={() => setConverting(null)}
        onCreated={(created) => {
          setConverting(null)
          navigate(`/safety/cases/${created.id}`)
        }}
      />
    </>
  )
}
