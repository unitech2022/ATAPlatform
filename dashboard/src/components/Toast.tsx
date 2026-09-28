import type { ToastItem, ToastTone } from '../context/toast'
import { Icon, type IconName } from './Icon'

const toneStyles: Record<ToastTone, { icon: IconName; iconClass: string }> = {
  success: { icon: 'check', iconClass: 'bg-brand text-white' },
  error: { icon: 'alert', iconClass: 'bg-danger text-white' },
  info: { icon: 'info', iconClass: 'bg-ink text-white' },
}

export function ToastViewport({ toasts, onDismiss }: { toasts: ToastItem[]; onDismiss: (id: number) => void }) {
  if (toasts.length === 0) return null
  return (
    <div className="pointer-events-none fixed inset-x-4 bottom-4 z-[60] flex flex-col items-center gap-2 sm:inset-x-auto sm:end-6 sm:bottom-6 sm:items-end">
      {toasts.map((toast) => {
        const style = toneStyles[toast.tone]
        return (
          <div
            key={toast.id}
            role="status"
            className="pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-2xl bg-white p-4 shadow-float ring-1 ring-line"
          >
            <span className={`grid size-9 shrink-0 place-items-center rounded-xl ${style.iconClass}`}>
              <Icon name={style.icon} className="size-4" />
            </span>
            <div className="min-w-0 flex-1">
              <p className="text-sm font-bold">{toast.title}</p>
              {toast.description && <p className="mt-0.5 break-words text-xs text-muted">{toast.description}</p>}
            </div>
            <button
              type="button"
              onClick={() => onDismiss(toast.id)}
              className="grid size-7 shrink-0 place-items-center rounded-full text-muted transition hover:bg-cloud"
            >
              <Icon name="x" className="size-3.5" />
            </button>
          </div>
        )
      })}
    </div>
  )
}
