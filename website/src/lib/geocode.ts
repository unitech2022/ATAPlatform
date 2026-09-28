import type { Lang } from '../i18n'

/**
 * Place search for the corporate booking map. Defaults to the public
 * OpenStreetMap Nominatim service (used on explicit search/click only, well
 * under its 1 req/s policy); set `VITE_GEOCODER_URL=""` to disable it.
 */
const GEOCODER_URL = (import.meta.env.VITE_GEOCODER_URL ?? 'https://nominatim.openstreetmap.org').replace(/\/+$/, '')

export const geocoderEnabled = GEOCODER_URL !== ''

export interface GeocodeResult {
  name: string
  address: string
  lat: number
  lng: number
}

interface NominatimPlace {
  lat: string
  lon: string
  display_name: string
  name?: string
}

function toResult(place: NominatimPlace): GeocodeResult {
  const parts = place.display_name.split(',').map((part) => part.trim())
  return {
    name: place.name || parts[0] || place.display_name,
    address: parts.slice(0, 4).join('، '),
    lat: Number(place.lat),
    lng: Number(place.lon),
  }
}

function isPlace(value: unknown): value is NominatimPlace {
  return typeof value === 'object' && value !== null && 'lat' in value && 'lon' in value && 'display_name' in value
}

export async function searchPlaces(query: string, lang: Lang): Promise<GeocodeResult[]> {
  if (!geocoderEnabled || !query.trim()) return []
  const params = new URLSearchParams({ format: 'jsonv2', q: query, countrycodes: 'sa', limit: '6', 'accept-language': lang })
  const response = await fetch(`${GEOCODER_URL}/search?${params.toString()}`, { headers: { Accept: 'application/json' } })
  if (!response.ok) throw new Error(`geocoder_${response.status}`)
  const data: unknown = await response.json()
  return Array.isArray(data) ? data.filter(isPlace).map(toResult) : []
}

export async function reverseGeocode(lat: number, lng: number, lang: Lang): Promise<GeocodeResult | null> {
  if (!geocoderEnabled) return null
  const params = new URLSearchParams({ format: 'jsonv2', lat: String(lat), lon: String(lng), zoom: '17', 'accept-language': lang })
  try {
    const response = await fetch(`${GEOCODER_URL}/reverse?${params.toString()}`, { headers: { Accept: 'application/json' } })
    if (!response.ok) return null
    const data: unknown = await response.json()
    return isPlace(data) ? { ...toResult(data), lat, lng } : null
  } catch {
    return null
  }
}
