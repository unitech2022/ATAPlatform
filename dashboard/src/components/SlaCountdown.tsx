import { useLang } from '../context/lang'
import { useSpanUnits } from '../hooks/useSpanUnits'
import { formatSpan, slaClock } from '../lib/support'
import { Badge, type BadgeTone } from './Badge'
import { Icon } from './Icon'

/**
 * SLA countdown pill: green while on track, amber within 30 minutes, red once breached and grey with a pause icon while the clock is
 * frozen (`pending_user`). `now` comes from `useNow` so a list re-renders once per tick instead of once per row.
 */
export function SlaCountdown({ dueAt, now, pausedAt = null, label }: { dueAt: string; now: number; pausedAt?: string | null; label?: string }) {
  const { t } = useLang()
  const units = useSpanUnits()
  const clock = slaClock(dueAt, now, pausedAt)
  const tone: BadgeTone = clock.paused ? 'muted' : clock.breached ? 'danger' : clock.dueSoon ? 'warning' : 'brand'
  return (
    <Badge tone={tone} className="gap-1.5">
      <Icon name={clock.paused ? 'pause' : 'clock'} className="size-3.5" />
      {label && <span>{label}</span>}
      <span className="ltr-nums">{formatSpan(clock.remainingMs, units)}</span>
      {clock.paused ? <span>· {t('spSlaPaused')}</span> : clock.breached ? <span>· {t('spSlaOverdue')}</span> : null}
    </Badge>
  )
}
