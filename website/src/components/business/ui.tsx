import { useEffect, useId, type ReactNode } from 'react'
import { useI18n } from '../../i18n'
import { Action, Button } from '../Button'
import { Icon, type IconName } from '../Icon'

export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div className="min-w-0">
        <h1 className="text-2xl font-bold sm:text-3xl">{title}</h1>
        {subtitle && <p className="mt-1 text-sm leading-6 text-muted">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  )
}

export function StatCard({
  icon,
  label,
  value,
  hint,
  children,
}: {
  icon: IconName
  label: string
  value: string
  hint?: string
  children?: ReactNode
}) {
  return (
    <div className="rounded-3xl bg-white p-5 shadow-soft">
      <div className="flex items-center gap-3">
        <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand">
          <Icon name={icon} className="size-5" />
        </span>
        <p className="text-sm font-bold text-muted">{label}</p>
      </div>
      <p className="mt-4 text-2xl font-bold" dir="auto">
        {value}
      </p>
      {hint && <p className="mt-1 text-xs font-bold text-muted">{hint}</p>}
      {children}
    </div>
  )
}

/** Horizontal scroll container for wide tables (keeps the page itself from scrolling sideways). */
export function TableWrap({ children }: { children: ReactNode }) {
  return (
    <div className="relative overflow-x-auto rounded-3xl bg-white shadow-soft">
      <table className="w-full min-w-[40rem] border-collapse text-sm">{children}</table>
    </div>
  )
}

export function Th({ children, className = '' }: { children?: ReactNode; className?: string }) {
  return (
    <th scope="col" className={`whitespace-nowrap border-b border-line bg-cloud px-4 py-3 text-start text-xs font-bold text-muted ${className}`}>
      {children}
    </th>
  )
}

export function Td({ children, className = '' }: { children?: ReactNode; className?: string }) {
  return <td className={`border-b border-line px-4 py-3 align-middle ${className}`}>{children}</td>
}

export type PillTone = 'brand' | 'solid' | 'ink' | 'muted' | 'danger' | 'warning'

const pillTones: Record<PillTone, string> = {
  brand: 'bg-brand-soft text-brand',
  solid: 'bg-brand text-white',
  ink: 'bg-ink text-white',
  muted: 'bg-cloud text-muted',
  danger: 'bg-danger-soft text-danger',
  warning: 'bg-amber-50 text-amber-700',
}

export function Pill({ tone, children }: { tone: PillTone; children: ReactNode }) {
  return <span className={`inline-flex whitespace-nowrap rounded-full px-3 py-1 text-xs font-bold ${pillTones[tone]}`}>{children}</span>
}

export function Modal({
  open,
  title,
  onClose,
  children,
  wide = false,
}: {
  open: boolean
  title: string
  onClose: () => void
  children: ReactNode
  wide?: boolean
}) {
  const { t } = useI18n()
  const titleId = useId()

  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKey)
    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = previous
    }
  }, [open, onClose])

  if (!open) return null
  return (
    <div className="fixed inset-0 z-[2000] flex items-end justify-center bg-ink/40 p-0 sm:items-center sm:p-4" onMouseDown={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onMouseDown={(event) => event.stopPropagation()}
        className={`max-h-[92vh] w-full overflow-y-auto rounded-t-3xl bg-white p-5 shadow-panel sm:rounded-3xl sm:p-6 ${wide ? 'sm:max-w-3xl' : 'sm:max-w-lg'}`}
      >
        <div className="mb-5 flex items-center justify-between gap-3">
          <h2 id={titleId} className="text-xl font-bold">
            {title}
          </h2>
          <Action onClick={onClose} aria-label={t('action.close')} className="grid size-10 place-items-center rounded-full bg-cloud text-muted hover:text-ink">
            <Icon name="close" className="size-5" />
          </Action>
        </div>
        {children}
      </div>
    </div>
  )
}

export function ConfirmModal({
  open,
  title,
  message,
  confirmLabel,
  danger = false,
  busy = false,
  onConfirm,
  onClose,
  children,
}: {
  open: boolean
  title: string
  message: string
  confirmLabel: string
  danger?: boolean
  busy?: boolean
  onConfirm: () => void
  onClose: () => void
  children?: ReactNode
}) {
  const { t } = useI18n()
  return (
    <Modal open={open} title={title} onClose={onClose}>
      <p className="leading-7 text-muted">{message}</p>
      {children}
      <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
        <Button variant="secondary" onClick={onClose} disabled={busy}>
          {t('action.cancel')}
        </Button>
        <Button variant={danger ? 'danger' : 'primary'} onClick={onConfirm} disabled={busy}>
          {busy ? t('action.saving') : confirmLabel}
        </Button>
      </div>
    </Modal>
  )
}

/** Accessible switch built on a checkbox. */
export function Toggle({
  checked,
  onChange,
  label,
  hint,
  disabled = false,
}: {
  checked: boolean
  onChange: (next: boolean) => void
  label: string
  hint?: string
  disabled?: boolean
}) {
  const id = useId()
  return (
    <label htmlFor={id} className="flex cursor-pointer items-start justify-between gap-4 rounded-2xl border border-line p-4 hover:border-brand">
      <span className="min-w-0">
        <span className="block text-sm font-bold">{label}</span>
        {hint && <span className="mt-1 block text-xs leading-5 text-muted">{hint}</span>}
      </span>
      <span className="relative mt-0.5 inline-flex shrink-0">
        <input
          id={id}
          type="checkbox"
          role="switch"
          className="peer sr-only"
          checked={checked}
          disabled={disabled}
          onChange={(event) => onChange(event.target.checked)}
        />
        <span className="h-7 w-12 rounded-full bg-line transition peer-checked:bg-brand peer-focus-visible:ring-4 peer-focus-visible:ring-brand/30" />
        <span className="absolute top-1 size-5 rounded-full bg-white shadow-soft transition-all start-1 peer-checked:start-6" />
      </span>
    </label>
  )
}
