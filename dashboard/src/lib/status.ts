import type { TranslationKey } from '../i18n'
import type { DocumentStatus, DriverStatus, UserStatus } from './types'

export type StatusTone = 'brand' | 'ink' | 'muted' | 'danger' | 'warning'

export interface StatusMeta {
  tone: StatusTone
  key: TranslationKey
}

export const driverStatusMeta: Record<DriverStatus, StatusMeta> = {
  draft: { tone: 'muted', key: 'statusDraft' },
  submitted: { tone: 'warning', key: 'statusSubmitted' },
  under_review: { tone: 'ink', key: 'statusUnderReview' },
  approved: { tone: 'brand', key: 'statusApproved' },
  rejected: { tone: 'danger', key: 'statusRejected' },
  suspended: { tone: 'danger', key: 'statusSuspended' },
}

export const documentStatusMeta: Record<DocumentStatus, StatusMeta> = {
  pending: { tone: 'warning', key: 'docPending' },
  verified: { tone: 'brand', key: 'docVerified' },
  rejected: { tone: 'danger', key: 'docRejected' },
  expired: { tone: 'danger', key: 'docExpired' },
}

export const userStatusMeta: Record<UserStatus, StatusMeta> = {
  active: { tone: 'brand', key: 'statusActive' },
  suspended: { tone: 'danger', key: 'statusSuspended' },
  deleted: { tone: 'muted', key: 'statusDeleted' },
}

export function driverStatusKey(status: DriverStatus | null | undefined): TranslationKey | null {
  return status ? (driverStatusMeta[status]?.key ?? null) : null
}
