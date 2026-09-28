import { useI18n } from '../i18n'
import type { ApplicationStatus, DocumentStatus } from '../lib/types'

const applicationTones: Record<ApplicationStatus, string> = {
  draft: 'bg-cloud text-muted',
  submitted: 'bg-brand-soft text-brand',
  under_review: 'bg-ink text-white',
  approved: 'bg-brand text-white',
  rejected: 'bg-danger-soft text-danger',
  suspended: 'bg-danger text-white',
}

export function ApplicationStatusBadge({ status, className = '' }: { status: ApplicationStatus; className?: string }) {
  const { t } = useI18n()
  return (
    <span className={`inline-flex items-center rounded-full px-3 py-1.5 text-xs font-bold ${applicationTones[status]} ${className}`}>
      {t(`status.${status}`)}
    </span>
  )
}

type DocumentChipStatus = DocumentStatus | 'missing'

const documentTones: Record<DocumentChipStatus, string> = {
  missing: 'border border-line text-muted',
  pending: 'bg-cloud text-muted',
  verified: 'bg-brand-soft text-brand',
  rejected: 'bg-danger-soft text-danger',
}

export function DocumentStatusBadge({ status, className = '' }: { status: DocumentChipStatus; className?: string }) {
  const { t } = useI18n()
  return (
    <span className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-bold ${documentTones[status]} ${className}`}>
      {t(`docs.status.${status}`)}
    </span>
  )
}
