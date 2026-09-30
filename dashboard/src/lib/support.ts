import type { TranslationKey } from '../i18n'
import type {
  CannedResponse,
  DisputeReason,
  DisputeResolution,
  DisputeStatus,
  FareDispute,
  HelpAudience,
  SlaPolicy,
  SupportTicketDetail,
  TicketChannel,
  TicketListItem,
  TicketPriority,
  TicketRequesterRole,
  TicketSlaState,
  TicketStatus,
  TicketType,
} from './types'

/** Fallback polling cadence of the support feed while the SignalR hub is not connected. */
export const SUPPORT_POLL_INTERVAL_MS = 15_000

/** `slaState = due_soon` means "due within 30 minutes" (docs/11 §F18.3). */
export const DUE_SOON_MINUTES = 30

export const TICKET_STATUSES: TicketStatus[] = ['open', 'in_progress', 'pending_user', 'resolved', 'closed']
export const TICKET_PRIORITIES: TicketPriority[] = ['urgent', 'high', 'normal', 'low']
export const TICKET_TYPES: TicketType[] = ['trip_issue', 'payment_issue', 'lost_item', 'safety', 'account', 'other']
export const TICKET_CHANNELS: TicketChannel[] = ['app', 'website', 'dashboard', 'phone']
export const TICKET_SLA_FILTERS: Exclude<TicketSlaState, 'ok'>[] = ['breached', 'due_soon']
export const QUEUE_TABS = ['all', 'mine', 'unassigned', 'pending_user'] as const
export type QueueTab = (typeof QUEUE_TABS)[number]

export const DISPUTE_STATUSES: DisputeStatus[] = ['open', 'under_review', 'approved', 'partially_approved', 'rejected']
export const DISPUTE_REASONS: DisputeReason[] = ['overcharged', 'route_longer', 'waiting_charged', 'cancellation_fee', 'promo_not_applied', 'other']
export const DISPUTE_RESOLUTIONS: DisputeResolution[] = ['refund_full', 'refund_partial', 'no_refund']
export const HELP_AUDIENCES: HelpAudience[] = ['passenger', 'driver', 'all']

/** Priorities in the order of the SLA policies editor (most urgent first). */
export const SLA_PRIORITIES: TicketPriority[] = ['urgent', 'high', 'normal', 'low']

export const TICKET_TYPE_KEY: Record<TicketType, TranslationKey> = {
  trip_issue: 'spTypeTripIssue',
  payment_issue: 'spTypePaymentIssue',
  lost_item: 'spTypeLostItem',
  safety: 'spTypeSafety',
  account: 'spTypeAccount',
  other: 'spTypeOther',
}

export const TICKET_CHANNEL_KEY: Record<TicketChannel, TranslationKey> = {
  app: 'spChannelApp',
  website: 'spChannelWebsite',
  dashboard: 'spChannelDashboard',
  phone: 'spChannelPhone',
}

export const REQUESTER_ROLE_KEY: Record<TicketRequesterRole, TranslationKey> = {
  passenger: 'actorPassenger',
  driver: 'actorDriver',
  corporate_admin: 'spRoleCorporateAdmin',
}

export const DISPUTE_REASON_KEY: Record<DisputeReason, TranslationKey> = {
  overcharged: 'spReasonOvercharged',
  route_longer: 'spReasonRouteLonger',
  waiting_charged: 'spReasonWaitingCharged',
  cancellation_fee: 'spReasonCancellationFee',
  promo_not_applied: 'spReasonPromoNotApplied',
  other: 'spReasonOther',
}

export const DISPUTE_RESOLUTION_KEY: Record<DisputeResolution, TranslationKey> = {
  refund_full: 'spResolutionFull',
  refund_partial: 'spResolutionPartial',
  no_refund: 'spResolutionNone',
}

export const AUDIENCE_KEY: Record<HelpAudience, TranslationKey> = {
  passenger: 'hcAudiencePassenger',
  driver: 'hcAudienceDriver',
  all: 'hcAudienceAll',
}

/** Default priority per ticket type (§F18.2); the server applies it on creation, the dashboard only uses it as a hint. */
export const DEFAULT_PRIORITY_BY_TYPE: Record<TicketType, TicketPriority> = {
  safety: 'urgent',
  payment_issue: 'high',
  trip_issue: 'normal',
  lost_item: 'normal',
  account: 'normal',
  other: 'normal',
}

