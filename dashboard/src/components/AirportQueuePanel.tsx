import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useNow } from '../hooks/useNow'
import { useQuery } from '../hooks/useQuery'
import { airports } from '../lib/admin'
import { minutesSince } from '../lib/airports'
import { formatDateTime, formatNumber, formatTime } from '../lib/format'
import { airportQueueStatusMeta } from '../lib/status'
import type { AirportQueueEntry } from '../lib/types'
import { Badge, MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { Icon } from './Icon'
import { PermissionError } from './PermissionError'
import { ReasonModal } from './ReasonModal'
import { Table, type Column } from './Table'

/** Refresh cadence of the live queue view (there is no SignalR channel for admins in §F17.8). */
export const QUEUE_POLL_MS = 10_000

/** FIFO driver queue of an airport (`GET /admin/airports/{id}/queue`, polled every 10 s) with removal by reason (`airport.manage`). */
export function AirportQueuePanel({ airportId, queueEnabled }: { airportId: string; queueEnabled: boolean }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [updatedAt, setUpdatedAt] = useState<number | null>(null)
  const query = useQuery(async () => {
    const entries = await airports.queue(airportId)
    setUpdatedAt(Date.now())
    return entries
  }, `airport-queue:${airportId}`)
  const now = useNow(30_000)
  const [removing, setRemoving] = useState<AirportQueueEntry | null>(null)
  const { reload } = query

  useEffect(() => {
    const timer = window.setInterval(reload, QUEUE_POLL_MS)
    return () => window.clearInterval(timer)
  }, [reload])

  // FIFO by entry time (the server also sends `position`, which stays the tie-breaker).
  const rows = [...(query.data ?? [])].sort((a, b) => new Date(a.enteredAt).getTime() - new Date(b.enteredAt).getTime() || a.position - b.position)

  const remove = async (reason: string) => {
    if (!removing) return
    try {
      await airports.removeFromQueue(airportId, removing.entryId, reason)
      toast.success(t('apQueueRemovedToast'))
      setRemoving(null)
      reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<AirportQueueEntry>[] = [
    { key: 'position', header: '#', className: 'w-12', render: (row) => <span className="ltr-nums font-bold">{formatNumber(row.position)}</span> },
    {
      key: 'driver',
      header: t('driver'),
      render: (row) =>
        row.driverId ? (
          <Link to={`/drivers/${row.driverId}`} className="font-bold text-brand hover:underline">
            {row.driverName || t('unnamed')}
          </Link>
        ) : (
          <span className="font-bold">{row.driverName || t('unnamed')}</span>
        ),
    },
    { key: 'category', header: t('category'), render: (row) => <span className="ltr-nums">{row.categoryCode ?? '—'}</span> },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={airportQueueStatusMeta} value={row.status} /> },
    {
      key: 'enteredAt',
      header: t('apQueueEntered'),
      render: (row) => {
        const waited = minutesSince(row.enteredAt, now)
        return (
          <span className="block whitespace-nowrap">
            <span className="block text-xs">{formatDateTime(row.enteredAt, lang)}</span>
            {waited !== null && (
              <span className="ltr-nums block text-xs text-muted">
                {t('apQueueWaited')} {formatNumber(waited)} {t('min')}
              </span>
            )}
          </span>
        )
      },
    },
    { key: 'lastSeen', header: t('apQueueLastSeen'), render: (row) => <span className="ltr-nums whitespace-nowrap text-xs">{row.lastSeenAt ? formatTime(row.lastSeenAt, lang) : '—'}</span> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <Button variant="danger-outline" size="sm" icon="x" onClick={() => setRemoving(row)}>
          {t('apQueueRemove')}
        </Button>
      ),
    },
  ]

  return (
    <Card
      title={t('apQueueTitle')}
      description={t('apQueueCopy')}
      flush
      action={
        <div className="flex items-center gap-3 pe-5 sm:pe-6">
          {queueEnabled ? (
            <Badge tone="brand">
              <span className="me-1.5 inline-block size-2 rounded-full bg-brand motion-safe:animate-pulse" aria-hidden="true" />
              {t('apQueueLive')}
            </Badge>
          ) : (
            <Badge tone="muted">{t('apQueueDisabled')}</Badge>
          )}
          <Button variant="secondary" size="sm" icon="refresh" onClick={reload} loading={query.loading && Boolean(query.data)}>
            {t('refresh')}
          </Button>
        </div>
      }
    >
      {!queueEnabled && (
        <p className="mx-5 mb-4 flex items-start gap-2 rounded-2xl bg-amber-50 px-4 py-3 text-xs font-bold text-amber-800 sm:mx-6">
          <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
          {t('apQueueDisabledCopy')}
        </p>
      )}
      {query.error && !query.data ? (
        <PermissionError error={query.error} permission="airport.manage" onRetry={reload} />
      ) : (
        <>
          <Table columns={columns} rows={rows} rowKey={(row) => row.entryId} loading={query.loading && !query.data} emptyTitle={t('apQueueEmpty')} emptyDescription={t('apQueueEmptyCopy')} />
          <p className="border-t border-line px-5 py-3 text-xs text-muted sm:px-6">
            {formatNumber(rows.length)} {t('apQueueDrivers')}
            {updatedAt !== null && ` · ${t('apUpdatedAt')} ${formatTime(updatedAt, lang)}`}
          </p>
        </>
      )}

      <ReasonModal
        open={removing !== null}
        title={t('apQueueRemoveTitle')}
        description={removing ? `${removing.driverName || t('unnamed')} — ${t('apQueueRemoveCopy')}` : undefined}
        confirmLabel={t('apQueueRemove')}
        onClose={() => setRemoving(null)}
        onConfirm={remove}
      />
    </Card>
  )
}
