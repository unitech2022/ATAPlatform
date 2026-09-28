import type { TranslationKey } from '../i18n'
import { formatKm, formatMinutes } from './format'
import type {
  LatLngTuple,
  LostItemCategory,
  LostItemStatus,
  SafetyAlert,
  SafetyAlertStatus,
  SafetyAlertType,
  SafetyCaseListItem,
  SafetyCaseSource,
  SafetyCaseStatus,
  SafetyCaseTrip,
  SafetyCaseType,
  SafetyEscalationTarget,
  SafetyNoteKind,
  SafetyPriority,
  SafetyReportCategory,
  SafetyReporterRole,
  SafetyResolutionCode,
  TripDetail,
  TripMessageSender,
  TripShareChannel,
} from './types'

/**
 * `Safety:EmergencyNumber` default (docs/09 §F12.9). The admin API does not expose the setting, so it is only
 * used for the "call emergency services" shortcut on the case page.
 */
export const EMERGENCY_NUMBER = '911'

/** Fallback polling cadence of the safety feed when the SignalR hub is not connected. */
export const SAFETY_POLL_INTERVAL_MS = 10_000

export const SAFETY_CASE_STATUSES: SafetyCaseStatus[] = ['open', 'in_progress', 'escalated', 'resolved']
export const SAFETY_PRIORITIES: SafetyPriority[] = ['critical', 'high', 'medium', 'low']
export const SAFETY_CASE_TYPES: SafetyCaseType[] = ['sos', 'unexpected_stop', 'route_deviation', 'trip_overrun', 'safety_report']
export const SAFETY_ALERT_TYPES: SafetyAlertType[] = ['unexpected_stop', 'route_deviation', 'trip_overrun']
export const SAFETY_ALERT_STATUSES: SafetyAlertStatus[] = ['pending_rider', 'escalated', 'no_response', 'resolved_ok', 'dismissed']
export const ESCALATION_TARGETS: SafetyEscalationTarget[] = ['police', 'ambulance', 'civil_defense', 'management', 'other']
export const RESOLUTION_CODES: SafetyResolutionCode[] = [
  'resolved_contacted',
  'false_alarm',
  'escalated_authorities',
  'action_taken_driver',
  'action_taken_passenger',
  'no_action',
  'other',
]
export const LOST_ITEM_STATUSES: LostItemStatus[] = ['open', 'driver_contacted', 'found', 'returned', 'not_found', 'closed']
export const LOST_ITEM_CATEGORIES: LostItemCategory[] = ['phone', 'wallet', 'bag', 'keys', 'documents', 'other']

export const SAFETY_TYPE_KEY: Record<SafetyCaseType, TranslationKey> = {
  sos: 'sfTypeSos',
  unexpected_stop: 'sfTypeUnexpectedStop',
  route_deviation: 'sfTypeRouteDeviation',
  trip_overrun: 'sfTypeTripOverrun',
  safety_report: 'sfTypeSafetyReport',
}

export const SAFETY_SOURCE_KEY: Record<SafetyCaseSource, TranslationKey> = {
  rider_sos: 'sfSourceRiderSos',
  driver_sos: 'sfSourceDriverSos',
  alert: 'sfSourceAlert',
  report: 'sfSourceReport',
  support: 'sfSourceSupport',
  admin: 'sfSourceAdmin',
}

export const REPORTER_ROLE_KEY: Record<SafetyReporterRole, TranslationKey> = {
  passenger: 'actorPassenger',
  driver: 'actorDriver',
  system: 'actorSystem',
  admin: 'actorAdmin',
}

export const ESCALATION_KEY: Record<SafetyEscalationTarget, TranslationKey> = {
  police: 'sfEscPolice',
  ambulance: 'sfEscAmbulance',
  civil_defense: 'sfEscCivilDefense',
  management: 'sfEscManagement',
  other: 'sfEscOther',
}

export const RESOLUTION_KEY: Record<SafetyResolutionCode, TranslationKey> = {
  false_alarm: 'sfResFalseAlarm',
  resolved_contacted: 'sfResContacted',
  escalated_authorities: 'sfResAuthorities',
  action_taken_driver: 'sfResActionDriver',
  action_taken_passenger: 'sfResActionPassenger',
  no_action: 'sfResNoAction',
  other: 'sfResOther',
}

export const NOTE_KIND_KEY: Record<SafetyNoteKind, TranslationKey> = {
  note: 'sfNoteKindNote',
  status_change: 'sfNoteKindStatus',
  assignment: 'sfNoteKindAssignment',
  contact_attempt: 'sfNoteKindContact',
  system: 'sfNoteKindSystem',
}

