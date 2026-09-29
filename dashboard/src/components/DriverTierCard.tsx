import { useState } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { driverTiers } from '../lib/admin'
import { formatDateTime, formatNumber } from '../lib/format'
import { DRIVER_TIERS, formatAvg, formatRatio } from '../lib/rewards'
import { driverTierMeta } from '../lib/status'
import type { DriverTier, DriverTierHistoryEntry } from '../lib/types'
import { MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { ErrorState } from './ErrorState'
import { Select, Textarea } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { PageSpinner } from './Spinner'

/**
 * Driver tier (§F15.7): current tier, history (`GET /admin/drivers/{id}/tier-history`) with the metrics
 * that decided each change, and a manual override (`POST /admin/drivers/{id}/tier`). Hidden on 403.
 */
export function DriverTierCard({ driverId, tier, onChanged }: { driverId: string; tier: DriverTier | null | undefined; onChanged: () => void }) {
  const { t, lang } = useLang()
  const history = useQuery(() => driverTiers.history(driverId), `driver-tier-history:${driverId}`)
  const [setting, setSetting] = useState(false)
  if (history.error?.status === 403) return null
  const entries = [...(history.data ?? [])].sort((a, b) => new Date(b.computedAt).getTime() - new Date(a.computedAt).getTime())
  const current = tier ?? entries[0]?.toTier ?? null

  return (
    <Card
      title={t('tierCardTitle')}
      className="mb-6"
      action={
        <Button variant="secondary" size="sm" icon="edit" onClick={() => setSetting(true)}>
          {t('tierSetManual')}
        </Button>
      }
    >
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <span className="grid size-11 place-items-center rounded-xl bg-brand-soft text-brand">
          <Icon name="trophy" className="size-5" />
        </span>
        {current ? <MetaBadge record={driverTierMeta} value={current} className="text-sm" /> : <span className="text-sm text-muted">{t('tierNotComputed')}</span>}
      </div>
      {history.loading && !history.data ? (
        <PageSpinner />
      ) : history.error && history.error.status !== 404 ? (
        <ErrorState error={history.error} onRetry={history.reload} />
      ) : entries.length === 0 ? (
        <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('tierNoHistory')}</p>
      ) : (
        <ol className="space-y-3">
          {entries.slice(0, 8).map((entry) => (
            <HistoryItem key={entry.id} entry={entry} lang={lang} />
          ))}
        </ol>
      )}
      {setting && (
        <SetTierModal
          driverId={driverId}
          current={current}
          onClose={() => setSetting(false)}
          onDone={() => {
            setSetting(false)
            history.reload()
            onChanged()
          }}
        />
      )}
    </Card>
  )
}

function HistoryItem({ entry, lang }: { entry: DriverTierHistoryEntry; lang: 'ar' | 'en' }) {
  const { t } = useLang()
  return (
    <li className="rounded-2xl border border-line px-4 py-3">
      <div className="flex flex-wrap items-center gap-2 text-sm">
        {entry.fromTier ? <MetaBadge record={driverTierMeta} value={entry.fromTier} /> : <span className="text-muted">—</span>}
        <Icon name="arrow" className="size-4 text-muted ltr:rotate-180" />
        <MetaBadge record={driverTierMeta} value={entry.toTier} />
        <span className="text-xs text-muted">
          {entry.reason === 'admin' ? t('tierReasonAdmin') : entry.reason === 'weekly_recalc' ? t('tierReasonWeekly') : entry.reason} · {formatDateTime(entry.computedAt, lang)}
          {entry.actorName ? ` · ${entry.actorName}` : ''}
        </span>
      </div>
      {entry.metrics && (
        <p className="ltr-nums mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted">
          <span>
            {t('icTrips')}: <b className="text-ink">{formatNumber(entry.metrics.completedTrips)}</b>
          </span>
          <span>
            ★ <b className="text-ink">{formatAvg(entry.metrics.ratingAvg)}</b>
          </span>
          <span>
            {t('tierAcceptance')}: <b className="text-ink">{formatRatio(entry.metrics.acceptanceRate)}</b>
          </span>
          <span>
            {t('rlCancellationRate')}: <b className="text-ink">{formatRatio(entry.metrics.cancellationRate)}</b>
          </span>
        </p>
      )}
      {entry.note && <p className="mt-2 rounded-xl bg-cloud px-3 py-2 text-sm">{entry.note}</p>}
    </li>
  )
}

function SetTierModal({ driverId, current, onClose, onDone }: { driverId: string; current: DriverTier | null; onClose: () => void; onDone: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [tier, setTier] = useState<DriverTier>(current ?? 'bronze')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const submit = async () => {
    if (!reason.trim()) {
      setError(t('reasonRequired'))
      return
    }
    setSaving(true)
    try {
      await driverTiers.setTier(driverId, tier, reason.trim())
      toast.success(t('tierSetDone'))
      onDone()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={t('tierSetManual')}
      description={t('tierSetManualCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button onClick={submit} loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <Select id="set-tier" label={t('tierLabel')} value={tier} onChange={(event) => setTier(event.target.value as DriverTier)}>
          {DRIVER_TIERS.map((value) => (
            <option key={value} value={value}>
              {t(driverTierMeta[value].key)}
            </option>
          ))}
        </Select>
        <Textarea
          id="set-tier-reason"
          label={t('reason')}
          placeholder={t('reasonPlaceholder')}
          value={reason}
          error={error}
          onChange={(event) => {
            setReason(event.target.value)
            setError(null)
          }}
        />
      </div>
    </Modal>
  )
}
