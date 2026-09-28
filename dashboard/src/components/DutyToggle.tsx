import { useState } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { duty } from '../lib/admin'

/**
 * "On duty" switch for `safety.alert` recipients (docs/08 §F13.10). Rendered only when `/admin/me/duty`
 * answers, i.e. for admins holding `safety.manage`; anyone else simply does not see it.
 */
export function DutyToggle() {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => duty.get(), 'me-duty')
  const [override, setOverride] = useState<boolean | null>(null)
  const [saving, setSaving] = useState(false)

  if (!query.data) return null
  const onDuty = override ?? query.data.onDuty

  const toggle = async () => {
    setSaving(true)
    try {
      const result = await duty.set(!onDuty)
      setOverride(result?.onDuty ?? !onDuty)
      toast.success(!onDuty ? t('onDutyEnabled') : t('onDutyDisabled'))
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <button
      type="button"
      role="switch"
      aria-checked={onDuty}
      disabled={saving}
      onClick={toggle}
      title={t('onDutyCopy')}
      className={`hidden h-9 items-center gap-2 rounded-full border px-3 text-xs font-bold transition sm:inline-flex ${onDuty ? 'border-brand bg-brand-soft text-brand' : 'border-line bg-white text-muted hover:bg-cloud'}`}
    >
      <span className={`size-2 rounded-full ${onDuty ? 'bg-brand' : 'bg-line'}`} />
      {t('onDuty')}
    </button>
  )
}
