import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { helpCategories } from '../lib/admin'
import { AUDIENCE_KEY, HELP_AUDIENCES } from '../lib/support'
import type { HelpAudience, HelpCategory, HelpCategoryInput } from '../lib/types'
import { Button } from './Button'
import { Input, Select, Toggle } from './Field'
import { Modal } from './Modal'

type FormState = { code: string; nameAr: string; nameEn: string; icon: string; audience: HelpAudience; sortOrder: string; isActive: boolean }
type Errors = Partial<Record<'code' | 'nameAr' | 'nameEn' | 'icon' | 'sortOrder', string>>

const EMPTY: FormState = { code: '', nameAr: '', nameEn: '', icon: '', audience: 'all', sortOrder: '10', isActive: true }

/** Create / edit a help category (`/admin/help/categories`): bilingual name, icon (≤ 40 chars), audience, order. */
export function HelpCategoryFormModal({ target, onClose, onSaved }: { target: HelpCategory | 'new' | null; onClose: () => void; onSaved: () => void }) {
  return target ? <FormDialog key={target === 'new' ? 'new' : target.id} target={target} onClose={onClose} onSaved={onSaved} /> : null
}

function FormDialog({ target, onClose, onSaved }: { target: HelpCategory | 'new'; onClose: () => void; onSaved: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const editing = target === 'new' ? null : target
  const [form, setForm] = useState<FormState>(
    editing
      ? { code: editing.code, nameAr: editing.nameAr, nameEn: editing.nameEn, icon: editing.icon ?? '', audience: editing.audience, sortOrder: String(editing.sortOrder), isActive: editing.isActive }
      : EMPTY,
  )
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const next: Errors = {}
    if (!/^[a-z0-9_-]{2,40}$/.test(form.code.trim())) next.code = t('hcErrCode')
    if (!form.nameAr.trim()) next.nameAr = t('fieldRequired')
    if (!form.nameEn.trim()) next.nameEn = t('fieldRequired')
    if (form.icon.trim().length > 40) next.icon = t('hcErrIcon')
    if (form.sortOrder.trim() === '' || !Number.isInteger(Number(form.sortOrder))) next.sortOrder = t('invalidNumber')
    setErrors(next)
    if (Object.keys(next).length > 0) return

    const input: HelpCategoryInput = {
      code: form.code.trim(),
      nameAr: form.nameAr.trim(),
      nameEn: form.nameEn.trim(),
      icon: form.icon.trim() || null,
      audience: form.audience,
      sortOrder: Number(form.sortOrder),
      isActive: form.isActive,
    }
    setSaving(true)
    try {
      if (editing) await helpCategories.update(editing.id, input)
      else await helpCategories.create(input)
      toast.success(t('hcCategorySaved'))
      onSaved()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      size="lg"
      title={editing ? t('hcCategoryEdit') : t('hcCategoryNew')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="help-category-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="help-category-form" onSubmit={save} noValidate className="grid gap-4 sm:grid-cols-2">
        <Input id="hc-code" label={t('code')} dir="ltr" value={form.code} error={errors.code} disabled={Boolean(editing)} maxLength={40} onChange={(event) => set('code', event.target.value)} />
        <Input id="hc-icon" label={t('icon')} hint={t('hcIconHint')} dir="ltr" value={form.icon} error={errors.icon} maxLength={40} onChange={(event) => set('icon', event.target.value)} />
        <Input id="hc-name-ar" label={t('nameAr')} dir="rtl" value={form.nameAr} error={errors.nameAr} onChange={(event) => set('nameAr', event.target.value)} />
        <Input id="hc-name-en" label={t('nameEn')} dir="ltr" value={form.nameEn} error={errors.nameEn} onChange={(event) => set('nameEn', event.target.value)} />
        <Select id="hc-audience" label={t('hcAudience')} value={form.audience} onChange={(event) => set('audience', event.target.value as HelpAudience)}>
          {HELP_AUDIENCES.map((value) => (
            <option key={value} value={value}>
              {t(AUDIENCE_KEY[value])}
            </option>
          ))}
        </Select>
        <Input id="hc-sort" type="number" min={0} label={t('sortOrder')} dir="ltr" value={form.sortOrder} error={errors.sortOrder} onChange={(event) => set('sortOrder', event.target.value)} />
        <div className="sm:col-span-2">
          <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
        </div>
      </form>
    </Modal>
  )
}
