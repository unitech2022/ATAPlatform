import { useLang } from '../context/lang'
import { formatMoney } from '../lib/format'

export interface MoneyProps {
  value: number | null | undefined
  /** Colours negative values (debts) in danger and prefixes positives with "+". */
  signed?: boolean
  strong?: boolean
  className?: string
}

/** Amount + currency, always LTR (design system §2). */
export function Money({ value, signed = false, strong = false, className = '' }: MoneyProps) {
  const { t } = useLang()
  const negative = typeof value === 'number' && value < 0
  const tone = signed && negative ? 'text-danger' : ''
  const prefix = signed && typeof value === 'number' && value > 0 ? '+' : ''
  return (
    <span className={`ltr-nums whitespace-nowrap ${tone} ${className}`}>
      <span className={strong ? 'font-bold' : ''}>
        {prefix}
        {formatMoney(value)}
      </span>{' '}
      <span className="text-xs text-muted">{t('sar')}</span>
    </span>
  )
}
