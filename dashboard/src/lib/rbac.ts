import type { TranslationKey } from '../i18n'
import type { AdminLoginResponse, AdminUser, AuthResponse, MfaChallenge, MfaEnrollmentChallenge, Role } from './types'

/**
 * Permission catalogue (docs/12 §F20.2 — the single reference). The server syncs the same list into `permissions`
 * and remains the authority; the dashboard only uses it to hide navigation and buttons and as the role matrix
 * fallback when `GET /admin/permissions` is unavailable.
 */
export const PERMISSION_CATALOG = [
  { code: 'dashboard.view', module: 'dashboard' },
  { code: 'drivers.view', module: 'drivers' },
  { code: 'drivers.review', module: 'drivers' },
  { code: 'passengers.view', module: 'passengers' },
  { code: 'users.suspend', module: 'users' },
  { code: 'catalog.manage', module: 'catalog' },
  { code: 'trips.view', module: 'trips' },
  { code: 'trips.cancel', module: 'trips' },
  { code: 'live.view', module: 'trips' },
  { code: 'pricing.view', module: 'pricing' },
  { code: 'pricing.edit', module: 'pricing' },
  { code: 'matching.edit', module: 'matching' },
  { code: 'payments.view', module: 'payments' },
  { code: 'payments.refund', module: 'payments' },
  { code: 'payments.refund_approve', module: 'payments' },
  { code: 'payouts.approve', module: 'payments' },
  { code: 'settlements.manage', module: 'payments' },
  { code: 'wallets.adjust', module: 'payments' },
  { code: 'notifications.view', module: 'notifications' },
  { code: 'notifications.manage', module: 'notifications' },
  { code: 'notifications.sms_broadcast', module: 'notifications' },
  { code: 'safety.manage', module: 'safety' },
  { code: 'cancellation.manage', module: 'cancellation' },
  { code: 'cancellation.review', module: 'cancellation' },
  { code: 'reliability.manage', module: 'cancellation' },
  { code: 'ratings.manage', module: 'ratings' },
  { code: 'promotions.manage', module: 'promotions' },
  { code: 'incentives.manage', module: 'incentives' },
  { code: 'favorites.manage', module: 'favorites' },
  { code: 'scheduling.manage', module: 'scheduling' },
  { code: 'airport.manage', module: 'airport' },
  { code: 'support.view', module: 'support' },
  { code: 'support.manage', module: 'support' },
  { code: 'support.disputes', module: 'support' },
  { code: 'help.manage', module: 'support' },
  { code: 'corporate.manage', module: 'corporate' },
  { code: 'reports.view', module: 'reports' },
  { code: 'reports.export', module: 'reports' },
  { code: 'admin.users.manage', module: 'admin' },
  { code: 'admin.roles.manage', module: 'admin' },
  { code: 'audit.view', module: 'admin' },
] as const

export type PermissionCode = (typeof PERMISSION_CATALOG)[number]['code']

/** `*` grants every permission (`super_admin`). */
export const ALL_PERMISSIONS = '*'

/** Module headings of the role matrix; unknown modules fall back to the raw code. */
export const MODULE_KEY: Record<string, TranslationKey> = {
  dashboard: 'rbModDashboard',
  drivers: 'rbModDrivers',
  passengers: 'rbModPassengers',
  users: 'rbModUsers',
  catalog: 'rbModCatalog',
  trips: 'rbModTrips',
  pricing: 'rbModPricing',
  matching: 'rbModMatching',
  payments: 'rbModPayments',
  notifications: 'rbModNotifications',
  safety: 'rbModSafety',
  cancellation: 'rbModCancellation',
  ratings: 'rbModRatings',
  promotions: 'rbModPromotions',
  incentives: 'rbModIncentives',
  favorites: 'rbModFavorites',
  scheduling: 'rbModScheduling',
  airport: 'rbModAirport',
  support: 'rbModSupport',
  corporate: 'rbModCorporate',
  reports: 'rbModReports',
  admin: 'rbModAdmin',
}

/**
 * `null` permissions mean "unknown" (e.g. `/admin/me` unavailable on an older backend): nothing is hidden and the
 * server-side `403` + `PermissionError` stays the guard. `required` as an array means "any of".
 */
export function hasPermission(permissions: readonly string[] | null, required: PermissionCode | readonly PermissionCode[] | undefined): boolean {
  if (!required || permissions === null) return true
  if (permissions.includes(ALL_PERMISSIONS)) return true
  const list: readonly PermissionCode[] = typeof required === 'string' ? [required] : required
  if (list.length === 0) return true
  return list.some((code) => permissions.includes(code))
}

