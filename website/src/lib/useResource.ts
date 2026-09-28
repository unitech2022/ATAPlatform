import { useCallback, useEffect, useRef, useState } from 'react'

interface ResourceState<T> {
  data: T | null
  error: unknown
  loading: boolean
}

export interface Resource<T> extends ResourceState<T> {
  /** Re-run the loader; `silent` keeps the current data visible without a loading state. */
  reload: (silent?: boolean) => void
}

/** Minimal async loader with cancellation on unmount / dependency change. */
export function useResource<T>(loader: () => Promise<T>, deps: readonly unknown[]): Resource<T> {
  const [state, setState] = useState<ResourceState<T>>({ data: null, error: null, loading: true })
  const [tick, setTick] = useState(0)
  const silentRef = useRef(false)
  const loaderRef = useRef(loader)
  const depsKey = JSON.stringify(deps)

  // Keep the latest loader without re-running the request on every render.
  useEffect(() => {
    loaderRef.current = loader
  })

  useEffect(() => {
    let cancelled = false
    if (!silentRef.current) setState((current) => ({ ...current, loading: true }))
    silentRef.current = false
    loaderRef
      .current()
      .then((data) => {
        if (!cancelled) setState({ data, error: null, loading: false })
      })
      .catch((error: unknown) => {
        if (!cancelled) setState((current) => ({ data: current.data, error, loading: false }))
      })
    return () => {
      cancelled = true
    }
  }, [depsKey, tick])

  const reload = useCallback((silent = false) => {
    silentRef.current = silent
    setTick((current) => current + 1)
  }, [])

  return { ...state, reload }
}
