import { useState, type FormEvent } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input, Select, Textarea, Toggle } from '../components/Field'
import { Icon, type IconName } from '../components/Icon'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { usePermission } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { rideCategories } from '../lib/admin'
import { formatNumber } from '../lib/format'
import type { RideCategory, RideCategoryInput } from '../lib/types'

type FormState = {
  code: string
  nameAr: string
  nameEn: string
  descriptionAr: string
  descriptionEn: string
  icon: string
  seats: string
  maxStops: string
  sortOrder: string
  isActive: boolean
}

type FormErrors = Partial<Record<keyof FormState, string>>

/** Icons a category may use in the apps (subset of the design-system set). */
const ICON_OPTIONS: IconName[] = ['car', 'shield', 'users', 'pin']

function iconFor(icon: string | null): IconName {
  return ICON_OPTIONS.find((name) => name === icon) ?? 'car'
}

const EMPTY_FORM: FormState = {
  code: '',
  nameAr: '',
  nameEn: '',
  descriptionAr: '',
  descriptionEn: '',
  icon: 'car',
  seats: '4',
  maxStops: '2',
  sortOrder: '1',
  isActive: true,
}

function toForm(category: RideCategory): FormState {
  return {
    code: category.code,
    nameAr: category.nameAr,
    nameEn: category.nameEn,
    descriptionAr: category.descriptionAr ?? '',
    descriptionEn: category.descriptionEn ?? '',
    icon: category.icon ?? '',
    seats: String(category.seats),
    maxStops: String(category.maxStops),
    sortOrder: String(category.sortOrder),
    isActive: category.isActive,
  }
}

function toInput(form: FormState): RideCategoryInput {
  return {
    code: form.code.trim(),
    nameAr: form.nameAr.trim(),
    nameEn: form.nameEn.trim(),
    descriptionAr: form.descriptionAr.trim() || null,
    descriptionEn: form.descriptionEn.trim() || null,
    icon: form.icon.trim() || null,
    seats: Number(form.seats),
    maxStops: Number(form.maxStops),
    sortOrder: Number(form.sortOrder),
    isActive: form.isActive,
  }
}

type Editing = { mode: 'create' } | { mode: 'edit'; category: RideCategory } | null

