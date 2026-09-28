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
