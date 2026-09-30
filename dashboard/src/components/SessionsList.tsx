import { useState } from 'react'
import { useLang } from '../context/lang'
import { formatDateTime } from '../lib/format'
import { describeUserAgent } from '../lib/rbac'
import type { AdminSession } from '../lib/types'
import { Badge } from './Badge'
import { Button } from './Button'
import { EmptyState } from './EmptyState'
import { Icon } from './Icon'

/** Admin refresh-token sessions (§F20.5 `/admin/me/sessions`), newest activity first; the current one is marked. */
export function SessionsList({ sessions, onRevoke }: { sessions: AdminSession[]; onRevoke?: (session: AdminSession) => Promise<void> }) {
  const { t, lang } = useLang()
  const [revoking, setRevoking] = useState<string | null>(null)

  if (sessions.length === 0) return <EmptyState icon="shield" title={t('ssEmpty')} />

  const sorted = [...sessions].sort((a, b) => {
    if (a.current !== b.current) return a.current ? -1 : 1
    return (b.lastUsedAt ?? b.createdAt).localeCompare(a.lastUsedAt ?? a.createdAt)
  })

  return (
    <ul className="divide-y divide-line" data-testid="sessions-list">
      {sorted.map((item) => (
        <li key={item.id} className="flex flex-col gap-3 py-4 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 items-start gap-3">
            <span className={`grid size-10 shrink-0 place-items-center rounded-xl ${item.current ? 'bg-brand-soft text-brand' : 'bg-cloud text-muted'}`}>
              <Icon name="globe" className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="flex flex-wrap items-center gap-2 font-bold">
                <span className="truncate" title={item.userAgent ?? undefined}>
                  {describeUserAgent(item.userAgent)}
                </span>
                {item.current && <Badge tone="brand">{t('ssCurrent')}</Badge>}
              </p>
              <p className="text-xs text-muted">
                <span className="ltr-nums">{item.ipAddress ?? '—'}</span> · {t('ssSignedIn')} {formatDateTime(item.createdAt, lang)}
              </p>
              <p className="text-xs text-muted">
                {t('ssLastUsed')} {formatDateTime(item.lastUsedAt ?? item.createdAt, lang)}
              </p>
            </div>
          </div>
          {onRevoke && (
            <Button
              variant="danger-outline"
              size="sm"
              icon="x"
              loading={revoking === item.id}
              disabled={revoking !== null && revoking !== item.id}
              onClick={async () => {
                setRevoking(item.id)
                try {
                  await onRevoke(item)
                } finally {
                  setRevoking(null)
                }
              }}
            >
              {item.current ? t('ssSignOutHere') : t('ssRevoke')}
            </Button>
          )}
        </li>
      ))}
    </ul>
  )
}
