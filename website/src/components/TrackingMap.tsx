import { useEffect, useRef } from 'react'
import { useI18n } from '../i18n'
import {
  DEFAULT_ZOOM,
  driverIcon,
  L,
  OSM_ATTRIBUTION,
  OSM_TILES,
  pinIcon,
  plannedRouteStyle,
  RIYADH,
  stopIcon,
  travelledRouteStyle,
} from '../lib/leaflet'
import type { LatLngTuple, Place } from '../lib/types'
import { Action } from './Button'
import { Icon } from './Icon'

export interface TrackingMapProps {
  pickup: Place
  dropoff: Place
  stops: Place[]
  planned: LatLngTuple[]
  travelled: LatLngTuple[]
  driver: { lat: number; lng: number; heading: number | null } | null
  className?: string
}

function boundsOf({ pickup, dropoff, stops, planned, driver }: TrackingMapProps): L.LatLngBounds {
  const points: L.LatLngExpression[] = [
    [pickup.lat, pickup.lng],
    [dropoff.lat, dropoff.lng],
    ...stops.map((stop): L.LatLngTuple => [stop.lat, stop.lng]),
    ...planned,
  ]
  if (driver) points.push([driver.lat, driver.lng])
  return L.latLngBounds(points)
}

interface Layers {
  planned: L.Polyline
  travelled: L.Polyline
  points: L.LayerGroup
  driver: L.Marker | null
}

/**
 * Leaflet + OpenStreetMap map for live trip tracking. The map is created once
 * (strict-mode safe) and its layers are updated in place on every poll, so the
 * viewer's pan/zoom is kept; "recenter" re-fits everything.
 */
export function TrackingMap(props: TrackingMapProps) {
  const { pickup, dropoff, stops, planned, travelled, driver, className = '' } = props
  const { t } = useI18n()
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const layersRef = useRef<Layers | null>(null)
  const fittedRef = useRef(false)

  useEffect(() => {
    const container = containerRef.current
    if (!container || mapRef.current) return
    const map = L.map(container, { zoomControl: true, attributionControl: true }).setView(RIYADH, DEFAULT_ZOOM)
    L.tileLayer(OSM_TILES, { attribution: OSM_ATTRIBUTION, maxZoom: 19 }).addTo(map)
    layersRef.current = {
      planned: L.polyline([], plannedRouteStyle).addTo(map),
      travelled: L.polyline([], travelledRouteStyle).addTo(map),
      points: L.layerGroup().addTo(map),
      driver: null,
    }
    mapRef.current = map
    const observer = new ResizeObserver(() => map.invalidateSize())
    observer.observe(container)
    return () => {
      observer.disconnect()
      map.remove()
      mapRef.current = null
      layersRef.current = null
      fittedRef.current = false
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    const layers = layersRef.current
    if (!map || !layers) return

    layers.planned.setLatLngs(planned)
    layers.travelled.setLatLngs(travelled)

    layers.points.clearLayers()
    L.marker([pickup.lat, pickup.lng], { icon: pinIcon('brand'), title: pickup.name, keyboard: false }).addTo(layers.points)
    stops.forEach((stop, index) =>
      L.marker([stop.lat, stop.lng], { icon: stopIcon(index + 1), title: stop.name, keyboard: false }).addTo(layers.points),
    )
    L.marker([dropoff.lat, dropoff.lng], { icon: pinIcon('ink'), title: dropoff.name, keyboard: false }).addTo(layers.points)

    if (driver) {
      if (layers.driver) {
        layers.driver.setLatLng([driver.lat, driver.lng])
        layers.driver.setIcon(driverIcon(driver.heading))
      } else {
        layers.driver = L.marker([driver.lat, driver.lng], {
          icon: driverIcon(driver.heading),
          zIndexOffset: 1000,
          keyboard: false,
        }).addTo(map)
      }
    } else if (layers.driver) {
      layers.driver.remove()
      layers.driver = null
    }

    if (!fittedRef.current) {
      fittedRef.current = true
      map.fitBounds(boundsOf({ pickup, dropoff, stops, planned, travelled, driver }), { padding: [40, 40], maxZoom: 16 })
    }
  }, [pickup, dropoff, stops, planned, travelled, driver])

  return (
    <div className={`relative isolate overflow-hidden rounded-3xl bg-map ${className}`}>
      <div ref={containerRef} dir="ltr" className="absolute inset-0" role="region" aria-label={t('share.mapLabel')} />
      <Action
        onClick={() => mapRef.current?.fitBounds(boundsOf(props), { padding: [40, 40], maxZoom: 16 })}
        className="absolute bottom-4 end-4 z-[1000] flex items-center gap-2 rounded-full bg-white px-4 py-2.5 text-sm font-bold text-ink shadow-float hover:bg-cloud"
      >
        <Icon name="location" className="size-4" />
        {t('share.recenter')}
      </Action>
    </div>
  )
}
