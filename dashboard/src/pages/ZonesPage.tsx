import { useMemo, useState, type FormEvent } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input, Toggle } from '../components/Field'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { PolygonEditor, type PolygonContext } from '../components/PolygonEditor'
import { Table, type Column } from '../components/Table'
import { ZonesMap } from '../components/ZonesMap'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { rideCategories, zones } from '../lib/admin'
import { formatNumber } from '../lib/format'
import {
  closeRing,
  DEFAULT_OPERATING_HOURS,
  isUsablePolygon,
  isValidTime,
  localName,
  numberOrNull,
  polygonCenter,
  WEEKDAYS,
} from '../lib/pricing'
import type { LatLngTuple, RideCategory, Zone, ZoneInput } from '../lib/types'

type HourRow = { enabled: boolean; from: string; to: string }
type CategoryRow = { isEnabled: boolean; surgeCap: string }

type FormState = {
  code: string
  nameAr: string
  nameEn: string
  cityId: string
  priority: string
  isActive: boolean
  polygon: LatLngTuple[]
  alwaysOpen: boolean
  hours: Record<number, HourRow>
  categories: Record<string, CategoryRow>
}

type FormErrors = Partial<Record<'code' | 'nameAr' | 'nameEn' | 'priority' | 'polygon' | 'hours' | 'categories', string>>

const DEFAULT_SURGE_CAP = '2.5'

function defaultHours(): Record<number, HourRow> {
  return Object.fromEntries(DEFAULT_OPERATING_HOURS.map((row) => [row.day, { enabled: true, from: row.from, to: row.to }]))
}

function emptyForm(categories: RideCategory[]): FormState {
  return {
    code: '',
    nameAr: '',
    nameEn: '',
    cityId: '',
    priority: '0',
    isActive: true,
    polygon: [],
    alwaysOpen: true,
    hours: defaultHours(),
    categories: Object.fromEntries(categories.map((category) => [category.id, { isEnabled: category.isActive, surgeCap: DEFAULT_SURGE_CAP }])),
  }
}

function toForm(zone: Zone, categories: RideCategory[]): FormState {
  const hours = defaultHours()
  if (zone.operatingHours) {
    for (const { day } of WEEKDAYS) hours[day].enabled = false
    for (const entry of zone.operatingHours) {
      if (hours[entry.day]) hours[entry.day] = { enabled: true, from: entry.from, to: entry.to }
    }
  }
  const settings = new Map(zone.zoneCategorySettings.map((setting) => [setting.rideCategoryId, setting]))
  return {
    code: zone.code,
    nameAr: zone.nameAr,
    nameEn: zone.nameEn,
    cityId: zone.cityId ?? '',
    priority: String(zone.priority),
    isActive: zone.isActive,
    polygon: zone.polygon ?? [],
    alwaysOpen: zone.operatingHours === null,
    hours,
    categories: Object.fromEntries(
      categories.map((category) => {
        const setting = settings.get(category.id)
        return [category.id, { isEnabled: setting?.isEnabled ?? false, surgeCap: String(setting?.surgeCap ?? DEFAULT_SURGE_CAP) }]
      }),
    ),
  }
}

function toInput(form: FormState, categories: RideCategory[]): ZoneInput {
  const polygon = closeRing(form.polygon)
  const center = polygonCenter(polygon) ?? [0, 0]
  return {
    cityId: form.cityId.trim() || null,
    code: form.code.trim(),
    nameAr: form.nameAr.trim(),
    nameEn: form.nameEn.trim(),
    polygon,
    centerLat: center[0],
    centerLng: center[1],
    priority: Number(form.priority),
    isActive: form.isActive,
    operatingHours: form.alwaysOpen
      ? null
      : WEEKDAYS.filter(({ day }) => form.hours[day]?.enabled).map(({ day }) => ({ day, from: form.hours[day].from, to: form.hours[day].to })),
    zoneCategorySettings: categories.map((category) => ({
      rideCategoryId: category.id,
      isEnabled: form.categories[category.id]?.isEnabled ?? false,
      surgeCap: numberOrNull(form.categories[category.id]?.surgeCap ?? '') ?? Number(DEFAULT_SURGE_CAP),
    })),
  }
}

type Editing = { mode: 'create' } | { mode: 'edit'; zone: Zone } | null

