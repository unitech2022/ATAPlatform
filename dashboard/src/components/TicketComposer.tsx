import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { support } from '../lib/admin'
import type { SupportTicketDetail } from '../lib/types'
import { Button } from './Button'
import { CannedPickerModal } from './CannedPickerModal'
import { Icon } from './Icon'
import { Textarea } from './Field'

const MAX_BODY = 4000

/**
 * Reply box of the ticket thread: a segmented "reply to user / internal note" switch (the textarea turns amber for notes), the
 * canned-response picker and a character counter for the 4000-character limit (§F18.1 `support_messages.body`).
 */
export function TicketComposer({ ticket, onSent }: { ticket: SupportTicketDetail; onSent: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [body, setBody] = useState('')
  const [internal, setInternal] = useState(false)
  const [cannedCode, setCannedCode] = useState<string | null>(null)
  const [picking, setPicking] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  const closed = ticket.status === 'closed'
  if (closed) {
    return (
      <div className="flex items-start gap-3 rounded-2xl bg-cloud p-4 text-sm text-muted">
        <Icon name="info" className="mt-0.5 size-4 shrink-0" />
        <p>{t('spClosedNotice')}</p>
      </div>
    )
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const text = body.trim()
    if (!text) {
      setError(t('fieldRequired'))
      return
    }
    if (text.length > MAX_BODY) {
      setError(t('spBodyTooLong'))
      return
    }
    setSending(true)
    try {
      await support.reply(ticket.id, { body: text, isInternal: internal, cannedResponseCode: cannedCode ?? undefined })
      toast.success(internal ? t('spNoteAdded') : t('spReplySent'))
      setBody('')
      setCannedCode(null)
      onSent()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSending(false)
    }
  }

  const insert = (text: string, code: string) => {
    setBody((current) => (current.trim() ? `${current.replace(/\s+$/, '')}\n\n${text}` : text))
    setCannedCode(code)
    setError(null)
    setPicking(false)
  }

  return (
    <form onSubmit={submit} noValidate className={`space-y-3 rounded-2xl border-2 p-4 transition ${internal ? 'border-dashed border-amber-300 bg-amber-50/60' : 'border-line'}`}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex gap-1 rounded-2xl bg-cloud p-1" role="group" aria-label={t('spComposerMode')}>
          {([false, true] as const).map((value) => (
            <button
              key={String(value)}
              type="button"
              aria-pressed={internal === value}
              onClick={() => setInternal(value)}
              className={`rounded-xl px-3.5 py-1.5 text-sm font-bold transition ${internal === value ? (value ? 'bg-amber-500 text-white' : 'bg-ink text-white') : 'text-muted hover:bg-line'}`}
            >
              {value ? t('spInternalNote') : t('spReplyToUser')}
            </button>
          ))}
        </div>
        <Button variant="secondary" size="sm" icon="chat" onClick={() => setPicking(true)}>
          {t('spCannedInsert')}
        </Button>
      </div>
      <Textarea
        id="ticket-reply"
        aria-label={internal ? t('spInternalNote') : t('spReplyToUser')}
        placeholder={internal ? t('spInternalPlaceholder') : t('spReplyPlaceholder')}
        className={`min-h-32 ${internal ? 'bg-amber-50' : ''}`}
        maxLength={MAX_BODY}
        value={body}
        error={error}
        onChange={(event) => {
          setBody(event.target.value)
          setError(null)
        }}
      />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className={`text-xs ${internal ? 'font-bold text-amber-700' : 'text-muted'}`}>
          {internal ? t('spInternalHint') : t('spReplyHint')} <span className="ltr-nums text-muted">· {body.length}/{MAX_BODY}</span>
        </p>
        <Button type="submit" icon={internal ? 'edit' : 'send'} variant={internal ? 'secondary' : 'primary'} loading={sending}>
          {internal ? t('spAddNote') : t('spSendReply')}
        </Button>
      </div>
      <CannedPickerModal
        open={picking}
        onClose={() => setPicking(false)}
        onPick={insert}
        context={{
          type: ticket.type,
          ticketNumber: ticket.ticketNumber,
          tripNumber: ticket.trip?.tripNumber ?? null,
          userName: ticket.requester.fullName,
          userLanguage: ticket.requester.language ?? null,
        }}
      />
    </form>
  )
}
