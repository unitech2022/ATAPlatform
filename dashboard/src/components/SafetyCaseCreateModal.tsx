import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { safety, trips } from '../lib/admin'
import { SAFETY_PRIORITIES } from '../lib/safety'
import { safetyPriorityMeta } from '../lib/status'
import type { SafetyCaseDetail, SafetyPriority } from '../lib/types'
import { Button } from './Button'
import { Input, Select, Textarea } from './Field'
import { Modal } from './Modal'

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export interface SafetyCaseCreatePreset {
  tripId?: string
  tripLabel?: string
  priority?: SafetyPriority
  description?: string
}

export interface SafetyCaseCreateModalProps {
  open: boolean
  /** Prefill (e.g. converting an automatic alert into a case). */
  preset?: SafetyCaseCreatePreset
  onClose: () => void
  onCreated: (created: SafetyCaseDetail) => void
}

/** Manual case (`POST /admin/safety/cases`, type `safety_report`) — phone reports or alert conversion. */
export function SafetyCaseCreateModal({ open, ...props }: SafetyCaseCreateModalProps) {
  return open ? <CreateDialog {...props} /> : null
}

function CreateDialog({ preset, onClose, onCreated }: Omit<SafetyCaseCreateModalProps, 'open'>) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [trip, setTrip] = useState(preset?.tripLabel ?? preset?.tripId ?? '')
  const [priority, setPriority] = useState<SafetyPriority>(preset?.priority ?? 'high')
  const [description, setDescription] = useState(preset?.description ?? '')
  const [errors, setErrors] = useState<{ trip?: string; description?: string }>({})
  const [saving, setSaving] = useState(false)

  /** Accepts a trip id or a trip number (resolved through the trips search). */
  const resolveTripId = async (): Promise<string | null | undefined> => {
    const value = trip.trim()
    if (!value) return undefined
    if (preset?.tripId && (value === preset.tripLabel || value === preset.tripId)) return preset.tripId
    if (UUID.test(value)) return value
    const page = await trips.list({ search: value, page: 1, pageSize: 5 })
    return page.items.find((item) => item.tripNumber.toLowerCase() === value.toLowerCase())?.id ?? null
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!description.trim()) {
      setErrors({ description: t('fieldRequired') })
      return
    }
    setSaving(true)
    try {
      const tripId = await resolveTripId()
      if (tripId === null) {
        setErrors({ trip: t('sfTripNotFound') })
        return
      }
      const created = await safety.create({ tripId, type: 'safety_report', priority, description: description.trim() })
      toast.success(t('sfCaseCreated'))
      onCreated(created)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={t('sfNewCase')}
      description={t('sfNewCaseCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="safety-case-form" variant="danger" loading={saving}>
            {t('sfCreateCase')}
          </Button>
        </>
      }
    >
      <form id="safety-case-form" onSubmit={submit} noValidate className="grid gap-4">
        <Input
          id="case-trip"
          label={t('sfTripOptional')}
          hint={t('sfTripHint')}
          dir="ltr"
          value={trip}
          error={errors.trip}
          onChange={(event) => {
            setTrip(event.target.value)
            setErrors((current) => ({ ...current, trip: undefined }))
          }}
        />
        <Select id="case-priority" label={t('priority')} value={priority} onChange={(event) => setPriority(event.target.value as SafetyPriority)}>
          {SAFETY_PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {t(safetyPriorityMeta[value].key)}
            </option>
          ))}
        </Select>
        <Textarea
          id="case-description"
          label={t('description')}
          value={description}
          maxLength={2000}
          error={errors.description}
          onChange={(event) => {
            setDescription(event.target.value)
            setErrors((current) => ({ ...current, description: undefined }))
          }}
        />
      </form>
    </Modal>
  )
}
