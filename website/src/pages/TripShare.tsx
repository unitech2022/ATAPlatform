import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import logo from '../assets/logo.png'
import { Card } from '../components/Card'
import { Icon, type IconName } from '../components/Icon'
import { LanguageToggle } from '../components/LanguageToggle'
import { Notice } from '../components/Notice'
import { TrackingMap } from '../components/TrackingMap'
import { useI18n, type TranslationKey } from '../i18n'
import { resolveApiUrl } from '../lib/config'
import { formatEtaSeconds, formatNumber, formatTime } from '../lib/format'
import { isTerminalPhase, sharePhase, usePublicShare, type SharePhase } from '../lib/publicShare'
import type { Place, PublicTripShare } from '../lib/types'

const phaseCopy: Record<SharePhase, { title: TranslationKey; icon: IconName; tone: string }> = {
  searching: { title: 'share.phase.searching', icon: 'search', tone: 'bg-cloud text-muted' },
  en_route: { title: 'share.phase.en_route', icon: 'car', tone: 'bg-brand-soft text-brand' },
  arrived: { title: 'share.phase.arrived', icon: 'pin', tone: 'bg-brand text-white' },
  in_trip: { title: 'share.phase.in_trip', icon: 'location', tone: 'bg-ink text-white' },
  completed: { title: 'share.phase.completed', icon: 'check', tone: 'bg-brand-soft text-brand' },
  cancelled: { title: 'share.phase.cancelled', icon: 'close', tone: 'bg-danger-soft text-danger' },
}

function Shell({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-canvas text-ink">
      <meta name="robots" content="noindex, nofollow" />
      <header className="border-b border-line bg-white shadow-soft">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-3 px-4 sm:px-5 lg:px-10">
          <Link to="/" aria-label="ATA">
            <img src={logo} alt="ATA" className="h-10 w-24 object-contain" />
          </Link>
          <LanguageToggle />
        </div>
      </header>
      {children}
    </div>
  )
}

function DeadEnd({ icon, title, copy }: { icon: IconName; title: string; copy: string }) {
  const { t } = useI18n()
  return (
    <div className="mx-auto flex max-w-md flex-col items-center px-4 py-20 text-center">
      <div className="mb-6 grid size-16 place-items-center rounded-2xl bg-white text-muted shadow-soft">
        <Icon name={icon} className="size-8" />
      </div>
      <h1 className="text-2xl font-bold">{title}</h1>
      <p className="mt-3 leading-7 text-muted">{copy}</p>
      <Link to="/" className="mt-8 rounded-2xl bg-ink px-6 py-3.5 font-bold text-white shadow-button hover:bg-ink-soft">
        {t('share.goHome')}
      </Link>
    </div>
  )
}

function PlaceRow({ place, label, tone }: { place: Place; label: string; tone: 'brand' | 'ink' | 'muted' }) {
  const dot = tone === 'brand' ? 'bg-brand' : tone === 'ink' ? 'bg-ink' : 'bg-muted'
  return (
    <li className="flex items-start gap-3">
      <span className={`mt-1.5 size-3 shrink-0 rounded-full ring-4 ring-white ${dot}`} />
      <div className="min-w-0">
        <p className="text-xs font-bold text-muted">{label}</p>
        <p className="truncate font-bold">{place.name}</p>
        {place.address && <p className="truncate text-xs text-muted">{place.address}</p>}
      </div>
    </li>
  )
}

function DriverPhoto({ url, name }: { url: string | null; name: string }) {
  const [failed, setFailed] = useState(false)
  if (!url || failed) {
    return (
      <div className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-xl font-bold text-brand">
        {name.charAt(0) || <Icon name="user" />}
      </div>
    )
  }
  return <img src={url} alt="" onError={() => setFailed(true)} className="size-14 shrink-0 rounded-2xl object-cover" />
}

