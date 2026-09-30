import type { TranslationKey } from '../i18n'
import { isUsablePolygon, isValidLatLng } from './pricing'
import type { Airport, AirportInput, AirportZone, AirportZoneInput, AirportZoneKind } from './types'

// ---------------------------------------------------------------------------
// F17 — airports (docs/11 §F17.6–§F17.8)
// ---------------------------------------------------------------------------

export const AIRPORT_ZONE_KINDS: AirportZoneKind[] = ['terminal', 'pickup_zone', 'driver_waiting_area']

export const AIRPORT_ZONE_KIND_KEY: Record<AirportZoneKind, TranslationKey> = {
  terminal: 'apKindTerminal',
  pickup_zone: 'apKindPickup',
  driver_waiting_area: 'apKindWaiting',
}

/** IATA code (`airports.code CHAR(3)`), stored uppercase. */
export const AIRPORT_CODE = /^[A-Z]{3}$/
/** `airport_zones.terminal_code VARCHAR(10)`: `T1`…`T5`. */
export const TERMINAL_CODE = /^[A-Z0-9]{1,10}$/

/** `airport_zones.code VARCHAR(30)`. */
export const ZONE_CODE_MAX = 30
export const INSTRUCTIONS_MAX = 500

/** Seeded default of `default_free_waiting_minutes` (§ Seed / decision 7). */
export const DEFAULT_AIRPORT_FREE_WAITING = 15

export const EMPTY_AIRPORT: AirportInput = {
  cityId: '',
  code: '',
  nameAr: '',
  nameEn: '',
  lat: Number.NaN,
  lng: Number.NaN,
  geofence: [],
  requiresPickupZone: true,
  defaultFreeWaitingMinutes: DEFAULT_AIRPORT_FREE_WAITING,
  defaultWaitingPerMinute: null,
  queueEnabled: false,
  isActive: true,
}

export function airportInputOf(airport: Airport, patch: Partial<AirportInput> = {}): AirportInput {
  return {
    cityId: airport.cityId,
    code: airport.code,
    nameAr: airport.nameAr,
    nameEn: airport.nameEn,
    lat: airport.lat,
    lng: airport.lng,
    geofence: airport.geofence,
    requiresPickupZone: airport.requiresPickupZone,
    defaultFreeWaitingMinutes: airport.defaultFreeWaitingMinutes ?? null,
    defaultWaitingPerMinute: airport.defaultWaitingPerMinute ?? null,
    queueEnabled: airport.queueEnabled,
    isActive: airport.isActive,
    ...patch,
  }
}

const hasAtMostTwoDecimals = (value: number) => Math.abs(value * 100 - Math.round(value * 100)) < 1e-6

export type AirportErrorField = 'cityId' | 'code' | 'nameAr' | 'nameEn' | 'lat' | 'lng' | 'geofence' | 'defaultFreeWaitingMinutes' | 'defaultWaitingPerMinute'

/** Waiting policy pair shared by airports and zones: minutes ≥ 0 whole, per-minute fee ≥ 0 with two decimals. */
function waitingErrors(free: number | null, perMinute: number | null) {
  return {
    free: free !== null && (!Number.isInteger(free) || free < 0),
    perMinute: perMinute !== null && (!Number.isFinite(perMinute) || perMinute < 0 || !hasAtMostTwoDecimals(perMinute)),
  }
}

export function validateAirport(input: AirportInput, others: Airport[] = []): Partial<Record<AirportErrorField, TranslationKey>> {
  const errors: Partial<Record<AirportErrorField, TranslationKey>> = {}
  if (!input.cityId) errors.cityId = 'fieldRequired'
  if (!AIRPORT_CODE.test(input.code)) errors.code = 'apErrCode'
  else if (others.some((other) => other.code === input.code)) errors.code = 'apErrCodeExists'
  if (!input.nameAr.trim()) errors.nameAr = 'fieldRequired'
  if (!input.nameEn.trim()) errors.nameEn = 'fieldRequired'
  if (!isValidLatLng(input.lat, input.lng)) {
    if (!Number.isFinite(input.lat) || input.lat < -90 || input.lat > 90) errors.lat = 'apErrLat'
    if (!Number.isFinite(input.lng) || input.lng < -180 || input.lng > 180) errors.lng = 'apErrLng'
  }
  if (!isUsablePolygon(input.geofence)) errors.geofence = 'polygonTooFew'
  const waiting = waitingErrors(input.defaultFreeWaitingMinutes, input.defaultWaitingPerMinute)
  if (waiting.free) errors.defaultFreeWaitingMinutes = 'sdErrNonNegativeInt'
  if (waiting.perMinute) errors.defaultWaitingPerMinute = 'apErrPerMinute'
  return errors
}

