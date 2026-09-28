import { useState } from 'react'
import { Action, Button } from '../../components/Button'
import { Field, Input } from '../../components/Field'
import { FileDrop, type FileRejection } from '../../components/FileDrop'
import { Icon } from '../../components/Icon'
import { DocumentStatusBadge } from '../../components/StatusBadge'
import { useI18n } from '../../i18n'
import { driverApi, openFilePreview } from '../../lib/api'
import { describeError } from '../../lib/errors'
import { formatDate, todayIsoDate } from '../../lib/format'
import type { DriverDocument, RequiredDocument } from '../../lib/types'

interface DocumentRowProps {
  required: RequiredDocument
  document: DriverDocument | undefined
  canUpload: boolean
  onChanged: (message: string) => void
}

const rejectionMessages: Record<FileRejection, 'docs.tooLarge' | 'docs.badType'> = {
  tooLarge: 'docs.tooLarge',
  badType: 'docs.badType',
}

export function DocumentRow({ required, document, canUpload, onChanged }: DocumentRowProps) {
  const { t, lang } = useI18n()
  const [open, setOpen] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [expiresAt, setExpiresAt] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const status = document?.status ?? 'missing'
  const today = todayIsoDate()
  const inputId = `doc-${required.documentTypeId}`

  const iconTone =
    status === 'verified'
      ? 'bg-brand-soft text-brand'
      : status === 'rejected'
        ? 'bg-danger-soft text-danger'
        : 'bg-cloud text-ink'

  const reset = () => {
    setOpen(false)
    setFile(null)
    setExpiresAt('')
    setError(null)
  }

  const upload = async () => {
    if (!file) {
      setError(t('docs.fileRequired'))
      return
    }
    if (required.requiresExpiry && !expiresAt) {
      setError(t('docs.expiryRequired'))
      return
    }
    setBusy(true)
    setError(null)
    try {
      await driverApi.uploadDocument({
        documentTypeId: required.documentTypeId,
        file,
        ...(required.requiresExpiry ? { expiresAt } : {}),
      })
      reset()
      onChanged(t('docs.uploaded'))
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
    }
  }

  const remove = async () => {
    if (!document || !window.confirm(t('docs.deleteConfirm'))) return
    setBusy(true)
    setError(null)
    try {
      await driverApi.deleteDocument(document.id)
      onChanged(t('docs.deleted'))
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
    }
  }

  const preview = async () => {
    if (!document) return
    setError(null)
    try {
      await openFilePreview(document.fileId)
    } catch (caught) {
      setError(describeError(caught, t))
    }
  }

  return (
    <div className="border-b border-line py-4 last:border-0">
      <div className="flex items-start gap-4">
        <div className={`grid size-11 shrink-0 place-items-center rounded-xl ${iconTone}`}>
          <Icon name="document" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-bold">{required.name}</p>
            <DocumentStatusBadge status={status} />
            <span className="text-xs font-bold text-muted">
              {required.isRequired ? t('docs.required') : t('docs.optional')} · {t(`docs.applies.${required.appliesTo}`)}
            </span>
          </div>
          {document && (
            <p className="mt-1 truncate text-xs text-muted">
              <span dir="auto">{document.fileName}</span>
              {' · '}
              {t('docs.uploadedOn', { date: formatDate(document.uploadedAt, lang) })}
              {document.expiresAt && <> · {t('docs.expiresOn', { date: formatDate(document.expiresAt, lang) })}</>}
            </p>
          )}
          {document?.status === 'rejected' && document.reviewNote && (
            <div className="mt-3 rounded-2xl bg-danger-soft p-3 text-sm">
              <p className="text-xs font-bold text-danger">{t('docs.reviewNote')}</p>
              <p className="mt-1 font-bold text-ink" dir="auto">
                {document.reviewNote}
              </p>
            </div>
          )}

          <div className="mt-3 flex flex-wrap gap-2">
            {document && (
              <Button variant="secondary" size="sm" onClick={() => void preview()} disabled={busy}>
                <Icon name="search" className="size-4" />
                {t('action.preview')}
              </Button>
            )}
            {document?.status === 'pending' && canUpload && (
              <Button variant="danger" size="sm" onClick={() => void remove()} disabled={busy}>
                {t('action.delete')}
              </Button>
            )}
            {canUpload && !open && (
              <Button variant={document ? 'secondary' : 'brand'} size="sm" onClick={() => setOpen(true)} disabled={busy}>
                <Icon name="upload" className="size-4" />
                {document ? t('action.replace') : t('action.upload')}
              </Button>
            )}
          </div>

          {open && canUpload && (
            <div className="mt-4 rounded-2xl border border-line p-4">
              <FileDrop
                id={inputId}
                file={file}
                disabled={busy}
                onChange={(next) => {
                  setFile(next)
                  setError(null)
                }}
                onReject={(reason) => setError(t(rejectionMessages[reason]))}
              />
              {required.requiresExpiry && (
                <Field label={t('docs.expiresAt')} htmlFor={`${inputId}-expiry`} className="mt-4">
                  <Input
                    id={`${inputId}-expiry`}
                    dir="ltr"
                    type="date"
                    min={today}
                    disabled={busy}
                    value={expiresAt}
                    onChange={(event) => {
                      setExpiresAt(event.target.value)
                      setError(null)
                    }}
                  />
                </Field>
              )}
              <div className="mt-4 flex flex-col gap-2 sm:flex-row">
                <Button variant="primary" size="sm" onClick={() => void upload()} disabled={busy} className="sm:min-w-40">
                  <Icon name="upload" className="size-4" />
                  {busy ? t('docs.uploading') : t('action.upload')}
                </Button>
                <Action onClick={reset} disabled={busy} className="rounded-2xl px-4 py-2.5 text-center text-sm font-bold text-muted hover:bg-cloud">
                  {t('action.cancel')}
                </Action>
              </div>
            </div>
          )}

          {error && (
            <p className="mt-3 text-xs font-bold text-danger" role="alert">
              {error}
            </p>
          )}
        </div>
      </div>
    </div>
  )
}
