import { useState, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { Button, type ButtonVariant } from './Button'
import { Textarea } from './Field'
import { Modal } from './Modal'

export interface ReasonModalProps {
  open: boolean
  title: ReactNode
  description?: ReactNode
  confirmLabel: string
  confirmVariant?: ButtonVariant
  /** When false the textarea may be left empty (e.g. an optional review note). */
  required?: boolean
  label?: string
  onClose: () => void
  onConfirm: (reason: string) => Promise<void>
}

/** Shared "give a reason" dialog used for reject / suspend / document review. */
export function ReasonModal({ open, ...props }: ReasonModalProps) {
  // Mounted only while open so the textarea starts empty on every use.
  return open ? <ReasonDialog {...props} /> : null
}

function ReasonDialog({
  title,
  description,
  confirmLabel,
  confirmVariant = 'danger',
  required = true,
  label,
  onClose,
  onConfirm,
}: Omit<ReasonModalProps, 'open'>) {
  const { t } = useLang()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    const trimmed = reason.trim()
    if (required && !trimmed) {
      setError(t('reasonRequired'))
      return
    }
    setSubmitting(true)
    try {
      await onConfirm(trimmed)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={title}
      description={description}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button variant={confirmVariant} onClick={submit} loading={submitting}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <Textarea
        id="reason"
        label={label ?? (required ? t('reason') : t('noteOptional'))}
        placeholder={t('reasonPlaceholder')}
        value={reason}
        error={error}
        onChange={(event) => {
          setReason(event.target.value)
          setError(null)
        }}
        autoFocus
      />
    </Modal>
  )
}
