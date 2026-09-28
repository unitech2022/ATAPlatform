import type { ReactNode } from 'react'
import { useLang } from '../context/lang'
import { documentStatusMeta, driverStatusMeta, metaOf, tripStatusMeta, userStatusMeta, type StatusMeta, type StatusTone } from '../lib/status'
import type { DocumentStatus, DriverStatus, TripStatus, UserStatus } from '../lib/types'

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

export function TripStatusBadge({ status, className }: { status: TripStatus; className?: string }) {
  const { t } = useLang()
  const meta = tripStatusMeta[status] ?? tripStatusMeta.requested
  return (
    <Badge tone={meta.tone} className={className}>
      {t(meta.key)}
    </Badge>
  )
}

/** Badge for any status record in lib/status.ts; unknown values render as a muted raw code. */
export function MetaBadge<K extends string>({ record, value, className }: { record: Record<K, StatusMeta>; value: string | null | undefined; className?: string }) {
  const { t } = useLang()
  const meta = metaOf(record, value)
  if (!meta) {
    return (
      <Badge tone="muted" className={className}>
        <span className="ltr-nums">{value || '—'}</span>
      </Badge>
    )
  }
  return (
    <Badge tone={meta.tone} className={className}>
      {t(meta.key)}
    </Badge>
  )
}
