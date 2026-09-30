import { useLang } from '../context/lang'
import { formatDateTime } from '../lib/format'
import type { SupportMessage, TicketAttachment } from '../lib/types'
import { EmptyState } from './EmptyState'
import { Icon } from './Icon'

/**
 * Conversation of a ticket. Public agent replies sit on the end side in ink, user messages on the start side in brand, system lines
 * are centred, and internal notes (`isInternal`) are amber with a dashed border and a lock label so they can never be mistaken for a
 * reply the user will see.
 */
export function TicketThread({
  messages,
  onOpenAttachment,
}: {
  messages: SupportMessage[]
  onOpenAttachment: (attachment: TicketAttachment) => void
}) {
  const { t, lang } = useLang()
  const ordered = [...messages].sort((a, b) => a.createdAt.localeCompare(b.createdAt))

  if (ordered.length === 0) return <EmptyState icon="chat" title={t('spNoMessages')} />

  return (
    <ol className="space-y-3" aria-label={t('spThread')}>
      {ordered.map((message) => {
        const system = message.authorRole === 'system'
        const agent = message.authorRole === 'agent'
        const internal = message.isInternal

        if (system && !internal) {
          return (
            <li key={message.id} className="flex justify-center">
              <p className="max-w-[90%] rounded-2xl bg-cloud px-4 py-2 text-center text-xs text-muted">
                <span className="whitespace-pre-wrap break-words">{message.body}</span>
                <span className="mt-0.5 block text-[11px]">{formatDateTime(message.createdAt, lang)}</span>
              </p>
            </li>
          )
        }

        const tone = internal
          ? 'border-2 border-dashed border-amber-300 bg-amber-50 text-ink'
          : agent
            ? 'bg-ink text-white'
            : 'bg-brand-soft text-ink'
        const muted = internal ? 'text-amber-700' : agent ? 'text-white/70' : 'text-muted'
        const name = message.authorName || t(system ? 'actorSystem' : agent ? 'spAgent' : 'spUser')

        return (
          <li key={message.id} data-internal={internal ? 'true' : undefined} className={`flex ${agent || internal ? 'justify-end' : 'justify-start'}`}>
            <div className={`max-w-[92%] rounded-2xl px-4 py-3 text-sm sm:max-w-[80%] ${tone}`}>
              <p className={`mb-1 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[11px] font-bold ${internal ? 'text-amber-700' : agent ? 'text-white/70' : 'text-brand'}`}>
                {internal && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-amber-200/70 px-2 py-0.5 text-amber-800">
                    <Icon name="eye" className="size-3" />
                    {t('spInternalNote')}
                  </span>
                )}
                <span>{name}</span>
              </p>
              <p className="whitespace-pre-wrap break-words">{message.body}</p>
              {message.attachments.length > 0 && (
                <ul className="mt-2 flex flex-wrap gap-2">
                  {message.attachments.map((attachment) => (
                    <li key={attachment.fileId}>
                      <button
                        type="button"
                        onClick={() => onOpenAttachment(attachment)}
                        className={`inline-flex max-w-full items-center gap-1.5 rounded-xl px-2.5 py-1.5 text-xs font-bold transition ${
                          internal ? 'bg-white text-ink hover:bg-amber-100' : agent ? 'bg-white/15 text-white hover:bg-white/25' : 'bg-white text-ink hover:bg-cloud'
                        }`}
                      >
                        <Icon name="document" className="size-4 shrink-0" />
                        <span className="truncate">{attachment.fileName ?? t('spAttachment')}</span>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
              <p className={`mt-1.5 text-[11px] ${muted}`}>
                {formatDateTime(message.createdAt, lang)}
                {internal ? ` · ${t('spInternalHint')}` : ''}
              </p>
            </div>
          </li>
        )
      })}
    </ol>
  )
}
