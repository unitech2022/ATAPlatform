import { useEffect, useRef, useState } from 'react'
import { Button } from '../components/Button'
import { Header } from '../components/Header'
import { Icon } from '../components/Icon'
import { Notice } from '../components/Notice'
import { ProgressSteps } from '../components/ProgressSteps'
import { useI18n } from '../i18n'
import { catalogApi, driverApi } from '../lib/api'
import { useAuth } from '../lib/auth'
import { describeError } from '../lib/errors'
import type { ApplicationStatus } from '../lib/types'
import { useResource } from '../lib/useResource'
import { ApplicationHeader } from './portal/ApplicationHeader'
import { DocumentsSection } from './portal/DocumentsSection'
import { ProfileForm } from './portal/ProfileForm'
import { ReviewSummary } from './portal/ReviewSummary'
import { SubmitCard } from './portal/SubmitCard'
import { VehicleForm } from './portal/VehicleForm'

type StepKey = 'profile' | 'vehicle' | 'documents'

const EDITABLE_STATUSES: ApplicationStatus[] = ['draft', 'rejected']
const POLLED_STATUSES: ApplicationStatus[] = ['submitted', 'under_review']
const POLL_INTERVAL_MS = 30_000

export function DriverPortal() {
  const { t, lang } = useI18n()
  const { session, logout } = useAuth()

  const application = useResource(() => driverApi.getApplication(), [lang])
  const cities = useResource(() => catalogApi.cities(), [lang])
  const categories = useResource(() => catalogApi.rideCategories(), [lang])

  const [step, setStep] = useState<StepKey>('profile')
  const [flash, setFlash] = useState<string | null>(null)
  const initialised = useRef(false)

  const data = application.data
  const status = data?.status
  const steps = data?.steps
  const reload = application.reload

  // Poll while the application is being reviewed.
  useEffect(() => {
    if (!status || !POLLED_STATUSES.includes(status)) return
    const timer = window.setInterval(() => reload(true), POLL_INTERVAL_MS)
    return () => window.clearInterval(timer)
  }, [status, reload])

  // Open the first incomplete step on first load.
  useEffect(() => {
    if (!steps || initialised.current) return
    initialised.current = true
    setStep(!steps.profileComplete ? 'profile' : !steps.vehicleComplete ? 'vehicle' : 'documents')
  }, [steps])

  useEffect(() => {
    if (!flash) return
    const timer = window.setTimeout(() => setFlash(null), 6000)
    return () => window.clearTimeout(timer)
  }, [flash])

  const notify = (message: string) => {
    setFlash(message)
    reload(true)
  }

  const editable = status !== undefined && EDITABLE_STATUSES.includes(status)
  const name = session?.user.fullName ?? data?.profile.fullName ?? null

  return (
    <div className="min-h-screen bg-canvas text-ink">
      <Header
        title={t('portal.title')}
        actions={
          <Button variant="secondary" size="sm" onClick={() => void logout()} className="rounded-full">
            <Icon name="user" className="size-4" />
            <span className="hidden sm:inline">{t('action.logout')}</span>
          </Button>
        }
      />

      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-5 lg:px-10">
        {application.loading && !data && (
          <Notice tone="info">{t('state.loading')}</Notice>
        )}
        {application.error !== null && !data && (
          <Notice tone="error" onRetry={() => reload()}>
            {describeError(application.error, t)}
          </Notice>
        )}

        {data && (
          <>
            <ApplicationHeader application={data} name={name} />

            {flash && (
              <Notice tone="success" className="mb-6">
                {flash}
              </Notice>
            )}
            {application.error !== null && (
              <Notice tone="error" className="mb-6" onRetry={() => reload(true)}>
                {describeError(application.error, t)}
              </Notice>
            )}

            {editable ? (
              <>
                <div className="mb-6">
                  <ProgressSteps
                    active={step}
                    onSelect={setStep}
                    steps={[
                      { key: 'profile', label: t('portal.steps.profile'), done: data.steps.profileComplete },
                      { key: 'vehicle', label: t('portal.steps.vehicle'), done: data.steps.vehicleComplete },
                      { key: 'documents', label: t('portal.steps.documents'), done: data.steps.documentsComplete },
                    ]}
                  />
                </div>

                <div className="grid gap-6 lg:grid-cols-3">
                  <div className="min-w-0 lg:col-span-2">
                    {step === 'profile' && (
                      <ProfileForm
                        key={data.applicationNumber}
                        profile={data.profile}
                        cities={cities}
                        onSaved={() => {
                          notify(t('profile.saved'))
                          setStep('vehicle')
                        }}
                      />
                    )}
                    {step === 'vehicle' && (
                      <VehicleForm
                        key={data.vehicle?.id ?? 'new'}
                        vehicle={data.vehicle}
                        categories={categories}
                        onSaved={() => {
                          notify(t('vehicle.saved'))
                          setStep('documents')
                        }}
                      />
                    )}
                    {step === 'documents' && <DocumentsSection application={data} uploadPolicy="all" onChanged={notify} />}
                  </div>
                  <div className="min-w-0">
                    <SubmitCard application={data} onSubmitted={() => notify(t('portal.submit.success'))} />
                  </div>
                </div>
              </>
            ) : (
              <div className="grid gap-6">
                <ReviewSummary application={data} cities={cities.data} categories={categories.data} />
                <DocumentsSection
                  application={data}
                  uploadPolicy={status !== undefined && POLLED_STATUSES.includes(status) ? 'rejected' : 'none'}
                  onChanged={notify}
                />
              </div>
            )}
          </>
        )}
      </main>
    </div>
  )
}