/** Default SLA targets from the seed (§ Seed: minutes for first response / resolution). */
export const DEFAULT_SLA: Record<TicketPriority, { firstResponseMinutes: number; resolutionMinutes: number }> = {
  urgent: { firstResponseMinutes: 15, resolutionMinutes: 240 },
  high: { firstResponseMinutes: 60, resolutionMinutes: 1440 },
  normal: { firstResponseMinutes: 240, resolutionMinutes: 2880 },
  low: { firstResponseMinutes: 1440, resolutionMinutes: 4320 },
}

/**
 * Status transitions an agent may trigger (§F18.2). `closed` is final (a reply answers `409 ticket_closed`), `resolved` may be
 * reopened to `in_progress`. `open` is only ever an initial state, so it is never a target.
 */
export const NEXT_STATUSES: Record<TicketStatus, Exclude<TicketStatus, 'open'>[]> = {
  open: ['in_progress', 'pending_user', 'resolved', 'closed'],
  in_progress: ['pending_user', 'resolved', 'closed'],
  pending_user: ['in_progress', 'resolved', 'closed'],
  resolved: ['in_progress', 'closed'],
  closed: [],
}

export function isActiveTicket(status: TicketStatus) {
  return status !== 'resolved' && status !== 'closed'
}

const PRIORITY_RANK: Record<TicketPriority, number> = { urgent: 0, high: 1, normal: 2, low: 3 }
const SLA_RANK: Record<TicketSlaState, number> = { breached: 0, due_soon: 1, ok: 2 }

export function ticketPriorityRank(priority: TicketPriority) {
  return PRIORITY_RANK[priority] ?? 9
}

/** Queue order: active tickets first, then breached → due soon → ok, then priority, then the earliest resolution deadline. */
export function sortTickets<T extends Pick<TicketListItem, 'status' | 'slaState' | 'priority' | 'resolutionDueAt'>>(tickets: T[]): T[] {
  return [...tickets].sort((a, b) => {
    const active = Number(isActiveTicket(b.status)) - Number(isActiveTicket(a.status))
    if (active !== 0) return active
    if (isActiveTicket(a.status)) {
      const sla = (SLA_RANK[a.slaState] ?? 3) - (SLA_RANK[b.slaState] ?? 3)
      if (sla !== 0) return sla
    }
    const priority = ticketPriorityRank(a.priority) - ticketPriorityRank(b.priority)
    if (priority !== 0) return priority
    return new Date(a.resolutionDueAt).getTime() - new Date(b.resolutionDueAt).getTime()
  })
}

/** A user message that still waits for an agent: the queue highlights these rows. */
export function awaitsAgent(ticket: Pick<TicketListItem, 'status' | 'lastMessageBy'>) {
  return isActiveTicket(ticket.status) && ticket.status !== 'pending_user' && ticket.lastMessageBy === 'user'
}

// ---------------------------------------------------------------------------
// SLA clocks (§F18.2: paused while `pending_user`, resolved tickets stop counting)
// ---------------------------------------------------------------------------

export interface SlaClock {
  /** Milliseconds until the deadline; negative once breached. */
  remainingMs: number
  breached: boolean
  dueSoon: boolean
  /** True while the clock is frozen (`pending_user`). */
  paused: boolean
}

/** `pausedAt` freezes the clock at that instant (the stored due date only absorbs the pause once the user replies). */
export function slaClock(dueAt: string, now: number, pausedAt: string | null = null): SlaClock {
  const due = new Date(dueAt).getTime()
  const pausedMs = pausedAt ? new Date(pausedAt).getTime() : Number.NaN
  const paused = Number.isFinite(pausedMs)
  const reference = paused ? pausedMs : now
  const remainingMs = Number.isFinite(due) ? due - reference : 0
  return {
    remainingMs,
    breached: remainingMs < 0,
    dueSoon: remainingMs >= 0 && remainingMs <= DUE_SOON_MINUTES * 60_000,
    paused,
  }
}

export interface SpanUnits {
  day: string
  hour: string
  minute: string
}

