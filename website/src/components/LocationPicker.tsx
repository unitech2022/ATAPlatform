import { useEffect, useRef } from 'react'
import { useI18n } from '../i18n'
import { DEFAULT_ZOOM, L, OSM_ATTRIBUTION, OSM_TILES, pinIcon, plannedRouteStyle, RIYADH } from '../lib/leaflet'

export type PickTarget = 'pickup' | 'dropoff'

export interface LatLng {
  lat: number
  lng: number
}

interface LocationPickerProps {
  pickup: LatLng | null
  dropoff: LatLng | null
  target: PickTarget
  onPick: (target: PickTarget, point: LatLng) => void
  className?: string
}

/**
 * Click-to-place map for booking: a click sets the active target (pickup or
 * dropoff); markers can also be dragged. A straight dashed line previews the
 * trip until the quote returns the real distance.
 */
export function LocationPicker({ pickup, dropoff, target, onPick, className = '' }: LocationPickerProps) {
  const { t } = useI18n()
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const markersRef = useRef<{ pickup: L.Marker | null; dropoff: L.Marker | null; line: L.Polyline | null }>({
    pickup: null,
    dropoff: null,
    line: null,
  })
  const targetRef = useRef(target)
  const onPickRef = useRef(onPick)

  useEffect(() => {
    targetRef.current = target
    onPickRef.current = onPick
  })

  useEffect(() => {
    const container = containerRef.current
    if (!container || mapRef.current) return
    const map = L.map(container).setView(RIYADH, DEFAULT_ZOOM)
    L.tileLayer(OSM_TILES, { attribution: OSM_ATTRIBUTION, maxZoom: 19 }).addTo(map)
    map.on('click', (event: L.LeafletMouseEvent) => onPickRef.current(targetRef.current, { lat: event.latlng.lat, lng: event.latlng.lng }))
    markersRef.current.line = L.polyline([], plannedRouteStyle).addTo(map)
    mapRef.current = map
    const observer = new ResizeObserver(() => map.invalidateSize())
    observer.observe(container)
    return () => {
      observer.disconnect()
      map.remove()
      mapRef.current = null
      markersRef.current = { pickup: null, dropoff: null, line: null }
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    if (!map) return
    const markers = markersRef.current
    const place = (key: PickTarget, point: LatLng | null) => {
      const existing = markers[key]
      if (!point) {
        existing?.remove()
        markers[key] = null
        return
      }
      if (existing) {
        existing.setLatLng([point.lat, point.lng])
        return
      }
      const marker = L.marker([point.lat, point.lng], {
        icon: pinIcon(key === 'pickup' ? 'brand' : 'ink'),
        draggable: true,
        title: t(key === 'pickup' ? 'biz.book.pickup' : 'biz.book.dropoff'),
      }).addTo(map)
      marker.on('dragend', () => {
        const position = marker.getLatLng()
        onPickRef.current(key, { lat: position.lat, lng: position.lng })
      })
      markers[key] = marker
    }
    place('pickup', pickup)
    place('dropoff', dropoff)
    markers.line?.setLatLngs(pickup && dropoff ? [[pickup.lat, pickup.lng], [dropoff.lat, dropoff.lng]] : [])
    if (pickup && dropoff) map.fitBounds(L.latLngBounds([[pickup.lat, pickup.lng], [dropoff.lat, dropoff.lng]]), { padding: [50, 50], maxZoom: 15 })
  }, [pickup, dropoff, t])

  return (
    <div className={`relative isolate overflow-hidden rounded-3xl bg-map ${className}`}>
      <div
        ref={containerRef}
        dir="ltr"
        className={`absolute inset-0 ${target ? 'cursor-crosshair' : ''}`}
        role="application"
        aria-label={t('biz.book.mapLabel')}
      />
    </div>
  )
}
