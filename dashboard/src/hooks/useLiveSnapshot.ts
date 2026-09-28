import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useCallback, useEffect, useState } from 'react'
import { live } from '../lib/admin'
import { API_BASE_URL, ApiError } from '../lib/api'
import { session } from '../lib/session'
import type { LiveSnapshot } from '../lib/types'

export const LIVE_POLL_INTERVAL_MS = 5000

export type LiveSource = 'connecting' | 'hub' | 'polling'

export interface LiveState {
  snapshot: LiveSnapshot | null
  error: ApiError | null
  /** Where the latest snapshot came from: the SignalR hub or the REST poller. */
  source: LiveSource
  updatedAt: number | null
  refresh: () => void
}

/** `/hubs/trips` lives at the API origin (outside `/api/v1`), overridable with VITE_HUB_URL. */
export function tripsHubUrl() {
  const explicit = import.meta.env.VITE_HUB_URL
  if (explicit) return explicit
  try {
    return `${new URL(API_BASE_URL).origin}/hubs/trips`
  } catch {
    return '/hubs/trips'
  }
}

function isSnapshot(value: unknown): value is LiveSnapshot {
  if (!value || typeof value !== 'object') return false
  const candidate = value as Partial<LiveSnapshot>
  return Array.isArray(candidate.drivers) && Array.isArray(candidate.activeTrips) && Array.isArray(candidate.searchingTrips)
}

/**
 * Live snapshot of drivers and trips. Polls `GET /admin/live` every 5s (the guaranteed path)
 * and, when the SignalR hub connects, switches to its `LiveSnapshot` pushes; polling resumes
 * automatically whenever the hub drops.
 */
export function useLiveSnapshot(): LiveState {
  const [snapshot, setSnapshot] = useState<LiveSnapshot | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  const [updatedAt, setUpdatedAt] = useState<number | null>(null)
  const [hubConnected, setHubConnected] = useState(false)

  const apply = useCallback((next: LiveSnapshot) => {
    setSnapshot(next)
    setError(null)
    setUpdatedAt(Date.now())
  }, [])

  const fail = useCallback((caught: unknown) => {
    setError(caught instanceof ApiError ? caught : new ApiError(0, 'unknown', String(caught)))
  }, [])

  // Polling: runs while the hub is not connected.
  useEffect(() => {
    if (hubConnected) return
    let active = true
    const load = () =>
      live
        .snapshot()
        .then((next) => active && apply(next))
        .catch((caught: unknown) => active && fail(caught))
    void load()
    const timer = window.setInterval(() => void load(), LIVE_POLL_INTERVAL_MS)
    return () => {
      active = false
      window.clearInterval(timer)
    }
  }, [hubConnected, apply, fail])

  // SignalR: best effort. Any failure simply leaves polling in charge.
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

    hub.on('LiveSnapshot', (payload: unknown) => {
      if (active && isSnapshot(payload)) apply(payload)
    })
    hub.onreconnecting(() => active && setHubConnected(false))
    hub.onreconnected(() => active && setHubConnected(true))
    hub.onclose(() => active && setHubConnected(false))

    hub
      .start()
      .then(() => {
        if (active && hub.state === HubConnectionState.Connected) setHubConnected(true)
      })
      .catch(() => {
        // hub unavailable (older backend, proxy, auth) — polling keeps the page alive
      })

    return () => {
      active = false
      setHubConnected(false)
      void hub.stop().catch(() => undefined)
    }
  }, [apply])

  /** One-off manual refetch, useful in either mode. */
  const refresh = useCallback(() => {
    live.snapshot().then(apply).catch(fail)
  }, [apply, fail])

  const source: LiveSource = hubConnected ? 'hub' : snapshot === null && error === null ? 'connecting' : 'polling'

  return { snapshot, error, source, updatedAt, refresh }
}
