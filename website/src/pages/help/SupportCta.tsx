import { Card } from '../../components/Card'
import { Icon } from '../../components/Icon'
import { StoreLinks } from '../../components/StoreLinks'
import { useI18n } from '../../i18n'
import { SUPPORT_EMAIL, SUPPORT_PHONE, SUPPORT_PHONE_HREF } from '../../lib/catalog'

/** "Didn't find your answer? Open a ticket from the ATA app" (tickets are app-only, F18 decision 9). */
export function SupportCta({ className = '' }: { className?: string }) {
  const { t } = useI18n()
  return (
    <Card tone="dark" className={`sm:p-8 ${className}`}>
      <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-start gap-4">
          <span className="grid size-12 shrink-0 place-items-center rounded-2xl bg-brand text-white">
            <Icon name="help" className="size-6" />
          </span>
          <div>
            <p className="text-xl font-bold">{t('help.cta.title')}</p>
            <p className="mt-2 text-sm leading-7 text-white/70">{t('help.cta.copy')}</p>
            <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-sm font-bold">
              <a href={SUPPORT_PHONE_HREF} className="flex items-center gap-2 text-white hover:text-brand">
                <Icon name="phone" className="size-4" />
                <span dir="ltr">{SUPPORT_PHONE}</span>
              </a>
              <a href={`mailto:${SUPPORT_EMAIL}`} className="flex items-center gap-2 text-white hover:text-brand">
                <Icon name="mail" className="size-4" />
                <span dir="ltr">{SUPPORT_EMAIL}</span>
              </a>
            </div>
          </div>
        </div>
        <StoreLinks />
      </div>
    </Card>
  )
}
