import { useState } from 'react'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Icon } from '../../components/Icon'
import { Notice } from '../../components/Notice'
import { useI18n } from '../../i18n'
import { driverApi, isApiError } from '../../lib/api'
import { describeError } from '../../lib/errors'
import type { DriverApplication } from '../../lib/types'

interface SubmitCardProps {
  application: DriverApplication
  onSubmitted: () => void
}

export function SubmitCard({ application, onSubmitted }: SubmitCardProps) {
  const { t } = useI18n()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [serverMissing, setServerMissing] = useState<string[]>([])

  const { steps, requiredDocuments } = application

  /** Translate a `details.missing` entry ("profile" | "vehicle" | "documents:insurance"). */
  const describeMissing = (code: string): string => {
    if (code === 'profile') return t('portal.missing.profile')
    if (code === 'vehicle') return t('portal.missing.vehicle')
    if (code === 'documents') return t('portal.missing.documents')
    if (code.startsWith('documents:')) {
      const documentCode = code.slice('documents:'.length)
      const match = requiredDocuments.find((item) => item.code === documentCode)
      return t('portal.missing.document', { name: match?.name ?? documentCode })
    }
    return code
  }

  const localMissing: string[] = []
  if (!steps.profileComplete) localMissing.push(t('portal.missing.profile'))
  if (!steps.vehicleComplete) localMissing.push(t('portal.missing.vehicle'))
  if (!steps.documentsComplete) {
    const pending = requiredDocuments.filter((item) => item.isRequired && !item.uploaded)
    if (pending.length > 0) pending.forEach((item) => localMissing.push(t('portal.missing.document', { name: item.name })))
    else localMissing.push(t('portal.missing.documents'))
  }

  const missing = serverMissing.length > 0 ? serverMissing : localMissing

  const submit = async () => {
    if (!steps.canSubmit || busy) return
    setBusy(true)
    setError(null)
    setServerMissing([])
    try {
      await driverApi.submit()
      onSubmitted()
    } catch (caught) {
      setError(describeError(caught, t))
      if (isApiError(caught) && caught.status === 422) {
        setServerMissing(caught.detailStrings('missing').map(describeMissing))
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card tone="dark" className="lg:sticky lg:top-6">
      <div className="mb-6 grid size-12 place-items-center rounded-2xl bg-brand text-white">
        <Icon name="upload" />
      </div>
      <p className="text-xl font-bold">{t('portal.submit.title')}</p>
      <p className="mt-2 text-sm leading-7 text-white/70">{t('portal.submit.copy')}</p>

      {missing.length > 0 && (
        <div className="mt-5 rounded-2xl bg-white/10 p-4">
          <p className="mb-2 text-xs font-bold text-brand">{t('portal.submit.missing')}</p>
          <ul className="space-y-1 text-sm">
            {missing.map((item) => (
              <li key={item} className="flex items-center gap-2">
                <span className="size-1.5 shrink-0 rounded-full bg-white/60" />
                {item}
              </li>
            ))}
          </ul>
        </div>
      )}

      {error && (
        <Notice tone="error" className="mt-5">
          {error}
        </Notice>
      )}

      <Button
        block
        variant="brand"
        className="mt-6 shadow-float disabled:bg-white/10 disabled:text-white/40"
        disabled={!steps.canSubmit || busy}
        onClick={() => void submit()}
      >
        {busy ? t('portal.submit.sending') : t('portal.submit.cta')}
        {!busy && <Icon name="arrow" className="size-5 ltr:rotate-180" />}
      </Button>
    </Card>
  )
}
