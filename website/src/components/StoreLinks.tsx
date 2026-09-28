import { useI18n } from '../i18n'
import { Button } from './Button'

/** App Store / Google Play buttons (disabled until the store listings are live). */
export function StoreLinks({ className = '' }: { className?: string }) {
  const { t } = useI18n()
  return (
    <div className={`flex w-full flex-col gap-3 sm:w-auto sm:flex-row ${className}`}>
      {['App Store', 'Google Play'].map((store) => (
        <Button key={store} variant="secondary" disabled className="justify-between gap-4 sm:min-w-44">
          <span dir="ltr">{store}</span>
          <span className="rounded-full bg-cloud px-2 py-0.5 text-xs text-muted">{t('stores.soon')}</span>
        </Button>
      ))}
    </div>
  )
}
