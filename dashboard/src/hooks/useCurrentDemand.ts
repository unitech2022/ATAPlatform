import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useCallback, useEffect, useState } from 'react'
import { demand } from '../lib/admin'
import { ApiError } from '../lib/api'
import { session } from '../lib/session'
import type { CurrentDemand } from '../lib/types'
import { tripsHubUrl } from './useLiveSnapshot'

export const DEMAND_POLL_INTERVAL_MS = 30_000

export interface CurrentDemandState {
  items: CurrentDemand[] | null
  error: ApiError | null
  updatedAt: number | null
  /** True while the SignalR hub is connected and pushing `DemandChanged`. */
  live: boolean
  refresh: () => void
}

/**
 * `GET /admin/demand/current` every 30s (the guaranteed path). When the trips hub connects, a
 * `DemandChanged` push triggers an immediate refetch so the grid reacts within a second.
 */
export function useCurrentDemand(): CurrentDemandState {
  const [items, setItems] = useState<CurrentDemand[] | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  const [updatedAt, setUpdatedAt] = useState<number | null>(null)
  const [live, setLive] = useState(false)

  const refresh = useCallback(() => {
    demand
      .current()
      .then((next) => {
        setItems(Array.isArray(next) ? next : [])
        setError(null)
        setUpdatedAt(Date.now())
      })
      .catch((caught: unknown) => {
        setError(caught instanceof ApiError ? caught : new ApiError(0, 'unknown', String(caught)))
      })
  }, [])

  useEffect(() => {
    refresh()
    const timer = window.setInterval(refresh, DEMAND_POLL_INTERVAL_MS)
    return () => window.clearInterval(timer)
  }, [refresh])

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
    hub.on('DemandChanged', () => {
      if (active) refresh()
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
        // hub unavailable — polling keeps the page alive
      })
    return () => {
      active = false
      setLive(false)
      void hub.stop().catch(() => undefined)
    }
  }, [refresh])

  return { items, error, updatedAt, live, refresh }
}
