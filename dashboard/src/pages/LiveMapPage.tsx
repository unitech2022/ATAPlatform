import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { Badge, TripStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { MapView, type MapViewHandle } from '../components/MapView'
import { Spinner } from '../components/Spinner'
import { useLang } from '../context/lang'
import { useLiveSnapshot, type LiveSource } from '../hooks/useLiveSnapshot'
import type { TranslationKey } from '../i18n'
import { formatNumber, formatTime } from '../lib/format'
import { driverIcon, escapeHtml, L, pinIcon, routeLineActiveStyle, routeLineStyle, type MarkerTone } from '../lib/leaflet'
import type { LiveDriver, LiveSnapshot, LiveTrip } from '../lib/types'

type Selection = { kind: 'trip'; id: string } | { kind: 'driver'; id: string } | null

const SOURCE_LABEL: Record<LiveSource, TranslationKey> = {
  hub: 'liveViaHub',
  polling: 'liveViaPolling',
  connecting: 'liveConnecting',
}

function driverTone(driver: LiveDriver): MarkerTone {
  if (!driver.isOnline) return 'muted'
  return driver.status === 'on_trip' ? 'ink' : 'brand'
}

function tripLatLng(trip: LiveTrip): L.LatLngExpression {
  return [trip.pickup.lat, trip.pickup.lng]
}

export function LiveMapPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const { snapshot, error, source, updatedAt, refresh } = useLiveSnapshot()
  const [map, setMap] = useState<L.Map | null>(null)
  const mapHandle = useRef<MapViewHandle>(null)
  const [selected, setSelected] = useState<Selection>(null)

  useEffect(() => {
    if (!map || !snapshot) return
    const group = L.layerGroup().addTo(map)
    drawSnapshot(group, snapshot, selected, t)
    return () => {
      group.remove()
    }
  }, [map, snapshot, selected, t])

  const focus = (target: Selection, at: L.LatLngExpression) => {
    setSelected(target)
    const live = mapHandle.current?.getMap()
    if (live) live.flyTo(at, Math.max(live.getZoom(), 14), { duration: 0.6 })
  }

  const drivers = snapshot?.drivers ?? []
  const counts = {
    idle: drivers.filter((driver) => driver.isOnline && driver.status !== 'on_trip').length,
    onTrip: drivers.filter((driver) => driver.isOnline && driver.status === 'on_trip').length,
    offline: drivers.filter((driver) => !driver.isOnline).length,
  }

  return (
    <div className="flex h-[calc(100vh-11rem)] min-h-[36rem] flex-col gap-4 lg:grid lg:grid-cols-[minmax(0,1fr)_22rem]">
      <section className="relative min-h-72 flex-1 overflow-hidden rounded-3xl bg-white p-2 shadow-soft">
        <MapView ref={mapHandle} className="h-full w-full" onReady={setMap} onDispose={() => setMap(null)} />
        <div className="pointer-events-none absolute inset-x-4 top-4 z-[500] flex flex-wrap items-center justify-between gap-2">
          <div className="pointer-events-auto flex items-center gap-2 rounded-full bg-white/95 px-3 py-1.5 text-xs font-bold shadow-soft backdrop-blur">
            <span className={`size-2 rounded-full ${source === 'hub' ? 'bg-brand' : source === 'polling' ? 'bg-ink' : 'bg-muted'}`} />
            {t(SOURCE_LABEL[source])}
            {updatedAt !== null && (
              <span className="ltr-nums font-normal text-muted">
                · {t('lastUpdated')} {formatTime(updatedAt, lang)}
              </span>
            )}
          </div>
          <Legend />
        </div>
      </section>

      <aside className="flex min-h-0 flex-col overflow-hidden rounded-3xl bg-white shadow-soft">
        <header className="flex items-start justify-between gap-3 border-b border-line px-5 py-4">
          <div className="min-w-0">
            <h2 className="text-lg font-bold">{t('liveMapTitle')}</h2>
            <p className="mt-0.5 text-xs text-muted">{t('liveMapCopy')}</p>
          </div>
          <Button variant="secondary" size="sm" icon="refresh" onClick={refresh} aria-label={t('refresh')} title={t('refresh')} />
        </header>

        <div className="grid grid-cols-3 gap-2 border-b border-line px-5 py-4">
          <MiniStat label={t('driversIdle')} value={counts.idle} dot="bg-brand" />
          <MiniStat label={t('driversOnTrip')} value={counts.onTrip} dot="bg-ink" />
          <MiniStat label={t('driversOffline')} value={counts.offline} dot="bg-muted" />
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-4">
          {error && !snapshot ? (
            <ErrorState error={error} onRetry={refresh} />
          ) : !snapshot ? (
            <div className="grid min-h-40 place-items-center text-brand">
              <Spinner className="size-7" />
            </div>
          ) : (
            <>
              <TripList
                title={t('searchingTrips')}
                tone="warning"
                trips={snapshot.searchingTrips}
                empty={t('noSearchingTrips')}
                selectedId={selected?.kind === 'trip' ? selected.id : null}
                onSelect={(trip) => focus({ kind: 'trip', id: trip.id }, tripLatLng(trip))}
                onOpen={(trip) => navigate(`/trips/${trip.id}`)}
              />
              <TripList
                title={t('activeTrips')}
                tone="brand"
                trips={snapshot.activeTrips}
                empty={t('noActiveTrips')}
                selectedId={selected?.kind === 'trip' ? selected.id : null}
                onSelect={(trip) => focus({ kind: 'trip', id: trip.id }, tripLatLng(trip))}
                onOpen={(trip) => navigate(`/trips/${trip.id}`)}
              />
              <section>
                <h3 className="mb-2 flex items-center justify-between text-sm font-bold">
                  {t('driversOnline')}
                  <Badge tone="brand">{formatNumber(counts.idle + counts.onTrip)}</Badge>
                </h3>
                <ul className="space-y-1.5">
                  {drivers
                    .filter((driver) => driver.isOnline)
                    .map((driver) => {
                      const active = selected?.kind === 'driver' && selected.id === driver.driverId
                      return (
                        <li key={driver.driverId}>
                          <button
                            type="button"
                            onClick={() => focus({ kind: 'driver', id: driver.driverId }, [driver.lat, driver.lng])}
                            className={`flex w-full items-center gap-3 rounded-2xl border px-3 py-2 text-start text-sm transition ${
                              active ? 'border-brand bg-brand-soft/40' : 'border-line hover:bg-cloud'
                            }`}
                          >
                            <span className={`size-2.5 shrink-0 rounded-full ${driver.status === 'on_trip' ? 'bg-ink' : 'bg-brand'}`} />
                            <span className="min-w-0 flex-1 truncate font-bold">{driver.name || t('unnamed')}</span>
                            {driver.categoryCode && <span className="ltr-nums text-xs text-muted">{driver.categoryCode}</span>}
                          </button>
                        </li>
                      )
                    })}
                </ul>
              </section>
            </>
          )}
        </div>
      </aside>
    </div>
  )
}

