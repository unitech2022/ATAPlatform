import { useState } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { scheduledTrips } from '../lib/admin'
import type { DriverListItem } from '../lib/types'
import { Button } from './Button'
import { Textarea } from './Field'
import { Icon } from './Icon'
import { DriverPicker } from './DriverPicker'
import { Modal } from './Modal'

export interface AssignDriverModalProps {
  open: boolean
  tripId: string
  tripNumber: string
  /** True when a reservation is active: the admin reassigns, so the current one is released first (a reason is required). */
  reassign: boolean
  onClose: () => void
  onDone: () => void
}

/**
 * Manual reservation (`POST /admin/scheduled-trips/{tripId}/assign`, `source=admin`, subject to the overlap rules).
 * Reassigning releases the active reservation first (`POST …/release-reservation`, no penalty points), then assigns.
 */
export function AssignDriverModal({ open, ...props }: AssignDriverModalProps) {
  return open ? <AssignDriverDialog {...props} /> : null
}

function AssignDriverDialog({ tripId, tripNumber, reassign, onClose, onDone }: Omit<AssignDriverModalProps, 'open'>) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [driver, setDriver] = useState<DriverListItem | null>(null)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    if (!driver) {
      setError(t('sdPickDriverRequired'))
      return
    }
    if (reassign && !reason.trim()) {
      setError(t('reasonRequired'))
      return
    }
    setSubmitting(true)
    let released = false
    try {
      if (reassign) {
        await scheduledTrips.releaseReservation(tripId, reason.trim())
        released = true
      }
      await scheduledTrips.assign(tripId, driver.id)
      toast.success(t(reassign ? 'sdReassigned' : 'sdAssigned'))
      onDone()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
      if (released) {
        toast.push({ tone: 'info', title: t('sdReleasedOnly') })
        onDone()
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      size="lg"
      title={reassign ? t('sdReassignTitle') : t('sdAssignTitle')}
      description={`${tripNumber} — ${t('sdAssignCopy')}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button onClick={submit} loading={submitting} icon="check">
            {reassign ? t('sdReassign') : t('sdAssign')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4">
        <DriverPicker
          value={driver}
          onChange={(next) => {
            setDriver(next)
            setError(null)
          }}
        />
        {reassign && (
          <Textarea
            id="assign-reason"
            label={t('sdReleaseReason')}
            placeholder={t('reasonPlaceholder')}
            hint={t('sdReleaseNoPenalty')}
            value={reason}
            onChange={(event) => {
              setReason(event.target.value)
              setError(null)
            }}
          />
        )}
        {error && (
          <p className="flex items-start gap-2 rounded-2xl bg-danger-soft px-4 py-3 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            {error}
          </p>
        )}
        <p className="flex items-start gap-2 text-xs text-muted">
          <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
          {t('sdAssignNote')}
        </p>
      </div>
    </Modal>
  )
}
