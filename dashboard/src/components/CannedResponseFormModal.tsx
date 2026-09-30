import { useRef, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { cannedResponses } from '../lib/admin'
import { CANNED_CODE_PATTERN, CANNED_PLACEHOLDERS, renderCanned, TICKET_TYPE_KEY, TICKET_TYPES, unknownPlaceholders } from '../lib/support'
import type { CannedResponse, CannedResponseInput, TicketType } from '../lib/types'
import { Button } from './Button'
import { Input, Select, Textarea, Toggle } from './Field'
import { Modal } from './Modal'

type FormState = { code: string; title: string; bodyAr: string; bodyEn: string; ticketType: TicketType | ''; isActive: boolean }
type Errors = Partial<Record<'code' | 'title' | 'bodyAr' | 'bodyEn', string>>

const EMPTY: FormState = { code: '', title: '', bodyAr: '', bodyEn: '', ticketType: '', isActive: true }

const SAMPLE = { userName: 'Sara', ticketNumber: 'ST-20260930-00042', tripNumber: 'T-1042' }

/** Create / edit a canned response (`/admin/canned-responses`): bilingual body ≤ 4000 chars with placeholder chips and a rendered preview. */
export function CannedResponseFormModal({ target, onClose, onSaved }: { target: CannedResponse | 'new' | null; onClose: () => void; onSaved: () => void }) {
  return target ? <FormDialog key={target === 'new' ? 'new' : target.id} target={target} onClose={onClose} onSaved={onSaved} /> : null
}

function FormDialog({ target, onClose, onSaved }: { target: CannedResponse | 'new'; onClose: () => void; onSaved: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const editing = target === 'new' ? null : target
  const [form, setForm] = useState<FormState>(
    editing ? { code: editing.code, title: editing.title, bodyAr: editing.bodyAr, bodyEn: editing.bodyEn, ticketType: editing.ticketType ?? '', isActive: editing.isActive } : EMPTY,
  )
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)
  const arRef = useRef<HTMLTextAreaElement>(null)
  const enRef = useRef<HTMLTextAreaElement>(null)

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const insertPlaceholder = (field: 'bodyAr' | 'bodyEn', placeholder: string) => {
    const element = (field === 'bodyAr' ? arRef : enRef).current
    const token = `{${placeholder}}`
    const current = form[field]
    const start = element?.selectionStart ?? current.length
    const end = element?.selectionEnd ?? current.length
    set(field, `${current.slice(0, start)}${token}${current.slice(end)}`)
    requestAnimationFrame(() => {
      element?.focus()
      element?.setSelectionRange(start + token.length, start + token.length)
    })
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const next: Errors = {}
    if (!CANNED_CODE_PATTERN.test(form.code.trim())) next.code = t('spCannedErrCode')
    if (!form.title.trim()) next.title = t('fieldRequired')
    else if (form.title.trim().length > 120) next.title = t('spCannedErrTitle')
    if (!form.bodyAr.trim()) next.bodyAr = t('fieldRequired')
    else if (form.bodyAr.length > 4000) next.bodyAr = t('spBodyTooLong')
    else if (unknownPlaceholders(form.bodyAr).length > 0) next.bodyAr = `${t('spCannedErrPlaceholder')} ${unknownPlaceholders(form.bodyAr).join(' ')}`
    if (!form.bodyEn.trim()) next.bodyEn = t('fieldRequired')
    else if (form.bodyEn.length > 4000) next.bodyEn = t('spBodyTooLong')
    else if (unknownPlaceholders(form.bodyEn).length > 0) next.bodyEn = `${t('spCannedErrPlaceholder')} ${unknownPlaceholders(form.bodyEn).join(' ')}`
    setErrors(next)
    if (Object.keys(next).length > 0) return

    const input: CannedResponseInput = {
      code: form.code.trim(),
      title: form.title.trim(),
      bodyAr: form.bodyAr.trim(),
      bodyEn: form.bodyEn.trim(),
      ticketType: form.ticketType || null,
      isActive: form.isActive,
    }
    setSaving(true)
    try {
      if (editing) await cannedResponses.update(editing.id, input)
      else await cannedResponses.create(input)
      toast.success(t('spCannedSaved'))
      onSaved()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const chips = (field: 'bodyAr' | 'bodyEn') => (
    <div className="mt-2 flex flex-wrap gap-1.5">
      {CANNED_PLACEHOLDERS.map((placeholder) => (
        <button
          key={placeholder}
          type="button"
          onClick={() => insertPlaceholder(field, placeholder)}
          className="ltr-nums rounded-full bg-cloud px-2.5 py-1 text-xs font-bold text-muted transition hover:bg-line"
        >
          {`{${placeholder}}`}
        </button>
      ))}
    </div>
  )

  return (
    <Modal
      open
      size="lg"
      title={editing ? t('spCannedEdit') : t('spCannedNew')}
      description={t('spCannedFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="canned-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="canned-form" onSubmit={save} noValidate className="grid gap-4 sm:grid-cols-2">
        <Input id="canned-code" label={t('code')} dir="ltr" value={form.code} error={errors.code} disabled={Boolean(editing)} maxLength={40} onChange={(event) => set('code', event.target.value)} />
        <Input id="canned-title" label={t('spCannedTitle')} value={form.title} error={errors.title} maxLength={120} onChange={(event) => set('title', event.target.value)} />
        <Select id="canned-type" label={t('spCannedType')} hint={t('spCannedTypeHint')} value={form.ticketType} onChange={(event) => set('ticketType', event.target.value as TicketType | '')}>
          <option value="">{t('spAllTypes')}</option>
          {TICKET_TYPES.map((value) => (
            <option key={value} value={value}>
              {t(TICKET_TYPE_KEY[value])}
            </option>
          ))}
        </Select>
        <div className="self-end">
          <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
        </div>
        <div className="sm:col-span-2">
          <Textarea id="canned-body-ar" ref={arRef} label={t('spCannedBodyAr')} dir="rtl" maxLength={4000} value={form.bodyAr} error={errors.bodyAr} onChange={(event) => set('bodyAr', event.target.value)} />
          {chips('bodyAr')}
          {form.bodyAr && (
            <p dir="rtl" className="mt-2 whitespace-pre-wrap break-words rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">
              <span className="block text-[11px] font-bold">{t('spCannedPreview')}</span>
              {renderCanned(form.bodyAr, SAMPLE)}
            </p>
          )}
        </div>
        <div className="sm:col-span-2">
          <Textarea id="canned-body-en" ref={enRef} label={t('spCannedBodyEn')} dir="ltr" maxLength={4000} value={form.bodyEn} error={errors.bodyEn} onChange={(event) => set('bodyEn', event.target.value)} />
          {chips('bodyEn')}
          {form.bodyEn && (
            <p dir="ltr" className="mt-2 whitespace-pre-wrap break-words rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">
              <span className="block text-[11px] font-bold">{t('spCannedPreview')}</span>
              {renderCanned(form.bodyEn, SAMPLE)}
            </p>
          )}
        </div>
      </form>
    </Modal>
  )
}