export function RideCategoriesPage() {
  // F20: any admin reads ride categories; writing needs `catalog.manage`.
  const canEdit = usePermission('catalog.manage')
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => rideCategories.list(), 'ride-categories')

  const [editing, setEditing] = useState<Editing>(null)
  const [form, setForm] = useState<FormState>(EMPTY_FORM)
  const [errors, setErrors] = useState<FormErrors>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<RideCategory | null>(null)

  const openCreate = () => {
    setForm(EMPTY_FORM)
    setErrors({})
    setEditing({ mode: 'create' })
  }

  const openEdit = (category: RideCategory) => {
    setForm(toForm(category))
    setErrors({})
    setEditing({ mode: 'edit', category })
  }

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const validate = (): boolean => {
    const next: FormErrors = {}
    if (!form.code.trim()) next.code = t('fieldRequired')
    if (!form.nameAr.trim()) next.nameAr = t('fieldRequired')
    if (!form.nameEn.trim()) next.nameEn = t('fieldRequired')
    for (const key of ['seats', 'maxStops', 'sortOrder'] as const) {
      if (form[key].trim() === '' || Number.isNaN(Number(form[key]))) next[key] = t('fieldRequired')
    }
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!editing || !validate()) return
    setSaving(true)
    try {
      const input = toInput(form)
      if (editing.mode === 'create') await rideCategories.create(input)
      else await rideCategories.update(editing.category.id, input)
      toast.success(t('categorySaved'))
      setEditing(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await rideCategories.remove(deleting.id)
      toast.success(t('categoryDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const sorted = [...(query.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder)

  const columns: Column<RideCategory>[] = [
    {
      key: 'sortOrder',
      header: t('sortOrder'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums text-muted">{formatNumber(row.sortOrder)}</span>,
    },
    {
      key: 'name',
      header: t('fullName'),
      render: (row) => (
        <span className="flex items-center gap-3">
          <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand">
            <Icon name={iconFor(row.icon)} className="size-5" />
          </span>
          <span className="min-w-0">
            <span className="block font-bold">{lang === 'ar' ? row.nameAr : row.nameEn}</span>
            <span className="block text-xs text-muted">{lang === 'ar' ? row.nameEn : row.nameAr}</span>
          </span>
        </span>
      ),
    },
    { key: 'code', header: t('code'), render: (row) => <span className="ltr-nums text-muted">{row.code}</span> },
    {
      key: 'description',
      header: lang === 'ar' ? t('descriptionAr') : t('descriptionEn'),
      render: (row) => <span className="block max-w-64 truncate text-muted">{(lang === 'ar' ? row.descriptionAr : row.descriptionEn) ?? '—'}</span>,
    },
    { key: 'seats', header: t('seats'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.seats)}</span> },
    { key: 'maxStops', header: t('maxStops'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.maxStops)}</span> },
    {
      key: 'isActive',
      header: t('status'),
      render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge>,
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => openEdit(row)}>
            {t('edit')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('rideCategoriesTitle')}
        description={t('rideCategoriesCopy')}
        actions={
          canEdit ? (
            <Button icon="plus" onClick={openCreate}>
              {t('addCategory')}
            </Button>
          ) : undefined
        }
      />

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table
            columns={canEdit ? columns : columns.filter((column) => column.key !== 'actions')}
            rows={sorted}
            rowKey={(row) => row.id}
            loading={query.loading}
            emptyTitle={t('noCategories')}
            emptyDescription=""
          />
        )}
      </Card>

      <Modal
        open={editing !== null}
        size="lg"
        title={editing?.mode === 'edit' ? t('editCategory') : t('newCategory')}
        onClose={() => setEditing(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditing(null)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="ride-category-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        <form id="ride-category-form" onSubmit={save} noValidate className="grid gap-4 sm:grid-cols-2">
          <Input id="code" label={t('code')} dir="ltr" value={form.code} error={errors.code} onChange={(e) => setField('code', e.target.value)} />
          <Select id="icon" label={t('icon')} dir="ltr" value={form.icon} onChange={(e) => setField('icon', e.target.value)}>
            {ICON_OPTIONS.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </Select>
          <Input id="nameAr" label={t('nameAr')} dir="rtl" value={form.nameAr} error={errors.nameAr} onChange={(e) => setField('nameAr', e.target.value)} />
          <Input id="nameEn" label={t('nameEn')} dir="ltr" value={form.nameEn} error={errors.nameEn} onChange={(e) => setField('nameEn', e.target.value)} />
          <Textarea
            id="descriptionAr"
            label={t('descriptionAr')}
            dir="rtl"
            className="min-h-20"
            value={form.descriptionAr}
            onChange={(e) => setField('descriptionAr', e.target.value)}
          />
          <Textarea
            id="descriptionEn"
            label={t('descriptionEn')}
            dir="ltr"
            className="min-h-20"
            value={form.descriptionEn}
            onChange={(e) => setField('descriptionEn', e.target.value)}
          />
          <div className="grid grid-cols-3 gap-3 sm:col-span-2">
            <Input id="seats" type="number" min={1} label={t('seats')} dir="ltr" value={form.seats} error={errors.seats} onChange={(e) => setField('seats', e.target.value)} />
            <Input
              id="maxStops"
              type="number"
              min={0}
              label={t('maxStops')}
              dir="ltr"
              value={form.maxStops}
              error={errors.maxStops}
              onChange={(e) => setField('maxStops', e.target.value)}
            />
            <Input
              id="sortOrder"
              type="number"
              min={0}
              label={t('sortOrder')}
              dir="ltr"
              value={form.sortOrder}
              error={errors.sortOrder}
              onChange={(e) => setField('sortOrder', e.target.value)}
            />
          </div>
          <div className="sm:col-span-2">
            <Toggle id="isActive" checked={form.isActive} onChange={(value) => setField('isActive', value)} label={t('isActive')} />
          </div>
        </form>
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('deleteCategory')}
        description={deleting ? `${lang === 'ar' ? deleting.nameAr : deleting.nameEn} — ${t('deleteCategoryCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
