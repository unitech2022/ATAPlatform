import { useEffect, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { Icon } from './Icon'

export interface ModalProps {
  open: boolean
  title: ReactNode
  description?: ReactNode
  onClose: () => void
  footer?: ReactNode
  size?: 'md' | 'lg' | 'xl'
  children?: ReactNode
}

const sizes = { md: 'sm:max-w-lg', lg: 'sm:max-w-2xl', xl: 'sm:max-w-5xl' }

export function Modal({ open, title, description, onClose, footer, size = 'md', children }: ModalProps) {
  const { t } = useLang()

  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKey)
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = previousOverflow
    }
  }, [open, onClose])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-6">
      <button type="button" aria-label={t('close')} onClick={onClose} className="absolute inset-0 bg-ink/50 backdrop-blur-[2px]" />
      <div
        role="dialog"
        aria-modal="true"
        className={`relative flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-[32px] bg-white shadow-panel sm:rounded-3xl ${sizes[size]}`}
      >
        <header className="flex items-start justify-between gap-4 border-b border-line px-5 py-4 sm:px-6">
          <div className="min-w-0">
            <h2 className="text-lg font-bold">{title}</h2>
            {description && <p className="mt-1 break-words text-sm text-muted">{description}</p>}
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('close')}
            className="grid size-10 shrink-0 place-items-center rounded-full bg-cloud text-ink transition hover:bg-line"
          >
            <Icon name="x" className="size-4" />
          </button>
        </header>
        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5 sm:px-6">{children}</div>
        {footer && <footer className="flex flex-wrap justify-end gap-2 border-t border-line px-5 py-4 sm:px-6">{footer}</footer>}
      </div>
    </div>
  )
}
