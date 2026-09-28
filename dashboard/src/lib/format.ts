import type { Lang } from '../i18n'

const dateFormatters: Record<Lang, Intl.DateTimeFormat> = {
  ar: new Intl.DateTimeFormat('ar-SA-u-nu-latn-ca-gregory', { dateStyle: 'medium' }),
  en: new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium' }),
}

const dateTimeFormatters: Record<Lang, Intl.DateTimeFormat> = {
  ar: new Intl.DateTimeFormat('ar-SA-u-nu-latn-ca-gregory', { dateStyle: 'medium', timeStyle: 'short' }),
  en: new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short' }),
}

const numberFormatter = new Intl.NumberFormat('en-US')

function toDate(value: string | null | undefined) {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

export function formatDate(value: string | null | undefined, lang: Lang) {
  const date = toDate(value)
  return date ? dateFormatters[lang].format(date) : '—'
}

export function formatDateTime(value: string | null | undefined, lang: Lang) {
  const date = toDate(value)
  return date ? dateTimeFormatters[lang].format(date) : '—'
}

export function formatNumber(value: number | null | undefined) {
  return typeof value === 'number' ? numberFormatter.format(value) : '—'
}

export function formatJson(value: unknown) {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'string') {
    try {
      return JSON.stringify(JSON.parse(value), null, 2)
    } catch {
      return value
    }
  }
  return JSON.stringify(value, null, 2)
}

const moneyFormatter = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const oneDecimalFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 })

/** Amount without a currency symbol; pages append `t('sar')` so the unit follows the language. */
export function formatMoney(value: number | null | undefined) {
  return typeof value === 'number' ? moneyFormatter.format(value) : '—'
}

/** Metres → kilometres with one decimal (unit appended by the caller). */
export function formatKm(meters: number | null | undefined) {
  return typeof meters === 'number' ? oneDecimalFormatter.format(meters / 1000) : '—'
}

/** Seconds → whole minutes (unit appended by the caller). */
export function formatMinutes(seconds: number | null | undefined) {
  return typeof seconds === 'number' ? numberFormatter.format(Math.round(seconds / 60)) : '—'
}

const timeFormatters: Record<Lang, Intl.DateTimeFormat> = {
  ar: new Intl.DateTimeFormat('ar-SA-u-nu-latn-ca-gregory', { timeStyle: 'medium' }),
  en: new Intl.DateTimeFormat('en-GB', { timeStyle: 'medium' }),
}

export function formatTime(value: string | number | Date | null | undefined, lang: Lang) {
  if (value === null || value === undefined) return '—'
  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : timeFormatters[lang].format(date)
}
