import { useI18n } from '../../i18n'
import { EMPLOYEE_TONES, INVOICE_TONES, TRIP_TONES } from '../../lib/corporate'
import type { CorporateUserStatus, InvoiceStatus, TripStatus } from '../../lib/types'
import { Pill } from './ui'

export function TripStatusPill({ status }: { status: TripStatus }) {
  const { t } = useI18n()
  return <Pill tone={TRIP_TONES[status] ?? 'muted'}>{t(`trip.status.${status}`)}</Pill>
}

export function InvoiceStatusPill({ status }: { status: InvoiceStatus }) {
  const { t } = useI18n()
  return <Pill tone={INVOICE_TONES[status] ?? 'muted'}>{t(`invoice.status.${status}`)}</Pill>
}

export function EmployeeStatusPill({ status }: { status: CorporateUserStatus }) {
  const { t } = useI18n()
  return <Pill tone={EMPLOYEE_TONES[status] ?? 'muted'}>{t(`employee.status.${status}`)}</Pill>
}
