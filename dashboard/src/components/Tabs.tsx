import { formatNumber } from '../lib/format'

export interface TabOption<T extends string> {
  value: T
  label: string
  count?: number | null
}

/** Segmented status tabs (same look as the trips status tabs); scrolls horizontally on phones. */
export function Tabs<T extends string>({ options, value, onChange, className = '' }: { options: TabOption<T>[]; value: T; onChange: (value: T) => void; className?: string }) {
  return (
    <div className={`max-w-full overflow-x-auto rounded-2xl bg-white p-1.5 shadow-soft ${className}`}>
      <div className="flex w-max gap-1">
        {options.map((option) => {
          const active = option.value === value
          return (
            <button
              key={option.value || 'all'}
              type="button"
              onClick={() => onChange(option.value)}
              aria-pressed={active}
              className={`flex items-center gap-2 whitespace-nowrap rounded-xl px-3.5 py-2 text-sm font-bold transition ${active ? 'bg-ink text-white' : 'text-muted hover:bg-cloud'}`}
            >
              {option.label}
              {typeof option.count === 'number' && (
                <span className={`ltr-nums rounded-full px-2 text-xs ${active ? 'bg-white/15' : 'bg-cloud'}`}>{formatNumber(option.count)}</span>
              )}
            </button>
          )
        })}
      </div>
    </div>
  )
}
