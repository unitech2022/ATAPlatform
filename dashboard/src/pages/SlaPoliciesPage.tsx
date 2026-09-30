import { useState } from 'react'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { Input } from '../components/Field'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { PermissionError } from '../components/PermissionError'
import { PageSpinner } from '../components/Spinner'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useSpanUnits } from '../hooks/useSpanUnits'
import { slaPolicies } from '../lib/admin'
import { completePolicies, DEFAULT_SLA, formatMinuteSpan, SLA_PRIORITIES, validateSlaPolicy, type SlaPolicyErrors } from '../lib/support'
import { ticketPriorityMeta } from '../lib/status'
import type { SlaPolicy, TicketPriority } from '../lib/types'

type Draft = Record<TicketPriority, { first: string; resolution: string }>

function toDraft(policies: SlaPolicy[]): Draft {
  const complete = completePolicies(policies)
  const draft = {} as Draft
  for (const policy of complete) draft[policy.priority] = { first: String(policy.firstResponseMinutes), resolution: String(policy.resolutionMinutes) }
  return draft
}

/** SLA policies (`/support/sla`, `support.manage`): first-response and resolution targets for the four priorities, saved together. */
export function SlaPoliciesPage() {
  const { t } = useLang()
  const query = useQuery(() => slaPolicies.list(), 'sla-policies')

  return (
    <>
      <PageHeader title={t('spSlaTitle')} description={t('spSlaCopy')} />
      {query.error ? (
        <Card>
          <PermissionError error={query.error} permission="support.manage" onRetry={query.reload} />
        </Card>
      ) : query.data === null ? (
        <PageSpinner />
      ) : (
        // Re-mounted after every reload so the draft always starts from the saved values.
        <PolicyEditor key={JSON.stringify(query.data)} policies={query.data} onSaved={query.reload} />
      )}
    </>
  )
}

function PolicyEditor({ policies, onSaved }: { policies: SlaPolicy[]; onSaved: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const units = useSpanUnits()
  const [draft, setDraft] = useState<Draft>(() => toDraft(policies))
  const [errors, setErrors] = useState<Partial<Record<TicketPriority, SlaPolicyErrors>>>({})
  const [saving, setSaving] = useState(false)

  const saved = toDraft(policies)
  const dirty = SLA_PRIORITIES.some((priority) => draft[priority].first !== saved[priority].first || draft[priority].resolution !== saved[priority].resolution)

  const set = (priority: TicketPriority, key: 'first' | 'resolution', value: string) => {
    setDraft((current) => ({ ...current, [priority]: { ...current[priority], [key]: value } }))
    setErrors((current) => ({ ...current, [priority]: undefined }))
  }

  const save = async () => {
    const next: Partial<Record<TicketPriority, SlaPolicyErrors>> = {}
    for (const priority of SLA_PRIORITIES) {
      const found = validateSlaPolicy(draft[priority].first, draft[priority].resolution)
      if (Object.keys(found).length > 0) next[priority] = found
    }
    setErrors(next)
    if (Object.keys(next).length > 0) return
    setSaving(true)
    try {
      await slaPolicies.save(
        SLA_PRIORITIES.map((priority) => ({ priority, firstResponseMinutes: Number(draft[priority].first), resolutionMinutes: Number(draft[priority].resolution) })),
      )
      toast.success(t('spSlaSaved'))
      onSaved()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const resetDefaults = () => {
    const defaults = {} as Draft
    for (const priority of SLA_PRIORITIES) defaults[priority] = { first: String(DEFAULT_SLA[priority].firstResponseMinutes), resolution: String(DEFAULT_SLA[priority].resolutionMinutes) }
    setDraft(defaults)
    setErrors({})
  }

  return (
    <>
      <div className="mb-4 flex items-start gap-3 rounded-2xl bg-brand-soft p-4 text-sm">
        <Icon name="info" className="mt-0.5 size-4 shrink-0 text-brand" />
        <p>{t('spSlaRules')}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {SLA_PRIORITIES.map((priority) => {
          const row = draft[priority]
          const rowErrors = errors[priority]
          return (
            <Card key={priority} title={<MetaBadge record={ticketPriorityMeta} value={priority} />} description={`${t('spSlaDefault')}: ${formatMinuteSpan(DEFAULT_SLA[priority].firstResponseMinutes, units)} / ${formatMinuteSpan(DEFAULT_SLA[priority].resolutionMinutes, units)}`}>
              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  id={`sla-${priority}-first`}
                  label={t('spSlaFirstResponseMinutes')}
                  type="number"
                  min={1}
                  step={1}
                  dir="ltr"
                  value={row.first}
                  hint={formatMinuteSpan(Number(row.first), units)}
                  error={rowErrors?.firstResponseMinutes ? t('spSlaErrMinutes') : undefined}
                  onChange={(event) => set(priority, 'first', event.target.value)}
                />
                <Input
                  id={`sla-${priority}-resolution`}
                  label={t('spSlaResolutionMinutes')}
                  type="number"
                  min={1}
                  step={1}
                  dir="ltr"
                  value={row.resolution}
                  hint={formatMinuteSpan(Number(row.resolution), units)}
                  error={rowErrors?.resolutionMinutes === 'lessThanFirst' ? t('spSlaErrOrder') : rowErrors?.resolutionMinutes ? t('spSlaErrMinutes') : undefined}
                  onChange={(event) => set(priority, 'resolution', event.target.value)}
                />
              </div>
            </Card>
          )
        })}
      </div>

      <div className="mt-6 flex flex-wrap justify-end gap-2">
        <Button variant="secondary" icon="refresh" onClick={resetDefaults} disabled={saving}>
          {t('spSlaResetDefaults')}
        </Button>
        <Button icon="check" onClick={save} loading={saving} disabled={!dirty}>
          {t('save')}
        </Button>
      </div>
    </>
  )
}
