import type { ReactNode } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { copyText } from '../lib/rbac'
import { Button } from './Button'
import { Icon } from './Icon'
import { Modal } from './Modal'

/** Shows a temporary password exactly once (create admin user / reset password, §F20.5) with a copy button. */
export function SecretRevealModal({ open, title, description, secret, onClose }: { open: boolean; title: ReactNode; description?: ReactNode; secret: string; onClose: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  return (
    <Modal
      open={open}
      title={title}
      description={description}
      onClose={onClose}
      footer={
        <Button onClick={onClose} icon="check">
          {t('auSecretDone')}
        </Button>
      }
    >
      <div role="alert" className="mb-4 flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
        <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
        <p>{t('auSecretOnce')}</p>
      </div>
      <p className="mb-1 text-xs font-bold text-muted">{t('auTemporaryPassword')}</p>
      <div className="flex flex-wrap items-center gap-2">
        <code dir="ltr" data-testid="temporary-password" className="ltr-nums min-w-0 flex-1 break-all rounded-2xl bg-cloud px-4 py-3 font-mono text-base font-bold">
          {secret}
        </code>
        <Button
          variant="secondary"
          icon="copy"
          onClick={async () => {
            if (await copyText(secret)) toast.success(t('auSecretCopied'))
            else toast.error(t('errorTitle'), t('mfCopyFailed'))
          }}
        >
          {t('copy')}
        </Button>
      </div>
    </Modal>
  )
}