function drawSnapshot(group: L.LayerGroup, snapshot: LiveSnapshot, selected: Selection, t: (key: TranslationKey) => string) {
  for (const trip of [...snapshot.activeTrips, ...snapshot.searchingTrips]) {
    const isSelected = selected?.kind === 'trip' && selected.id === trip.id
    const searching = trip.status === 'searching' || trip.status === 'requested'
    const pickup: L.LatLngExpression = [trip.pickup.lat, trip.pickup.lng]
    const dropoff: L.LatLngExpression = [trip.dropoff.lat, trip.dropoff.lng]
    const label = `<strong class="ltr-nums">${escapeHtml(trip.tripNumber ?? trip.id)}</strong>`
    if (!searching || isSelected) {
      L.polyline([pickup, dropoff], isSelected ? routeLineActiveStyle : routeLineStyle).addTo(group)
      L.marker(dropoff, { icon: pinIcon('ink') })
        .bindPopup(`${label}<br>${escapeHtml(t('dropoff'))}: ${escapeHtml(trip.dropoff.name ?? trip.dropoff.address)}`)
        .addTo(group)
    }
    const pickupMarker = L.marker(pickup, { icon: pinIcon(searching ? 'warning' : 'brand'), zIndexOffset: isSelected ? 1000 : 0 })
      .bindPopup(`${label}<br>${escapeHtml(t('pickup'))}: ${escapeHtml(trip.pickup.name ?? trip.pickup.address)}`)
      .addTo(group)
    if (isSelected) pickupMarker.openPopup()
  }

  for (const driver of snapshot.drivers) {
    const isSelected = selected?.kind === 'driver' && selected.id === driver.driverId
    const marker = L.marker([driver.lat, driver.lng], { icon: driverIcon(driverTone(driver), isSelected), zIndexOffset: isSelected ? 1000 : 0 })
      .bindTooltip(escapeHtml(driver.name || t('unnamed')), { direction: 'top', offset: [0, -8] })
      .addTo(group)
    if (isSelected) marker.openTooltip()
  }
}

