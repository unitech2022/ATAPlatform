import type { ReactNode } from 'react'
import { useI18n } from '../i18n'
import { describeError } from '../lib/errors'
import { Icon, type IconName } from './Icon'
import { Notice } from './Notice'

export function LoadingState({ className = '' }: { className?: string }) {
  const { t } = useI18n()
  return (
    <p role="status" className={`py-10 text-center text-sm font-bold text-muted ${className}`}>
      {t('state.loading')}
    </p>
  )
}

export function EmptyState({ icon = 'search', title, children }: { icon?: IconName; title: string; children?: ReactNode }) {
  return (
    <div className="flex flex-col items-center rounded-3xl border border-dashed border-line bg-white/60 px-6 py-12 text-center">
      <div className="mb-4 grid size-12 place-items-center rounded-2xl bg-cloud text-muted">
        <Icon name={icon} className="size-6" />
      </div>
      <p className="font-bold">{title}</p>
      {children && <div className="mt-2 max-w-md text-sm leading-7 text-muted">{children}</div>}
    </div>
  )
}

export function ErrorState({ error, onRetry, className = '' }: { error: unknown; onRetry?: () => void; className?: string }) {
  const { t } = useI18n()
  return (
    <Notice tone="error" onRetry={onRetry} className={className}>
      {describeError(error, t)}
    </Notice>
  )
}
