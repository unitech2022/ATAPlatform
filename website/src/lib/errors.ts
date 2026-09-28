import type { I18nContextValue } from '../i18n'
import { isApiError } from './api'

/** Human-readable, localized message for any thrown value. */
export function describeError(error: unknown, t: I18nContextValue['t']): string {
  if (isApiError(error)) {
    if (error.code === 'network_error') return t('error.network')
    if (error.code === 'rate_limited') {
      const seconds = error.detailNumber('retryAfterSeconds')
      if (seconds !== null) return t('login.rateLimited', { n: seconds })
    }
    return error.message || t('error.generic')
  }
  return t('error.generic')
}

/**
 * Per-field messages from a `validation_failed` envelope. The API returns a
 * flat `{ field: "code" }` map in `details`; nested `details.errors` /
 * `details.fields` (`{ field: "msg" | ["msg"] }`) are accepted too and keys
 * are camelCased.
 */
export function fieldErrorsFrom(error: unknown): Record<string, string> {
  if (!isApiError(error)) return {}
  const source = error.details.errors ?? error.details.fields ?? error.details
  if (typeof source !== 'object' || source === null) return {}
  const result: Record<string, string> = {}
  for (const [key, value] of Object.entries(source as Record<string, unknown>)) {
    const name = key.charAt(0).toLowerCase() + key.slice(1)
    if (typeof value === 'string') result[name] = value
    else if (Array.isArray(value)) {
      const first = value.find((item): item is string => typeof item === 'string')
      if (first) result[name] = first
    }
  }
  return result
}
