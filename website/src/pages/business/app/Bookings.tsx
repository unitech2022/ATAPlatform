import { useState } from 'react'
import { Link } from 'react-router'
import { PageHeader } from '../../../components/business/ui'
import { Field, Input, Select } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Pagination } from '../../../components/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { corporateApi } from '../../../lib/api'
import { useResource } from '../../../lib/useResource'
import { TripsTable } from './Dashboard'

const STATUS_FILTERS = ['active', 'scheduled', 'completed', 'cancelled'] as const

export function Bookings() {
  const { t, lang } = useI18n()
  const [status, setStatus] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [guest, setGuest] = useState('')
  const [page, setPage] = useState(1)

  const bookings = useResource(
    () =>
      corporateApi.bookings({
        status: status || undefined,
        from: from || undefined,
        to: to || undefined,
        isGuest: guest === '' ? undefined : guest === 'guest',
        page,
      }),
    [status, from, to, guest, page, lang],
  )

  const update = (setter: (value: string) => void) => (value: string) => {
    setter(value)
    setPage(1)
  }

  return (
    <>
      <title>{t('biz.nav.bookings')} · ATA</title>
      <PageHeader
        title={t('biz.bookings.title')}
        subtitle={t('biz.bookings.subtitle')}
        actions={
          <Link
            to="/business/app/bookings/new"
            className="inline-flex items-center gap-2 rounded-2xl bg-ink px-4 py-2.5 text-sm font-bold text-white shadow-button hover:bg-ink-soft"
          >
            <Icon name="plus" className="size-4" />
            {t('biz.bookings.new')}
          </Link>
        }
      />
      <div className="mb-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Field label={t('biz.trip.status')} htmlFor="bookings-status">
          <Select id="bookings-status" value={status} onChange={(event) => update(setStatus)(event.target.value)}>
            <option value="">{t('biz.allStatuses')}</option>
            {STATUS_FILTERS.map((value) => (
              <option key={value} value={value}>
                {t(`biz.bookings.filter.${value}`)}
              </option>
            ))}
          </Select>
        </Field>
        <Field label={t('biz.bookings.rider')} htmlFor="bookings-guest">
          <Select id="bookings-guest" value={guest} onChange={(event) => update(setGuest)(event.target.value)}>
            <option value="">{t('biz.bookings.allRiders')}</option>
            <option value="employee">{t('biz.bookings.employees')}</option>
            <option value="guest">{t('biz.bookings.guests')}</option>
          </Select>
        </Field>
        <Field label={t('biz.from')} htmlFor="bookings-from">
          <Input id="bookings-from" type="date" value={from} max={to || undefined} onChange={(event) => update(setFrom)(event.target.value)} />
        </Field>
        <Field label={t('biz.to')} htmlFor="bookings-to">
          <Input id="bookings-to" type="date" value={to} min={from || undefined} onChange={(event) => update(setTo)(event.target.value)} />
        </Field>
      </div>

      {bookings.loading && !bookings.data ? (
        <LoadingState />
      ) : bookings.error && !bookings.data ? (
        <ErrorState error={bookings.error} onRetry={() => bookings.reload()} />
      ) : bookings.data && bookings.data.items.length > 0 ? (
        <>
          <TripsTable trips={bookings.data.items} />
          <Pagination className="mt-4" page={bookings.data.page} pageSize={bookings.data.pageSize} total={bookings.data.total} onChange={setPage} />
        </>
      ) : (
        <EmptyState icon="car" title={t('biz.bookings.empty')} />
      )}
    </>
  )
}
