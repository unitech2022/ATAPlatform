import { useState } from 'react'
import { useLang } from '../context/lang'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useQuery } from '../hooks/useQuery'
import { drivers } from '../lib/admin'
import type { DriverListItem } from '../lib/types'
import { ErrorState } from './ErrorState'
import { SearchInput } from './Field'
import { Icon } from './Icon'
import { Spinner } from './Spinner'

/** Search + pick one approved driver (`GET /admin/drivers?status=approved&search=`); reports the selected list row. */
export function DriverPicker({ value, onChange }: { value: DriverListItem | null; onChange: (driver: DriverListItem | null) => void }) {
  const { t } = useLang()
  const [search, setSearch] = useState('')
  const debounced = useDebouncedValue(search.trim())
  const query = useQuery(() => drivers.list({ status: 'approved', search: debounced, page: 1, pageSize: 8 }), `driver-picker:${debounced}`)
  const rows = query.data?.items ?? []

  return (
    <div className="space-y-3">
      <SearchInput aria-label={t('sdPickDriver')} placeholder={t('searchDrivers')} value={search} onChange={(event) => setSearch(event.target.value)} />
      {value && (
        <p className="flex items-center gap-2 rounded-2xl bg-brand-soft px-4 py-2.5 text-sm font-bold text-brand">
          <Icon name="check" className="size-4 shrink-0" />
          <span className="min-w-0 flex-1 truncate">{value.fullName || t('unnamed')}</span>
          <button type="button" className="text-xs underline" onClick={() => onChange(null)}>
            {t('reset')}
          </button>
        </p>
      )}
      {query.error ? (
        <ErrorState error={query.error} onRetry={query.reload} />
      ) : (
        <ul className="relative max-h-64 divide-y divide-line overflow-y-auto rounded-2xl border border-line">
          {rows.length === 0 && !query.loading && <li className="px-4 py-3 text-sm text-muted">{t('noResults')}</li>}
          {rows.map((row) => (
            <li key={row.id}>
              <button
                type="button"
                aria-pressed={value?.id === row.id}
                onClick={() => onChange(row)}
                className={`flex w-full items-center gap-3 px-4 py-3 text-start transition hover:bg-cloud ${value?.id === row.id ? 'bg-brand-soft/50' : ''}`}
              >
                <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-cloud">
                  <Icon name="car" className="size-4" />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-bold">{row.fullName || t('unnamed')}</span>
                  <span className="ltr-nums block truncate text-xs text-muted">
                    {row.phoneNumber ?? '—'}
                    {row.vehicle ? ` · ${row.vehicle}` : ''}
                  </span>
                </span>
              </button>
            </li>
          ))}
          {query.loading && (
            <li className="absolute inset-0 grid place-items-center bg-white/70 text-brand">
              <Spinner className="size-6" />
            </li>
          )}
        </ul>
      )}
    </div>
  )
}
