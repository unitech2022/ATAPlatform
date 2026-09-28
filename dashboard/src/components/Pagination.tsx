import { useLang } from '../context/lang'
import { formatNumber } from '../lib/format'
import { Button } from './Button'

export interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onChange: (page: number) => void
}

export function Pagination({ page, pageSize, total, onChange }: PaginationProps) {
  const { t } = useLang()
  if (total === 0) return null
  const pageCount = Math.max(1, Math.ceil(total / Math.max(1, pageSize)))
  const from = (page - 1) * pageSize + 1
  const to = Math.min(total, page * pageSize)

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-line px-5 py-4 text-sm text-muted sm:px-6">
      <p>
        {t('showing')}{' '}
        <span className="ltr-nums font-bold text-ink">
          {formatNumber(from)}–{formatNumber(to)}
        </span>{' '}
        {t('of')} <span className="ltr-nums font-bold text-ink">{formatNumber(total)}</span>
      </p>
      <div className="flex items-center gap-2">
        <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>
          {t('previous')}
        </Button>
        <span className="ltr-nums px-1 font-bold text-ink">
          {formatNumber(page)} / {formatNumber(pageCount)}
        </span>
        <Button variant="secondary" size="sm" disabled={page >= pageCount} onClick={() => onChange(page + 1)}>
          {t('next')}
        </Button>
      </div>
    </div>
  )
}
