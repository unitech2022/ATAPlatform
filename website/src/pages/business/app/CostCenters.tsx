import { useState, type FormEvent } from 'react'
import { ConfirmModal, Modal, PageHeader, Pill, TableWrap, Td, Th, Toggle } from '../../../components/business/ui'
import { Action, Button } from '../../../components/Button'
import { Field, Input } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { EmptyState, ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { useFlash } from '../../../lib/useFlash'
import { corporateApi } from '../../../lib/api'
import { describeError, fieldErrorsFrom } from '../../../lib/errors'
import type { CostCenter, CostCenterPayload } from '../../../lib/types'
import { useResource } from '../../../lib/useResource'

function CostCenterForm({
  initial,
  onSubmit,
  onCancel,
}: {
  initial: CostCenterPayload
  onSubmit: (payload: CostCenterPayload) => Promise<void>
  onCancel: () => void
}) {
  const { t } = useI18n()
  const [form, setForm] = useState(initial)
  const [busy, setBusy] = useState(false)
  const [touched, setTouched] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const codeInvalid = !/^[A-Za-z0-9_-]{1,30}$/.test(form.code.trim())
  const nameInvalid = form.name.trim() === ''

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setTouched(true)
    if (codeInvalid || nameInvalid) return
    setBusy(true)
    setError(null)
    try {
      await onSubmit({ ...form, code: form.code.trim(), name: form.name.trim() })
    } catch (caught) {
      setError(describeError(caught, t))
      setFieldErrors(fieldErrorsFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={submit} noValidate className="space-y-4">
      <Field
        label={t('biz.costCenter.code')}
        htmlFor="cc-code"
        hint={t('biz.costCenter.codeHint')}
        error={(touched && codeInvalid ? t('biz.costCenter.codeInvalid') : null) ?? fieldErrors.code}
      >
        <Input id="cc-code" dir="ltr" value={form.code} maxLength={30} onChange={(event) => setForm({ ...form, code: event.target.value })} />
      </Field>
      <Field label={t('biz.costCenter.name')} htmlFor="cc-name" error={(touched && nameInvalid ? t('form.required') : null) ?? fieldErrors.name}>
        <Input id="cc-name" value={form.name} maxLength={120} onChange={(event) => setForm({ ...form, name: event.target.value })} />
      </Field>
      <Toggle checked={form.isActive} onChange={(isActive) => setForm({ ...form, isActive })} label={t('biz.policy.active')} />
      {error && <Notice tone="error">{error}</Notice>}
      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
        <Button variant="secondary" onClick={onCancel} disabled={busy}>
          {t('action.cancel')}
        </Button>
        <Button type="submit" disabled={busy}>
          {busy ? t('action.saving') : t('action.save')}
        </Button>
      </div>
    </form>
  )
}

export function CostCenters() {
  const { t, lang } = useI18n()
  const centers = useResource(() => corporateApi.costCenters(), [lang])
  const [editing, setEditing] = useState<CostCenter | 'new' | null>(null)
  const [deleting, setDeleting] = useState<CostCenter | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [flash, setFlash] = useFlash()

  const remove = async () => {
    if (!deleting) return
    setBusy(true)
    setError(null)
    try {
      await corporateApi.deleteCostCenter(deleting.id)
      setFlash(t('biz.deleted'))
      centers.reload(true)
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
      setDeleting(null)
    }
  }

  return (
    <>
      <title>{t('biz.nav.costCenters')} · ATA</title>
      <PageHeader
        title={t('biz.costCenters.title')}
        subtitle={t('biz.costCenters.subtitle')}
        actions={
          <Button size="sm" onClick={() => setEditing('new')}>
            <Icon name="plus" className="size-4" />
            {t('biz.costCenters.new')}
          </Button>
        }
      />
      {flash && (
        <Notice tone="success" className="mb-4">
          {flash}
        </Notice>
      )}
      {error && (
        <Notice tone="error" className="mb-4">
          {error}
        </Notice>
      )}
      {centers.loading && !centers.data ? (
        <LoadingState />
      ) : centers.error && !centers.data ? (
        <ErrorState error={centers.error} onRetry={() => centers.reload()} />
      ) : centers.data && centers.data.length > 0 ? (
        <TableWrap>
          <thead>
            <tr>
              <Th>{t('biz.costCenter.code')}</Th>
              <Th>{t('biz.costCenter.name')}</Th>
              <Th>{t('biz.trip.status')}</Th>
              <Th className="w-28">
                <span className="sr-only">{t('biz.actions')}</span>
              </Th>
            </tr>
          </thead>
          <tbody>
            {centers.data.map((center) => (
              <tr key={center.id} className="hover:bg-cloud">
                <Td className="font-bold" >
                  <span dir="ltr">{center.code}</span>
                </Td>
                <Td>{center.name}</Td>
                <Td>{center.isActive ? <Pill tone="brand">{t('biz.active')}</Pill> : <Pill tone="muted">{t('biz.policy.inactive')}</Pill>}</Td>
                <Td>
                  <div className="flex gap-1">
                    <Action
                      aria-label={t('action.edit')}
                      onClick={() => setEditing(center)}
                      className="grid size-10 place-items-center rounded-xl text-muted hover:bg-white hover:text-ink"
                    >
                      <Icon name="edit" className="size-5" />
                    </Action>
                    <Action
                      aria-label={t('action.delete')}
                      onClick={() => setDeleting(center)}
                      className="grid size-10 place-items-center rounded-xl text-muted hover:bg-danger-soft hover:text-danger"
                    >
                      <Icon name="trash" className="size-5" />
                    </Action>
                  </div>
                </Td>
              </tr>
            ))}
          </tbody>
        </TableWrap>
      ) : (
        <EmptyState icon="wallet" title={t('biz.costCenters.empty')} />
      )}

      <Modal
        open={editing !== null}
        title={editing === 'new' ? t('biz.costCenters.new') : t('biz.costCenters.edit')}
        onClose={() => setEditing(null)}
      >
        {editing !== null && (
          <CostCenterForm
            initial={editing === 'new' ? { code: '', name: '', isActive: true } : { code: editing.code, name: editing.name, isActive: editing.isActive }}
            onCancel={() => setEditing(null)}
            onSubmit={async (payload) => {
              if (editing === 'new') await corporateApi.createCostCenter(payload)
              else await corporateApi.updateCostCenter(editing.id, payload)
              setEditing(null)
              setFlash(t('biz.saved'))
              centers.reload(true)
            }}
          />
        )}
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('biz.costCenters.deleteTitle')}
        message={t('biz.costCenters.deleteCopy', { name: deleting ? `${deleting.code} · ${deleting.name}` : '' })}
        confirmLabel={t('action.delete')}
        danger
        busy={busy}
        onConfirm={() => void remove()}
        onClose={() => setDeleting(null)}
      />
    </>
  )
}
