import { api } from './api'
import type {
  Airport,
  AirportInput,
  AirportQueueEntry,
  AirportZone,
  AirportZoneInput,
  ScheduledReservationFilter,
  ScheduledRule,
  ScheduledRuleInput,
  ScheduledTripRow,
  SchedulingStats,
  AdminRating,
  City,
  DriverIncentiveProgress,
  FavoriteDiscountRule,
  FavoriteDiscountRuleInput,
  FavoriteStats,
  DriverTier,
  DriverTierHistoryEntry,
  DriverTierRule,
  DriverTierRuleInput,
  Incentive,
  IncentiveInput,
  IncentiveProgress,
  IncentiveProgressStatus,
  Promotion,
  PromotionInput,
  PromotionListItem,
  PromotionListStatus,
  PromotionRedemption,
  PromotionStats,
  RaterRole,
  RatingFlag,
  RatingFlagReviewAction,
  RatingFlagStatus,
  RatingFlagType,
  RatingStatus,
  RatingTag,
  RedemptionStatus,
  AtFault,
  BookingType,
  CancellationEvent,
  CancellationFeeStatus,
  CancellationReason,
  CancellationReasonInput,
  CancellationRule,
  CancellationRuleInput,
  CancellationSimulateInput,
  CancellationSimulateResult,
  CancellationStage,
  CancellationStats,
  ExcuseQueueItem,
  ExcuseStatus,
  LostItemReport,
  LostItemStatus,
  ReasonActor,
  ReliabilityAdjustInput,
  ReliabilityProfileDetail,
  ReliabilityProfileListItem,
  ReliabilityRole,
  ReliabilityThreshold,
  ReliabilityThresholdInput,
  RestrictionLevel,
  RuleActor,
  RuleStage,
  SafetyAlert,
  SafetyAlertStatus,
  SafetyAlertType,
  SafetyCaseCreateInput,
  SafetyCaseDetail,
  SafetyCaseListItem,
  SafetyCaseStatus,
  SafetyCaseType,
  SafetyEscalationTarget,
  SafetyPriority,
  SafetyResolutionCode,
  SafetySummary,
  TripActor,
  TripMessage,
  TripShare,
  TrustedContact,
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
  /** F14: `atFault` (default `none`) decides who the cancellation counts against; `chargeFee` applies the matching rule's fee. */
  cancel: (id: string, reason: string, options: { atFault?: AtFault; chargeFee?: boolean } = {}) =>
    api.post<TripDetail>(`/admin/trips/${id}/cancel`, { reason, ...options }),
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

// ---------------------------------------------------------------------------
// F12 — safety (docs/09 §F12.7 "الإدارة")
// ---------------------------------------------------------------------------

/** Wraps a bare array answer into a single page so list screens can treat both shapes alike. */
function asPage<T>(value: Paginated<T> | T[]): Paginated<T> {
  return Array.isArray(value) ? { items: value, page: 1, pageSize: value.length || 1, total: value.length } : value
}

export type SafetyCaseListQuery = DateRange &
  PageQuery & {
    status?: SafetyCaseStatus | ''
    priority?: SafetyPriority | ''
    type?: SafetyCaseType | ''
    /** `me`, `unassigned` or an admin user id. */
    assignedTo?: string
    search?: string
  }

export type SafetyAlertListQuery = DateRange &
  PageQuery & {
    status?: SafetyAlertStatus | ''
    type?: SafetyAlertType | ''
    /** Not in §F12.7 — sent for backends that support it; callers still filter by trip client-side. */
    tripId?: string
    search?: string
  }

export const safety = {
  summary: () => api.get<SafetySummary>('/admin/safety/summary'),
  cases: (query: SafetyCaseListQuery) =>
    api.get<Paginated<SafetyCaseListItem> | SafetyCaseListItem[]>('/admin/safety/cases', query).then(asPage),
  get: (id: string) => api.get<SafetyCaseDetail>(`/admin/safety/cases/${id}`),
  /** `userId = null` assigns the case to the calling admin. */
  assign: (id: string, userId: string | null = null) => api.post<SafetyCaseDetail>(`/admin/safety/cases/${id}/assign`, { userId }),
  setStatus: (id: string, input: { status: 'in_progress' | 'escalated'; escalatedTo?: SafetyEscalationTarget; note?: string }) =>
    api.post<SafetyCaseDetail>(`/admin/safety/cases/${id}/status`, input),
  addNote: (id: string, input: { body: string; kind: 'note' | 'contact_attempt'; isInternal: boolean }) =>
    api.post<SafetyCaseDetail>(`/admin/safety/cases/${id}/notes`, input),
  resolve: (id: string, input: { resolutionCode: SafetyResolutionCode; resolution: string }) =>
    api.post<SafetyCaseDetail>(`/admin/safety/cases/${id}/resolve`, input),
  create: (input: SafetyCaseCreateInput) => api.post<SafetyCaseDetail>('/admin/safety/cases', input),
  alerts: (query: SafetyAlertListQuery) => api.get<Paginated<SafetyAlert> | SafetyAlert[]>('/admin/safety/alerts', query).then(asPage),
  dismissAlert: (id: string, note: string) => api.post<SafetyAlert>(`/admin/safety/alerts/${id}/dismiss`, { note }),
  /** Readable only while the user has an open case; the read is audited server-side. */
  trustedContacts: (userId: string) => api.get<TrustedContact[]>(`/admin/users/${userId}/trusted-contacts`),
}

export const tripSafety = {
  /** Reading the chat is audited as `trip_messages.view` (§F12.7). */
  messages: (tripId: string) => api.get<TripMessage[] | Paginated<TripMessage>>(`/admin/trips/${tripId}/messages`).then(unwrapList),
  /**
   * Assumed endpoint: §F12.7 lists no admin read of `trip_shares`; this mirrors `GET /safety/trips/{id}/shares`.
   * Callers treat 404 as "not available".
   */
  shares: (tripId: string) => api.get<TripShare[] | Paginated<TripShare>>(`/admin/trips/${tripId}/shares`).then(unwrapList),
}

export type LostItemListQuery = PageQuery & { status?: LostItemStatus | ''; search?: string }

export const lostItems = {
  list: (query: LostItemListQuery) => api.get<Paginated<LostItemReport> | LostItemReport[]>('/admin/lost-items', query).then(asPage),
  update: (id: string, status: LostItemStatus, note?: string) =>
    api.patch<LostItemReport>(`/admin/lost-items/${id}`, { status, note: note || undefined }),
}

// ---------------------------------------------------------------------------
// F14 — cancellation & reliability (docs/09 §F14.4 "الإدارة")
// ---------------------------------------------------------------------------

export const cancellationReasons = {
  list: (query: { actor?: ReasonActor | '' } = {}) =>
    api.get<Paginated<CancellationReason> | CancellationReason[]>('/admin/cancellation-reasons', { ...ALL, ...query }).then(unwrapList),
  create: (input: CancellationReasonInput) => api.post<CancellationReason>('/admin/cancellation-reasons', input),
  update: (id: string, input: CancellationReasonInput) => api.put<CancellationReason>(`/admin/cancellation-reasons/${id}`, input),
  /** Deactivates instead of deleting when the reason is already referenced. */
  remove: (id: string) => api.delete<void>(`/admin/cancellation-reasons/${id}`),
}

export type CancellationRuleQuery = { actor?: RuleActor | ''; stage?: RuleStage | ''; bookingType?: BookingType | '' }

export const cancellationRules = {
  list: (query: CancellationRuleQuery = {}) =>
    api.get<Paginated<CancellationRule> | CancellationRule[]>('/admin/cancellation-rules', { ...ALL, ...query }).then(unwrapList),
  create: (input: CancellationRuleInput) => api.post<CancellationRule>('/admin/cancellation-rules', input),
  update: (id: string, input: CancellationRuleInput) => api.put<CancellationRule>(`/admin/cancellation-rules/${id}`, input),
  remove: (id: string) => api.delete<void>(`/admin/cancellation-rules/${id}`),
  simulate: (input: CancellationSimulateInput) => api.post<CancellationSimulateResult>('/admin/cancellation-rules/simulate', input),
}

export const reliabilityThresholds = {
  list: (role?: ReliabilityRole) =>
    api.get<Paginated<ReliabilityThreshold> | ReliabilityThreshold[]>('/admin/reliability-thresholds', { ...ALL, role }).then(unwrapList),
  update: (id: string, input: ReliabilityThresholdInput) => api.put<ReliabilityThreshold>(`/admin/reliability-thresholds/${id}`, input),
}

export type CancellationListQuery = DateRange &
  PageQuery & {
    actor?: TripActor | ''
    stage?: CancellationStage | ''
    atFault?: AtFault | ''
    feeStatus?: CancellationFeeStatus | ''
    excuseStatus?: ExcuseStatus | ''
    search?: string
  }

export type CancellationStatsQuery = DateRange & { cityId?: string; zoneId?: string; rideCategoryId?: string }

export const cancellations = {
  list: (query: CancellationListQuery) =>
    api.get<Paginated<CancellationEvent> | CancellationEvent[]>('/admin/cancellations', query).then(asPage),
  excuses: (query: PageQuery & { status?: ExcuseStatus | '' }) =>
    api.get<Paginated<ExcuseQueueItem> | ExcuseQueueItem[]>('/admin/cancellations/excuses', query).then(asPage),
  review: (eventId: string, decision: 'approve' | 'reject', note: string) =>
    api.post<CancellationEvent>(`/admin/cancellations/${eventId}/review`, { decision, note }),
  stats: (query: CancellationStatsQuery) => api.get<CancellationStats>('/admin/cancellations/stats', query),
}

export type ReliabilityProfileQuery = PageQuery & { role?: ReliabilityRole | ''; level?: RestrictionLevel | ''; search?: string }

export const reliabilityProfiles = {
  list: (query: ReliabilityProfileQuery) =>
    api.get<Paginated<ReliabilityProfileListItem> | ReliabilityProfileListItem[]>('/admin/reliability-profiles', query).then(asPage),
  get: (userId: string, role: ReliabilityRole) => api.get<ReliabilityProfileDetail>(`/admin/reliability-profiles/${userId}`, { role }),
  adjust: (userId: string, input: ReliabilityAdjustInput) => api.post<ReliabilityProfileDetail>(`/admin/reliability-profiles/${userId}/adjust`, input),
}

// ---------------------------------------------------------------------------
// F15 — ratings, promotions, driver tiers, incentives (docs/10)
// ---------------------------------------------------------------------------

export const catalog = {
  cities: () => api.get<City[]>('/catalog/cities'),
  ratingTags: (target: RaterRole) => api.get<RatingTag[]>('/catalog/rating-tags', { target }),
}

export type RatingListQuery = DateRange &
  PageQuery & {
    raterRole?: RaterRole | ''
    stars?: number | ''
    flagged?: boolean | ''
    userId?: string
    /** Not in §F15.3 — sent for backends that support it; the page also filters the current page client-side. */
    tag?: string
    status?: RatingStatus | ''
    search?: string
  }

export const ratings = {
  list: (query: RatingListQuery) => api.get<Paginated<AdminRating> | AdminRating[]>('/admin/ratings', query).then(asPage),
  /** Audited as `rating.hide`; hiding recomputes the ratee's average. */
  hide: (id: string, reason: string) => api.post<AdminRating | undefined>(`/admin/ratings/${id}/hide`, { reason }),
  unhide: (id: string) => api.post<AdminRating | undefined>(`/admin/ratings/${id}/unhide`),
}

export const ratingFlags = {
  list: (query: PageQuery & { status?: RatingFlagStatus | ''; type?: RatingFlagType | '' }) =>
    api.get<Paginated<RatingFlag> | RatingFlag[]>('/admin/rating-flags', query).then(asPage),
  review: (id: string, action: RatingFlagReviewAction, note: string) =>
    api.post<RatingFlag | undefined>(`/admin/rating-flags/${id}/review`, { action, note }),
}

export type PromotionListQuery = PageQuery & { status?: PromotionListStatus | ''; search?: string }
export type RedemptionListQuery = PageQuery & { status?: RedemptionStatus | '' }

export const promotions = {
  list: (query: PromotionListQuery) =>
    api.get<Paginated<PromotionListItem> | PromotionListItem[]>('/admin/promotions', query).then(asPage),
  get: (id: string) => api.get<Promotion>(`/admin/promotions/${id}`),
  create: (input: PromotionInput) => api.post<Promotion>('/admin/promotions', input),
  /** `code` and `type` are locked after the first reservation (`409 conflict`). */
  update: (id: string, input: PromotionInput) => api.put<Promotion>(`/admin/promotions/${id}`, input),
  deactivate: (id: string) => api.post<Promotion | undefined>(`/admin/promotions/${id}/deactivate`),
  redemptions: (id: string, query: RedemptionListQuery) =>
    api.get<Paginated<PromotionRedemption> | PromotionRedemption[]>(`/admin/promotions/${id}/redemptions`, query).then(asPage),
  stats: (id: string) => api.get<PromotionStats>(`/admin/promotions/${id}/stats`),
  /**
   * Assumed endpoint (not in §F15.6): redemptions across all promotions, used for the dashboard's
   * "redemptions today" counter. Callers hide the card on 404/403.
   */
  allRedemptions: (query: DateRange & RedemptionListQuery) =>
    api.get<Paginated<PromotionRedemption> | PromotionRedemption[]>('/admin/promotion-redemptions', query).then(asPage),
}

export const driverTiers = {
  rules: () => api.get<Paginated<DriverTierRule> | DriverTierRule[]>('/admin/driver-tier-rules').then(unwrapList),
  updateRule: (id: string, input: DriverTierRuleInput) => api.put<DriverTierRule>(`/admin/driver-tier-rules/${id}`, input),
  /** `202 Accepted` — the weekly job runs now in the background. */
  recalculate: () => api.post<void>('/admin/driver-tiers/recalculate'),
  history: (driverId: string) =>
    api.get<Paginated<DriverTierHistoryEntry> | DriverTierHistoryEntry[]>(`/admin/drivers/${driverId}/tier-history`).then(unwrapList),
  /** Manual override until the next recalculation (audit `driver.tier_set`). */
  setTier: (driverId: string, tier: DriverTier, reason: string) => api.post<void>(`/admin/drivers/${driverId}/tier`, { tier, reason }),
}

export type IncentiveProgressQuery = PageQuery & { status?: IncentiveProgressStatus | '' }

export const incentives = {
  list: (query: PageQuery & { status?: string } = {}) =>
    api.get<Paginated<Incentive> | Incentive[]>('/admin/incentives', { ...ALL, ...query }).then(unwrapList),
  get: (id: string) => api.get<Incentive>(`/admin/incentives/${id}`),
  create: (input: IncentiveInput) => api.post<Incentive>('/admin/incentives', input),
  update: (id: string, input: IncentiveInput) => api.put<Incentive>(`/admin/incentives/${id}`, input),
  deactivate: (id: string) => api.post<Incentive | undefined>(`/admin/incentives/${id}/deactivate`),
  progress: (id: string, query: IncentiveProgressQuery) =>
    api.get<Paginated<IncentiveProgress> | IncentiveProgress[]>(`/admin/incentives/${id}/progress`, query).then(asPage),
  /** Only before payout (audit `incentive_progress.void`). */
  voidProgress: (progressId: string, reason: string) => api.post<void>(`/admin/incentive-progress/${progressId}/void`, { reason }),
  /** Assumed endpoint (not in §F15.10): a driver's progress across incentives; callers hide the section on 404. */
  forDriver: (driverId: string) =>
    api.get<Paginated<DriverIncentiveProgress> | DriverIncentiveProgress[]>(`/admin/drivers/${driverId}/incentives`).then(unwrapList),
}

// ---------------------------------------------------------------------------
// F16 — favorite driver discount rules and stats (docs/10 §F16.3, permission `favorites.manage`)
// ---------------------------------------------------------------------------

export type FavoriteStatsQuery = DateRange & { cityId?: string }

export const favorites = {
  rules: () =>
    api.get<Paginated<FavoriteDiscountRule> | FavoriteDiscountRule[]>('/admin/favorite-discount-rules', ALL).then(unwrapList),
  get: (id: string) => api.get<FavoriteDiscountRule>(`/admin/favorite-discount-rules/${id}`),
  /** Audited as `favorite_discount_rule.create`. */
  create: (input: FavoriteDiscountRuleInput) => api.post<FavoriteDiscountRule>('/admin/favorite-discount-rules', input),
  /** Audited as `favorite_discount_rule.update`; a full replacement body. */
  update: (id: string, input: FavoriteDiscountRuleInput) => api.put<FavoriteDiscountRule>(`/admin/favorite-discount-rules/${id}`, input),
  /** Audited as `favorite_discount_rule.delete` (`204`). */
  remove: (id: string) => api.delete<void>(`/admin/favorite-discount-rules/${id}`),
  stats: (query: FavoriteStatsQuery) => api.get<FavoriteStats>('/admin/favorites/stats', query),
}

// ---------------------------------------------------------------------------
// F17 — scheduled rides (permission `scheduling.manage`) and airports (permission `airport.manage`), docs/11 §F17.4/§F17.8
// ---------------------------------------------------------------------------

export type ScheduledTripQuery = DateRange &
  PageQuery & {
    reservation?: ScheduledReservationFilter | ''
    cityId?: string
    /** Not in §F17.4 — sent for backends that support them; the page also filters client-side when the rows carry the ids. */
    rideCategoryId?: string
    zoneId?: string
  }

export const scheduledRules = {
  list: () =>
    api.get<Paginated<ScheduledRule> | ScheduledRule[]>('/admin/scheduled-ride-rules', ALL).then(unwrapList),
  /** Audited as `scheduled_ride_rule.create`. */
  create: (input: ScheduledRuleInput) => api.post<ScheduledRule>('/admin/scheduled-ride-rules', input),
  /** Audited as `scheduled_ride_rule.update`; a full replacement body. */
  update: (id: string, input: ScheduledRuleInput) => api.put<ScheduledRule>(`/admin/scheduled-ride-rules/${id}`, input),
  /** Audited as `scheduled_ride_rule.delete` (`204`). */
  remove: (id: string) => api.delete<void>(`/admin/scheduled-ride-rules/${id}`),
}

export const scheduledTrips = {
  list: (query: ScheduledTripQuery) =>
    api.get<Paginated<ScheduledTripRow> | ScheduledTripRow[]>('/admin/scheduled-trips', query).then(asPage),
  /** Manual reservation (`source=admin`); bypasses the marketplace, subject to the overlap rules. Audited `scheduled_trip.assign`. */
  assign: (tripId: string, driverId: string) => api.post<unknown>(`/admin/scheduled-trips/${tripId}/assign`, { driverId }),
  /** Releases the active reservation without penalty points. Audited `scheduled_trip.release`. */
  releaseReservation: (tripId: string, reason: string) => api.post<unknown>(`/admin/scheduled-trips/${tripId}/release-reservation`, { reason }),
  stats: (query: DateRange) => api.get<SchedulingStats>('/admin/scheduling/stats', query),
}

export const airports = {
  list: () => api.get<Paginated<Airport> | Airport[]>('/admin/airports', ALL).then(unwrapList),
  get: (id: string) => api.get<Airport>(`/admin/airports/${id}`),
  /** Audited as `airport.create`. */
  create: (input: AirportInput) => api.post<Airport>('/admin/airports', input),
  /** Audited as `airport.update`; a full replacement body. */
  update: (id: string, input: AirportInput) => api.put<Airport>(`/admin/airports/${id}`, input),
  /** Audited as `airport.delete`. */
  remove: (id: string) => api.delete<void>(`/admin/airports/${id}`),
  zones: (airportId: string) => api.get<Paginated<AirportZone> | AirportZone[]>(`/admin/airports/${airportId}/zones`, ALL).then(unwrapList),
  createZone: (airportId: string, input: AirportZoneInput) => api.post<AirportZone>(`/admin/airports/${airportId}/zones`, input),
  updateZone: (airportId: string, zoneId: string, input: AirportZoneInput) =>
    api.put<AirportZone>(`/admin/airports/${airportId}/zones/${zoneId}`, input),
  removeZone: (airportId: string, zoneId: string) => api.delete<void>(`/admin/airports/${airportId}/zones/${zoneId}`),
  /** FIFO queue (by `enteredAt`), live. */
  queue: (airportId: string) => api.get<AirportQueueEntry[] | Paginated<AirportQueueEntry>>(`/admin/airports/${airportId}/queue`).then(unwrapList),
  /** Audited as `airport_queue.remove`; the body carries the reason. */
  removeFromQueue: (airportId: string, entryId: string, reason: string) =>
    api.delete<void>(`/admin/airports/${airportId}/queue/${entryId}`, { reason }),
}
