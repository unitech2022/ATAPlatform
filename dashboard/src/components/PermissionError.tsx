import { useLang } from '../context/lang'
import { isApiError } from '../lib/api'
import { EmptyState } from './EmptyState'
import { ErrorState } from './ErrorState'

/**
 * Query error view that treats `403` as "no permission" (naming the missing permission code, e.g. `scheduling.manage`)
 * instead of a generic failure; every other error keeps the usual retry state.
 */
export function PermissionError({ error, permission, onRetry }: { error: unknown; permission: string; onRetry?: () => void }) {
  const { t } = useLang()
  if (isApiError(error) && error.status === 403) {
    return <EmptyState tone="danger" icon="shield" title={t('unauthorized')} description={`${t('permissionRequired')}: ${permission}`} />
  }
  return <ErrorState error={error} onRetry={onRetry} />
}
