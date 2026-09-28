import { api } from './api'
import type {
  AuditLog,
  AuthResponse,
  CurrentDemand,
  DashboardSummary,
  DemandLevel,
  DemandOverride,
  DemandOverrideInput,
  DemandRule,
  DemandRuleInput,
  DriverDetail,
  DriverDocument,
  DriverListItem,
  DriverStatus,
  LiveSnapshot,
  MatchingSettings,
  MatchingSettingsInput,
  MatchingStats,
  Paginated,
  PassengerListItem,
  PricingRule,
  PricingRuleInput,
  RideCategory,
  RideCategoryInput,
  SimulateRequest,
  SimulateResult,
  TripDetail,
  TripListItem,
  TripMatching,
  TripStatus,
  Zone,
  ZoneInput,
} from './types'

export const auth = {
  login: (username: string, password: string) =>
    api.post<AuthResponse>('/auth/admin/login', { username, password }, { anonymous: true }),
  logout: (refreshToken: string) => api.post<void>('/auth/logout', { refreshToken }),
}

export const dashboard = {
  summary: () => api.get<DashboardSummary>('/admin/dashboard/summary'),
}

export type DriverListQuery = {
  status?: DriverStatus | ''
  search?: string
  page?: number
  pageSize?: number
}

export const drivers = {
  list: (query: DriverListQuery) => api.get<Paginated<DriverListItem>>('/admin/drivers', query),
  get: (id: string) => api.get<DriverDetail>(`/admin/drivers/${id}`),
  startReview: (id: string) => api.post<DriverDetail>(`/admin/drivers/${id}/review`, { action: 'start_review' }),
  approve: (id: string) => api.post<DriverDetail>(`/admin/drivers/${id}/approve`),
  reject: (id: string, reason: string) => api.post<DriverDetail>(`/admin/drivers/${id}/reject`, { reason }),
  suspend: (id: string, reason: string) => api.post<DriverDetail>(`/admin/drivers/${id}/suspend`, { reason }),
  reinstate: (id: string) => api.post<DriverDetail>(`/admin/drivers/${id}/reinstate`),
}

export const documents = {
  review: (id: string, status: 'verified' | 'rejected', note?: string) =>
    api.post<DriverDocument>(`/admin/documents/${id}/verify`, { status, note: note || undefined }),
}

export const files = {
  blob: (fileId: string) => api.blob(`/files/${fileId}`),
}

export type PassengerListQuery = {
  search?: string
  page?: number
  pageSize?: number
}

export const passengers = {
  list: (query: PassengerListQuery) => api.get<Paginated<PassengerListItem>>('/admin/passengers', query),
}

export const users = {
  suspend: (userId: string, reason: string) => api.post<void>(`/admin/users/${userId}/suspend`, { reason }),
  reinstate: (userId: string) => api.post<void>(`/admin/users/${userId}/reinstate`),
}

export const rideCategories = {
  list: () => api.get<RideCategory[]>('/admin/ride-categories'),
  create: (input: RideCategoryInput) => api.post<RideCategory>('/admin/ride-categories', input),
  update: (id: string, input: RideCategoryInput) => api.put<RideCategory>(`/admin/ride-categories/${id}`, input),
  remove: (id: string) => api.delete<void>(`/admin/ride-categories/${id}`),
}

export type AuditLogQuery = {
  entityType?: string
  entityId?: string
  page?: number
  pageSize?: number
}

export const auditLogs = {
  list: (query: AuditLogQuery) => api.get<Paginated<AuditLog>>('/admin/audit-logs', query),
}

export type TripListQuery = {
  status?: TripStatus | ''
  /** ISO date (YYYY-MM-DD) lower bound on requestedAt. */
  from?: string
  /** ISO date (YYYY-MM-DD) upper bound on requestedAt. */
  to?: string
  search?: string
  page?: number
  pageSize?: number
}

export const trips = {
  list: (query: TripListQuery) => api.get<Paginated<TripListItem>>('/admin/trips', query),
  get: (id: string) => api.get<TripDetail>(`/admin/trips/${id}`),
  cancel: (id: string, reason: string) => api.post<TripDetail>(`/admin/trips/${id}/cancel`, { reason }),
}

