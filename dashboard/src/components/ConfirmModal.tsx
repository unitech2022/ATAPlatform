import { useState, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { Button, type ButtonVariant } from './Button'
import { Modal } from './Modal'

export interface ConfirmModalProps {
  open: boolean
  title: ReactNode
  description?: ReactNode
  confirmLabel: string
  confirmVariant?: ButtonVariant
  onClose: () => void
  onConfirm: () => Promise<void>
}

export function ConfirmModal({
  open,
  title,
  description,
  confirmLabel,
  confirmVariant = 'danger',
  onClose,
  onConfirm,
}: ConfirmModalProps) {
  const { t } = useLang()
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    setSubmitting(true)
    try {
      await onConfirm()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open={open}
      title={title}
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
      {description && <p className="text-sm text-muted">{description}</p>}
    </Modal>
  )
}
