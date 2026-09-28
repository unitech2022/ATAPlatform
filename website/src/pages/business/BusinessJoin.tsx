import { Link } from 'react-router'
import { Card } from '../../components/Card'
import { Icon } from '../../components/Icon'
import { SiteLayout } from '../../components/SiteLayout'
import { StoreLinks } from '../../components/StoreLinks'
import { useI18n } from '../../i18n'

/**
 * `/business/join/:token` — landing for the SMS invitation link. Invitations are
 * accepted by phone-number match (employees in the app, admins on first portal
 * sign-in), so the token itself is never sent anywhere from this page.
 */
export function BusinessJoin() {
  const { t } = useI18n()
  return (
    <SiteLayout>
      <title>{t('bizJoin.pageTitle')}</title>
      <meta name="robots" content="noindex" />
      <div className="mx-auto max-w-5xl px-4 py-12 sm:px-5 lg:py-16">
        <div className="mb-10 text-center">
          <div className="mx-auto mb-6 grid size-16 place-items-center rounded-2xl bg-brand-soft text-brand">
            <Icon name="building" className="size-8" />
          </div>
          <h1 className="text-3xl font-bold sm:text-4xl">{t('bizJoin.title')}</h1>
          <p className="mx-auto mt-3 max-w-2xl leading-7 text-muted">{t('bizJoin.copy')}</p>
        </div>
        <div className="grid gap-5 lg:grid-cols-2">
          <Card className="flex flex-col sm:p-8">
            <div className="mb-5 flex items-center gap-3">
              <span className="grid size-11 place-items-center rounded-xl bg-brand-soft text-brand">
                <Icon name="user" />
              </span>
              <h2 className="text-xl font-bold">{t('bizJoin.employee.title')}</h2>
            </div>
            <ol className="list-decimal space-y-3 ps-5 leading-7 text-muted">
              <li>{t('bizJoin.employee.step1')}</li>
              <li>{t('bizJoin.employee.step2')}</li>
              <li>{t('bizJoin.employee.step3')}</li>
            </ol>
            <StoreLinks className="mt-6" />
          </Card>
          <Card className="flex flex-col sm:p-8">
            <div className="mb-5 flex items-center gap-3">
              <span className="grid size-11 place-items-center rounded-xl bg-ink text-white">
                <Icon name="lock" />
              </span>
              <h2 className="text-xl font-bold">{t('bizJoin.admin.title')}</h2>
            </div>
            <p className="leading-7 text-muted">{t('bizJoin.admin.copy')}</p>
            <Link
              to="/business/login"
              className="mt-6 inline-flex items-center justify-center gap-2 rounded-2xl bg-ink px-6 py-4 font-bold text-white shadow-button hover:bg-ink-soft sm:self-start"
            >
              <Icon name="building" className="size-5" />
              {t('bizJoin.admin.cta')}
            </Link>
          </Card>
        </div>
        <p className="mt-8 text-center text-sm text-muted">{t('bizJoin.expiry')}</p>
      </div>
    </SiteLayout>
  )
}