export const live = {
  snapshot: () => api.get<LiveSnapshot>('/admin/live'),
}

// ---------------------------------------------------------------------------
// F9 / F10 — zones, pricing, demand, matching
// ---------------------------------------------------------------------------

/** Some list endpoints answer with a bare array and others with a page envelope; both are accepted. */
export function unwrapList<T>(value: Paginated<T> | T[] | null | undefined): T[] {
  if (Array.isArray(value)) return value
  return value?.items ?? []
}

/** Large enough page so that admin reference lists (zones, rules, settings) arrive in one call. */
const ALL = { page: 1, pageSize: 200 }

export const zones = {
  list: () => api.get<Paginated<Zone> | Zone[]>('/admin/zones', ALL).then(unwrapList),
  get: (id: string) => api.get<Zone>(`/admin/zones/${id}`),
  create: (input: ZoneInput) => api.post<Zone>('/admin/zones', input),
  update: (id: string, input: ZoneInput) => api.put<Zone>(`/admin/zones/${id}`, input),
  remove: (id: string) => api.delete<void>(`/admin/zones/${id}`),
}

export type PricingRuleQuery = {
  rideCategoryId?: string
  zoneId?: string
  isActive?: boolean | ''
}

export const pricingRules = {
  list: (query: PricingRuleQuery = {}) =>
    api.get<Paginated<PricingRule> | PricingRule[]>('/admin/pricing-rules', { ...ALL, ...query }).then(unwrapList),
  get: (id: string) => api.get<PricingRule>(`/admin/pricing-rules/${id}`),
  create: (input: PricingRuleInput) => api.post<PricingRule>('/admin/pricing-rules', input),
  update: (id: string, input: PricingRuleInput) => api.put<PricingRule>(`/admin/pricing-rules/${id}`, input),
  remove: (id: string) => api.delete<void>(`/admin/pricing-rules/${id}`),
  simulate: (input: SimulateRequest) => api.post<SimulateResult>('/admin/pricing/simulate', input),
}

export const demand = {
  levels: () => api.get<Paginated<DemandLevel> | DemandLevel[]>('/admin/demand-levels', ALL).then(unwrapList),
  updateLevel: (id: string, multiplier: number) => api.put<DemandLevel>(`/admin/demand-levels/${id}`, { multiplier }),
  rules: () => api.get<Paginated<DemandRule> | DemandRule[]>('/admin/demand-rules', ALL).then(unwrapList),
  createRule: (input: DemandRuleInput) => api.post<DemandRule>('/admin/demand-rules', input),
  updateRule: (id: string, input: DemandRuleInput) => api.put<DemandRule>(`/admin/demand-rules/${id}`, input),
  removeRule: (id: string) => api.delete<void>(`/admin/demand-rules/${id}`),
  overrides: () => api.get<Paginated<DemandOverride> | DemandOverride[]>('/admin/demand-overrides', ALL).then(unwrapList),
  createOverride: (input: DemandOverrideInput) => api.post<DemandOverride>('/admin/demand-overrides', input),
  removeOverride: (id: string) => api.delete<void>(`/admin/demand-overrides/${id}`),
  current: () => api.get<CurrentDemand[]>('/admin/demand/current'),
}

export type MatchingStatsQuery = {
  /** ISO date (YYYY-MM-DD). */
  from?: string
  /** ISO date (YYYY-MM-DD). */
  to?: string
}

export const matching = {
  settings: () => api.get<Paginated<MatchingSettings> | MatchingSettings[]>('/admin/matching-settings', ALL).then(unwrapList),
  createSettings: (input: MatchingSettingsInput) => api.post<MatchingSettings>('/admin/matching-settings', input),
  updateSettings: (id: string, input: MatchingSettingsInput) => api.put<MatchingSettings>(`/admin/matching-settings/${id}`, input),
  removeSettings: (id: string) => api.delete<void>(`/admin/matching-settings/${id}`),
  forTrip: (tripId: string) => api.get<TripMatching>(`/admin/trips/${tripId}/matching`),
  stats: (query: MatchingStatsQuery) => api.get<MatchingStats>('/admin/matching/stats', query),
}
