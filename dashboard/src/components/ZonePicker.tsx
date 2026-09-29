import { useCallback, useState } from 'react'
import { useLang } from '../context/lang'
import { localName } from '../lib/pricing'
import type { Zone } from '../lib/types'
import { Button } from './Button'
import { ChipGroup } from './ChipGroup'
import { ZonesMap } from './ZonesMap'

/**
 * Multi-select of pickup zones: chips plus an optional map where clicking a polygon toggles it.
 * An empty selection means "no zone restriction" (sent as `null`).
 */
export function ZonePicker({ zones, value, onChange, emptyLabel }: { zones: Zone[]; value: string[]; onChange: (value: string[]) => void; emptyLabel?: string }) {
  const { t, lang } = useLang()
  const [showMap, setShowMap] = useState(false)
  const colorFor = useCallback((zone: Zone) => (value.includes(zone.id) ? '#123650' : '#6b7f8e'), [value])

  if (zones.length === 0) return <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('noZones')}</p>

  return (
    <div className="space-y-3">
      <ChipGroup options={zones.map((zone) => ({ value: zone.id, label: localName(zone, lang) }))} value={value} onChange={onChange} />
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-muted">{value.length === 0 ? (emptyLabel ?? t('allZones')) : `${t('selected')}: ${value.length}`}</p>
        <Button variant="ghost" size="sm" icon="map" onClick={() => setShowMap((current) => !current)} aria-expanded={showMap}>
          {showMap ? t('hideMap') : t('showMap')}
        </Button>
      </div>
      {showMap && (
        <div className="overflow-hidden rounded-2xl border border-line">
          <ZonesMap
            zones={zones}
            className="h-72 w-full"
            colorFor={colorFor}
            labelFor={(zone) => localName(zone, lang)}
            onSelect={(zone) => onChange(value.includes(zone.id) ? value.filter((id) => id !== zone.id) : [...value, zone.id])}
          />
        </div>
      )}
    </div>
  )
}
