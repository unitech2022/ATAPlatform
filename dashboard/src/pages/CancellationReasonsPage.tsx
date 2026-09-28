import { useState, type FormEvent } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input, Select, Toggle } from '../components/Field'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { cancellationReasons } from '../lib/admin'
import { ACTOR_KEY, CANCELLATION_STAGES, REASON_ACTORS, STAGE_KEY } from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { formatNumber } from '../lib/format'
import type { CancellationReason, CancellationReasonInput, CancellationStage, ReasonActor } from '../lib/types'

type FormState = Omit<CancellationReasonInput, 'sortOrder' | 'stages'> & { sortOrder: string; allStages: boolean; stages: CancellationStage[] }

const EMPTY: FormState = {
  code: '',
  actor: 'passenger',
  nameAr: '',
  nameEn: '',
  allStages: true,
  stages: [],
  isExcusable: false,
  isEmergency: false,
  requiresNote: false,
  isSelectable: true,
  sortOrder: '10',
  isActive: true,
}

function toForm(reason: CancellationReason): FormState {
  return {
    code: reason.code,
    actor: reason.actor,
    nameAr: reason.nameAr,
    nameEn: reason.nameEn,
    allStages: reason.stages === null,
    stages: reason.stages ?? [],
    isExcusable: reason.isExcusable,
    isEmergency: reason.isEmergency,
    requiresNote: reason.requiresNote,
    isSelectable: reason.isSelectable,
    sortOrder: String(reason.sortOrder),
    isActive: reason.isActive,
  }
}

type Editing = { mode: 'create' } | { mode: 'edit'; reason: CancellationReason } | null

