import type { ReactNode } from 'react'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { catalog } from '../lib/admin'
import { Input, Select } from './Field'

/** City picker backed by `GET /catalog/cities`; falls back to a free-text id when the catalog is unavailable. */
export function CityField({
  id,
  label,
  value,
  onChange,
  error,
  allowAll = true,
  allLabel,
}: {
  id: string
  label?: ReactNode
  value: string
  onChange: (value: string) => void
  error?: ReactNode
  allowAll?: boolean
  allLabel?: string
}) {
  const { t } = useLang()
  const cities = useQuery(() => catalog.cities(), 'catalog-cities')
  if (cities.error) {
    return <Input id={id} label={label} dir="ltr" value={value} error={error} hint={t('cityIdHint')} onChange={(event) => onChange(event.target.value.trim())} />
  }
  const list = cities.data ?? []
  const known = !value || list.some((city) => city.id === value)
  return (
    <Select id={id} label={label} value={value} error={error} onChange={(event) => onChange(event.target.value)} disabled={cities.loading && !cities.data}>
      {allowAll ? <option value="">{allLabel ?? t('allCities')}</option> : <option value="">{t('select')}</option>}
      {!known && <option value={value}>{value}</option>}
      {list.map((city) => (
        <option key={city.id} value={city.id}>
          {city.name}
        </option>
      ))}
    </Select>
  )
}
