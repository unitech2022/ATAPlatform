import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { Button } from './Button'
import { EmptyState } from './EmptyState'

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  return (
    <EmptyState
      tone="danger"
      icon="alert"
      title={t('errorTitle')}
      description={describe(error)}
      action={
        onRetry && (
          <Button variant="secondary" icon="refresh" onClick={onRetry}>
            {t('retry')}
          </Button>
        )
      }
    />
  )
}