/** Cancellation reasons (`/admin/cancellation-reasons`, docs/09 §F14.2): bilingual names, stages, flags. */
export function CancellationReasonsPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter } = useUrlState()
  const actor = parseEnum(params.get('actor'), REASON_ACTORS)
  const query = useQuery(() => cancellationReasons.list(), 'cancellation-reasons')

  const [editing, setEditing] = useState<Editing>(null)
  const [form, setForm] = useState<FormState>(EMPTY)
  const [errors, setErrors] = useState<Partial<Record<'code' | 'nameAr' | 'nameEn' | 'sortOrder' | 'stages', string>>>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<CancellationReason | null>(null)

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const open = (next: Editing) => {
    setForm(next?.mode === 'edit' ? toForm(next.reason) : { ...EMPTY, actor: actor || 'passenger' })
    setErrors({})
    setEditing(next)
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!editing) return
    const next: typeof errors = {}
    if (!/^[a-z0-9_]{2,60}$/.test(form.code.trim())) next.code = t('cxErrCode')
    if (!form.nameAr.trim()) next.nameAr = t('fieldRequired')
    if (!form.nameEn.trim()) next.nameEn = t('fieldRequired')
    if (form.sortOrder.trim() === '' || !Number.isInteger(Number(form.sortOrder))) next.sortOrder = t('invalidNumber')
    if (!form.allStages && form.stages.length === 0) next.stages = t('cxErrStages')
    setErrors(next)
    if (Object.keys(next).length > 0) return

    const input: CancellationReasonInput = {
      code: form.code.trim(),
      actor: form.actor,
      nameAr: form.nameAr.trim(),
      nameEn: form.nameEn.trim(),
      stages: form.allStages ? null : CANCELLATION_STAGES.filter((stage) => form.stages.includes(stage)),
      isExcusable: form.isExcusable || form.isEmergency,
      isEmergency: form.isEmergency,
      requiresNote: form.requiresNote,
      isSelectable: form.isSelectable,
      sortOrder: Number(form.sortOrder),
      isActive: form.isActive,
    }
    setSaving(true)
    try {
      if (editing.mode === 'create') await cancellationReasons.create(input)
      else await cancellationReasons.update(editing.reason.id, input)
      toast.success(t('cxReasonSaved'))
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
      await cancellationReasons.remove(deleting.id)
      toast.success(t('cxReasonDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const all = query.data ?? []
  const rows = all.filter((reason) => !actor || reason.actor === actor).sort((a, b) => a.actor.localeCompare(b.actor) || a.sortOrder - b.sortOrder)

  const columns: Column<CancellationReason>[] = [
    { key: 'sort', header: t('sortOrder'), className: 'text-center', render: (row) => <span className="ltr-nums text-muted">{formatNumber(row.sortOrder)}</span> },
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{lang === 'ar' ? row.nameAr : row.nameEn}</span>
          <span className="block text-xs text-muted">{lang === 'ar' ? row.nameEn : row.nameAr}</span>
        </span>
      ),
    },
    { key: 'code', header: t('code'), render: (row) => <span className="ltr-nums text-xs text-muted">{row.code}</span> },
    { key: 'actor', header: t('actor'), render: (row) => t(ACTOR_KEY[row.actor] ?? 'actorSystem') },
    {
      key: 'stages',
      header: t('cxStages'),
      render: (row) =>
        row.stages === null ? (
          <Badge tone="muted">{t('cxAllStages')}</Badge>
        ) : (
          <span className="flex max-w-72 flex-wrap gap-1">
            {row.stages.map((stage) => (
              <Badge key={stage} tone="ink" className="px-2 py-0.5 text-[11px]">
                {t(STAGE_KEY[stage] ?? 'cxStageBeforeAccept')}
              </Badge>
            ))}
          </span>
        ),
    },
    {
      key: 'flags',
      header: t('cxFlags'),
      render: (row) => (
        <span className="flex max-w-72 flex-wrap gap-1">
          {row.isEmergency && <Badge tone="danger">{t('cxEmergency')}</Badge>}
          {row.isExcusable && !row.isEmergency && <Badge tone="warning">{t('cxExcusable')}</Badge>}
          {row.requiresNote && <Badge tone="ink">{t('cxRequiresNote')}</Badge>}
          {!row.isSelectable && <Badge tone="muted">{t('cxNotSelectable')}</Badge>}
        </span>
      ),
    },
    { key: 'active', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => open({ mode: 'edit', reason: row })}>
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
        title={t('cxReasonsTitle')}
        description={t('cxReasonsCopy')}
        actions={
          <Button icon="plus" onClick={() => open({ mode: 'create' })}>
            {t('cxAddReason')}
          </Button>
        }
      />

      <Tabs
        className="mb-4"
        value={actor}
        onChange={(value) => setFilter('actor', value)}
        options={[
          { value: '' as ReasonActor | '', label: t('statusAll'), count: query.data ? all.length : null },
          ...REASON_ACTORS.map((value) => ({ value, label: t(ACTOR_KEY[value]), count: query.data ? all.filter((reason) => reason.actor === value).length : null })),
        ]}
      />

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('cxNoReasons')} emptyDescription="" />
        )}
      </Card>

      <Modal
        open={editing !== null}
        size="lg"
        title={editing?.mode === 'edit' ? t('cxEditReason') : t('cxNewReason')}
        onClose={() => setEditing(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditing(null)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="reason-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        <form id="reason-form" onSubmit={save} noValidate className="grid gap-4 sm:grid-cols-2">
          <Input id="reason-code" label={t('code')} dir="ltr" value={form.code} error={errors.code} disabled={editing?.mode === 'edit'} onChange={(event) => set('code', event.target.value)} />
          <Select id="reason-actor" label={t('actor')} value={form.actor} onChange={(event) => set('actor', event.target.value as ReasonActor)}>
            {REASON_ACTORS.map((value) => (
              <option key={value} value={value}>
                {t(ACTOR_KEY[value])}
              </option>
            ))}
          </Select>
          <Input id="reason-name-ar" label={t('nameAr')} dir="rtl" value={form.nameAr} error={errors.nameAr} onChange={(event) => set('nameAr', event.target.value)} />
          <Input id="reason-name-en" label={t('nameEn')} dir="ltr" value={form.nameEn} error={errors.nameEn} onChange={(event) => set('nameEn', event.target.value)} />
          <div className="sm:col-span-2">
            <Toggle checked={form.allStages} onChange={(value) => set('allStages', value)} label={t('cxAllStages')} description={t('cxAllStagesCopy')} />
            {!form.allStages && (
              <div className="mt-3 flex flex-wrap gap-2">
                {CANCELLATION_STAGES.map((stage) => {
                  const checked = form.stages.includes(stage)
                  return (
                    <button
                      key={stage}
                      type="button"
                      aria-pressed={checked}
                      onClick={() => set('stages', checked ? form.stages.filter((value) => value !== stage) : [...form.stages, stage])}
                      className={`rounded-xl px-3 py-1.5 text-xs font-bold transition ${checked ? 'bg-ink text-white' : 'bg-cloud text-muted hover:bg-line'}`}
                    >
                      {t(STAGE_KEY[stage])}
                    </button>
                  )
                })}
              </div>
            )}
            {errors.stages && <p className="mt-2 text-xs font-bold text-danger">{errors.stages}</p>}
          </div>
          <Toggle checked={form.isExcusable || form.isEmergency} onChange={(value) => set('isExcusable', value)} label={t('cxExcusable')} description={t('cxExcusableCopy')} />
          <Toggle checked={form.isEmergency} onChange={(value) => set('isEmergency', value)} label={t('cxEmergency')} description={t('cxEmergencyCopy')} />
          <Toggle checked={form.requiresNote} onChange={(value) => set('requiresNote', value)} label={t('cxRequiresNote')} />
          <Toggle checked={form.isSelectable} onChange={(value) => set('isSelectable', value)} label={t('cxSelectable')} description={t('cxSelectableCopy')} />
          <Input id="reason-sort" type="number" min={0} label={t('sortOrder')} dir="ltr" value={form.sortOrder} error={errors.sortOrder} onChange={(event) => set('sortOrder', event.target.value)} />
          <div className="self-end">
            <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
          </div>
        </form>
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('cxDeleteReason')}
        description={deleting ? `${lang === 'ar' ? deleting.nameAr : deleting.nameEn} (${deleting.code}) — ${t('cxDeleteReasonCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
