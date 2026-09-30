import { useState } from 'react'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import type { Lang } from '../i18n'
import { cannedResponses } from '../lib/admin'
import { cannedFor, renderCanned, TICKET_TYPE_KEY } from '../lib/support'
import type { CannedResponse, TicketType } from '../lib/types'
import { Badge } from './Badge'
import { EmptyState } from './EmptyState'
import { ErrorState } from './ErrorState'
import { Modal } from './Modal'
import { PageSpinner } from './Spinner'

export interface CannedContext {
  type: TicketType
  ticketNumber: string
  tripNumber: string | null
  userName: string | null
  /** Language of the requester when known; the picker opens in it, falling back to the dashboard language. */
  userLanguage: Lang | null
}

/**
 * Canned-response picker: lists active responses that are generic or match the ticket type, shows each in the chosen language with
 * `{userName}` `{ticketNumber}` `{tripNumber}` already substituted, and hands the final text back to the composer.
 */
export function CannedPickerModal({ open, context, onClose, onPick }: { open: boolean; context: CannedContext; onClose: () => void; onPick: (text: string, code: string) => void }) {
  // Mounted only while open so the list is fetched on demand and the language starts from the requester's.
  return open ? <PickerDialog context={context} onClose={onClose} onPick={onPick} /> : null
}

function PickerDialog({ context, onClose, onPick }: { context: CannedContext; onClose: () => void; onPick: (text: string, code: string) => void }) {
  const { t, lang } = useLang()
  const query = useQuery(() => cannedResponses.list(), 'canned-responses')
  const [pickLang, setPickLang] = useState<Lang>(context.userLanguage ?? lang)

  const textOf = (response: CannedResponse) =>
    renderCanned(pickLang === 'ar' ? response.bodyAr : response.bodyEn, {
      userName: context.userName ?? '',
      ticketNumber: context.ticketNumber,
      tripNumber: context.tripNumber ?? '',
    })

  const items = cannedFor(query.data ?? [], context.type)

  return (
    <Modal
      open
      size="lg"
      title={t('spCannedPick')}
      description={t('spCannedPickCopy')}
      onClose={onClose}
      footer={
        <div className="flex w-full flex-wrap items-center justify-between gap-2">
          <span className="text-xs text-muted">{t('spCannedLanguage')}</span>
          <div className="flex gap-1 rounded-2xl bg-cloud p-1">
            {(['ar', 'en'] as const).map((value) => (
              <button
                key={value}
                type="button"
                aria-pressed={pickLang === value}
                onClick={() => setPickLang(value)}
                className={`rounded-xl px-3.5 py-1.5 text-sm font-bold transition ${pickLang === value ? 'bg-ink text-white' : 'text-muted hover:bg-line'}`}
              >
                {value === 'ar' ? 'العربية' : 'English'}
              </button>
            ))}
          </div>
        </div>
      }
    >
      {query.loading && !query.data ? (
        <PageSpinner />
      ) : query.error ? (
        <ErrorState error={query.error} onRetry={query.reload} />
      ) : items.length === 0 ? (
        <EmptyState icon="chat" title={t('spCannedNone')} />
      ) : (
        <ul className="space-y-2">
          {items.map((response) => (
            <li key={response.id}>
              <button
                type="button"
                onClick={() => onPick(textOf(response), response.code)}
                className="block w-full rounded-2xl border border-line p-4 text-start transition hover:border-brand hover:bg-brand-soft/30"
              >
                <span className="flex flex-wrap items-center gap-2">
                  <span className="font-bold">{response.title}</span>
                  <span className="ltr-nums text-xs text-muted">{response.code}</span>
                  {response.ticketType && <Badge tone="ink">{t(TICKET_TYPE_KEY[response.ticketType])}</Badge>}
                </span>
                <span dir={pickLang === 'ar' ? 'rtl' : 'ltr'} className="mt-1 line-clamp-3 block whitespace-pre-wrap break-words text-sm text-muted">
                  {textOf(response)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </Modal>
  )
}
