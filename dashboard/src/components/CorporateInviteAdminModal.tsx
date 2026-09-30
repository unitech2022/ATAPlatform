import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { corporateAccounts } from '../lib/admin'
import { E164_PATTERN } from '../lib/corporate'
import { Button } from './Button'
import { Input } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

export interface CorporateInviteAdminModalProps {
  open: boolean
  accountId: string
  onClose: () => void
  onInvited: () => void
}

/** Invites a company admin by phone (`POST /admin/corporate/accounts/{id}/admins`, §F19.4). */
export function CorporateInviteAdminModal({ open, ...props }: CorporateInviteAdminModalProps) {
  return open ? <InviteDialog {...props} /> : null
}

function InviteDialog({ accountId, onClose, onInvited }: Omit<CorporateInviteAdminModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [phone, setPhone] = useState('')
  const [name, setName] = useState('')
  const [errors, setErrors] = useState<{ phone?: string; name?: string }>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const found: { phone?: string; name?: string } = {}
    if (!E164_PATTERN.test(phone)) found.phone = t('coErrPhone')
    if (!name.trim()) found.name = t('fieldRequired')
    setErrors(found)
    if (found.phone || found.name) return
    setSaving(true)
    try {
      await corporateAccounts.inviteAdmin(accountId, { phoneNumber: phone, fullName: name.trim() })
      onInvited()
    } catch (error) {
      setFormError(describe(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={t('coInviteTitle')}
      description={t('coInviteCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="corporate-invite-form" icon="send" loading={saving}>
            {t('coInviteSend')}
          </Button>
        </>
      }
    >
      <form id="corporate-invite-form" onSubmit={submit} noValidate className="space-y-4">
        {formError && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {formError}
          </p>
        )}
        <Input
          id="co-invite-phone"
          type="tel"
          dir="ltr"
          label={t('phoneNumber')}
          hint={t('coPhoneHint')}
          value={phone}
          error={errors.phone}
          onChange={(event) => {
            setPhone(event.target.value.trim())
            setErrors({})
          }}
          autoFocus
        />
        <Input
          id="co-invite-name"
          label={t('fullName')}
          value={name}
          error={errors.name}
          maxLength={120}
          onChange={(event) => {
            setName(event.target.value)
            setErrors({})
          }}
        />
      </form>
    </Modal>
  )
}
