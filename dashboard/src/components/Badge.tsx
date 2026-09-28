import type { ReactNode } from 'react'
import { useLang } from '../context/lang'
import { documentStatusMeta, driverStatusMeta, userStatusMeta, type StatusTone } from '../lib/status'
import type { DocumentStatus, DriverStatus, UserStatus } from '../lib/types'

export type BadgeTone = StatusTone | 'white'

const tones: Record<BadgeTone, string> = {
  brand: 'bg-brand-soft text-brand',
  ink: 'bg-ink text-white',
  muted: 'bg-cloud text-muted',
  danger: 'bg-danger-soft text-danger',
  warning: 'bg-amber-50 text-amber-700',
  white: 'bg-white/10 text-brand',
}

export function Badge({ tone = 'muted', children, className = '' }: { tone?: BadgeTone; children: ReactNode; className?: string }) {
  return (
    <span className={`inline-flex items-center whitespace-nowrap rounded-full px-3 py-1 text-xs font-bold ${tones[tone]} ${className}`}>
      {children}
    </span>
  )
}

export function DriverStatusBadge({ status }: { status: DriverStatus }) {
  const { t } = useLang()
  const meta = driverStatusMeta[status] ?? driverStatusMeta.draft
  return <Badge tone={meta.tone}>{t(meta.key)}</Badge>
}

export function DocumentStatusBadge({ status }: { status: DocumentStatus }) {
  const { t } = useLang()
  const meta = documentStatusMeta[status] ?? documentStatusMeta.pending
  return <Badge tone={meta.tone}>{t(meta.key)}</Badge>
}

export function UserStatusBadge({ status }: { status: UserStatus }) {
  const { t } = useLang()
  const meta = userStatusMeta[status] ?? userStatusMeta.active
  return <Badge tone={meta.tone}>{t(meta.key)}</Badge>
}