export function ZonesPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [editing, setEditing] = useState<Editing>(null)
  const [form, setForm] = useState<FormState>(() => emptyForm([]))
  const [errors, setErrors] = useState<FormErrors>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<Zone | null>(null)

  const sorted = useMemo(
    () => [...(zonesQuery.data ?? [])].sort((a, b) => b.priority - a.priority || a.code.localeCompare(b.code)),
    [zonesQuery.data],
  )

  const openCreate = () => {
    setForm(emptyForm(categories))
    setErrors({})
    setEditing({ mode: 'create' })
  }

  const openEdit = (zone: Zone) => {
    setForm(toForm(zone, categories))
    setErrors({})
    setEditing({ mode: 'edit', zone })
  }

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const setHour = (day: number, patch: Partial<HourRow>) => {
    setForm((current) => ({ ...current, hours: { ...current.hours, [day]: { ...current.hours[day], ...patch } } }))
    setErrors((current) => ({ ...current, hours: undefined }))
  }

  const setCategory = (id: string, patch: Partial<CategoryRow>) => {
    setForm((current) => ({
      ...current,
      categories: { ...current.categories, [id]: { ...(current.categories[id] ?? { isEnabled: false, surgeCap: DEFAULT_SURGE_CAP }), ...patch } },
    }))
    setErrors((current) => ({ ...current, categories: undefined }))
  }

  const validate = (): boolean => {
    const next: FormErrors = {}
    if (!form.code.trim()) next.code = t('fieldRequired')
    if (!form.nameAr.trim()) next.nameAr = t('fieldRequired')
    if (!form.nameEn.trim()) next.nameEn = t('fieldRequired')
    if (numberOrNull(form.priority) === null) next.priority = t('invalidNumber')
    if (!isUsablePolygon(form.polygon)) next.polygon = t('polygonTooFew')
    if (!form.alwaysOpen) {
      const rows = WEEKDAYS.filter(({ day }) => form.hours[day]?.enabled)
      if (rows.some(({ day }) => !isValidTime(form.hours[day].from) || !isValidTime(form.hours[day].to))) next.hours = t('invalidTime')
    }
    if (categories.some((category) => numberOrNull(form.categories[category.id]?.surgeCap ?? '') === null)) next.categories = t('invalidNumber')
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!editing || !validate()) return
    setSaving(true)
    try {
      const input = toInput(form, categories)
      const saved = editing.mode === 'create' ? await zones.create(input) : await zones.update(editing.zone.id, input)
      toast.success(t('zoneSaved'))
      setEditing(null)
      if (saved?.id) setSelectedId(saved.id)
      zonesQuery.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await zones.remove(deleting.id)
      toast.success(t('zoneDeleted'))
      if (selectedId === deleting.id) setSelectedId(null)
      setDeleting(null)
      zonesQuery.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const context = useMemo<PolygonContext[]>(
    () =>
      sorted
        .filter((zone) => editing?.mode !== 'edit' || zone.id !== editing.zone.id)
        .map((zone) => ({ id: zone.id, name: localName(zone, lang), polygon: zone.polygon })),
    [sorted, editing, lang],
  )

  const columns: Column<Zone>[] = [
    { key: 'code', header: t('code'), render: (row) => <span className="ltr-nums font-bold">{row.code}</span> },
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block font-bold">{localName(row, lang)}</span>
          <span className="block text-xs text-muted">{lang === 'ar' ? row.nameEn : row.nameAr}</span>
        </span>
      ),
    },
    { key: 'priority', header: t('priority'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.priority)}</span> },
    {
      key: 'isActive',
      header: t('status'),
      render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge>,
    },
    {
      key: 'categories',
      header: t('categoriesEnabled'),
      className: 'text-center',
      render: (row) => (
        <span className="ltr-nums">
          {formatNumber(row.zoneCategorySettings.filter((setting) => setting.isEnabled).length)} / {formatNumber(categories.length || row.zoneCategorySettings.length)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2" onClick={(event) => event.stopPropagation()}>
          <Button variant="secondary" size="sm" icon="edit" onClick={() => openEdit(row)}>
            {t('edit')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  const centerPreview = polygonCenter(form.polygon)

  return (
    <>
      <PageHeader
        title={t('zonesTitle')}
        description={t('zonesCopy')}
        actions={
          <Button icon="plus" onClick={openCreate}>
            {t('addZone')}
          </Button>
        }
      />

      <div className="grid gap-6 xl:grid-cols-2">
        <Card flush>
          {zonesQuery.error ? (
            <ErrorState error={zonesQuery.error} onRetry={zonesQuery.reload} />
          ) : (
            <Table
              columns={columns}
              rows={sorted}
              rowKey={(row) => row.id}
              loading={zonesQuery.loading}
              onRowClick={(row) => setSelectedId(row.id)}
              emptyTitle={t('noZones')}
              emptyDescription=""
            />
          )}
        </Card>

        <Card title={t('zonesMap')} description={t('clickZoneToSelect')} flush>
          <div className="px-5 pb-5 sm:px-6 sm:pb-6">
            <ZonesMap
              zones={sorted}
              selectedId={selectedId}
              onSelect={(zone) => setSelectedId(zone.id)}
              labelFor={(zone) => localName(zone, lang)}
              className="h-96 w-full xl:h-[32rem]"
            />
          </div>
        </Card>
      </div>

      <Modal
        open={editing !== null}
        size="xl"
        title={editing?.mode === 'edit' ? t('editZone') : t('newZone')}
        onClose={() => setEditing(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditing(null)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="zone-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        {editing && (
          <form id="zone-form" onSubmit={save} noValidate className="grid gap-6">
            <div className="grid gap-4 sm:grid-cols-2">
              <Input id="code" label={t('code')} dir="ltr" value={form.code} error={errors.code} onChange={(e) => setField('code', e.target.value)} />
              <Input id="cityId" label={t('cityId')} dir="ltr" value={form.cityId} onChange={(e) => setField('cityId', e.target.value)} />
              <Input id="nameAr" label={t('nameAr')} dir="rtl" value={form.nameAr} error={errors.nameAr} onChange={(e) => setField('nameAr', e.target.value)} />
              <Input id="nameEn" label={t('nameEn')} dir="ltr" value={form.nameEn} error={errors.nameEn} onChange={(e) => setField('nameEn', e.target.value)} />
              <Input
                id="priority"
                type="number"
                label={t('priority')}
                dir="ltr"
                value={form.priority}
                error={errors.priority}
                onChange={(e) => setField('priority', e.target.value)}
              />
              <Input
                id="center"
                label={t('center')}
                dir="ltr"
                readOnly
                value={centerPreview ? `${centerPreview[0]}, ${centerPreview[1]}` : ''}
                hint={t('centerAuto')}
              />
              <div className="sm:col-span-2">
                <Toggle id="isActive" checked={form.isActive} onChange={(value) => setField('isActive', value)} label={t('isActive')} />
              </div>
            </div>

            <section>
              <h3 className="mb-3 text-base font-bold">{t('polygon')}</h3>
              <PolygonEditor value={form.polygon} onChange={(points) => setField('polygon', points)} context={context} error={errors.polygon} />
            </section>

            <section>
              <h3 className="mb-3 text-base font-bold">{t('operatingHours')}</h3>
              <Toggle id="alwaysOpen" checked={form.alwaysOpen} onChange={(value) => setField('alwaysOpen', value)} label={t('alwaysOpen')} description={t('alwaysOpenCopy')} />
              {!form.alwaysOpen && (
                <div className="mt-3 overflow-hidden rounded-2xl border border-line">
                  {WEEKDAYS.map(({ day, key }) => {
                    const row = form.hours[day]
                    return (
                      <div key={day} className="grid grid-cols-[auto_1fr_1fr] items-center gap-3 border-b border-line px-4 py-2 last:border-0 sm:grid-cols-[10rem_1fr_1fr]">
                        <label className="flex items-center gap-2 text-sm font-bold">
                          <input type="checkbox" className="size-4 accent-brand" checked={row.enabled} onChange={(e) => setHour(day, { enabled: e.target.checked })} />
                          {t(key)}
                        </label>
                        <Input aria-label={t('fromTime')} type="time" dir="ltr" className="h-10" disabled={!row.enabled} value={row.from} onChange={(e) => setHour(day, { from: e.target.value })} />
                        <Input aria-label={t('toTime')} type="time" dir="ltr" className="h-10" disabled={!row.enabled} value={row.to} onChange={(e) => setHour(day, { to: e.target.value })} />
                      </div>
                    )
                  })}
                  {errors.hours && <p className="px-4 py-2 text-xs font-bold text-danger">{errors.hours}</p>}
                </div>
              )}
            </section>

            <section>
              <h3 className="mb-1 text-base font-bold">{t('zoneCategorySettings')}</h3>
              <p className="mb-3 text-xs text-muted">{t('zoneCategorySettingsCopy')}</p>
              {categories.length === 0 ? (
                <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('noCategories')}</p>
              ) : (
                <div className="overflow-hidden rounded-2xl border border-line">
                  <div className="grid grid-cols-[1fr_auto_8rem] gap-3 bg-cloud/60 px-4 py-2 text-xs font-bold text-muted">
                    <span>{t('category')}</span>
                    <span>{t('enabled')}</span>
                    <span>{t('surgeCap')}</span>
                  </div>
                  {categories.map((category) => {
                    const row = form.categories[category.id] ?? { isEnabled: false, surgeCap: DEFAULT_SURGE_CAP }
                    return (
                      <div key={category.id} className="grid grid-cols-[1fr_auto_8rem] items-center gap-3 border-t border-line px-4 py-2">
                        <span className="min-w-0">
                          <span className="block truncate text-sm font-bold">{localName(category, lang)}</span>
                          <span className="ltr-nums block text-xs text-muted">{category.code}</span>
                        </span>
                        <input
                          type="checkbox"
                          aria-label={t('enabled')}
                          className="size-4 accent-brand"
                          checked={row.isEnabled}
                          onChange={(e) => setCategory(category.id, { isEnabled: e.target.checked })}
                        />
                        <Input
                          aria-label={t('surgeCap')}
                          type="number"
                          step="0.1"
                          min={1}
                          dir="ltr"
                          className="h-10"
                          value={row.surgeCap}
                          onChange={(e) => setCategory(category.id, { surgeCap: e.target.value })}
                        />
                      </div>
                    )
                  })}
                  {errors.categories && <p className="px-4 py-2 text-xs font-bold text-danger">{errors.categories}</p>}
                </div>
              )}
            </section>
          </form>
        )}
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('deleteZone')}
        description={deleting ? `${localName(deleting, lang)} — ${t('deleteZoneCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