export const REPORT_CATEGORY_KEY: Record<SafetyReportCategory, TranslationKey> = {
  unsafe_driving: 'sfCatUnsafeDriving',
  harassment: 'sfCatHarassment',
  vehicle_mismatch: 'sfCatVehicleMismatch',
  driver_mismatch: 'sfCatDriverMismatch',
  passenger_misconduct: 'sfCatPassengerMisconduct',
  other: 'sfCatOther',
}

export const ALERT_TYPE_KEY: Record<SafetyAlertType, TranslationKey> = {
  unexpected_stop: 'sfTypeUnexpectedStop',
  route_deviation: 'sfTypeRouteDeviation',
  trip_overrun: 'sfTypeTripOverrun',
}

export const LOST_CATEGORY_KEY: Record<LostItemCategory, TranslationKey> = {
  phone: 'liCatPhone',
  wallet: 'liCatWallet',
  bag: 'liCatBag',
  keys: 'liCatKeys',
  documents: 'liCatDocuments',
  other: 'liCatOther',
}

export const SHARE_CHANNEL_KEY: Record<TripShareChannel, TranslationKey> = {
  link: 'sfShareLink',
  sms: 'sfShareSms',
  auto: 'sfShareAuto',
}

export const MESSAGE_SENDER_KEY: Record<TripMessageSender, TranslationKey> = {
  passenger: 'actorPassenger',
  driver: 'actorDriver',
  system: 'actorSystem',
}

const PRIORITY_RANK: Record<SafetyPriority, number> = { critical: 0, high: 1, medium: 2, low: 3 }

export function isOpenCase(status: SafetyCaseStatus) {
  return status !== 'resolved'
}

export function priorityRank(priority: SafetyPriority) {
  return PRIORITY_RANK[priority] ?? 9
}

/** Queue order from docs/09 "لوحة الإدارة": unresolved first, then priority, then the oldest. */
export function sortCases<T extends Pick<SafetyCaseListItem, 'status' | 'priority' | 'openedAt'>>(cases: T[]): T[] {
  return [...cases].sort((a, b) => {
    const open = Number(isOpenCase(b.status)) - Number(isOpenCase(a.status))
    if (open !== 0) return open
    const priority = priorityRank(a.priority) - priorityRank(b.priority)
    if (priority !== 0) return priority
    return new Date(a.openedAt).getTime() - new Date(b.openedAt).getTime()
  })
}

/** Seconds → `h:mm:ss` / `m:ss` for the live age timers. */
export function formatDuration(seconds: number | null | undefined) {
  if (typeof seconds !== 'number' || !Number.isFinite(seconds)) return '—'
  const total = Math.max(0, Math.floor(seconds))
  const h = Math.floor(total / 3600)
  const m = Math.floor((total % 3600) / 60)
  const s = total % 60
  const pad = (value: number) => String(value).padStart(2, '0')
  return h > 0 ? `${h}:${pad(m)}:${pad(s)}` : `${m}:${pad(s)}`
}

export function mapsUrl(lat: number, lng: number) {
  return `https://maps.google.com/?q=${lat},${lng}`
}

/** `tel:` link; keeps the leading + and digits only. */
export function telHref(phone: string) {
  return `tel:${phone.replace(/[^\d+]/g, '')}`
}

/** Planned route of a trip, falling back to straight segments pickup → stops → dropoff (§F12.1 `straight`). */
export function plannedRouteOf(trip: Pick<SafetyCaseTrip, 'pickup' | 'dropoff' | 'stops' | 'plannedRoute'> | Pick<TripDetail, 'pickup' | 'dropoff' | 'stops' | 'plannedRoute'>): LatLngTuple[] {
  if (trip.plannedRoute && trip.plannedRoute.length >= 2) return trip.plannedRoute
  return [[trip.pickup.lat, trip.pickup.lng], ...(trip.stops ?? []).map((stop): LatLngTuple => [stop.lat, stop.lng]), [trip.dropoff.lat, trip.dropoff.lng]]
}

/** One-line description of what triggered an automatic alert (units appended from translations). */
export function alertMetricsText(alert: SafetyAlert, t: (key: TranslationKey) => string) {
  const m = alert.metrics ?? {}
  switch (alert.type) {
    case 'unexpected_stop':
      return typeof m.stoppedSeconds === 'number' ? `${t('sfStoppedFor')} ${formatMinutes(m.stoppedSeconds)} ${t('min')}` : '—'
    case 'route_deviation':
      return typeof m.deviationMeters === 'number'
        ? `${t('sfDeviatedBy')} ${formatKm(m.deviationMeters)} ${t('km')}${typeof m.deviationSeconds === 'number' ? ` · ${formatMinutes(m.deviationSeconds)} ${t('min')}` : ''}`
        : '—'
    case 'trip_overrun':
      return typeof m.elapsedSeconds === 'number'
        ? `${formatMinutes(m.elapsedSeconds)} / ${formatMinutes(m.estimatedSeconds)} ${t('min')}`
        : '—'
    default:
      return '—'
  }
}
