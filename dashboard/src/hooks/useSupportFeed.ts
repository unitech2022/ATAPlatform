import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useCallback, useEffect, useEffectEvent, useRef, useState } from 'react'
import { support } from '../lib/admin'
import { session } from '../lib/session'
import { SUPPORT_POLL_INTERVAL_MS } from '../lib/support'
import type { SupportTicketUpdate, TicketListItem } from '../lib/types'
import { tripsHubUrl } from './useLiveSnapshot'

export type SupportFeedEvent =
  | { kind: 'created'; ticket: TicketListItem }
  | { kind: 'updated'; update: SupportTicketUpdate }
  /** Polling fallback noticed a change it cannot describe precisely. */
  | { kind: 'changed' }

export interface SupportFeedOptions {
  /** Poll the newest tickets while the hub is down (detects new tickets and user messages). */
  watchQueue?: boolean
  onEvent?: (event: SupportFeedEvent) => void
}

export interface SupportFeedState {
  /** True while the SignalR hub is connected (events arrive instantly). */
  live: boolean
  /** Ids of tickets that received a user message (or were created by a user) since the page opened / the last `clear`. */
  incoming: string[]
  clear: () => void
}

function isTicketSummary(value: unknown): value is TicketListItem {
  if (!value || typeof value !== 'object') return false
  const candidate = value as Partial<TicketListItem>
  return typeof candidate.id === 'string' && typeof candidate.ticketNumber === 'string'
}

function isUpdate(value: unknown): value is SupportTicketUpdate {
  if (!value || typeof value !== 'object') return false
  const candidate = value as Partial<SupportTicketUpdate>
  return typeof candidate.ticketId === 'string' && typeof candidate.status === 'string'
}

const signatureOf = (item: TicketListItem) => `${item.status}|${item.priority}|${item.assignedToName ?? ''}|${item.lastMessageAt}|${item.lastMessageBy}|${item.slaState}`

/**
 * Support events for the `admins` group of `/hubs/trips` (docs/11 §F18.3 SignalR): `SupportTicketCreated(ticketSummary)` and
 * `SupportTicketUpdated({ ticketId, status, priority, lastMessageBy })`. The hub is assumed to be the same one the safety feed uses.
 * While it is not connected and `watchQueue` is set, the newest tickets are polled every 15 s and diffed so new user messages still
 * raise the live badge.
 */
export function useSupportFeed({ watchQueue = false, onEvent }: SupportFeedOptions = {}): SupportFeedState {
  const [live, setLive] = useState(false)
  const [incoming, setIncoming] = useState<string[]>([])
  const emit = useEffectEvent((event: SupportFeedEvent) => onEvent?.(event))
  const known = useRef<Map<string, string> | null>(null)

  const markIncoming = useCallback((id: string) => {
    setIncoming((current) => (current.includes(id) ? current : [id, ...current].slice(0, 99)))
  }, [])

  // Polling fallback.
  useEffect(() => {
    if (live || !watchQueue) return
    let active = true

    const poll = async () => {
      const page = await support.tickets({ page: 1, pageSize: 50 })
      if (!active) return
      const next = new Map(page.items.map((item) => [item.id, signatureOf(item)]))
      const previous = known.current
      known.current = next
      if (!previous) return
      let changed = false
      for (const item of page.items) {
        const before = previous.get(item.id)
        if (before === undefined) {
          emit({ kind: 'created', ticket: item })
          if (item.lastMessageBy === 'user') markIncoming(item.id)
        } else if (before !== next.get(item.id)) {
          changed = true
          if (item.lastMessageBy === 'user' && !before.includes(`|${item.lastMessageAt}|user|`)) markIncoming(item.id)
        }
      }
      if (changed) emit({ kind: 'changed' })
    }

    const tick = () => void poll().catch(() => undefined)
    tick()
    const timer = window.setInterval(tick, SUPPORT_POLL_INTERVAL_MS)
    return () => {
      active = false
      window.clearInterval(timer)
    }
  }, [live, watchQueue, markIncoming])

  // SignalR (best effort).
  useEffect(() => {
    let active = true
    let connection: HubConnection | null = null
    try {
      connection = new HubConnectionBuilder()
        .withUrl(tripsHubUrl(), { accessTokenFactory: () => session.getAccessToken() ?? '' })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.None)
        .build()
    } catch {
      return
    }
    const hub = connection
    hub.on('SupportTicketCreated', (payload: unknown) => {
      if (!active || !isTicketSummary(payload)) return
      emit({ kind: 'created', ticket: payload })
      if (payload.lastMessageBy === 'user') markIncoming(payload.id)
    })
    hub.on('SupportTicketUpdated', (payload: unknown) => {
      if (!active || !isUpdate(payload)) return
      emit({ kind: 'updated', update: payload })
      if (payload.lastMessageBy === 'user') markIncoming(payload.ticketId)
    })
    hub.onreconnecting(() => active && setLive(false))
    hub.onreconnected(() => active && setLive(true))
    hub.onclose(() => active && setLive(false))
    hub
      .start()
      .then(() => {
        if (active && hub.state === HubConnectionState.Connected) setLive(true)
      })
      .catch(() => {
        // hub unavailable — polling covers it
      })
    return () => {
      active = false
      setLive(false)
      void hub.stop().catch(() => undefined)
    }
  }, [markIncoming])

  const clear = useCallback(() => setIncoming([]), [])

  return { live, incoming, clear }
}
