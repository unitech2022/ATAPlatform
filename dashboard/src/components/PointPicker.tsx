import { useEffect, useEffectEvent, useRef, useState } from 'react'
import { isValidLatLng, round6 } from '../lib/pricing'
import { L, pinIcon } from '../lib/leaflet'
import { MapView } from './MapView'

/** Small map where a click (or dragging the pin) picks one `lat/lng`; `value` null/invalid shows no pin. */
export function PointPicker({ lat, lng, onChange, context, className = 'h-56 w-full' }: { lat: number; lng: number; onChange: (lat: number, lng: number) => void; context?: [number, number][][]; className?: string }) {
  const [map, setMap] = useState<L.Map | null>(null)
  const markerRef = useRef<L.Marker | null>(null)
  const emit = useEffectEvent((nextLat: number, nextLng: number) => onChange(round6(nextLat), round6(nextLng)))
  const fittedRef = useRef(false)
  /** The point the picker opened with; only it decides whether the first fit uses the context outline. */
  const initialPoint = useRef({ lat, lng })

  // Context outlines (e.g. the airport geofence) for orientation; the first fit prefers them when no point exists yet.
  useEffect(() => {
    if (!map || !context || context.length === 0) return
    const layer = L.layerGroup().addTo(map)
    const all: L.LatLngExpression[] = []
    for (const ring of context) {
      if (ring.length < 3) continue
      all.push(...ring)
      L.polygon(ring, { color: '#6b7f8e', weight: 1.5, fillColor: '#6b7f8e', fillOpacity: 0.08, dashArray: '4 6', interactive: false }).addTo(layer)
    }
    if (all.length > 0 && !fittedRef.current && !isValidLatLng(initialPoint.current.lat, initialPoint.current.lng)) {
      fittedRef.current = true
      map.fitBounds(L.latLngBounds(all), { padding: [24, 24], maxZoom: 15, animate: false })
    }
    return () => {
      layer.remove()
    }
  }, [map, context])

  useEffect(() => {
    if (!map) return
    const onClick = (event: L.LeafletMouseEvent) => emit(event.latlng.lat, event.latlng.lng)
    map.on('click', onClick)
    return () => {
      map.off('click', onClick)
    }
  }, [map])

  useEffect(() => {
    if (!map) return
    if (!isValidLatLng(lat, lng)) {
      markerRef.current?.remove()
      markerRef.current = null
      return
    }
    if (markerRef.current) {
      markerRef.current.setLatLng([lat, lng])
    } else {
      const marker = L.marker([lat, lng], { icon: pinIcon('brand'), draggable: true }).addTo(map)
      marker.on('dragend', () => {
        const position = marker.getLatLng()
        emit(position.lat, position.lng)
      })
      markerRef.current = marker
      map.setView([lat, lng], Math.max(map.getZoom(), 14), { animate: false })
    }
  }, [map, lat, lng])

  // A new map (strict-mode remount) must not reuse a marker that belonged to the destroyed one.
  useEffect(
    () => () => {
      markerRef.current?.remove()
      markerRef.current = null
    },
    [map],
  )

  return <MapView className={className} onReady={setMap} onDispose={() => setMap(null)} />
}
