import type { ReactNode } from 'react'
import { Icon, type IconName } from './Icon'

export interface EmptyStateProps {
  title: string
  description?: string
  icon?: IconName
  action?: ReactNode
  tone?: 'brand' | 'danger'
}

export function EmptyState({ title, description, icon = 'search', action, tone = 'brand' }: EmptyStateProps) {
  const iconTone = tone === 'danger' ? 'bg-danger-soft text-danger' : 'bg-brand-soft text-brand'
  return (
    <div className="flex flex-col items-center px-6 py-12 text-center">
      <div className={`mb-5 grid size-14 place-items-center rounded-2xl ${iconTone}`}>
        <Icon name={icon} className="size-7" />
      </div>
      <p className="font-bold">{title}</p>
      {description && <p className="mt-1 max-w-sm text-sm text-muted">{description}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}
