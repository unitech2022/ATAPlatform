import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { saveBlob } from '../lib/finance'
import { copyText, recoveryCodesText } from '../lib/rbac'
import { Button } from './Button'
import { Icon } from './Icon'

/** One-time display of MFA recovery codes (§F20.4) with copy and a `.txt` download. */
export function RecoveryCodesPanel({ codes, username }: { codes: readonly string[]; username?: string | null }) {
  const { t } = useLang()
  const toast = useToast()

  const copy = async () => {
    if (await copyText(codes.join('\n'))) toast.success(t('mfCodesCopied'))
    else toast.error(t('errorTitle'), t('mfCopyFailed'))
  }

  const download = () => {
    const heading = `ATA Admin — ${t('mfRecoveryCodes')}${username ? ` (${username})` : ''}`
    saveBlob(new Blob([recoveryCodesText(codes, heading)], { type: 'text/plain;charset=utf-8' }), `ata-admin-recovery-codes${username ? `-${username}` : ''}.txt`)
  }

  return (
    <div>
      <div role="alert" className="mb-4 flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
        <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
        <p>{t('mfCodesOnce')}</p>
      </div>
      <ol aria-label={t('mfRecoveryCodes')} className="grid grid-cols-2 gap-2 rounded-2xl bg-cloud p-4" dir="ltr">
        {codes.map((code) => (
          <li key={code} className="ltr-nums rounded-xl bg-white px-3 py-2 text-center font-mono text-sm font-bold tracking-wider">
            {code}
          </li>
        ))}
      </ol>
      <div className="mt-3 flex flex-wrap gap-2">
        <Button variant="secondary" size="sm" icon="copy" onClick={copy}>
          {t('mfCopyCodes')}
        </Button>
        <Button variant="secondary" size="sm" icon="download" onClick={download}>
          {t('mfDownloadCodes')}
        </Button>
      </div>
    </div>
  )
}
