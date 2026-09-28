import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { useDebouncedValue } from './useDebouncedValue'

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/

/** `YYYY-MM-DD` from a query-string value, or '' when missing/invalid. */
export function parseIsoDate(value: string | null) {
  return value && ISO_DATE.test(value) ? value : ''
}

/**
 * URL-synced list filters (same behaviour as TripsPage): every filter lives in the query string so the view
 * survives reloads and can be shared; changing a filter resets `page`.
 */
export function useUrlState() {
  const [params, setParams] = useSearchParams()

  const update = useCallback(
    (mutate: (next: URLSearchParams) => void, replace = false) => {
      setParams(
        (current) => {
          const next = new URLSearchParams(current)
          mutate(next)
          return next
        },
        { replace },
      )
    },
    [setParams],
  )

  const setFilter = useCallback(
    (key: string, value: string | boolean | null | undefined) => {
      update((next) => {
        if (value === true) next.set(key, 'true')
        else if (value) next.set(key, value)
        else next.delete(key)
        next.delete('page')
      })
    },
    [update],
  )

  const page = Math.max(1, Number(params.get('page')) || 1)
  const setPage = useCallback((value: number) => update((next) => next.set('page', String(value))), [update])

  return { params, update, setFilter, page, setPage }
}

/** Debounced search box bound to `?{key}=`; returns the input state plus the committed (URL) value. */
export function useUrlSearch(key = 'q') {
  const { params, update } = useUrlState()
  const committed = params.get(key) ?? ''
  const [input, setInput] = useState(committed)
  const debounced = useDebouncedValue(input.trim())

  useEffect(() => {
    if (debounced === committed) return
    update((next) => {
      if (debounced) next.set(key, debounced)
      else next.delete(key)
      next.delete('page')
    }, true)
  }, [debounced, committed, key, update])

  return { input, setInput, value: committed }
}
