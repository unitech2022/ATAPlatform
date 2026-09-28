import { useState } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { isApiError } from '../lib/api'
import { tripSafety } from '../lib/admin'
import { formatDateTime } from '../lib/format'
import { MESSAGE_SENDER_KEY } from '../lib/safety'
import type { TripMessage } from '../lib/types'
import { Button } from './Button'
import { EmptyState } from './EmptyState'
import { Icon } from './Icon'

type PanelState = { kind: 'idle' } | { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready'; messages: TripMessage[] }

/**
 * Read-only trip chat (`GET /admin/trips/{id}/messages`). Every read is written to the audit log as
 * `trip_messages.view` (docs/09 §F12.7), so nothing is fetched until the admin explicitly asks.
 */
export function TripMessagesPanel({ tripId }: { tripId: string }) {
  const { t, lang } = useLang()
  const describe = useApiErrorMessage()
  const [state, setState] = useState<PanelState>({ kind: 'idle' })

  const load = async () => {
    setState({ kind: 'loading' })
    try {
      const messages = await tripSafety.messages(tripId)
      setState({ kind: 'ready', messages: [...messages].sort((a, b) => a.createdAt.localeCompare(b.createdAt)) })
    } catch (error) {
      setState({ kind: 'error', message: isApiError(error) && error.status === 404 ? t('sfChatUnavailable') : describe(error) })
    }
  }

  if (state.kind === 'idle' || state.kind === 'loading') {
    return (
      <div className="flex flex-col items-center gap-4 px-6 py-8 text-center">
        <span className="grid size-12 place-items-center rounded-2xl bg-amber-50 text-amber-700">
          <Icon name="eye" className="size-6" />
        </span>
        <p className="max-w-md text-sm text-muted">{t('sfChatAuditNotice')}</p>
        <Button variant="secondary" icon="chat" loading={state.kind === 'loading'} onClick={load}>
          {t('sfShowChat')}
        </Button>
      </div>
    )
  }

  if (state.kind === 'error') {
    return (
      <EmptyState
        tone="danger"
        icon="alert"
        title={state.message}
        action={
          <Button variant="secondary" icon="refresh" onClick={load}>
            {t('retry')}
          </Button>
        }
      />
    )
  }

  if (state.messages.length === 0) return <EmptyState icon="chat" title={t('sfNoMessages')} />

  return (
    <div className="space-y-3 px-5 pb-5 sm:px-6 sm:pb-6">
      <p className="flex items-center gap-2 text-xs text-muted">
        <Icon name="info" className="size-3.5" />
        {t('sfChatReadOnly')}
      </p>
      <ol className="space-y-2">
        {state.messages.map((message) => {
          const system = message.senderRole === 'system' || message.kind === 'system'
          const driver = message.senderRole === 'driver'
          return (
            <li key={message.id} className={`flex ${system ? 'justify-center' : driver ? 'justify-end' : 'justify-start'}`}>
              <div
                className={`max-w-[85%] rounded-2xl px-4 py-2.5 text-sm ${
                  system ? 'bg-cloud text-center text-xs text-muted' : driver ? 'bg-ink text-white' : 'bg-brand-soft text-ink'
                }`}
              >
                {!system && (
                  <p className={`mb-0.5 text-[11px] font-bold ${driver ? 'text-white/70' : 'text-brand'}`}>
                    {message.senderName || t(MESSAGE_SENDER_KEY[message.senderRole] ?? 'actorSystem')}
                    {message.kind === 'quick_reply' ? ` · ${t('sfQuickReply')}` : ''}
                  </p>
                )}
                <p className="whitespace-pre-wrap break-words">{message.body}</p>
                <p className={`mt-1 text-[11px] ${driver ? 'text-white/60' : 'text-muted'}`}>
                  {formatDateTime(message.createdAt, lang)}
                  {message.readAt ? ` · ${t('sfRead')}` : ''}
                </p>
              </div>
            </li>
          )
        })}
      </ol>
    </div>
  )
}
