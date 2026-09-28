import type { ReactNode } from 'react'

export interface CardProps {
  title?: ReactNode
  description?: ReactNode
  action?: ReactNode
  /** Removes the inner padding so tables can bleed to the card edge. */
  flush?: boolean
  className?: string
  children?: ReactNode
}

export function Card({ title, description, action, flush = false, className = '', children }: CardProps) {
  const hasHeader = title !== undefined || action !== undefined
  return (
    <section className={`overflow-hidden rounded-3xl bg-white shadow-soft ${className}`}>
      {hasHeader && (
        <header className="flex flex-wrap items-start justify-between gap-3 px-5 pt-5 sm:px-6 sm:pt-6">
          <div className="min-w-0">
            {title !== undefined && <h2 className="text-lg font-bold">{title}</h2>}
            {description !== undefined && <p className="mt-1 text-sm text-muted">{description}</p>}
          </div>
          {action}
        </header>
      )}
      <div className={flush ? (hasHeader ? 'mt-4' : '') : `p-5 sm:p-6 ${hasHeader ? 'pt-4 sm:pt-4' : ''}`}>{children}</div>
    </section>
  )
}

/** Ink background card used for highlights (design system §4 "البطاقة الداكنة"). */
export function DarkCard({ className = '', children }: { className?: string; children: ReactNode }) {
  return <section className={`rounded-3xl bg-ink p-6 text-white shadow-button ${className}`}>{children}</section>
}
