import { useEffect, useEffectEvent, useRef, useState, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { escapeHtml, RIYADH, zonePolygonMutedStyle, zonePolygonSelectedStyle } from '../lib/leaflet'
import { L, localizeDraw } from '../lib/leafletDraw'
import { closeRing, formatPolygonText, openRing, parsePolygonText, round6 } from '../lib/pricing'
import type { LatLngTuple } from '../lib/types'
import { Textarea } from './Field'
import { MapView } from './MapView'

export interface PolygonContext {
  id: string
  name: string
  polygon: LatLngTuple[]
}

export interface PolygonEditorProps {
  /** Ring (open or closed); the editor always emits a closed ring. */
  value: LatLngTuple[]
  onChange: (points: LatLngTuple[]) => void
  /** Other zones drawn muted for orientation. */
  context?: PolygonContext[]
  error?: ReactNode
}

/** Reads the outer ring of a drawn polygon as `[lat, lng]` tuples. */
function ringOf(layer: L.Layer): LatLngTuple[] {
  if (!(layer instanceof L.Polyline)) return []
  let ring: unknown = layer.getLatLngs()
  while (Array.isArray(ring) && Array.isArray(ring[0])) ring = ring[0]
  if (!Array.isArray(ring)) return []
  return (ring as L.LatLng[]).map((point) => [round6(point.lat), round6(point.lng)])
}

const serialize = (points: LatLngTuple[]) => JSON.stringify(openRing(points))

/**
 * Polygon editor backed by leaflet-draw (draw / edit vertices / clear) with a JSON textarea fallback.
 * Both inputs write to the same `value`; the map only redraws when the ring actually changed.
 */
export function PolygonEditor({ value, onChange, context = [], error }: PolygonEditorProps) {
  const { t } = useLang()
  const [map, setMap] = useState<L.Map | null>(null)
  const groupRef = useRef<L.FeatureGroup | null>(null)
  /** Serialised ring currently drawn, so prop updates that came from the map itself are ignored. */
  const drawnRef = useRef('')
  /** Serialised ring the textarea was last synced from. */
  const textSyncRef = useRef(serialize(value))
  const [text, setText] = useState(() => formatPolygonText(openRing(value)))
  const [textError, setTextError] = useState<string | null>(null)
  const emit = useEffectEvent((points: LatLngTuple[]) => onChange(points))

  // Draw control + events (recreated when the language changes so the toolbar re-localises).
  useEffect(() => {
    if (!map) return
    localizeDraw(t)
    const group = L.featureGroup().addTo(map)
    groupRef.current = group
    const control = new L.Control.Draw({
      position: 'topleft',
      draw: {
        polyline: false,
        rectangle: false,
        circle: false,
        marker: false,
        circlemarker: false,
        polygon: { allowIntersection: false, showArea: false, shapeOptions: zonePolygonSelectedStyle },
      },
      edit: { featureGroup: group, remove: true },
    })
    map.addControl(control)

    const publish = (ring: LatLngTuple[]) => {
      drawnRef.current = serialize(ring)
      emit(closeRing(ring))
    }
    const onCreated = (event: L.LeafletEvent) => {
      const { layer } = event as L.DrawEvents.Created
      group.clearLayers()
      group.addLayer(layer)
      publish(ringOf(layer))
    }
    const onEdited = () => {
      const [layer] = group.getLayers()
      publish(layer ? ringOf(layer) : [])
    }
    const onDeleted = () => publish([])

    map.on(L.Draw.Event.CREATED, onCreated)
    map.on(L.Draw.Event.EDITED, onEdited)
    map.on(L.Draw.Event.DELETED, onDeleted)
    return () => {
      map.off(L.Draw.Event.CREATED, onCreated)
      map.off(L.Draw.Event.EDITED, onEdited)
      map.off(L.Draw.Event.DELETED, onDeleted)
      map.removeControl(control)
      group.remove()
      groupRef.current = null
      drawnRef.current = ''
    }
  }, [map, t])

  // Context polygons (other zones).
  useEffect(() => {
    if (!map) return
    const layer = L.layerGroup().addTo(map)
    for (const item of context) {
      const ring = openRing(item.polygon)
      if (ring.length < 3) continue
      L.polygon(ring, { ...zonePolygonMutedStyle, interactive: false })
        .bindTooltip(escapeHtml(item.name), { sticky: true })
        .addTo(layer)
    }
    return () => {
      layer.remove()
    }
  }, [map, context])

  // Sync the drawn shape from `value` (initial load, textarea edits, language change).
  useEffect(() => {
    const group = groupRef.current
    if (!map || !group) return
    const ring = openRing(value)
    const key = serialize(ring)
    if (key === drawnRef.current) return
    group.clearLayers()
    drawnRef.current = key
    if (ring.length >= 3) {
      L.polygon(ring, zonePolygonSelectedStyle).addTo(group)
      map.fitBounds(L.latLngBounds(ring), { padding: [24, 24], maxZoom: 15 })
    } else if (drawnRef.current === serialize([]) && context.length > 0) {
      const all = context.flatMap((item) => openRing(item.polygon))
      if (all.length > 0) map.fitBounds(L.latLngBounds(all), { padding: [24, 24], maxZoom: 13 })
    }
  }, [map, value, context, t])

  // Reflect external changes (map edits, form reset) into the textarea without clobbering typing.
  useEffect(() => {
    const key = serialize(value)
    if (key === textSyncRef.current) return
    textSyncRef.current = key
    setText(formatPolygonText(openRing(value)))
    setTextError(null)
  }, [value])

  const onTextChange = (next: string) => {
    setText(next)
    const parsed = parsePolygonText(next)
    if (!parsed) {
      setTextError(t('polygonInvalid'))
      return
    }
    setTextError(null)
    textSyncRef.current = serialize(parsed)
    onChange(closeRing(parsed))
  }

  const count = openRing(value).length

  return (
    <div className="grid gap-4">
      <p className="text-xs text-muted">{t('polygonHint')}</p>
      <div className={`rounded-2xl border-2 ${error ? 'border-danger' : 'border-transparent'}`}>
        <MapView className="h-72 w-full sm:h-80" center={RIYADH} zoom={11} onReady={setMap} onDispose={() => setMap(null)} />
      </div>
      <Textarea
        id="polygon-json"
        dir="ltr"
        label={t('polygonCoordinates')}
        hint={
          <span className="ltr-nums">
            {count} {t('points')}
          </span>
        }
        error={textError ?? error}
        className="min-h-32 font-mono text-xs leading-relaxed"
        value={text}
        onChange={(event) => onTextChange(event.target.value)}
        spellCheck={false}
      />
    </div>
  )
}