export type AirportZoneErrorField =
  | 'code'
  | 'terminalCode'
  | 'nameAr'
  | 'nameEn'
  | 'polygon'
  | 'lat'
  | 'lng'
  | 'instructionsAr'
  | 'instructionsEn'
  | 'freeWaitingMinutes'
  | 'waitingPerMinute'
  | 'sortOrder'

/** §F17.6 `airport_zones`: `UNIQUE(airport_id, code)`, polygon mandatory for `driver_waiting_area`, instructions ≤ 500. */
export function validateAirportZone(input: AirportZoneInput, others: AirportZone[] = []): Partial<Record<AirportZoneErrorField, TranslationKey>> {
  const errors: Partial<Record<AirportZoneErrorField, TranslationKey>> = {}
  const code = input.code.trim()
  if (!code) errors.code = 'fieldRequired'
  else if (code.length > ZONE_CODE_MAX) errors.code = 'apErrZoneCodeLength'
  else if (others.some((other) => other.code === code)) errors.code = 'apErrZoneCodeExists'
  if (input.terminalCode !== null && !TERMINAL_CODE.test(input.terminalCode)) errors.terminalCode = 'apErrTerminalCode'
  if (!input.nameAr.trim()) errors.nameAr = 'fieldRequired'
  if (!input.nameEn.trim()) errors.nameEn = 'fieldRequired'
  if (input.kind === 'driver_waiting_area' && !isUsablePolygon(input.polygon ?? [])) errors.polygon = 'polygonTooFew'
  if (!Number.isFinite(input.lat) || input.lat < -90 || input.lat > 90) errors.lat = 'apErrLat'
  if (!Number.isFinite(input.lng) || input.lng < -180 || input.lng > 180) errors.lng = 'apErrLng'
  if ((input.instructionsAr ?? '').length > INSTRUCTIONS_MAX) errors.instructionsAr = 'apErrInstructions'
  if ((input.instructionsEn ?? '').length > INSTRUCTIONS_MAX) errors.instructionsEn = 'apErrInstructions'
  const waiting = waitingErrors(input.freeWaitingMinutes, input.waitingPerMinute)
  if (waiting.free) errors.freeWaitingMinutes = 'sdErrNonNegativeInt'
  if (waiting.perMinute) errors.waitingPerMinute = 'apErrPerMinute'
  if (!Number.isInteger(input.sortOrder)) errors.sortOrder = 'invalidNumber'
  return errors
}

export function airportZoneInputOf(zone: AirportZone, patch: Partial<AirportZoneInput> = {}): AirportZoneInput {
  return {
    kind: zone.kind,
    code: zone.code,
    terminalCode: zone.terminalCode ?? null,
    nameAr: zone.nameAr,
    nameEn: zone.nameEn,
    polygon: zone.polygon ?? null,
    lat: zone.lat,
    lng: zone.lng,
    instructionsAr: zone.instructionsAr ?? null,
    instructionsEn: zone.instructionsEn ?? null,
    freeWaitingMinutes: zone.freeWaitingMinutes ?? null,
    waitingPerMinute: zone.waitingPerMinute ?? null,
    sortOrder: zone.sortOrder,
    isActive: zone.isActive,
    ...patch,
  }
}

/**
 * Effective waiting policy of a pickup point, in the spec's order (§F17.7 "الانتظار"):
 * zone value → airport default → the pricing rule (returned as `null`, resolved on the server).
 */
export function effectiveWaiting(zone: Pick<AirportZone, 'freeWaitingMinutes' | 'waitingPerMinute'>, airport: Pick<Airport, 'defaultFreeWaitingMinutes' | 'defaultWaitingPerMinute'> | null) {
  return {
    freeMinutes: zone.freeWaitingMinutes ?? airport?.defaultFreeWaitingMinutes ?? null,
    perMinute: zone.waitingPerMinute ?? airport?.defaultWaitingPerMinute ?? null,
    freeFromZone: zone.freeWaitingMinutes !== null && zone.freeWaitingMinutes !== undefined,
    perMinuteFromZone: zone.waitingPerMinute !== null && zone.waitingPerMinute !== undefined,
  }
}

/** Minutes since `since` (floored), never negative. */
export function minutesSince(since: string | null | undefined, now: number) {
  if (!since) return null
  const time = new Date(since).getTime()
  return Number.isNaN(time) ? null : Math.max(0, Math.floor((now - time) / 60_000))
}
