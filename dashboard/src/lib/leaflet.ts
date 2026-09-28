import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'

// Vite rewrites asset URLs, so Leaflet's runtime lookup of its default marker images breaks.
// Point the default icon at the bundled files once for the whole app.
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({ iconRetinaUrl: markerIcon2x, iconUrl: markerIcon, shadowUrl: markerShadow })

export const RIYADH: L.LatLngExpression = [24.7136, 46.6753]
export const DEFAULT_ZOOM = 12

export const OSM_TILES = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
export const OSM_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'

/** Palette tones available to map markers (design-system colours only). */
export type MarkerTone = 'brand' | 'ink' | 'muted' | 'danger' | 'warning'

const toneColor: Record<MarkerTone, string> = {
  brand: 'var(--color-brand)',
  ink: 'var(--color-ink)',
  muted: 'var(--color-muted)',
  danger: 'var(--color-danger)',
  warning: '#b45309',
}

/** Teardrop pin used for pickup / dropoff points. */
export function pinIcon(tone: MarkerTone, label?: string) {
  const size = 34
  return L.divIcon({
    className: 'ata-marker',
    iconSize: [size, size + 6],
    iconAnchor: [size / 2, size + 6],
    popupAnchor: [0, -size],
    html: `<svg width="${size}" height="${size + 6}" viewBox="0 0 34 40" aria-hidden="true">
      <path d="M17 39c0-8 14-14 14-23A14 14 0 0 0 3 16c0 9 14 15 14 23Z" fill="${toneColor[tone]}" stroke="#fff" stroke-width="2"/>
      ${
        label
          ? `<text x="17" y="21" text-anchor="middle" font-size="12" font-weight="700" fill="#fff" font-family="inherit">${label}</text>`
          : '<circle cx="17" cy="16" r="5" fill="#fff"/>'
      }
    </svg>`,
  })
}

/** Small numbered dot for intermediate stops. */
export function stopIcon(index: number) {
  const size = 22
  return L.divIcon({
    className: 'ata-marker',
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
    popupAnchor: [0, -size / 2],
    html: `<svg width="${size}" height="${size}" viewBox="0 0 22 22" aria-hidden="true">
      <circle cx="11" cy="11" r="9.5" fill="var(--color-ink)" stroke="#fff" stroke-width="2"/>
      <text x="11" y="15" text-anchor="middle" font-size="10" font-weight="700" fill="#fff" font-family="inherit">${index}</text>
    </svg>`,
  })
}

/** Round driver dot; `highlighted` draws a larger halo. */
export function driverIcon(tone: MarkerTone, highlighted = false) {
  const size = highlighted ? 30 : 20
  const r = highlighted ? 8 : 6.5
  const c = size / 2
  return L.divIcon({
    className: 'ata-marker',
    iconSize: [size, size],
    iconAnchor: [c, c],
    popupAnchor: [0, -c],
    html: `<svg width="${size}" height="${size}" viewBox="0 0 ${size} ${size}" aria-hidden="true">
      ${highlighted ? `<circle cx="${c}" cy="${c}" r="${c - 1}" fill="${toneColor[tone]}" fill-opacity="0.25"/>` : ''}
      <circle cx="${c}" cy="${c}" r="${r}" fill="${toneColor[tone]}" stroke="#fff" stroke-width="2.5"/>
    </svg>`,
  })
}

export const routeLineStyle: L.PolylineOptions = { color: '#123650', weight: 4, opacity: 0.85, dashArray: '2 8', lineCap: 'round' }
export const routeLineActiveStyle: L.PolylineOptions = { color: '#19b7a5', weight: 4, opacity: 0.95, lineCap: 'round' }

/** Escapes text before it is placed inside marker popups / tooltips (they are raw HTML). */
export function escapeHtml(value: string | null | undefined) {
  return (value ?? '').replace(/[&<>"']/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char] ?? char)
}

export { L }