/** `2h 15m` / `3d 4h` / `45m`, with a leading minus for overdue spans. Seconds are intentionally dropped. */
export function formatSpan(ms: number, units: SpanUnits) {
  if (!Number.isFinite(ms)) return '—'
  const sign = ms < 0 ? '-' : ''
  const totalMinutes = Math.floor(Math.abs(ms) / 60_000)
  const d = Math.floor(totalMinutes / 1440)
  const h = Math.floor((totalMinutes % 1440) / 60)
  const m = totalMinutes % 60
  if (d > 0) return `${sign}${d}${units.day} ${h}${units.hour}`
  if (h > 0) return `${sign}${h}${units.hour} ${m}${units.minute}`
  return `${sign}${m}${units.minute}`
}

/** Minutes → `45m` / `2h 15m` / `3d 4h` text for policy tables and KPI cards. */
export function formatMinuteSpan(minutes: number | null | undefined, units: SpanUnits) {
  if (typeof minutes !== 'number' || !Number.isFinite(minutes)) return '—'
  return formatSpan(minutes * 60_000, units)
}

/** The deadline that matters now: first response until an agent answered, then resolution. */
export function nextDeadline(ticket: Pick<TicketListItem, 'firstResponseAt' | 'firstResponseDueAt' | 'resolutionDueAt' | 'status'>): { kind: 'firstResponse' | 'resolution'; dueAt: string } {
  if (!ticket.firstResponseAt && ticket.firstResponseDueAt && ticket.status === 'open') return { kind: 'firstResponse', dueAt: ticket.firstResponseDueAt }
  return { kind: 'resolution', dueAt: ticket.resolutionDueAt }
}

// ---------------------------------------------------------------------------
// Canned responses
// ---------------------------------------------------------------------------

export const CANNED_PLACEHOLDERS = ['userName', 'ticketNumber', 'tripNumber'] as const
export type CannedPlaceholder = (typeof CANNED_PLACEHOLDERS)[number]

/** Replaces `{userName}` `{ticketNumber}` `{tripNumber}`; a missing value becomes empty, other braces stay untouched. */
export function renderCanned(body: string, values: Partial<Record<CannedPlaceholder, string | null | undefined>>) {
  return body.replace(/\{(userName|ticketNumber|tripNumber)\}/g, (_, key: CannedPlaceholder) => values[key] ?? '')
}

/** Canned responses offered for a ticket: active ones that are generic or target its type. */
export function cannedFor(responses: CannedResponse[], type: TicketType) {
  return responses.filter((response) => response.isActive && (response.ticketType === null || response.ticketType === type))
}

export const CANNED_CODE_PATTERN = /^[a-z0-9_]{1,40}$/

/** Placeholders present in a body that are not among the supported three. */
export function unknownPlaceholders(body: string) {
  const found = body.match(/\{[^{}\s]*\}/g) ?? []
  return [...new Set(found.filter((token) => !(CANNED_PLACEHOLDERS as readonly string[]).includes(token.slice(1, -1))))]
}

// ---------------------------------------------------------------------------
// Disputes
// ---------------------------------------------------------------------------

export function isOpenDispute(status: DisputeStatus) {
  return status === 'open' || status === 'under_review'
}

/** Parses an amount typed in the resolve form; `null` when it is not a positive number with at most 2 decimals. */
export function parseMoneyInput(value: string): number | null {
  const normalised = value.trim().replace(/,/g, '.')
  if (!/^\d+(\.\d{1,2})?$/.test(normalised)) return null
  const amount = Number(normalised)
  return amount > 0 ? amount : null
}

export interface DisputeFormErrors {
  amount?: 'invalid' | 'exceeds'
  note?: 'required' | 'tooLong'
}

/** §F18.2: `refund_partial` needs `0 < amount ≤ charged_amount`; the note is required (≤ 1000 chars). */
export function validateDisputeResolution(
  resolution: DisputeResolution,
  amountText: string,
  note: string,
  chargedAmount: number,
): { errors: DisputeFormErrors; amount: number | undefined } {
  const errors: DisputeFormErrors = {}
  let amount: number | undefined
  if (resolution === 'refund_partial') {
    const parsed = parseMoneyInput(amountText)
    if (parsed === null) errors.amount = 'invalid'
    else if (parsed > chargedAmount) errors.amount = 'exceeds'
    else amount = parsed
  }
  const trimmed = note.trim()
  if (!trimmed) errors.note = 'required'
  else if (trimmed.length > 1000) errors.note = 'tooLong'
  return { errors, amount }
}

