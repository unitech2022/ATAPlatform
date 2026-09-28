import { useEffect, useRef, useState } from 'react'
import { isApiError, publicShareApi } from './api'
import type { PublicTripShare, TripStatus } from './types'

/** What the public page shows, collapsed from the F8 trip status. */
export type SharePhase = 'searching' | 'en_route' | 'arrived' | 'in_trip' | 'completed' | 'cancelled'

export function sharePhase(status: TripStatus): SharePhase {
  switch (status) {
    case 'driver_assigned':
    case 'driver_en_route':
      return 'en_route'
    case 'driver_arrived':
    case 'waiting':
    case 'pin_verified': // shown as "waiting" per F12.7
      return 'arrived'
    case 'in_trip':
      return 'in_trip'
    case 'completed':
      return 'completed'
    case 'cancelled':
    case 'no_drivers':
      return 'cancelled'
    default:
      return 'searching'
  }
}

export function isTerminalPhase(phase: SharePhase): boolean {
  return phase === 'completed' || phase === 'cancelled'
}

export type ShareFailure = 'not_found' | 'expired' | 'rate_limited' | 'network'

export interface PublicShareState {
  data: PublicTripShare | null
  failure: ShareFailure | null
  loading: boolean
  updatedAt: Date | null
}

const DEFAULT_REFRESH_SECONDS = 10
const MIN_REFRESH_SECONDS = 5
const MAX_BACKOFF_SECONDS = 60

function failureOf(error: unknown): ShareFailure {
  if (isApiError(error)) {
    if (error.status === 404 || error.code === 'share_not_found') return 'not_found'
    if (error.status === 410 || error.code === 'share_expired') return 'expired'
    if (error.status === 429 || error.code === 'rate_limited') return 'rate_limited'
  }
  return 'network'
}

/**
 * Polls `GET /public/trip-shares/{token}` every `refreshSeconds` (server-driven),
 * backs off on 429/network errors, pauses while the tab is hidden and stops for
 * good on 404/410 or once the trip has ended. Mount it under `key={token}` so a
 * new token starts from a clean state.
 */
export function usePublicShare(token: string): PublicShareState {
  const [state, setState] = useState<PublicShareState>({ data: null, failure: null, loading: true, updatedAt: null })
  const timerRef = useRef<number | null>(null)

  useEffect(() => {
    let cancelled = false
    let backoff = 0
    let stopped = false

    const schedule = (seconds: number) => {
      if (cancelled || stopped) return
      timerRef.current = window.setTimeout(() => void tick(), seconds * 1000)
    }

    const tick = async () => {
      if (cancelled) return
      if (document.visibilityState === 'hidden') {
        // Resume from the visibility listener instead of polling in the background.
        timerRef.current = null
        return
      }
      try {
        const data = await publicShareApi.get(token)
        if (cancelled) return
        backoff = 0
        setState({ data, failure: null, loading: false, updatedAt: new Date() })
        if (isTerminalPhase(sharePhase(data.status))) {
          stopped = true
          return
        }
        schedule(Math.max(MIN_REFRESH_SECONDS, data.refreshSeconds ?? DEFAULT_REFRESH_SECONDS))
      } catch (error) {
        if (cancelled) return
        const failure = failureOf(error)
        setState((current) => ({ ...current, failure, loading: false }))
        if (failure === 'not_found' || failure === 'expired') {
          stopped = true
          return
        }
        const retryAfter = isApiError(error) ? error.detailNumber('retryAfterSeconds') : null
        backoff = Math.min(MAX_BACKOFF_SECONDS, backoff ? backoff * 2 : DEFAULT_REFRESH_SECONDS)
        schedule(retryAfter ?? backoff)
      }
    }

    const onVisible = () => {
      if (document.visibilityState === 'visible' && timerRef.current === null && !stopped && !cancelled) void tick()
    }

    void tick()
    document.addEventListener('visibilitychange', onVisible)
    return () => {
      cancelled = true
      document.removeEventListener('visibilitychange', onVisible)
      if (timerRef.current !== null) window.clearTimeout(timerRef.current)
      timerRef.current = null
    }
  }, [token])

  return state
}
