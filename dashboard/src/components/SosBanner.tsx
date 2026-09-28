import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useNow } from '../hooks/useNow'
import { formatDuration, SAFETY_TYPE_KEY } from '../lib/safety'
import type { SafetyCaseListItem } from '../lib/types'
import { Button } from './Button'
import { Icon } from './Icon'

/**
 * Sound-free "new SOS" banner: a pulsing danger strip per incoming critical case (motion only when the
 * user allows it). Items stay until dismissed or opened.
 */
export function SosBanner({ items, onDismiss, onDismissAll }: { items: SafetyCaseListItem[]; onDismiss: (id: string) => void; onDismissAll: () => void }) {
  const { t } = useLang()
  const now = useNow()
  if (items.length === 0) return null

  return (
    <div className="relative mb-6">
      <span aria-hidden="true" className="absolute -inset-1 rounded-[28px] bg-danger/40 motion-safe:animate-pulse" />
      <section role="alert" aria-live="assertive" className="relative overflow-hidden rounded-3xl bg-danger text-white shadow-float">
        <header className="flex flex-wrap items-center justify-between gap-3 px-5 pt-4 sm:px-6">
          <p className="flex items-center gap-3 font-bold">
            <span className="relative flex size-3">
              <span className="absolute inline-flex size-full rounded-full bg-white opacity-75 motion-safe:animate-ping" />
              <span className="relative inline-flex size-3 rounded-full bg-white" />
            </span>
            {t('sfNewSosTitle')}
          </p>
          {items.length > 1 && (
            <button type="button" onClick={onDismissAll} className="text-xs font-bold text-white/80 underline-offset-2 hover:underline">
              {t('sfDismissAll')}
            </button>
          )}
        </header>
        <ul className="divide-y divide-white/15 px-2 pb-2 pt-2 sm:px-3">
          {items.map((item) => {
            const age = Math.max(item.ageSeconds, (now - new Date(item.openedAt).getTime()) / 1000)
            return (
              <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl px-3 py-3">
                <div className="flex min-w-0 items-center gap-3">
                  <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-white/15">
                    <Icon name="siren" className="size-5" />
                  </span>
                  <div className="min-w-0">
                    <p className="truncate font-bold">
                      <span className="ltr-nums">{item.caseNumber}</span> · {t(SAFETY_TYPE_KEY[item.type] ?? 'sfTypeSos')}
                    </p>
                    <p className="truncate text-xs text-white/80">
                      {item.reporterName || t('unnamed')}
                      {item.tripNumber ? (
                        <>
                          {' · '}
                          <span className="ltr-nums">{item.tripNumber}</span>
                        </>
                      ) : null}
                      {' · '}
                      <span className="ltr-nums">{formatDuration(age)}</span>
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Link
                    to={`/safety/cases/${item.id}`}
                    onClick={() => onDismiss(item.id)}
                    className="inline-flex h-9 items-center gap-1.5 rounded-xl bg-white px-3 text-xs font-bold text-danger transition hover:bg-danger-soft"
                  >
                    <Icon name="eye" className="size-4" />
                    {t('sfOpenCase')}
                  </Link>
                  <Button variant="ghost" size="sm" className="text-white hover:bg-white/10" icon="x" onClick={() => onDismiss(item.id)} aria-label={t('close')} />
                </div>
              </li>
            )
          })}
        </ul>
      </section>
    </div>
  )
}
