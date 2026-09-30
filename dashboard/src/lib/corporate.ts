import type { TranslationKey } from '../i18n'
import type {
  CorporateAccountInput,
  CorporateAccountStatus,
  CorporateInvoice,
  CorporateInvoiceStatus,
  CorporateReceivable,
  CorporateUserStatus,
  CorporateRole,
  CorporateZoneMatch,
} from './types'

export const CORPORATE_ACCOUNT_STATUSES: CorporateAccountStatus[] = ['pending', 'active', 'suspended', 'closed']
export const CORPORATE_INVOICE_STATUSES: CorporateInvoiceStatus[] = ['draft', 'issued', 'paid', 'overdue', 'void']
export const CORPORATE_USER_STATUSES: CorporateUserStatus[] = ['invited', 'active', 'disabled']

export const CORPORATE_ROLE_KEY: Record<CorporateRole, TranslationKey> = {
  corporate_admin: 'coRoleAdmin',
  employee: 'coRoleEmployee',
}

export const CORPORATE_ZONE_MATCH_KEY: Record<CorporateZoneMatch, TranslationKey> = {
  pickup_and_dropoff: 'coPolZoneAnd',
  pickup_or_dropoff: 'coPolZoneOr',
}

/** `corporate_accounts.cr_number`: exactly 10 digits (§F19.1). */
export const CR_PATTERN = /^\d{10}$/
/** `corporate_accounts.vat_number`: 15 digits starting and ending with 3 (§F19.1). */
export const VAT_PATTERN = /^3\d{13}3$/
/** E.164 phone number (`contact_phone`, invitations). */
export const E164_PATTERN = /^\+[1-9]\d{7,14}$/
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export type CorporateAccountErrorField =
  | 'legalNameAr'
  | 'legalNameEn'
  | 'displayName'
  | 'crNumber'
  | 'vatNumber'
  | 'billingEmail'
  | 'contactName'
  | 'contactPhone'
  | 'creditLimit'
  | 'paymentTermsDays'
  | 'cityId'
  | 'adminPhone'

/** Client mirror of the §F19.1 column rules (the server stays authoritative with `422 validation_failed`). */
export function validateCorporateAccount(input: CorporateAccountInput, adminPhone: string): Partial<Record<CorporateAccountErrorField, TranslationKey>> {
  const errors: Partial<Record<CorporateAccountErrorField, TranslationKey>> = {}
  if (!input.legalNameAr.trim()) errors.legalNameAr = 'fieldRequired'
  if (!input.legalNameEn.trim()) errors.legalNameEn = 'fieldRequired'
  if (!input.displayName.trim()) errors.displayName = 'fieldRequired'
  if (!CR_PATTERN.test(input.crNumber)) errors.crNumber = 'coErrCr'
  if (input.vatNumber !== null && !VAT_PATTERN.test(input.vatNumber)) errors.vatNumber = 'coErrVat'
  if (!EMAIL_PATTERN.test(input.billingEmail)) errors.billingEmail = 'coErrEmail'
  if (!input.contactName.trim()) errors.contactName = 'fieldRequired'
  if (!E164_PATTERN.test(input.contactPhone)) errors.contactPhone = 'coErrPhone'
  if (!Number.isFinite(input.creditLimit) || input.creditLimit < 0) errors.creditLimit = 'invalidNumber'
  if (!Number.isInteger(input.paymentTermsDays) || input.paymentTermsDays < 0 || input.paymentTermsDays > 365) errors.paymentTermsDays = 'coErrTerms'
  if (!input.cityId) errors.cityId = 'fieldRequired'
  if (adminPhone && !E164_PATTERN.test(adminPhone)) errors.adminPhone = 'coErrPhone'
  return errors
}

/** Digits only, capped at `max` (CR / VAT number inputs). */
export function digitsOnly(value: string, max: number) {
  return value.replace(/\D/g, '').slice(0, max)
}

/** Share (0..1) of the credit limit that is used; null when there is no positive limit. */
export function creditShare(used: number | null | undefined, limit: number | null | undefined): number | null {
  if (typeof used !== 'number' || typeof limit !== 'number' || limit <= 0) return null
  return Math.min(1, Math.max(0, used / limit))
}

/** Credit used = unbilled trips + unpaid invoices (the credit-limit formula of §F19.2). */
export function creditUsedOf(receivable: CorporateReceivable | null | undefined): number | null {
  return receivable ? receivable.unbilled + receivable.unpaidInvoices : null
}

export function usageTone(share: number | null): 'brand' | 'warning' | 'danger' {
  if (share !== null && share >= 1) return 'danger'
  if (share !== null && share >= 0.8) return 'warning'
  return 'brand'
}

/** Amount still owed on an invoice (partial payments leave it `issued`/`overdue`, §F19.2). */
export function invoiceRemaining(invoice: CorporateInvoice) {
  return Math.max(0, Math.round((invoice.totalInclVat - (invoice.paidAmount ?? 0)) * 100) / 100)
}

/** Actions available per status: `issue` for drafts, `markPaid` for open invoices, `void` for anything unpaid. */
export function invoiceActions(status: CorporateInvoiceStatus) {
  return {
    issue: status === 'draft',
    markPaid: status === 'issued' || status === 'overdue',
    void: status !== 'paid' && status !== 'void',
    pdf: status !== 'draft' && status !== 'void',
  }
}

export function sumInvoices(invoices: CorporateInvoice[]) {
  return invoices.reduce(
    (total, invoice) => ({
      subtotalExclVat: total.subtotalExclVat + invoice.subtotalExclVat,
      vatAmount: total.vatAmount + invoice.vatAmount,
      totalInclVat: total.totalInclVat + invoice.totalInclVat,
    }),
    { subtotalExclVat: 0, vatAmount: 0, totalInclVat: 0 },
  )
}

/** `YYYY-MM` of the previous calendar month (the month the monthly job bills, §F19.2). */
export function previousMonth(now = new Date()) {
  const date = new Date(now.getFullYear(), now.getMonth() - 1, 1)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
}

/** `2026-09` → `2026-09-01` (`periodStart` of the generate call). */
export function periodStartOf(month: string) {
  return /^\d{4}-(0[1-9]|1[0-2])$/.test(month) ? `${month}-01` : null
}

/** Date-only `YYYY-MM-DD` of an API timestamp (period and due dates are dates, not instants). */
export function dateOnly(value: string | null | undefined) {
  return value ? value.slice(0, 10) : null
}

/** Keeps only the receivable of `accountId`. */
export function receivableOf(receivables: CorporateReceivable[] | null | undefined, accountId: string) {
  return receivables?.find((row) => row.accountId === accountId) ?? null
}

export function sumBy<T>(rows: T[], pick: (row: T) => number) {
  return rows.reduce((total, row) => total + pick(row), 0)
}
