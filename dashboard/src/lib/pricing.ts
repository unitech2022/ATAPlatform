import type { Lang, TranslationKey } from '../i18n'
import type { DemandSource, LatLngTuple, MatchingOutcome, MatchingWeights, OperatingHour } from './types'

/** Picks the Arabic or English name of a bilingual record. */
export function localName(item: { nameAr: string; nameEn: string } | null | undefined, lang: Lang) {
  if (!item) return '—'
  return (lang === 'ar' ? item.nameAr : item.nameEn) || item.nameEn || item.nameAr || '—'
}

/** Parses a numeric form field; null when empty or not a finite number. */
export function numberOrNull(value: string): number | null {
  const trimmed = value.trim()
  if (trimmed === '') return null
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? parsed : null
}

/** Soft tint of an API colour for card backgrounds (falls back gracefully for named colours). */
export function tint(color: string, percent = 14) {
  return `color-mix(in srgb, ${color} ${percent}%, white)`
}

/** 0 = Sunday … 6 = Saturday, in the order the region's week starts (Sunday first). */
export const WEEKDAYS: { day: number; key: TranslationKey }[] = [
  { day: 0, key: 'daySun' },
  { day: 1, key: 'dayMon' },
  { day: 2, key: 'dayTue' },
  { day: 3, key: 'dayWed' },
  { day: 4, key: 'dayThu' },
  { day: 5, key: 'dayFri' },
  { day: 6, key: 'daySat' },
]

export function weekdayKey(day: number | null | undefined): TranslationKey {
  return WEEKDAYS.find((entry) => entry.day === day)?.key ?? 'everyDay'
}

export const DEFAULT_OPERATING_HOURS: OperatingHour[] = WEEKDAYS.map(({ day }) => ({ day, from: '00:00', to: '23:59' }))

const TIME = /^([01]\d|2[0-3]):[0-5]\d$/

export function isValidTime(value: string) {
  return TIME.test(value)
}

/** Parses the textarea fallback: JSON `[[lat,lng],…]` (or `[{lat,lng},…]`). Returns null when invalid. */
export function parsePolygonText(text: string): LatLngTuple[] | null {
  let parsed: unknown
  try {
    parsed = JSON.parse(text)
  } catch {
    return null
  }
  if (!Array.isArray(parsed)) return null
  const points: LatLngTuple[] = []
  for (const entry of parsed) {
    let lat: unknown
    let lng: unknown
    if (Array.isArray(entry) && entry.length >= 2) {
      ;[lat, lng] = entry
    } else if (entry && typeof entry === 'object') {
      const record = entry as Record<string, unknown>
      lat = record.lat
      lng = record.lng
    } else {
      return null
    }
    if (typeof lat !== 'number' || typeof lng !== 'number' || !isValidLatLng(lat, lng)) return null
    points.push([lat, lng])
  }
  return points
}

export function isValidLatLng(lat: number, lng: number) {
  return Number.isFinite(lat) && Number.isFinite(lng) && lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180
}

/** True when the ring has at least three distinct vertices (closing point optional). */
export function isUsablePolygon(points: LatLngTuple[]) {
  return openRing(points).length >= 3
}

/** Drops a trailing vertex equal to the first one so editors work on an open ring. */
export function openRing(points: LatLngTuple[]): LatLngTuple[] {
  if (points.length > 1) {
    const [firstLat, firstLng] = points[0]
    const [lastLat, lastLng] = points[points.length - 1]
    if (firstLat === lastLat && firstLng === lastLng) return points.slice(0, -1)
  }
  return points
}

/** Returns the ring closed (first point repeated at the end), the shape stored in `zones.polygon`. */
export function closeRing(points: LatLngTuple[]): LatLngTuple[] {
  const open = openRing(points)
  return open.length >= 3 ? [...open, open[0]] : open
}

/** Arithmetic centre of the ring — good enough for the map label and the `centerLat/centerLng` columns. */
export function polygonCenter(points: LatLngTuple[]): LatLngTuple | null {
  const open = openRing(points)
  if (open.length === 0) return null
  const sum = open.reduce<[number, number]>((acc, [lat, lng]) => [acc[0] + lat, acc[1] + lng], [0, 0])
  return [round6(sum[0] / open.length), round6(sum[1] / open.length)]
}

export function round6(value: number) {
  return Math.round(value * 1e6) / 1e6
}

export function formatPolygonText(points: LatLngTuple[]) {
  return `[\n${points.map(([lat, lng]) => `  [${round6(lat)}, ${round6(lng)}]`).join(',\n')}\n]`
}

export const WEIGHT_KEYS: { key: keyof MatchingWeights; label: TranslationKey }[] = [
  { key: 'distance', label: 'weightDistance' },
  { key: 'eta', label: 'weightEta' },
  { key: 'rating', label: 'weightRating' },
  { key: 'acceptance', label: 'weightAcceptance' },
  { key: 'cancellation', label: 'weightCancellation' },
  { key: 'tier', label: 'weightTier' },
  { key: 'favorite', label: 'weightFavorite' },
]

export const DEFAULT_WEIGHTS: MatchingWeights = {
  distance: 0.35,
  eta: 0.2,
  rating: 0.15,
  acceptance: 0.1,
  cancellation: 0.1,
  tier: 0.05,
  favorite: 0.05,
}

export function weightsTotal(weights: Record<keyof MatchingWeights, number>) {
  return Math.round(WEIGHT_KEYS.reduce((sum, { key }) => sum + (Number.isFinite(weights[key]) ? weights[key] : 0), 0) * 100) / 100
}

export const DEMAND_SOURCE_KEY: Record<DemandSource, TranslationKey> = {
  override: 'demandSourceOverride',
  snapshot: 'demandSourceSnapshot',
  default: 'demandSourceDefault',
}

export const MATCHING_OUTCOME_KEY: Record<MatchingOutcome, TranslationKey> = {
  assigned: 'outcomeAssigned',
  exhausted: 'outcomeExhausted',
  timeout: 'outcomeTimeout',
  cancelled: 'outcomeCancelled',
  in_progress: 'outcomeInProgress',
}

/** `datetime-local` value (local time, no seconds) from an ISO string; empty when null/invalid. */
export function toLocalInput(value: string | null | undefined) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** ISO string from a `datetime-local` value; null when empty/invalid. */
export function fromLocalInput(value: string): string | null {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

/** Today's date as YYYY-MM-DD in local time. */
export function todayIso() {
  return toLocalInput(new Date().toISOString()).slice(0, 10)
}

/** ISO date `days` before today (local). */
export function daysAgoIso(days: number) {
  const date = new Date()
  date.setDate(date.getDate() - days)
  return toLocalInput(date.toISOString()).slice(0, 10)
}
