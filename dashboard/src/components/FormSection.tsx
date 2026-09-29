import type { ReactNode } from 'react'

/** Bordered group inside long forms (promotion, incentive). */
export function FormSection({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <section className="rounded-2xl border border-line p-4">
      <h3 className="font-bold">{title}</h3>
      {description && <p className="mt-0.5 text-xs text-muted">{description}</p>}
      <div className="mt-3">{children}</div>
    </section>
  )
}

/** Label + optional muted hint above a chip group or picker. */
export function ChoiceField({ label, hint, error, children }: { label: string; hint?: string; error?: string; children: ReactNode }) {
  return (
    <div>
      <p className="mb-2 text-sm font-bold">
        {label}
        {hint && <span className="ms-2 text-xs font-normal text-muted">({hint})</span>}
      </p>
      {children}
      {error && <p className="mt-1.5 text-xs font-bold text-danger">{error}</p>}
    </div>
  )
}
