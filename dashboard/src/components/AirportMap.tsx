import { useEffect, useEffectEvent, useState } from 'react'
import { useLang } from '../context/lang'
import { escapeHtml, L, pinIcon, tintedPolygonStyle, zonePolygonSelectedStyle, zonePolygonStyle } from '../lib/leaflet'
import { openRing } from '../lib/pricing'
import { AIRPORT_ZONE_KIND_KEY } from '../lib/airports'
import type { Airport, AirportZone } from '../lib/types'
import { MapView } from './MapView'

export interface AirportMapProps {
  /** Airport geofences (brand polygons). */
  airports?: Airport[]
  /** Highlighted airport (drawn in ink). */
  selectedAirportId?: string | null
  onSelectAirport?: (airport: Airport) => void
  /** Terminals and pickup zones render as pins, driver waiting areas as amber polygons. */
  zones?: AirportZone[]
  selectedZoneId?: string | null
  onSelectZone?: (zone: AirportZone) => void
  className?: string
}

const WAITING_COLOR = '#b45309'

/** Airport geofences plus their terminals (ink pins), pickup zones (brand pins) and driver waiting areas (amber polygons). */
export function AirportMap({ airports = [], selectedAirportId = null, onSelectAirport, zones = [], selectedZoneId = null, onSelectZone, className = 'h-80 w-full' }: AirportMapProps) {
  const { t, lang } = useLang()
  const [map, setMap] = useState<L.Map | null>(null)
  const selectAirport = useEffectEvent((airport: Airport) => onSelectAirport?.(airport))
  const selectZone = useEffectEvent((zone: AirportZone) => onSelectZone?.(zone))

  useEffect(() => {
    if (!map) return
    const group = L.layerGroup().addTo(map)
    const all: L.LatLngExpression[] = []
    for (const airport of airports) {
      const ring = openRing(airport.geofence ?? [])
      if (ring.length < 3) continue
      all.push(...ring)
      const name = (lang === 'ar' ? airport.nameAr : airport.nameEn) || airport.nameEn || airport.nameAr
      L.polygon(ring, airport.id === selectedAirportId ? zonePolygonSelectedStyle : zonePolygonStyle)
        .bindTooltip(`${escapeHtml(airport.code)} · ${escapeHtml(name)}`, { sticky: true })
        .on('click', () => selectAirport(airport))
        .addTo(group)
    }
    for (const zone of zones) {
      const name = (lang === 'ar' ? zone.nameAr : zone.nameEn) || zone.nameEn || zone.nameAr
      const tooltip = `${escapeHtml(t(AIRPORT_ZONE_KIND_KEY[zone.kind]))} · ${escapeHtml(name)}`
      if (zone.kind === 'driver_waiting_area') {
        const ring = openRing(zone.polygon ?? [])
        if (ring.length < 3) continue
        all.push(...ring)
        L.polygon(ring, zone.id === selectedZoneId ? zonePolygonSelectedStyle : tintedPolygonStyle(WAITING_COLOR))
          .bindTooltip(tooltip, { sticky: true })
          .on('click', () => selectZone(zone))
          .addTo(group)
        continue
      }
      if (!Number.isFinite(zone.lat) || !Number.isFinite(zone.lng)) continue
      all.push([zone.lat, zone.lng])
      const label = zone.kind === 'terminal' ? escapeHtml((zone.terminalCode ?? '').slice(0, 3)) : undefined
      L.marker([zone.lat, zone.lng], { icon: pinIcon(zone.kind === 'terminal' ? 'ink' : zone.id === selectedZoneId ? 'danger' : 'brand', label) })
        .bindTooltip(tooltip)
        .on('click', () => selectZone(zone))
        .addTo(group)
    }
    if (all.length > 0) map.fitBounds(L.latLngBounds(all), { padding: [24, 24], maxZoom: 15 })
    return () => {
      group.remove()
    }
  }, [map, airports, selectedAirportId, zones, selectedZoneId, lang, t])

  return <MapView className={className} onReady={setMap} onDispose={() => setMap(null)} />
}
