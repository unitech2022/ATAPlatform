import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useCallback, useEffect, useEffectEvent, useRef, useState } from 'react'
import { safety } from '../lib/admin'
import { SAFETY_POLL_INTERVAL_MS } from '../lib/safety'
import { session } from '../lib/session'
import type { SafetyAlert, SafetyCaseListItem } from '../lib/types'
import { tripsHubUrl } from './useLiveSnapshot'

export type SafetyFeedEvent =
  | { kind: 'case_opened'; item: SafetyCaseListItem }
  | { kind: 'case_updated'; item: SafetyCaseListItem }
  | { kind: 'alert_raised'; alert: SafetyAlert }
  /** Polling fallback noticed a change it cannot describe precisely (status/assignee of an open case). */
  | { kind: 'changed' }

export interface SafetyFeedOptions {
  /** Poll open cases while the hub is down (detects new SOS cases and changes). */
  watchCases?: boolean
  /** Poll pending alerts while the hub is down. */
  watchAlerts?: boolean
  onEvent?: (event: SafetyFeedEvent) => void
}

export interface SafetyFeedState {
  /** True while the SignalR hub is connected (events arrive instantly). */
  live: boolean
  /** New critical / SOS cases since the page opened, newest first, until dismissed. */
  incoming: SafetyCaseListItem[]
  dismiss: (id: string) => void
  dismissAll: () => void
}

function isCase(value: unknown): value is SafetyCaseListItem {
  if (!value || typeof value !== 'object') return false
  const candidate = value as Partial<SafetyCaseListItem>
  return typeof candidate.id === 'string' && typeof candidate.caseNumber === 'string'
}

function isAlert(value: unknown): value is SafetyAlert {
  if (!value || typeof value !== 'object') return false
  const candidate = value as Partial<SafetyAlert>
  return typeof candidate.id === 'string' && typeof candidate.tripId === 'string'
}

function isUrgent(item: SafetyCaseListItem) {
  return item.type === 'sos' || item.priority === 'critical'
}

/**
 * Safety events for the `admins` group of `/hubs/trips` (docs/09 §F12.8): `SafetyCaseOpened`,
 * `SafetyCaseUpdated`, `SafetyAlertRaised`. Whenever the hub is not connected, open cases and pending
 * alerts are polled every 10 s and diffed so new SOS cases still raise the banner.
 */
export function useSafetyFeed({ watchCases = false, watchAlerts = false, onEvent }: SafetyFeedOptions = {}): SafetyFeedState {
  const [live, setLive] = useState(false)
  const [incoming, setIncoming] = useState<SafetyCaseListItem[]>([])
  const emit = useEffectEvent((event: SafetyFeedEvent) => onEvent?.(event))
  const knownCases = useRef<Map<string, string> | null>(null)
  const knownAlerts = useRef<Set<string> | null>(null)

  const pushIncoming = useCallback((item: SafetyCaseListItem) => {
    if (!isUrgent(item)) return
    setIncoming((current) => [item, ...current.filter((existing) => existing.id !== item.id)].slice(0, 5))
  }, [])

  // Polling fallback.
  useEffect(() => {
    if (live || (!watchCases && !watchAlerts)) return
    let active = true

    const pollCases = async () => {
      const page = await safety.cases({ status: 'open', page: 1, pageSize: 50 })
      if (!active) return
      const next = new Map(page.items.map((item) => [item.id, `${item.status}|${item.priority}|${item.assignedToName ?? ''}`]))
      const previous = knownCases.current
      knownCases.current = next
      if (!previous) return
      let changed = previous.size !== next.size
      for (const item of page.items) {
        const before = previous.get(item.id)
        if (before === undefined) {
          emit({ kind: 'case_opened', item })
          pushIncoming(item)
        } else if (before !== next.get(item.id)) {
          changed = true
        }
      }
      if (changed) emit({ kind: 'changed' })
    }

    const pollAlerts = async () => {
      const page = await safety.alerts({ status: 'pending_rider', page: 1, pageSize: 50 })
      if (!active) return
      const next = new Set(page.items.map((alert) => alert.id))
      const previous = knownAlerts.current
      knownAlerts.current = next
      if (!previous) return
      for (const alert of page.items) if (!previous.has(alert.id)) emit({ kind: 'alert_raised', alert })
      if (previous.size !== next.size) emit({ kind: 'changed' })
    }

    const tick = () => {
      if (watchCases) void pollCases().catch(() => undefined)
      if (watchAlerts) void pollAlerts().catch(() => undefined)
    }
    tick()
    const timer = window.setInterval(tick, SAFETY_POLL_INTERVAL_MS)
    return () => {
      active = false
      window.clearInterval(timer)
    }
  }, [live, watchCases, watchAlerts, pushIncoming])

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
    hub.on('SafetyCaseOpened', (payload: unknown) => {
      if (!active || !isCase(payload)) return
      emit({ kind: 'case_opened', item: payload })
      pushIncoming(payload)
    })
    hub.on('SafetyCaseUpdated', (payload: unknown) => {
      if (active && isCase(payload)) emit({ kind: 'case_updated', item: payload })
    })
    hub.on('SafetyAlertRaised', (payload: unknown) => {
      if (active && isAlert(payload)) emit({ kind: 'alert_raised', alert: payload })
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
  }, [pushIncoming])

  const dismiss = useCallback((id: string) => setIncoming((current) => current.filter((item) => item.id !== id)), [])
  const dismissAll = useCallback(() => setIncoming([]), [])

  return { live, incoming, dismiss, dismissAll }
}
