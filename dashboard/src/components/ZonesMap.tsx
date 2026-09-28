import { useEffect, useEffectEvent, useRef, useState } from 'react'
import { escapeHtml, L, RIYADH, tintedPolygonStyle, zonePolygonSelectedStyle, zonePolygonStyle } from '../lib/leaflet'
import { openRing } from '../lib/pricing'
import type { Zone } from '../lib/types'
import { MapView } from './MapView'

export interface ZonesMapProps {
  zones: Zone[]
  selectedId?: string | null
  onSelect?: (zone: Zone) => void
  /** Fill colour per zone (e.g. the demand level colour); falls back to the brand style. */
  colorFor?: (zone: Zone) => string | null | undefined
  /** Tooltip HTML-safe text per zone; defaults to the zone name. */
  labelFor?: (zone: Zone) => string
  className?: string
}

/** All zone polygons on one map; clicking a polygon selects it, the selected one is drawn in ink. */
export function ZonesMap({ zones, selectedId = null, onSelect, colorFor, labelFor, className = 'h-96 w-full' }: ZonesMapProps) {
  const [map, setMap] = useState<L.Map | null>(null)
  const fittedRef = useRef('')
  const select = useEffectEvent((zone: Zone) => onSelect?.(zone))
  const label = useEffectEvent((zone: Zone) => labelFor?.(zone) ?? zone.nameAr)

  useEffect(() => {
    if (!map) return
    const group = L.layerGroup().addTo(map)
    const all: L.LatLngExpression[] = []
    for (const zone of zones) {
      const ring = openRing(zone.polygon)
      if (ring.length < 3) continue
      all.push(...ring)
      const isSelected = zone.id === selectedId
      const color = colorFor?.(zone)
      const style = isSelected ? zonePolygonSelectedStyle : color ? tintedPolygonStyle(color) : zonePolygonStyle
      L.polygon(ring, style)
        .bindTooltip(escapeHtml(label(zone)), { sticky: true })
        .on('click', () => select(zone))
        .addTo(group)
    }
    const key = zones.map((zone) => zone.id).join(',')
    if (all.length > 0 && fittedRef.current !== key) {
      fittedRef.current = key
      map.fitBounds(L.latLngBounds(all), { padding: [24, 24], maxZoom: 13 })
    }
    return () => {
      group.remove()
    }
  }, [map, zones, selectedId, colorFor])

  useEffect(() => {
    if (!map || !selectedId) return
    const zone = zones.find((entry) => entry.id === selectedId)
    const ring = zone ? openRing(zone.polygon) : []
    if (ring.length >= 3) map.flyToBounds(L.latLngBounds(ring), { padding: [32, 32], maxZoom: 14, duration: 0.5 })
  }, [map, selectedId, zones])

  return <MapView className={className} center={RIYADH} zoom={11} onReady={setMap} onDispose={() => setMap(null)} />
}
