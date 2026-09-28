import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { useLang } from '../context/lang'
import { useNow } from '../hooks/useNow'
import { useQuery } from '../hooks/useQuery'
import { safety, tripSafety } from '../lib/admin'
import { formatDateTime, formatNumber } from '../lib/format'
import { ALERT_TYPE_KEY, alertMetricsText, SAFETY_TYPE_KEY, SHARE_CHANNEL_KEY } from '../lib/safety'
import { safetyAlertStatusMeta, safetyCaseStatusMeta, safetyPriorityMeta } from '../lib/status'
import type { TripDetail } from '../lib/types'
import { Badge, MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { Icon } from './Icon'
import { SafetyCaseCreateModal } from './SafetyCaseCreateModal'
import { PageSpinner } from './Spinner'

/**
 * "السلامة" section of the trip page: safety cases, automatic alerts and share links of the trip.
 * The admin list endpoints are filtered by trip number / id and narrowed again client-side.
 */
export function TripSafetyCard({ trip }: { trip: TripDetail }) {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const [creating, setCreating] = useState(false)
  const now = useNow(60_000)

  const cases = useQuery(async () => {
    const page = await safety.cases({ search: trip.tripNumber, page: 1, pageSize: 20 })
    return page.items.filter((item) => item.tripId === trip.id || item.tripNumber === trip.tripNumber)
  }, `trip-safety-cases:${trip.id}`)
  const alerts = useQuery(async () => {
    const page = await safety.alerts({ tripId: trip.id, search: trip.tripNumber, page: 1, pageSize: 50 })
    return page.items.filter((alert) => alert.tripId === trip.id)
  }, `trip-safety-alerts:${trip.id}`)
  const shares = useQuery(() => tripSafety.shares(trip.id), `trip-shares:${trip.id}`)

  // Without `safety.manage` every call answers 403: hide the whole section.
  if (cases.error?.status === 403 && alerts.error?.status === 403) return null

  return (
    <Card
      title={t('sfTripSection')}
      className="mb-6"
      action={
        <Button variant="danger-outline" size="sm" icon="shield" onClick={() => setCreating(true)}>
          {t('sfNewCase')}
        </Button>
      }
    >
      <div className="grid gap-6 lg:grid-cols-3">
        <section>
          <h3 className="mb-2 text-sm font-bold">{t('sfCasesHeading')}</h3>
          {cases.loading && !cases.data ? (
            <PageSpinner />
          ) : cases.error ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfUnavailable')}</p>
          ) : (cases.data ?? []).length === 0 ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfNoCases')}</p>
          ) : (
            <ul className="space-y-2">
              {(cases.data ?? []).map((item) => (
                <li key={item.id}>
                  <Link to={`/safety/cases/${item.id}`} className="block rounded-2xl border border-line px-4 py-3 transition hover:bg-cloud">
                    <span className="flex flex-wrap items-center justify-between gap-2">
                      <span className="ltr-nums font-bold">{item.caseNumber}</span>
                      <MetaBadge record={safetyCaseStatusMeta} value={item.status} />
                    </span>
                    <span className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted">
                      <MetaBadge record={safetyPriorityMeta} value={item.priority} />
                      {t(SAFETY_TYPE_KEY[item.type] ?? 'sfTypeSafetyReport')} · {formatDateTime(item.openedAt, lang)}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section>
          <h3 className="mb-2 text-sm font-bold">{t('sfAlertsHeading')}</h3>
          {alerts.loading && !alerts.data ? (
            <PageSpinner />
          ) : alerts.error ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfUnavailable')}</p>
          ) : (alerts.data ?? []).length === 0 ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfNoAlerts')}</p>
          ) : (
            <ul className="space-y-2">
              {(alerts.data ?? []).map((alert) => (
                <li key={alert.id} className="rounded-2xl border border-line px-4 py-3">
                  <span className="flex flex-wrap items-center justify-between gap-2">
                    <span className="font-bold">{t(ALERT_TYPE_KEY[alert.type] ?? 'sfTypeUnexpectedStop')}</span>
                    <MetaBadge record={safetyAlertStatusMeta} value={alert.status} />
                  </span>
                  <span className="ltr-nums mt-1 block text-xs text-muted">{alertMetricsText(alert, t)}</span>
                  <span className="mt-1 flex flex-wrap items-center justify-between gap-2 text-xs text-muted">
                    {formatDateTime(alert.detectedAt, lang)}
                    {alert.safetyCaseId && (
                      <button type="button" onClick={() => navigate(`/safety/cases/${alert.safetyCaseId}`)} className="font-bold text-brand hover:underline">
                        {alert.safetyCaseNumber ?? t('sfOpenCase')}
                      </button>
                    )}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section>
          <h3 className="mb-2 text-sm font-bold">{t('sfSharesHeading')}</h3>
          {shares.loading && !shares.data ? (
            <PageSpinner />
          ) : shares.error ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{shares.error.status === 404 ? t('sfSharesUnavailable') : t('sfUnavailable')}</p>
          ) : (shares.data ?? []).length === 0 ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('sfNoShares')}</p>
          ) : (
            <ul className="space-y-2">
              {(shares.data ?? []).map((share) => {
                const state = share.revokedAt ? 'revoked' : share.expiresAt && new Date(share.expiresAt).getTime() < now ? 'expired' : 'active'
                return (
                  <li key={share.id} className="rounded-2xl border border-line px-4 py-3 text-sm">
                    <span className="flex flex-wrap items-center justify-between gap-2">
                      <span className="font-bold">
                        {t(SHARE_CHANNEL_KEY[share.channel] ?? 'sfShareLink')}
                        {share.trustedContactName ? ` · ${share.trustedContactName}` : ''}
                      </span>
                      <Badge tone={state === 'active' ? 'brand' : 'muted'}>{state === 'active' ? t('active') : state === 'revoked' ? t('sfRevoked') : t('expired')}</Badge>
                    </span>
                    <span className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted">
                      <span className="inline-flex items-center gap-1">
                        <Icon name="eye" className="size-3.5" />
                        <span className="ltr-nums">{formatNumber(share.viewCount)}</span>
                      </span>
                      <span>{formatDateTime(share.createdAt, lang)}</span>
                      {share.expiresAt && (
                        <span>
                          {t('expiresAt')}: {formatDateTime(share.expiresAt, lang)}
                        </span>
                      )}
                    </span>
                  </li>
                )
              })}
            </ul>
          )}
          <p className="mt-2 text-[11px] text-muted">{t('sfSharesPrivacy')}</p>
        </section>
      </div>

      <SafetyCaseCreateModal
        open={creating}
        preset={{ tripId: trip.id, tripLabel: trip.tripNumber, priority: 'high' }}
        onClose={() => setCreating(false)}
        onCreated={(created) => navigate(`/safety/cases/${created.id}`)}
      />
    </Card>
  )
}