/** The refund state of a dispute across the list / detail shapes (`refund` object or flat `refundId` + `refundStatus`). */
export function disputeRefundOf(dispute: Pick<FareDispute, 'refund' | 'refundId' | 'refundNumber' | 'refundStatus'>) {
  if (dispute.refund) return { id: dispute.refund.id, refundNumber: dispute.refund.refundNumber ?? null, status: dispute.refund.status as string | null }
  if (dispute.refundId) return { id: dispute.refundId, refundNumber: dispute.refundNumber ?? null, status: (dispute.refundStatus ?? null) as string | null }
  return null
}

// ---------------------------------------------------------------------------
// SLA policies
// ---------------------------------------------------------------------------

export interface SlaPolicyErrors {
  firstResponseMinutes?: 'invalid'
  resolutionMinutes?: 'invalid' | 'lessThanFirst'
}

/** Largest accepted target: one year in minutes (backend range 1–525600). */
export const MAX_SLA_MINUTES = 525_600

/** Both targets are whole minutes in 1–525600 and the resolution target cannot be shorter than the first-response one. */
export function validateSlaPolicy(first: string, resolution: string): SlaPolicyErrors {
  const errors: SlaPolicyErrors = {}
  const f = Number(first)
  const r = Number(resolution)
  const validF = first.trim() !== '' && Number.isInteger(f) && f >= 1 && f <= MAX_SLA_MINUTES
  const validR = resolution.trim() !== '' && Number.isInteger(r) && r >= 1 && r <= MAX_SLA_MINUTES
  if (!validF) errors.firstResponseMinutes = 'invalid'
  if (!validR) errors.resolutionMinutes = 'invalid'
  else if (validF && r < f) errors.resolutionMinutes = 'lessThanFirst'
  return errors
}

/** Orders the policies of the four priorities and fills gaps with the seed defaults so the editor always shows four rows. */
export function completePolicies(policies: SlaPolicy[]): SlaPolicy[] {
  return SLA_PRIORITIES.map((priority) => policies.find((policy) => policy.priority === priority) ?? { priority, ...DEFAULT_SLA[priority] })
}

// ---------------------------------------------------------------------------
// Help center
// ---------------------------------------------------------------------------

export const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

/** `How do I schedule a ride?` → `how-do-i-schedule-a-ride` (≤ 120 chars). */
export function slugify(value: string) {
  return value
    .toLowerCase()
    .normalize('NFKD')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 120)
    .replace(/-+$/g, '')
}

export function parseTags(value: string): string[] {
  return [...new Set(value.split(/[,،]/).map((tag) => tag.trim()).filter(Boolean))]
}

/** Share of "helpful" votes, or `null` without votes. */
export function helpfulRate(yes: number, no: number): number | null {
  const total = yes + no
  return total > 0 ? yes / total : null
}

// ---------------------------------------------------------------------------
// Per-user ticket lists (driver / passenger detail)
// ---------------------------------------------------------------------------

/**
 * The admin list has no documented per-requester filter, so the user cards ask with an assumed `requesterUserId` plus the phone / name as
 * `search` (which the API does document) and keep only rows that belong to the user: matched by `requesterUserId` when the row carries
 * it, otherwise by the requester's display name (full name, or the phone number when the user has no name).
 */
export function ticketsOfUser(items: TicketListItem[], user: { userId: string; name?: string | null; phone?: string | null }) {
  return items.filter((item) => {
    if (item.requesterUserId) return item.requesterUserId === user.userId
    if (!item.requesterName) return false
    return item.requesterName === user.name || item.requesterName === user.phone
  })
}

/** Linked-case helpers for the ticket detail: `linked.*` (backend) with the flat §F18.1 ids as a fallback. */
export function linkedSafetyCase(detail: Pick<SupportTicketDetail, 'safetyCaseId' | 'linked'>) {
  const id = detail.linked?.safetyCase?.id ?? detail.safetyCaseId ?? null
  return id ? { id, number: detail.linked?.safetyCase?.number ?? null, status: detail.linked?.safetyCase?.status ?? null } : null
}

export function linkedLostItem(detail: Pick<SupportTicketDetail, 'lostItemReportId' | 'linked'>) {
  const id = detail.linked?.lostItemReport?.id ?? detail.lostItemReportId ?? null
  return id ? { id, number: detail.linked?.lostItemReport?.number ?? null, status: detail.linked?.lostItemReport?.status ?? null } : null
}
