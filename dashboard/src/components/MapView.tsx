import { useEffect, useEffectEvent, useImperativeHandle, useRef, type Ref } from 'react'
import { DEFAULT_ZOOM, L, OSM_ATTRIBUTION, OSM_TILES, RIYADH } from '../lib/leaflet'

export interface MapViewHandle {
  /** The live Leaflet map, or null before mount / after unmount. */
  getMap: () => L.Map | null
}

export interface MapViewProps {
  ref?: Ref<MapViewHandle>
  className?: string
  center?: L.LatLngExpression
  zoom?: number
  /** Called once the map exists; add layers in an effect keyed on the map you receive. */
  onReady?: (map: L.Map) => void
  /** Called right before the map is destroyed so parents can drop their reference. */
  onDispose?: () => void
}

/**
 * Imperative Leaflet wrapper. The map is created in an effect and destroyed in its cleanup,
 * so React strict mode's mount → unmount → mount cycle never leaves a second map on the same
 * container. Rendering the container `ltr` avoids Leaflet's tile offset bugs under RTL.
 */
export function MapView({ ref, className = 'h-80 w-full', center = RIYADH, zoom = DEFAULT_ZOOM, onReady, onDispose }: MapViewProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const ready = useEffectEvent((map: L.Map) => onReady?.(map))
  const dispose = useEffectEvent(() => onDispose?.())
  const initialView = useRef({ center, zoom })

  useImperativeHandle(ref, () => ({ getMap: () => mapRef.current }), [])

  useEffect(() => {
    const container = containerRef.current
    if (!container || mapRef.current) return
    const map = L.map(container, { zoomControl: true, attributionControl: true })
    map.setView(initialView.current.center, initialView.current.zoom)
    L.tileLayer(OSM_TILES, { attribution: OSM_ATTRIBUTION, maxZoom: 19 }).addTo(map)
    mapRef.current = map
    ready(map)

    // Leaflet measures the container once; keep it in sync with layout changes.
    const observer = new ResizeObserver(() => map.invalidateSize())
    observer.observe(container)

    return () => {
      observer.disconnect()
      dispose()
      map.remove()
      mapRef.current = null
    }
  }, [])

  return <div ref={containerRef} dir="ltr" className={`overflow-hidden rounded-2xl bg-map ${className}`} />
}
