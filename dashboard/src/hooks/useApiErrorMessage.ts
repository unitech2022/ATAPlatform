import { useCallback } from 'react'
import { useLang } from '../context/lang'
import { isApiError } from '../lib/api'

/** Turns any thrown value into a human-readable message in the current language. */
export function useApiErrorMessage() {
  const { t } = useLang()
  return useCallback(
    (error: unknown): string => {
      if (!isApiError(error)) return t('errorGeneric')
      if (error.code === 'network_error') return t('networkError')
      if (error.status === 401) return t('sessionExpired')
      if (error.status === 403) return t('unauthorized')
      if (error.status === 404) return t('notFound')
      return error.message || t('errorGeneric')
    },
    [t],
  )
}
