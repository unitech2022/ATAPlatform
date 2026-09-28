import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, DocumentStatusBadge, DriverStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { FilePreviewModal, type FilePreviewTarget } from '../components/FilePreviewModal'
import { Icon } from '../components/Icon'
import { PageSpinner } from '../components/Spinner'
import { ReasonModal } from '../components/ReasonModal'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { documents as documentsApi, drivers } from '../lib/admin'
import { isApiError } from '../lib/api'
import { formatDate, formatDateTime, formatNumber } from '../lib/format'
import { driverStatusKey } from '../lib/status'
import { allowedActions } from '../lib/transitions'
import type { DriverDetail, DriverDocument, DriverReviewAction, Gender, StatusHistoryEntry } from '../lib/types'

type DriverModal = 'reject' | 'suspend' | null

const SUCCESS_KEY: Record<DriverReviewAction, TranslationKey> = {
  start_review: 'reviewStarted',
  approve: 'driverApproved',
  reject: 'driverRejected',
  suspend: 'driverSuspended',
  reinstate: 'driverReinstated',
}

const GENDER_KEY: Record<Gender, TranslationKey> = {
  male: 'genderMale',
  female: 'genderFemale',
  unknown: 'genderUnknown',
}

export function DriverDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => drivers.get(id), `driver:${id}`)

  const [busyAction, setBusyAction] = useState<DriverReviewAction | null>(null)
  const [modal, setModal] = useState<DriverModal>(null)
  const [busyDocumentId, setBusyDocumentId] = useState<string | null>(null)
  const [rejectingDocument, setRejectingDocument] = useState<DriverDocument | null>(null)
  const [preview, setPreview] = useState<FilePreviewTarget | null>(null)

  const runAction = async (action: DriverReviewAction, reason = '') => {
    setBusyAction(action)
    try {
      if (action === 'start_review') await drivers.startReview(id)
      else if (action === 'approve') await drivers.approve(id)
      else if (action === 'reject') await drivers.reject(id, reason)
      else if (action === 'suspend') await drivers.suspend(id, reason)
      else await drivers.reinstate(id)
      toast.success(t(SUCCESS_KEY[action]))
      setModal(null)
      query.reload()
    } catch (error) {
      if (isApiError(error) && error.status === 422) {
        const missing = error.details?.missing
        const detail = Array.isArray(missing) ? missing.map(String).join(lang === 'ar' ? '، ' : ', ') : error.message
        toast.error(action === 'approve' ? t('approveBlocked') : t('errorTitle'), detail)
      } else {
        toast.error(t('errorTitle'), describe(error))
      }
    } finally {
      setBusyAction(null)
    }
  }

  const reviewDocument = async (document: DriverDocument, status: 'verified' | 'rejected', note?: string) => {
    setBusyDocumentId(document.id)
    try {
      await documentsApi.review(document.id, status, note)
      toast.success(t('documentUpdated'))
      setRejectingDocument(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyDocumentId(null)
    }
  }

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const driver = query.data
  const actions = allowedActions(driver.status)
  const missingRequired = driver.requiredDocuments.filter((doc) => doc.isRequired && !doc.uploaded)

  const documentColumns: Column<DriverDocument>[] = [
    {
      key: 'type',
      header: t('documentType'),
      render: (row) => (
        <span className="flex items-center gap-3">
          <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-cloud text-ink">
            <Icon name="document" className="size-5" />
          </span>
          <span className="min-w-0">
            <span className="block font-bold">{row.documentTypeName}</span>
            <span className="ltr-nums block max-w-48 truncate text-xs text-muted">{row.fileName}</span>
          </span>
        </span>
      ),
    },
    { key: 'expiresAt', header: t('expiresAt'), render: (row) => formatDate(row.expiresAt, lang) },
    { key: 'uploadedAt', header: t('uploadedAt'), render: (row) => formatDate(row.uploadedAt, lang) },
    { key: 'status', header: t('status'), render: (row) => <DocumentStatusBadge status={row.status} /> },
    {
      key: 'reviewNote',
      header: t('reviewNote'),
      render: (row) => <span className="block max-w-56 truncate text-muted">{row.reviewNote ?? '—'}</span>,
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button
            variant="secondary"
            size="sm"
            icon="eye"
            onClick={() => setPreview({ fileId: row.fileId, fileName: row.fileName, title: row.documentTypeName })}
          >
            {t('preview')}
          </Button>
          <Button
            variant="brand"
            size="sm"
            icon="check"
            disabled={row.status === 'verified'}
            loading={busyDocumentId === row.id}
            onClick={() => reviewDocument(row, 'verified')}
          >
            {t('verify')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="x" disabled={row.status === 'rejected'} onClick={() => setRejectingDocument(row)}>
            {t('reject')}
          </Button>
        </span>
      ),
    },
  ]

  return (
    <>
      <Link to="/drivers" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('backToDrivers')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="user" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="ltr-nums text-sm font-bold text-brand">{driver.applicationNumber}</p>
              <h2 className="text-2xl font-bold leading-tight">{driver.profile.fullName || driver.user.fullName || t('unnamed')}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <DriverStatusBadge status={driver.status} />
                <span className="ltr-nums">{driver.user.phoneNumber ?? '—'}</span>
                {driver.submittedAt && (
                  <span>
                    {t('submittedAt')}: {formatDateTime(driver.submittedAt, lang)}
                  </span>
                )}
              </div>
            </div>
          </div>

          {actions.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {actions.includes('start_review') && (
                <Button icon="eye" loading={busyAction === 'start_review'} onClick={() => runAction('start_review')}>
                  {t('startReview')}
                </Button>
              )}
              {actions.includes('approve') && (
                <Button variant="brand" icon="check" loading={busyAction === 'approve'} onClick={() => runAction('approve')}>
                  {t('approve')}
                </Button>
              )}
              {actions.includes('reject') && (
                <Button variant="danger-outline" icon="x" onClick={() => setModal('reject')}>
                  {t('reject')}
                </Button>
              )}
              {actions.includes('suspend') && (
                <Button variant="danger-outline" icon="pause" onClick={() => setModal('suspend')}>
                  {t('suspend')}
                </Button>
              )}
              {actions.includes('reinstate') && (
                <Button variant="brand" icon="play" loading={busyAction === 'reinstate'} onClick={() => runAction('reinstate')}>
                  {t('reinstate')}
                </Button>
              )}
            </div>
          )}
        </div>

        {driver.status === 'rejected' && driver.rejectionReason && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>
              <span className="font-bold">{t('rejectionReason')}: </span>
              {driver.rejectionReason}
            </p>
          </div>
        )}
      </Card>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('profile')} className="lg:col-span-2">
          <DefinitionList
            items={[
              { label: t('fullName'), value: driver.profile.fullName || '—' },
              { label: t('phoneNumber'), value: driver.user.phoneNumber ?? '—', ltr: true },
              { label: t('nationalId'), value: driver.profile.nationalId ?? '—', ltr: true },
              { label: t('dateOfBirth'), value: formatDate(driver.profile.dateOfBirth, lang) },
              { label: t('city'), value: driver.profile.cityName ?? driver.profile.cityId ?? '—' },
              { label: t('gender'), value: t(GENDER_KEY[driver.profile.gender] ?? 'genderUnknown') },
              { label: t('iban'), value: driver.profile.iban ?? '—', ltr: true },
              { label: t('language'), value: driver.user.language === 'en' ? 'English' : 'العربية' },
              { label: t('joinedAt'), value: formatDate(driver.user.createdAt, lang) },
              { label: t('approvedAt'), value: formatDate(driver.approvedAt, lang) },
            ]}
          />
        </Card>

        <VehicleCard driver={driver} />
      </div>

      <Card
        className="mb-6"
        flush
        title={t('documents')}
        action={
          <Badge tone={driver.documents.some((doc) => doc.status === 'pending') ? 'warning' : 'brand'}>
            {formatNumber(driver.documents.filter((doc) => doc.status === 'verified').length)} / {formatNumber(driver.documents.length)}{' '}
            {t('docVerified')}
          </Badge>
        }
      >
        {missingRequired.length > 0 && (
          <div className="mx-5 mb-4 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger sm:mx-6">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>
              <span className="font-bold">{t('missingDocuments')} </span>
              {missingRequired.map((doc) => doc.name).join(lang === 'ar' ? '، ' : ', ')}
            </p>
          </div>
        )}
        <Table
          columns={documentColumns}
          rows={driver.documents}
          rowKey={(row) => row.id}
          emptyTitle={t('noDocuments')}
          emptyDescription=""
        />
      </Card>

      <Card title={t('statusHistory')}>
        <StatusHistory entries={driver.statusHistory} />
      </Card>

      <ReasonModal
        open={modal === 'reject'}
        title={t('rejectDriverTitle')}
        description={t('rejectDriverCopy')}
        confirmLabel={t('reject')}
        onClose={() => setModal(null)}
        onConfirm={(reason) => runAction('reject', reason)}
      />
      <ReasonModal
        open={modal === 'suspend'}
        title={t('suspendDriverTitle')}
        description={t('suspendDriverCopy')}
        confirmLabel={t('suspend')}
        onClose={() => setModal(null)}
        onConfirm={(reason) => runAction('suspend', reason)}
      />
      <ReasonModal
        open={rejectingDocument !== null}
        title={t('rejectDocument')}
        description={rejectingDocument ? `${rejectingDocument.documentTypeName} — ${t('rejectDocumentCopy')}` : undefined}
        confirmLabel={t('reject')}
        required={false}
        label={t('noteOptional')}
        onClose={() => setRejectingDocument(null)}
        onConfirm={(note) => (rejectingDocument ? reviewDocument(rejectingDocument, 'rejected', note) : Promise.resolve())}
      />
      <FilePreviewModal target={preview} onClose={() => setPreview(null)} />
    </>
  )
}

