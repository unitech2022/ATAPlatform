import type { ReactNode } from 'react'
import { useI18n } from '../i18n'
import { Action } from './Button'

type Tone = 'error' | 'success' | 'info'

const tones: Record<Tone, string> = {
  error: 'bg-danger-soft text-danger',
  success: 'bg-brand-soft text-brand',
  info: 'bg-cloud text-muted',
}

interface NoticeProps {
  tone: Tone
  children: ReactNode
  className?: string
  onRetry?: () => void
}

export function Notice({ tone, children, className = '', onRetry }: NoticeProps) {
  const { t } = useI18n()
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={`flex flex-wrap items-center justify-between gap-3 rounded-2xl p-4 text-sm font-bold leading-6 ${tones[tone]} ${className}`}
    >
      <div className="min-w-0 flex-1">{children}</div>
      {onRetry && (
        <Action onClick={onRetry} className="rounded-xl bg-white/80 px-3 py-1.5 text-xs font-bold text-ink hover:bg-white">
          {t('action.retry')}
        </Action>
      )}
    </div>
  )
}
