import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'

// Vite rewrites asset URLs, so Leaflet's runtime lookup of its default marker
// images (derived from the CSS path) breaks. Point the default icon at the
// bundled files once for the whole app.
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({ iconRetinaUrl: markerIcon2x, iconUrl: markerIcon, shadowUrl: markerShadow })

export const RIYADH: L.LatLngTuple = [24.7136, 46.6753]
export const DEFAULT_ZOOM = 12

export const OSM_TILES = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
export const OSM_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'

const BRAND = '#19b7a5'
const INK = '#123650'

/** Teardrop pin for pickup (brand) / dropoff (ink) points. */
export function pinIcon(tone: 'brand' | 'ink', label?: string): L.DivIcon {
  const color = tone === 'brand' ? BRAND : INK
  return L.divIcon({
    className: 'ata-marker',
    iconSize: [34, 40],
    iconAnchor: [17, 40],
    html: `<svg width="34" height="40" viewBox="0 0 34 40" aria-hidden="true">
      <path d="M17 39c0-8 14-14 14-23A14 14 0 0 0 3 16c0 9 14 15 14 23Z" fill="${color}" stroke="#fff" stroke-width="2"/>
      ${
        label
          ? `<text x="17" y="21" text-anchor="middle" font-size="12" font-weight="700" fill="#fff" font-family="inherit">${label}</text>`
          : '<circle cx="17" cy="16" r="5" fill="#fff"/>'
      }
    </svg>`,
  })
}

/** Small numbered dot for intermediate stops. */
export function stopIcon(index: number): L.DivIcon {
  return L.divIcon({
    className: 'ata-marker',
    iconSize: [22, 22],
    iconAnchor: [11, 11],
    html: `<svg width="22" height="22" viewBox="0 0 22 22" aria-hidden="true">
      <circle cx="11" cy="11" r="9.5" fill="${INK}" stroke="#fff" stroke-width="2"/>
      <text x="11" y="15" text-anchor="middle" font-size="10" font-weight="700" fill="#fff" font-family="inherit">${index}</text>
    </svg>`,
  })
}

/** Driver marker: a brand disc with an arrow rotated to the heading (degrees, 0 = north). */
export function driverIcon(heading: number | null): L.DivIcon {
  const rotation = typeof heading === 'number' && Number.isFinite(heading) ? heading : 0
  const arrow =
    heading === null
      ? `<circle cx="22" cy="22" r="5" fill="#fff"/>`
      : `<path d="M22 11 29 29 22 25 15 29Z" fill="#fff" transform="rotate(${rotation} 22 22)"/>`
  return L.divIcon({
    className: 'ata-marker ata-driver-marker',
    iconSize: [44, 44],
    iconAnchor: [22, 22],
    html: `<svg width="44" height="44" viewBox="0 0 44 44" aria-hidden="true">
      <circle cx="22" cy="22" r="21" fill="${BRAND}" fill-opacity="0.2"/>
      <circle cx="22" cy="22" r="15" fill="${BRAND}" stroke="#fff" stroke-width="3"/>
      ${arrow}
    </svg>`,
  })
}

/** Planned route: dashed brand line (design system §map). */
export const plannedRouteStyle: L.PolylineOptions = {
  color: BRAND,
  weight: 5,
  opacity: 0.9,
  dashArray: '2 10',
  lineCap: 'round',
}

/** Travelled route: solid ink line. */
export const travelledRouteStyle: L.PolylineOptions = { color: INK, weight: 5, opacity: 0.85, lineCap: 'round' }

export { L }
