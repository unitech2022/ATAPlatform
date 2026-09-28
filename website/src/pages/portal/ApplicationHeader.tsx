import { Icon, type IconName } from '../../components/Icon'
import { ApplicationStatusBadge } from '../../components/StatusBadge'
import { useI18n, type TranslationKey } from '../../i18n'
import { formatDate } from '../../lib/format'
import type { ApplicationStatus, DriverApplication } from '../../lib/types'

interface Banner {
  title: TranslationKey
  copy: TranslationKey
  icon: IconName
  className: string
  iconClassName: string
  showReason: boolean
  showPolling: boolean
}

const banners: Partial<Record<ApplicationStatus, Banner>> = {
  submitted: {
    title: 'portal.review.title',
    copy: 'portal.review.copy',
    icon: 'clock',
    className: 'bg-brand-soft text-ink',
    iconClassName: 'bg-brand text-white',
    showReason: false,
    showPolling: true,
  },
  under_review: {
    title: 'portal.review.title',
    copy: 'portal.review.copy',
    icon: 'clock',
    className: 'bg-brand-soft text-ink',
    iconClassName: 'bg-brand text-white',
    showReason: false,
    showPolling: true,
  },
  approved: {
    title: 'portal.approved.title',
    copy: 'portal.approved.copy',
    icon: 'check',
    className: 'bg-ink text-white',
    iconClassName: 'bg-brand text-white',
    showReason: false,
    showPolling: false,
  },
  rejected: {
    title: 'portal.rejected.title',
    copy: 'portal.rejected.copy',
    icon: 'document',
    className: 'bg-danger-soft text-ink',
    iconClassName: 'bg-danger text-white',
    showReason: true,
    showPolling: false,
  },
  suspended: {
    title: 'portal.suspended.title',
    copy: 'portal.suspended.copy',
    icon: 'shield',
    className: 'bg-danger-soft text-ink',
    iconClassName: 'bg-danger text-white',
    showReason: true,
    showPolling: false,
  },
}

export function ApplicationHeader({ application, name }: { application: DriverApplication; name: string | null }) {
  const { t, lang } = useI18n()
  const banner = banners[application.status]
  const muted = application.status === 'approved' ? 'text-white/70' : 'text-muted'

  return (
    <section className="mb-7">
      <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
        <div>
          <p className="mb-2 text-sm font-bold text-brand">{t('portal.eyebrow')}</p>
          <h1 className="text-3xl font-bold sm:text-4xl">{name ? t('portal.welcome', { name }) : t('portal.welcomeAnon')}</h1>
          <p className="mt-2 text-muted">{t('portal.copy')}</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="rounded-2xl bg-white px-5 py-4 text-center shadow-soft">
            <p className="text-xs font-bold text-muted">{t('portal.applicationNumber')}</p>
            <p className="mt-1 font-bold text-ink" dir="ltr">
              {application.applicationNumber}
            </p>
          </div>
          <div className="rounded-2xl bg-white px-5 py-4 text-center shadow-soft">
            <p className="mb-2 text-xs font-bold text-muted">{t('portal.status')}</p>
            <ApplicationStatusBadge status={application.status} />
          </div>
        </div>
      </div>

      {banner && (
        <div className={`mt-6 flex flex-col gap-4 rounded-3xl p-6 sm:flex-row sm:items-start ${banner.className}`}>
          <div className={`grid size-12 shrink-0 place-items-center rounded-full ${banner.iconClassName}`}>
            <Icon name={banner.icon} className="size-6" />
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-lg font-bold">{t(banner.title)}</p>
            <p className={`mt-1 text-sm leading-7 ${muted}`}>{t(banner.copy)}</p>
            {banner.showReason && application.rejectionReason && (
              <div className="mt-4 rounded-2xl bg-white p-4 text-sm">
                <p className="text-xs font-bold text-danger">{t('portal.rejectionReason')}</p>
                <p className="mt-1 font-bold text-ink" dir="auto">
                  {application.rejectionReason}
                </p>
              </div>
            )}
            <div className={`mt-3 flex flex-wrap gap-x-5 gap-y-1 text-xs ${muted}`}>
              {application.submittedAt && (
                <span>
                  {t('portal.submittedAt')}: {formatDate(application.submittedAt, lang)}
                </span>
              )}
              {application.approvedAt && (
                <span>
                  {t('portal.approvedAt')}: {formatDate(application.approvedAt, lang)}
                </span>
              )}
              {banner.showPolling && <span>{t('portal.review.autoRefresh')}</span>}
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