function MiniStat({ label, value, dot }: { label: string; value: number; dot: string }) {
  return (
    <div className="rounded-2xl bg-cloud px-3 py-2">
      <p className="flex items-center gap-1.5 text-[11px] font-bold text-muted">
        <span className={`size-2 rounded-full ${dot}`} />
        {label}
      </p>
      <p className="ltr-nums mt-0.5 text-lg font-bold leading-tight">{formatNumber(value)}</p>
    </div>
  )
}

function Legend() {
  const { t } = useLang()
  return (
    <div className="pointer-events-auto flex items-center gap-3 rounded-full bg-white/95 px-3 py-1.5 text-[11px] font-bold text-muted shadow-soft backdrop-blur">
      <span className="flex items-center gap-1">
        <span className="size-2.5 rounded-full bg-brand" /> {t('driversIdle')}
      </span>
      <span className="flex items-center gap-1">
        <span className="size-2.5 rounded-full bg-ink" /> {t('driversOnTrip')}
      </span>
      <span className="flex items-center gap-1">
        <span className="size-2.5 rounded-full bg-muted" /> {t('driversOffline')}
      </span>
    </div>
  )
}

interface TripListProps {
  title: string
  tone: 'warning' | 'brand'
  trips: LiveTrip[]
  empty: string
  selectedId: string | null
  onSelect: (trip: LiveTrip) => void
  onOpen: (trip: LiveTrip) => void
}

function TripList({ title, tone, trips, empty, selectedId, onSelect, onOpen }: TripListProps) {
  const { t } = useLang()
  return (
    <section className="mb-5">
      <h3 className="mb-2 flex items-center justify-between text-sm font-bold">
        {title}
        <Badge tone={tone}>{formatNumber(trips.length)}</Badge>
      </h3>
      {trips.length === 0 ? (
        <p className="rounded-2xl bg-cloud px-3 py-3 text-xs text-muted">{empty}</p>
      ) : (
        <ul className="space-y-1.5">
          {trips.map((trip) => {
            const active = selectedId === trip.id
            return (
              <li key={trip.id}>
                <div
                  className={`flex items-center gap-2 rounded-2xl border px-3 py-2 text-sm transition ${
                    active ? 'border-brand bg-brand-soft/40' : 'border-line hover:bg-cloud'
                  }`}
                >
                  <button type="button" onClick={() => onSelect(trip)} className="min-w-0 flex-1 text-start">
                    <span className="ltr-nums block truncate font-bold">{trip.tripNumber ?? trip.id}</span>
                    <span className="block truncate text-xs text-muted">
                      {trip.pickup.name || trip.pickup.address || '—'} ← {trip.dropoff.name || trip.dropoff.address || '—'}
                    </span>
                    <TripStatusBadge status={trip.status} className="mt-1" />
                  </button>
                  <button
                    type="button"
                    onClick={() => onOpen(trip)}
                    aria-label={t('viewTrip')}
                    title={t('viewTrip')}
                    className="grid size-9 shrink-0 place-items-center rounded-full bg-cloud text-ink transition hover:bg-brand-soft hover:text-brand"
                  >
                    <Icon name="chevron" className="size-4 rtl:rotate-180" />
                  </button>
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
