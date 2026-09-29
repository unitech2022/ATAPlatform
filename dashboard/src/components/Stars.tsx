import { Icon } from './Icon'

/** Compact star rating: filled icon + the value, always LTR. `low` colours ratings at or below the flag threshold. */
export function Stars({ value, low = false, className = '' }: { value: number | null | undefined; low?: boolean; className?: string }) {
  if (typeof value !== 'number') return <span className="text-muted">—</span>
  return (
    <span className={`ltr-nums inline-flex items-center gap-1 whitespace-nowrap font-bold ${low ? 'text-danger' : ''} ${className}`}>
      <Icon name="star" className={`size-4 ${low ? 'text-danger' : 'text-amber-500'}`} />
      {Number.isInteger(value) ? value : value.toFixed(2)}
    </span>
  )
}

/** Five-star row for detail views. */
export function StarRow({ value }: { value: number }) {
  return (
    <span className="inline-flex items-center gap-0.5" aria-label={`${value} / 5`}>
      {[1, 2, 3, 4, 5].map((index) => (
        <Icon key={index} name="star" className={`size-5 ${index <= Math.round(value) ? 'text-amber-500' : 'text-line'}`} />
      ))}
    </span>
  )
}
