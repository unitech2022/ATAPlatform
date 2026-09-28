import { useCallback, useEffect, useEffectEvent, useState } from 'react'
import { ApiError } from '../lib/api'

export interface QueryState<T> {
  data: T | null
  error: ApiError | null
  loading: boolean
  reload: () => void
}

interface Result<T> {
  key: string
  data: T | null
  error: ApiError | null
}

/**
 * Minimal data-fetching hook: runs `fetcher` whenever `key` changes, ignores stale results
 * and exposes a manual reload. `key` must serialise every input the fetcher depends on.
 * While a new key loads, the previous data stays available so lists can show an overlay.
 */
export function useQuery<T>(fetcher: () => Promise<T>, key: string): QueryState<T> {
  const [tick, setTick] = useState(0)
  const [result, setResult] = useState<Result<T> | null>(null)
  const effectiveKey = `${key}#${tick}`
  const run = useEffectEvent(fetcher)

  useEffect(() => {
    let active = true
    run()
      .then((data) => {
        if (active) setResult({ key: effectiveKey, data, error: null })
      })
      .catch((caught: unknown) => {
        if (!active) return
        const error = caught instanceof ApiError ? caught : new ApiError(0, 'unknown', String(caught))
        setResult((current) => ({ key: effectiveKey, data: current?.data ?? null, error }))
      })
    return () => {
      active = false
    }
  }, [effectiveKey])

  const reload = useCallback(() => setTick((current) => current + 1), [])
  const loading = result === null || result.key !== effectiveKey

  return {
    data: result?.data ?? null,
    error: loading ? null : result.error,
    loading,
    reload,
  }
}
