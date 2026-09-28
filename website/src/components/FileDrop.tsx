import { useRef, useState, type DragEvent } from 'react'
import { useI18n } from '../i18n'
import { formatFileSize } from '../lib/format'
import { Action } from './Button'
import { Icon } from './Icon'

const MAX_FILE_BYTES = 10 * 1024 * 1024
const ACCEPTED_TYPES = ['application/pdf', 'image/jpeg', 'image/png']
const ACCEPTED_EXTENSIONS = /\.(pdf|jpe?g|png)$/i
const FILE_ACCEPT = '.pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png'

export type FileRejection = 'tooLarge' | 'badType'

function validateFile(file: File): FileRejection | null {
  const typeOk = ACCEPTED_TYPES.includes(file.type) || (file.type === '' && ACCEPTED_EXTENSIONS.test(file.name))
  if (!typeOk) return 'badType'
  if (file.size > MAX_FILE_BYTES) return 'tooLarge'
  return null
}

interface FileDropProps {
  id: string
  file: File | null
  onChange: (file: File | null) => void
  onReject: (reason: FileRejection) => void
  disabled?: boolean
}

export function FileDrop({ id, file, onChange, onReject, disabled = false }: FileDropProps) {
  const { t } = useI18n()
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = useState(false)

  const accept = (candidate: File | undefined) => {
    if (!candidate) return
    const rejection = validateFile(candidate)
    if (rejection) {
      onChange(null)
      onReject(rejection)
      return
    }
    onChange(candidate)
  }

  const onDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    setDragging(false)
    if (disabled) return
    accept(event.dataTransfer.files[0])
  }

  return (
    <div
      onDragOver={(event) => {
        event.preventDefault()
        if (!disabled) setDragging(true)
      }}
      onDragLeave={() => setDragging(false)}
      onDrop={onDrop}
      className={`rounded-2xl border-2 border-dashed p-4 text-center transition ${
        dragging ? 'border-brand bg-brand-soft' : file ? 'border-brand bg-white' : 'border-line bg-cloud'
      } ${disabled ? 'opacity-60' : ''}`}
    >
      <input
        ref={inputRef}
        id={id}
        type="file"
        accept={FILE_ACCEPT}
        className="sr-only"
        disabled={disabled}
        onChange={(event) => {
          accept(event.target.files?.[0])
          event.target.value = ''
        }}
      />
      {file ? (
        <div className="flex items-center gap-3 text-start">
          <div className="grid size-11 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand">
            <Icon name="document" />
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-bold" dir="auto">
              {file.name}
            </p>
            <p className="text-xs text-muted">{formatFileSize(file.size)}</p>
          </div>
          <Action
            disabled={disabled}
            onClick={() => onChange(null)}
            className="rounded-lg bg-danger-soft px-2 py-1 text-xs font-bold text-danger"
          >
            {t('action.delete')}
          </Action>
        </div>
      ) : (
        <Action
          disabled={disabled}
          onClick={() => inputRef.current?.click()}
          className="flex w-full flex-col items-center gap-2 py-2 text-center"
        >
          <span className="grid size-11 place-items-center rounded-xl bg-white text-brand shadow-soft">
            <Icon name="upload" />
          </span>
          <span className="text-sm font-bold">{t('docs.chooseFile')}</span>
          <span className="text-xs text-muted">{t('docs.fileTypes')}</span>
        </Action>
      )}
    </div>
  )
}
