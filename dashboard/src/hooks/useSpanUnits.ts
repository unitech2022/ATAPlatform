import { useMemo } from 'react'
import { useLang } from '../context/lang'
import type { SpanUnits } from '../lib/support'

/** Translated unit suffixes for `formatSpan` (`d` / `h` / `m`). */
export function useSpanUnits(): SpanUnits {
  const { t } = useLang()
  return useMemo(() => ({ day: t('spUnitDay'), hour: t('spUnitHour'), minute: t('spUnitMinute') }), [t])
}
