import { useState } from 'react'

export interface BarDatum {
  key: string
  label: string
  value: number
  /** Formatted value shown at the bar tip. */
  display: string
  /** Extra lines for the hover tooltip. */
  details: string[]
}

/**
 * Single-series horizontal bar chart (magnitude by category): one brand hue,
 * ≤20px bars with a 4px rounded data end, value at the tip in ink, hover/focus
 * tooltip with the details. The table next to it is the accessible data view.
 */
export function BarChart({ data, title, max = 12 }: { data: BarDatum[]; title: string; max?: number }) {
  const [active, setActive] = useState<string | null>(null)
  const rows = [...data].sort((a, b) => b.value - a.value).slice(0, max)
  const peak = Math.max(1, ...rows.map((row) => row.value))

  return (
    <figure aria-label={title} className="space-y-2">
      {rows.map((row) => {
        const width = Math.max(0.5, (row.value / peak) * 100)
        const open = active === row.key
        return (
          <div
            key={row.key}
            tabIndex={0}
            onMouseEnter={() => setActive(row.key)}
            onMouseLeave={() => setActive(null)}
            onFocus={() => setActive(row.key)}
            onBlur={() => setActive(null)}
            className="group relative grid grid-cols-[minmax(0,8rem)_minmax(0,1fr)] items-center gap-3 rounded-xl py-1 outline-none focus-visible:ring-2 focus-visible:ring-brand sm:grid-cols-[minmax(0,12rem)_minmax(0,1fr)]"
          >
            <span className="truncate text-sm font-bold text-ink" title={row.label}>
              {row.label}
            </span>
            <span className="flex min-w-0 items-center gap-2">
              <span
                className={`h-5 rounded-e transition-opacity ${active && !open ? 'opacity-50' : ''}`}
                style={{ width: `${width}%`, backgroundColor: 'var(--color-brand)', maxWidth: 'calc(100% - 6.5rem)' }}
              />
              <span className="shrink-0 text-xs font-bold text-ink">{row.display}</span>
            </span>
            {open && (
              <span
                role="tooltip"
                className="pointer-events-none absolute start-32 top-full z-10 mt-1 min-w-44 rounded-2xl bg-ink px-3 py-2 text-xs text-white shadow-float sm:start-48"
              >
                <span className="block font-bold">{row.label}</span>
                {row.details.map((line) => (
                  <span key={line} className="block text-white/80">
                    {line}
                  </span>
                ))}
              </span>
            )}
          </div>
        )
      })}
    </figure>
  )
}
