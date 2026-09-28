import { useI18n } from '../i18n'
import { Action } from './Button'
import { Icon } from './Icon'

interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onChange: (page: number) => void
  className?: string
}

export function Pagination({ page, pageSize, total, onChange, className = '' }: PaginationProps) {
  const { t } = useI18n()
  const pages = Math.max(1, Math.ceil(total / Math.max(1, pageSize)))
  if (pages <= 1) return null
  return (
    <nav aria-label={t('pagination.label')} className={`flex items-center justify-between gap-3 ${className}`}>
      <Action
        onClick={() => onChange(page - 1)}
        disabled={page <= 1}
        className="flex items-center gap-1 rounded-xl border border-line bg-white px-3 py-2 text-sm font-bold text-ink hover:bg-cloud disabled:text-muted disabled:hover:bg-white"
      >
        <Icon name="chevron" className="size-4 ltr:rotate-180" />
        {t('pagination.prev')}
      </Action>
      <span className="text-sm font-bold text-muted">{t('pagination.status', { page, pages })}</span>
      <Action
        onClick={() => onChange(page + 1)}
        disabled={page >= pages}
        className="flex items-center gap-1 rounded-xl border border-line bg-white px-3 py-2 text-sm font-bold text-ink hover:bg-cloud disabled:text-muted disabled:hover:bg-white"
      >
        {t('pagination.next')}
        <Icon name="chevron" className="size-4 rtl:rotate-180" />
      </Action>
    </nav>
  )
}