function VehicleCard({ driver }: { driver: DriverDetail }) {
  const { t } = useLang()
  const vehicle = driver.vehicle
  return (
    <Card>
      <div className="mb-6 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand">
        <Icon name="car" className="size-7" />
      </div>
      {vehicle ? (
        <>
          <p className="text-xl font-bold">
            {vehicle.make} {vehicle.model}
          </p>
          <p className="mt-1 text-sm text-muted">
            {vehicle.color} · <span className="ltr-nums">{vehicle.year}</span>
          </p>
          <div className="my-6 rounded-2xl bg-cloud p-4 text-center">
            <p className="text-xs text-muted">{t('plateNumber')}</p>
            <p className="mt-2 text-xl font-bold tracking-widest">{vehicle.plateNumber}</p>
          </div>
          <DefinitionList
            items={[
              { label: t('seats'), value: formatNumber(vehicle.seats), ltr: true },
              { label: t('rideCategory'), value: vehicle.rideCategoryName ?? vehicle.rideCategoryId },
            ]}
          />
        </>
      ) : (
        <EmptyState icon="car" title={t('noVehicle')} />
      )}
    </Card>
  )
}

function StatusHistory({ entries }: { entries: StatusHistoryEntry[] }) {
  const { t, lang } = useLang()
  if (entries.length === 0) return <EmptyState icon="clock" title={t('noHistory')} />
  return (
    <ol className="relative space-y-5 border-s-2 border-line ps-6">
      {entries.map((entry) => {
        const fromKey = driverStatusKey(entry.fromStatus)
        const toKey = driverStatusKey(entry.toStatus)
        return (
          <li key={entry.id} className="relative">
            <span className="absolute -start-[31px] top-1 grid size-5 place-items-center rounded-full bg-brand-soft text-brand ring-4 ring-white">
              <Icon name="check" className="size-3" />
            </span>
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-bold">{entry.action}</span>
              {(fromKey || toKey) && (
                <span className="text-sm text-muted">
                  {fromKey ? t(fromKey) : '—'} ← {toKey ? t(toKey) : '—'}
                </span>
              )}
            </div>
            <p className="mt-1 text-xs text-muted">
              {formatDateTime(entry.createdAt, lang)}
              {entry.actorName ? ` · ${t('by')} ${entry.actorName}` : ''}
            </p>
            {entry.reason && <p className="mt-2 rounded-xl bg-cloud px-3 py-2 text-sm">{entry.reason}</p>}
          </li>
        )
      })}
    </ol>
  )
}
