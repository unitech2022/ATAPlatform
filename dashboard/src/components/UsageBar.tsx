/** Thin progress bar for usage/budget/quest progress; `share` is 0..1 (null renders nothing). */
export function UsageBar({ share, tone = 'brand', className = '' }: { share: number | null; tone?: 'brand' | 'warning' | 'danger'; className?: string }) {
  if (share === null) return null
  const color = tone === 'danger' ? 'bg-danger' : tone === 'warning' ? 'bg-amber-500' : 'bg-brand'
  return (
    <span className={`block h-1.5 w-full overflow-hidden rounded-full bg-line ${className}`} role="presentation">
      <span className={`block h-full rounded-full ${color}`} style={{ width: `${Math.round(share * 100)}%` }} />
    </span>
  )
}
