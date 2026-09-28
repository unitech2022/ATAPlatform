import { api } from './api'
import type {
  AudiencePreview,
  AuditLog,
  Campaign,
  CampaignAudience,
  CampaignInput,
  CampaignStatus,
  DeliveryStatus,
  LedgerBalance,
  NotificationChannel,
  NotificationDelivery,
  NotificationEvent,
  NotificationTemplate,
  NotificationTemplateInput,
  PaymentChannel,
  PaymentDetail,
  PaymentListItem,
  PaymentPurpose,
  PaymentStatus,
  Payout,
  PayoutBatch,
  PayoutStatus,
  Refund,
  RefundInput,
  RefundStatus,
  Receipt,
  Settlement,
  SettlementBatch,
  SettlementBatchInput,
  SettlementDirection,
  TemplatePreview,
  WalletAdjustmentInput,
  WalletDetail,
  WalletKind,
  WalletListItem,
  WalletTransaction,
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

// ---------------------------------------------------------------------------
// F11 — payments, refunds, payouts, settlements, wallets, ledger
// ---------------------------------------------------------------------------

type DateRange = {
  /** ISO date (YYYY-MM-DD). */
  from?: string
  /** ISO date (YYYY-MM-DD). */
  to?: string
}

type PageQuery = { page?: number; pageSize?: number }

export type PaymentListQuery = DateRange &
  PageQuery & {
    status?: PaymentStatus | ''
    purpose?: PaymentPurpose | ''
    method?: PaymentChannel | ''
    provider?: string
    search?: string
  }

export const payments = {
  list: (query: PaymentListQuery) => api.get<Paginated<PaymentListItem>>('/admin/payments', query),
  get: (id: string) => api.get<PaymentDetail>(`/admin/payments/${id}`),
  refund: (id: string, input: RefundInput) => api.post<Refund>(`/admin/payments/${id}/refunds`, input),
  /** Cash/wallet trips — the server forces `destination = wallet`. */
  refundTrip: (tripId: string, input: Omit<RefundInput, 'destination'>) => api.post<Refund>(`/admin/trips/${tripId}/refunds`, input),
  receipt: (tripId: string) => api.get<Receipt>(`/admin/trips/${tripId}/receipt`),
}

export type RefundListQuery = DateRange & PageQuery & { status?: RefundStatus | '' }

export const refunds = {
  list: (query: RefundListQuery) => api.get<Paginated<Refund>>('/admin/refunds', query),
  approve: (id: string) => api.post<Refund>(`/admin/refunds/${id}/approve`),
  reject: (id: string, reason: string) => api.post<Refund>(`/admin/refunds/${id}/reject`, { reason }),
  retry: (id: string) => api.post<Refund>(`/admin/refunds/${id}/retry`),
}

export type PayoutListQuery = DateRange & PageQuery & { status?: PayoutStatus | ''; driverId?: string }

export const payouts = {
  list: (query: PayoutListQuery) => api.get<Paginated<Payout>>('/admin/payouts', query),
  approve: (id: string) => api.post<Payout>(`/admin/payouts/${id}/approve`),
  reject: (id: string, reason: string) => api.post<Payout>(`/admin/payouts/${id}/reject`, { reason }),
  markPaid: (id: string, bankReference: string, paidAt?: string) =>
    api.post<Payout>(`/admin/payouts/${id}/mark-paid`, { bankReference, paidAt: paidAt || undefined }),
}

export const payoutBatches = {
  list: (query: PageQuery) => api.get<Paginated<PayoutBatch>>('/admin/payout-batches', query),
  get: (id: string) => api.get<PayoutBatch>(`/admin/payout-batches/${id}`),
  create: (input: { payoutIds: string[] } | { allApproved: true }) => api.post<PayoutBatch>('/admin/payout-batches', input),
  exportCsv: (id: string) => api.download(`/admin/payout-batches/${id}/export`, { format: 'csv' }),
  markPaid: (id: string, bankReference: string) => api.post<PayoutBatch>(`/admin/payout-batches/${id}/mark-paid`, { bankReference }),
}

export type SettlementListQuery = PageQuery & { direction?: SettlementDirection | ''; search?: string }

export const settlements = {
  batches: (query: PageQuery) => api.get<Paginated<SettlementBatch>>('/admin/settlement-batches', query),
  batch: (id: string) => api.get<SettlementBatch>(`/admin/settlement-batches/${id}`),
  generate: (input: SettlementBatchInput) => api.post<SettlementBatch>('/admin/settlement-batches', input),
  lines: (batchId: string, query: SettlementListQuery) =>
    api.get<Paginated<Settlement>>(`/admin/settlement-batches/${batchId}/settlements`, query),
  get: (id: string) => api.get<Settlement>(`/admin/settlements/${id}`),
  exportCsv: (batchId: string) => api.download(`/admin/settlement-batches/${batchId}/export`, { format: 'csv' }),
  finalize: (batchId: string) => api.post<SettlementBatch>(`/admin/settlement-batches/${batchId}/finalize`),
  regenerate: (batchId: string) => api.post<SettlementBatch>(`/admin/settlement-batches/${batchId}/regenerate`),
}

export type WalletListQuery = PageQuery & { kind?: WalletKind | ''; search?: string; negativeOnly?: boolean | '' }

export const wallets = {
  list: (query: WalletListQuery) => api.get<Paginated<WalletListItem>>('/admin/wallets', query),
  get: (id: string) => api.get<WalletDetail>(`/admin/wallets/${id}`),
  adjust: (id: string, input: WalletAdjustmentInput) => api.post<WalletTransaction>(`/admin/wallets/${id}/adjustments`, input),
  freeze: (id: string, reason: string) => api.post<WalletDetail>(`/admin/wallets/${id}/freeze`, { reason }),
  unfreeze: (id: string, reason: string) => api.post<WalletDetail>(`/admin/wallets/${id}/unfreeze`, { reason }),
}

export const ledger = {
  balances: (query: DateRange) => api.get<LedgerBalance[]>('/admin/ledger/balances', query),
}

// ---------------------------------------------------------------------------
// F13 — notification templates, campaigns, deliveries
// ---------------------------------------------------------------------------

export type TemplateListQuery = { code?: string; channel?: NotificationChannel | ''; isActive?: boolean | '' }

export const notificationEvents = {
  list: () => api.get<NotificationEvent[]>('/admin/notification-events'),
}

export const notificationTemplates = {
  list: (query: TemplateListQuery = {}) =>
    api.get<Paginated<NotificationTemplate> | NotificationTemplate[]>('/admin/notification-templates', query).then(unwrapList),
  create: (input: NotificationTemplateInput) => api.post<NotificationTemplate>('/admin/notification-templates', input),
  update: (id: string, input: Omit<NotificationTemplateInput, 'code' | 'channel'>) =>
    api.put<NotificationTemplate>(`/admin/notification-templates/${id}`, input),
  preview: (id: string, placeholders: Record<string, string>, language: 'ar' | 'en') =>
    api.post<TemplatePreview>(`/admin/notification-templates/${id}/preview`, { placeholders, language }),
  test: (id: string, userId: string) => api.post<void>(`/admin/notification-templates/${id}/test`, { userId }),
}

export type CampaignListQuery = PageQuery & { status?: CampaignStatus | '' }

export const campaigns = {
  list: (query: CampaignListQuery) =>
    api.get<Paginated<Campaign> | Campaign[]>('/admin/notification-campaigns', query).then((value) =>
      Array.isArray(value) ? { items: value, page: 1, pageSize: value.length || 1, total: value.length } : value,
    ),
  get: (id: string) => api.get<Campaign>(`/admin/notification-campaigns/${id}`),
  create: (input: CampaignInput) => api.post<Campaign>('/admin/notification-campaigns', input),
  update: (id: string, input: CampaignInput) => api.put<Campaign>(`/admin/notification-campaigns/${id}`, input),
  remove: (id: string) => api.delete<void>(`/admin/notification-campaigns/${id}`),
  schedule: (id: string, scheduledAt: string) => api.post<Campaign>(`/admin/notification-campaigns/${id}/schedule`, { scheduledAt }),
  sendNow: (id: string) => api.post<Campaign>(`/admin/notification-campaigns/${id}/send-now`),
  cancel: (id: string) => api.post<Campaign>(`/admin/notification-campaigns/${id}/cancel`),
  audiencePreview: (audience: CampaignAudience) =>
    api.post<AudiencePreview>('/admin/notification-campaigns/audience-preview', { audience }),
}

export type DeliveryListQuery = DateRange &
  PageQuery & {
    userId?: string
    eventCode?: string
    channel?: 'push' | 'sms' | ''
    status?: DeliveryStatus | ''
    campaignId?: string
  }

export const deliveries = {
  list: (query: DeliveryListQuery) => api.get<Paginated<NotificationDelivery>>('/admin/notification-deliveries', query),
  retry: (id: string) => api.post<void>(`/admin/notification-deliveries/${id}/retry`),
}

export const duty = {
  get: () => api.get<{ onDuty: boolean }>('/admin/me/duty'),
  set: (onDuty: boolean) => api.put<{ onDuty: boolean }>('/admin/me/duty', { onDuty }),
}
