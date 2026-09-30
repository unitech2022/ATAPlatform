import { useState, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { rideCategories, zones } from '../lib/admin'
import { isoDay, presetRange, RANGE_PRESETS } from '../lib/reports'
import { CityField } from './CityField'
import { Input, Select } from './Field'

export interface ReportFilterValue {
  from: string
  to: string
  cityId: string
  zoneId: string
  rideCategoryId: string
}

/**
 * One filter row above everything it scopes (reports & exports, §F20.9): date presets + custom range, city,
 * zone (filtered by the chosen city) and ride category. Zones need `pricing.view`; without it the zone picker hides.
 */
export function ReportFilters({
  value,
  onChange,
  error,
  extra,
}: {
  value: ReportFilterValue
  onChange: (patch: Partial<ReportFilterValue>) => void
  error?: string | null
  extra?: ReactNode
}) {
  const { t, lang } = useLang()
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const [today] = useState(() => isoDay(new Date()))
  const zoneOptions = (zonesQuery.data ?? []).filter((zone) => !value.cityId || !zone.cityId || zone.cityId === value.cityId)

  return (
    <section aria-label={t('rpFilters')} className="mb-6 rounded-3xl bg-white p-4 shadow-soft sm:p-5">
      <div className="mb-4 flex flex-wrap gap-2" role="group" aria-label={t('rpPresets')}>
        {RANGE_PRESETS.map((preset) => {
          const range = presetRange(preset.value, today)
          const active = range.from === value.from && range.to === value.to
          return (
            <button
              key={preset.value}
              type="button"
              aria-pressed={active}
              onClick={() => onChange(range)}
              className={`rounded-full border px-3.5 py-1.5 text-sm font-bold transition ${active ? 'border-ink bg-ink text-white' : 'border-line bg-white text-muted hover:bg-cloud'}`}
            >
              {t(preset.key)}
            </button>
          )
        })}
      </div>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
        <Input id="rp-from" type="date" label={t('rpFrom')} dir="ltr" max={value.to || today} value={value.from} onChange={(event) => onChange({ from: event.target.value })} />
        <Input id="rp-to" type="date" label={t('rpTo')} dir="ltr" min={value.from} max={today} value={value.to} onChange={(event) => onChange({ to: event.target.value })} />
        <CityField id="rp-city" label={t('rpCity')} value={value.cityId} onChange={(cityId) => onChange({ cityId, zoneId: '' })} />
        {!zonesQuery.error && (
          <Select id="rp-zone" label={t('rpZone')} value={value.zoneId} disabled={zonesQuery.loading && !zonesQuery.data} onChange={(event) => onChange({ zoneId: event.target.value })}>
            <option value="">{t('rpAllZones')}</option>
            {value.zoneId && !zoneOptions.some((zone) => zone.id === value.zoneId) && <option value={value.zoneId}>{value.zoneId}</option>}
            {zoneOptions.map((zone) => (
              <option key={zone.id} value={zone.id}>
                {lang === 'en' ? zone.nameEn || zone.nameAr : zone.nameAr || zone.nameEn}
              </option>
            ))}
          </Select>
        )}
        {!categoriesQuery.error && (
          <Select id="rp-category" label={t('rpCategory')} value={value.rideCategoryId} onChange={(event) => onChange({ rideCategoryId: event.target.value })}>
            <option value="">{t('rpAllCategories')}</option>
            {(categoriesQuery.data ?? []).map((category) => (
              <option key={category.id} value={category.id}>
                {lang === 'en' ? category.nameEn || category.nameAr : category.nameAr || category.nameEn}
              </option>
            ))}
          </Select>
        )}
      </div>
      {extra && <div className="mt-4">{extra}</div>}
      {error && (
        <p role="alert" className="mt-3 text-sm font-bold text-danger">
          {error}
        </p>
      )}
    </section>
  )
}