/** Groups catalogue rows by module, keeping the first-seen module order (the server sorts by `sort_order`). */
export function groupByModule<T extends { module: string }>(rows: readonly T[]): { module: string; items: T[] }[] {
  const groups = new Map<string, T[]>()
  for (const row of rows) {
    const list = groups.get(row.module)
    if (list) list.push(row)
    else groups.set(row.module, [row])
  }
  return Array.from(groups, ([module, items]) => ({ module, items }))
}

// ---------------------------------------------------------------------------
// Login responses (§F20.5)
// ---------------------------------------------------------------------------

export function isMfaChallenge(response: AdminLoginResponse): response is MfaChallenge {
  return 'mfaRequired' in response && response.mfaRequired === true
}

export function isEnrollmentChallenge(response: AdminLoginResponse): response is MfaEnrollmentChallenge {
  return 'mfaEnrollmentRequired' in response && response.mfaEnrollmentRequired === true
}

export function isAuthResponse(response: AdminLoginResponse): response is AuthResponse {
  return 'accessToken' in response && typeof response.accessToken === 'string'
}

/** Six digits (TOTP, RFC 6238 — §F20.4). */
export const TOTP_LENGTH = 6

/** Recovery codes are `xxxx-xxxx` lower-case base32; input is normalised before sending. */
export function normalizeRecoveryCode(value: string) {
  const compact = value.trim().toLowerCase().replace(/[^a-z0-9]/g, '')
  return compact.length === 8 ? `${compact.slice(0, 4)}-${compact.slice(4)}` : value.trim().toLowerCase()
}

/** Seconds left from a `429` answer (`details.retryAfterSeconds`, §F20.4); null when absent. */
export function retryAfterOf(details: Record<string, unknown> | undefined): number | null {
  const raw = details?.retryAfterSeconds ?? details?.retryAfter
  const value = typeof raw === 'string' ? Number(raw) : raw
  return typeof value === 'number' && Number.isFinite(value) && value > 0 ? Math.ceil(value) : null
}

/** `m:ss` countdown label. */
export function formatCountdownSeconds(seconds: number) {
  const safe = Math.max(0, Math.ceil(seconds))
  const minutes = Math.floor(safe / 60)
  return `${minutes}:${String(safe % 60).padStart(2, '0')}`
}

// ---------------------------------------------------------------------------
// Password policy (§F20.4: ≥ 12 chars, upper, lower, digit, symbol)
// ---------------------------------------------------------------------------

export const PASSWORD_MIN_LENGTH = 12

export type PasswordRule = 'min_length' | 'uppercase' | 'lowercase' | 'digit' | 'symbol'

export const PASSWORD_RULES: { rule: PasswordRule; key: TranslationKey; test: (value: string) => boolean }[] = [
  { rule: 'min_length', key: 'pwRuleLength', test: (value) => value.length >= PASSWORD_MIN_LENGTH },
  { rule: 'uppercase', key: 'pwRuleUpper', test: (value) => /[A-Z]/.test(value) },
  { rule: 'lowercase', key: 'pwRuleLower', test: (value) => /[a-z]/.test(value) },
  { rule: 'digit', key: 'pwRuleDigit', test: (value) => /\d/.test(value) },
  { rule: 'symbol', key: 'pwRuleSymbol', test: (value) => /[^A-Za-z0-9\s]/.test(value) },
]

export function passwordMeetsPolicy(value: string) {
  return PASSWORD_RULES.every((rule) => rule.test(value))
}

/** Aliases the server may use in `password_policy_violation.details.rules` (not fixed by the doc). */
const SERVER_RULE_ALIASES: Record<string, PasswordRule> = {
  min_length: 'min_length',
  minlength: 'min_length',
  length: 'min_length',
  too_short: 'min_length',
  uppercase: 'uppercase',
  upper: 'uppercase',
  requires_upper: 'uppercase',
  lowercase: 'lowercase',
  lower: 'lowercase',
  requires_lower: 'lowercase',
  digit: 'digit',
  digits: 'digit',
  number: 'digit',
  requires_digit: 'digit',
  symbol: 'symbol',
  symbols: 'symbol',
  special: 'symbol',
  non_alphanumeric: 'symbol',
  requires_symbol: 'symbol',
}

const EXTRA_RULE_KEY: Record<string, TranslationKey> = {
  reused: 'pwRuleNotReused',
  not_reused: 'pwRuleNotReused',
  same_as_current: 'pwRuleNotReused',
  contains_username: 'pwRuleNoUsername',
  not_username: 'pwRuleNoUsername',
}

/** Server rule → translation key; unknown rules are shown as their raw code. */
export function serverRuleKey(rule: string): TranslationKey | null {
  // The server may append the parameter (`min_length:12`).
  const normalized = rule.trim().toLowerCase().split(':')[0].replace(/[\s-]+/g, '_')
  const alias = SERVER_RULE_ALIASES[normalized]
  if (alias) return PASSWORD_RULES.find((entry) => entry.rule === alias)?.key ?? null
  return EXTRA_RULE_KEY[normalized] ?? null
}

