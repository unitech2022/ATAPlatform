import type { Lang } from '../i18n'

/** Gregorian dates with Latin digits in both languages (design system §5). */
export function formatDate(value: string, lang: Lang): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat(lang === 'ar' ? 'ar-SA-u-ca-gregory-nu-latn' : 'en-GB', {
    dateStyle: 'medium',
  }).format(date)
}

/** Today's date as yyyy-mm-dd, for <input type="date"> bounds. */
export function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

export function currentYear(): number {
  return new Date().getFullYear()
}

/** ISO date (yyyy-mm-dd) for <input type="date">. */
export function toDateInputValue(value: string | null): string {
  if (!value) return ''
  return value.length >= 10 ? value.slice(0, 10) : value
}

/** "38 ر.س" / "38 SAR" — number first, currency after, Latin digits. */
export function formatPrice(amount: number, lang: Lang): string {
  const number = new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 }).format(amount)
  return lang === 'ar' ? `${number} ر.س` : `${number} SAR`
}

export function formatMinutes(minutes: number, lang: Lang): string {
  if (lang === 'en') return `${minutes} min`
  if (minutes === 1) return 'دقيقة'
  if (minutes === 2) return 'دقيقتان'
  if (minutes <= 10) return `${minutes} دقائق`
  return `${minutes} دقيقة`
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

/** Display a local 9-digit number as "+966 5X XXX XXXX". */
export function formatPhone(local: string): string {
  const groups = [local.slice(0, 2), local.slice(2, 5), local.slice(5, 9)].filter(Boolean)
  return `+966 ${groups.join(' ')}`.trim()
}

/**
 * Normalise anything typed/pasted into the phone field to the 9 local digits
 * (accepts 05XXXXXXXX, 5XXXXXXXX, +9665XXXXXXXX, 9665XXXXXXXX).
 */
export function normalizeLocalPhone(raw: string): string {
  let digits = raw.replace(/\D/g, '')
  if (digits.startsWith('966')) digits = digits.slice(3)
  if (digits.startsWith('0')) digits = digits.slice(1)
  return digits.slice(0, 9)
}

export function isValidLocalPhone(local: string): boolean {
  return /^5\d{8}$/.test(local)
}