function ShareDetails({ data, updatedAt, phase }: { data: PublicTripShare; updatedAt: Date | null; phase: SharePhase }) {
  const { t, lang } = useI18n()
  const copy = phaseCopy[phase]
  const showEta = !isTerminalPhase(phase) && data.etaSeconds !== null && data.etaTarget !== null
  const finishedAt = data.timeline.completedAt ?? data.timeline.cancelledAt

  return (
    <div className="space-y-4">
      <Card>
        <div className="flex items-start gap-4">
          <div className={`grid size-12 shrink-0 place-items-center rounded-2xl ${copy.tone}`}>
            <Icon name={copy.icon} className="size-6" />
          </div>
          <div className="min-w-0 flex-1">
            {data.passengerFirstName && (
              <p className="text-xs font-bold text-muted">{t('share.tripOf', { name: data.passengerFirstName })}</p>
            )}
            <h1 className="text-xl font-bold sm:text-2xl">{t(copy.title)}</h1>
            {showEta && data.etaSeconds !== null && (
              <p className="mt-1 font-bold text-brand">
                {t(data.etaTarget === 'pickup' ? 'share.eta.pickup' : 'share.eta.dropoff', {
                  time: formatEtaSeconds(data.etaSeconds, lang),
                })}
              </p>
            )}
            {isTerminalPhase(phase) && finishedAt && (
              <p className="mt-1 text-sm text-muted">{t('share.endedAt', { time: formatTime(finishedAt, lang) })}</p>
            )}
          </div>
        </div>
        {!isTerminalPhase(phase) && updatedAt && (
          <p className="mt-4 flex items-center gap-2 text-xs font-bold text-muted">
            <span className="relative flex size-2">
              <span className="absolute inline-flex size-full animate-ping rounded-full bg-brand opacity-60" />
              <span className="relative inline-flex size-2 rounded-full bg-brand" />
            </span>
            {t('share.live', { time: formatTime(updatedAt, lang) })}
          </p>
        )}
        {phase === 'completed' && <Notice tone="success" className="mt-4">{t('share.completedNote')}</Notice>}
      </Card>

      {data.driver && (
        <Card>
          <p className="mb-4 text-sm font-bold text-brand">{t('share.driver')}</p>
          <div className="flex items-center gap-4">
            <DriverPhoto url={resolveApiUrl(data.driver.photoUrl)} name={data.driver.firstName} />
            <div className="min-w-0 flex-1">
              <p className="truncate text-lg font-bold">{data.driver.firstName}</p>
              {data.driver.ratingAvg !== null && (
                <p className="mt-0.5 flex items-center gap-1 text-sm font-bold text-muted">
                  <Icon name="star" className="size-4 text-brand" />
                  <span dir="ltr">{formatNumber(data.driver.ratingAvg)}</span>
                </p>
              )}
            </div>
            {data.rideCategory && (
              <span className="rounded-full bg-cloud px-3 py-1.5 text-xs font-bold text-ink">{data.rideCategory.name}</span>
            )}
          </div>
          {data.vehicle && (
            <div className="mt-5 flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-cloud p-4">
              <div className="min-w-0">
                <p className="text-xs font-bold text-muted">{t('share.vehicle')}</p>
                <p className="font-bold">
                  {data.vehicle.make} {data.vehicle.model} · {data.vehicle.color}
                </p>
              </div>
              <span
                dir="ltr"
                className="rounded-xl border-2 border-ink bg-white px-3 py-1.5 text-sm font-bold tracking-wider"
                aria-label={t('share.plate')}
              >
                {data.vehicle.plateNumber}
              </span>
            </div>
          )}
        </Card>
      )}

      <Card>
        <p className="mb-4 text-sm font-bold text-brand">{t('share.route')}</p>
        <ol className="relative space-y-4 before:absolute before:inset-y-2 before:start-[5px] before:w-0.5 before:bg-line">
          <PlaceRow place={data.pickup} label={t('share.pickup')} tone="brand" />
          {data.stops.map((stop, index) => (
            <PlaceRow key={`${stop.lat},${stop.lng},${index}`} place={stop} label={t('share.stop', { n: index + 1 })} tone="muted" />
          ))}
          <PlaceRow place={data.dropoff} label={t('share.dropoff')} tone="ink" />
        </ol>
      </Card>

      <p className="flex items-start gap-2 px-1 text-xs leading-6 text-muted">
        <Icon name="shield" className="mt-0.5 size-4 shrink-0 text-brand" />
        {t('share.privacyNote')}
      </p>
    </div>
  )
}

export function TripShare() {
  const { token = '' } = useParams()
  return <TripShareView key={token} token={token} />
}

function TripShareView({ token }: { token: string }) {
  const { t } = useI18n()
  const { data, failure, loading, updatedAt } = usePublicShare(token)

  if (failure === 'expired') {
    return (
      <Shell>
        <title>{t('share.pageTitle')}</title>
        <DeadEnd icon="clock" title={t('share.expired.title')} copy={t('share.expired.copy')} />
      </Shell>
    )
  }
  if (failure === 'not_found') {
    return (
      <Shell>
        <title>{t('share.pageTitle')}</title>
        <DeadEnd icon="close" title={t('share.notFound.title')} copy={t('share.notFound.copy')} />
      </Shell>
    )
  }

  if (!data) {
    return (
      <Shell>
        <title>{t('share.pageTitle')}</title>
        <div className="mx-auto max-w-md px-4 py-20 text-center">
          {loading ? (
            <p className="font-bold text-muted" role="status">
              {t('state.loading')}
            </p>
          ) : (
            <Notice tone="error">{t(failure === 'rate_limited' ? 'share.rateLimited' : 'error.network')}</Notice>
          )}
        </div>
      </Shell>
    )
  }

  const phase = sharePhase(data.status)
  const driver = data.driverLocation && !isTerminalPhase(phase) ? data.driverLocation : null

  return (
    <Shell>
      <title>{t('share.pageTitle')}</title>
      <div className="mx-auto grid max-w-7xl gap-4 px-4 py-4 sm:px-5 lg:grid-cols-[minmax(0,1fr)_24rem] lg:gap-6 lg:px-10 lg:py-8">
        <div className="space-y-3">
          {failure && (
            <Notice tone="info">{t(failure === 'rate_limited' ? 'share.rateLimited' : 'share.reconnecting')}</Notice>
          )}
          <TrackingMap
            className="h-[55vh] min-h-80 lg:h-[calc(100vh-8rem)]"
            pickup={data.pickup}
            dropoff={data.dropoff}
            stops={data.stops}
            planned={data.route?.planned ?? []}
            travelled={data.route?.travelled ?? []}
            driver={driver}
          />
        </div>
        <ShareDetails data={data} updatedAt={updatedAt} phase={phase} />
      </div>
    </Shell>
  )
}
