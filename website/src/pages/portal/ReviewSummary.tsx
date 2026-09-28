import { Card } from '../../components/Card'
import { Icon } from '../../components/Icon'
import { useI18n } from '../../i18n'
import { formatDate } from '../../lib/format'
import type { City, DriverApplication, RideCategory } from '../../lib/types'

interface ReviewSummaryProps {
  application: DriverApplication
  cities: City[] | null
  categories: RideCategory[] | null
}

function Row({ label, value }: { label: string; value: string | null }) {
  const { t } = useI18n()
  return (
    <div className="flex items-start justify-between gap-4 border-b border-line py-3 last:border-0">
      <dt className="text-sm text-muted">{label}</dt>
      <dd className={`text-end text-sm font-bold ${value ? 'text-ink' : 'text-muted'}`} dir="auto">
        {value ?? t('portal.notProvided')}
      </dd>
    </div>
  )
}

/** Read-only profile + vehicle summary shown once the application is no longer editable. */
export function ReviewSummary({ application, cities, categories }: ReviewSummaryProps) {
  const { t, lang } = useI18n()
  const { profile, vehicle } = application

  const cityName = cities?.find((city) => city.id === profile.cityId)?.name ?? profile.cityId
  const gender = profile.gender === 'male' || profile.gender === 'female' ? t(`profile.${profile.gender}`) : null
  const categoryName = vehicle ? (categories?.find((item) => item.id === vehicle.rideCategoryId)?.name ?? vehicle.rideCategoryId) : null

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card>
        <div className="mb-4 flex items-center gap-3">
          <div className="grid size-11 place-items-center rounded-xl bg-brand-soft text-brand">
            <Icon name="user" />
          </div>
          <p className="text-lg font-bold">{t('portal.steps.profile')}</p>
        </div>
        <dl>
          <Row label={t('profile.fullName')} value={profile.fullName} />
          <Row label={t('profile.nationalId')} value={profile.nationalId} />
          <Row label={t('profile.dateOfBirth')} value={profile.dateOfBirth ? formatDate(profile.dateOfBirth, lang) : null} />
          <Row label={t('profile.city')} value={cityName} />
          <Row label={t('profile.gender')} value={gender} />
          <Row label={t('profile.iban')} value={profile.iban} />
        </dl>
      </Card>

      <Card>
        <div className="mb-4 flex items-center gap-3">
          <div className="grid size-11 place-items-center rounded-xl bg-brand-soft text-brand">
            <Icon name="car" />
          </div>
          <p className="text-lg font-bold">{t('portal.steps.vehicle')}</p>
        </div>
        {vehicle ? (
          <>
            <p className="text-xl font-bold">
              {vehicle.make} {vehicle.model}
            </p>
            <p className="mt-1 text-sm text-muted">
              {vehicle.color} · {vehicle.year}
            </p>
            <div className="my-5 rounded-2xl bg-cloud p-4 text-center">
              <p className="text-xs text-muted">{t('vehicle.plate')}</p>
              <p className="mt-2 text-xl font-bold tracking-widest" dir="auto">
                {vehicle.plateNumber}
              </p>
            </div>
            <dl>
              <Row label={t('vehicle.category')} value={categoryName} />
              <Row label={t('vehicle.seats')} value={String(vehicle.seats)} />
            </dl>
          </>
        ) : (
          <p className="text-sm text-muted">{t('portal.vehicle.none')}</p>
        )}
      </Card>
    </div>
  )
}