/** `details.rules` of a `422 password_policy_violation` (strings, or objects with `code`/`rule`/`message`). */
export function violatedRules(details: Record<string, unknown> | undefined): string[] {
  const raw = details?.rules
  if (!Array.isArray(raw)) return []
  return raw
    .map((entry: unknown) => {
      if (typeof entry === 'string') return entry
      if (entry && typeof entry === 'object') {
        const record = entry as Record<string, unknown>
        const value = record.code ?? record.rule ?? record.message
        return typeof value === 'string' ? value : ''
      }
      return ''
    })
    .filter(Boolean)
}

/** Random policy-compliant temporary password (Web Crypto); the server may also generate one when omitted. */
export function generateTemporaryPassword(length = 16) {
  const sets = ['ABCDEFGHJKLMNPQRSTUVWXYZ', 'abcdefghijkmnopqrstuvwxyz', '23456789', '!@#$%^&*-_=+?']
  const all = sets.join('')
  const random = (max: number) => {
    const buffer = new Uint32Array(1)
    crypto.getRandomValues(buffer)
    return buffer[0] % max
  }
  const chars = sets.map((set) => set[random(set.length)])
  while (chars.length < length) chars.push(all[random(all.length)])
  for (let index = chars.length - 1; index > 0; index -= 1) {
    const swap = random(index + 1)
    ;[chars[index], chars[swap]] = [chars[swap], chars[index]]
  }
  return chars.join('')
}

/** `409 conflict { reason }` guards of the admin-user actions (§F20.3). */
export function conflictReasonKey(details: Record<string, unknown> | undefined): TranslationKey | null {
  const reason = typeof details?.reason === 'string' ? details.reason : ''
  if (reason === 'last_super_admin') return 'auErrLastSuperAdmin'
  if (reason === 'self_disable' || reason === 'cannot_disable_self' || reason === 'self') return 'auErrSelfDisable'
  if (reason === 'system_role') return 'roErrSystemRole'
  if (reason === 'role_in_use' || reason === 'has_users') return 'roErrInUse'
  return null
}

/** Readable user-agent summary for session lists ("Chrome · Windows"). */
export function describeUserAgent(userAgent: string | null | undefined): string {
  if (!userAgent) return '—'
  const browser = /Edg\//.test(userAgent)
    ? 'Edge'
    : /OPR\//.test(userAgent)
      ? 'Opera'
      : /Firefox\//.test(userAgent)
        ? 'Firefox'
        : /Chrome\//.test(userAgent)
          ? 'Chrome'
          : /Safari\//.test(userAgent)
            ? 'Safari'
            : ''
  const os = /Windows/.test(userAgent)
    ? 'Windows'
    : /Android/.test(userAgent)
      ? 'Android'
      : /iPhone|iPad|iOS/.test(userAgent)
        ? 'iOS'
        : /Mac OS X|Macintosh/.test(userAgent)
          ? 'macOS'
          : /Linux/.test(userAgent)
            ? 'Linux'
            : ''
  const summary = [browser, os].filter(Boolean).join(' · ')
  return summary || userAgent.slice(0, 60)
}

/** Recovery codes as a downloadable text file (one code per line). */
export function recoveryCodesText(codes: readonly string[], heading: string) {
  return `${heading}\n${new Date().toISOString()}\n\n${codes.join('\n')}\n`
}

/** Clipboard write with a hidden-textarea fallback (non-secure contexts); resolves false when both fail. */
export async function copyText(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text)
    return true
  } catch {
    try {
      const area = document.createElement('textarea')
      area.value = text
      area.setAttribute('readonly', '')
      area.style.position = 'fixed'
      area.style.opacity = '0'
      document.body.appendChild(area)
      area.select()
      const ok = document.execCommand('copy')
      area.remove()
      return ok
    } catch {
      return false
    }
  }
}

/** Base32 secret in groups of four for manual entry. */
export function groupSecret(secret: string) {
  return secret.replace(/\s+/g, '').replace(/(.{4})/g, '$1 ').trim()
}

/** Role name in the current language (falls back to the localised `name`, then the code). */
export function roleLabel(role: Pick<Role, 'nameAr' | 'nameEn' | 'code'> & { name?: string | null }, lang: 'ar' | 'en') {
  return (lang === 'en' ? role.nameEn : role.nameAr) || role.name || role.code
}

/** `lockedUntil` in the future (5 wrong passwords → `Admin:LockoutMinutes`, §F20.4). */
export function isLocked(user: Pick<AdminUser, 'lockedUntil'>, now = Date.now()) {
  if (!user.lockedUntil) return false
  const until = Date.parse(user.lockedUntil)
  return Number.isFinite(until) && until > now
}
