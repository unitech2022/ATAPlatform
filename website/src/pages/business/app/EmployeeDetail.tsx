import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { EmployeeStatusPill } from '../../../components/business/StatusPills'
import { ConfirmModal, PageHeader } from '../../../components/business/ui'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { useFlash } from '../../../lib/useFlash'
import { corporateApi } from '../../../lib/api'
import { costCenterLabel } from '../../../lib/corporate'
import { describeError } from '../../../lib/errors'
import { formatDate, formatMoney } from '../../../lib/format'
import { useResource } from '../../../lib/useResource'
import { EmployeeForm } from './EmployeeForm'

type PendingAction = 'disable' | 'enable' | 'resend' | 'delete'

export function EmployeeDetail() {
  const { id = '' } = useParams()
  const { t, lang } = useI18n()
  const navigate = useNavigate()
  const employee = useResource(() => corporateApi.employee(id), [id, lang])
  const costCenters = useResource(() => corporateApi.costCenters(), [lang])
  const policies = useResource(() => corporateApi.policies(), [lang])
  const [pending, setPending] = useState<PendingAction | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [flash, setFlash] = useFlash()
  const data = employee.data

  const run = async () => {
    if (!pending) return
    setBusy(true)
    setError(null)
    try {
      if (pending === 'disable') await corporateApi.disableEmployee(id)
      if (pending === 'enable') await corporateApi.enableEmployee(id)
      if (pending === 'resend') await corporateApi.resendInvitation(id)
      if (pending === 'delete') {
        await corporateApi.deleteEmployee(id)
        navigate('/business/app/employees', { replace: true })
        return
      }
      setFlash(t(`biz.employee.done.${pending}`))
      setPending(null)
      employee.reload(true)
    } catch (caught) {
      setError(describeError(caught, t))
      setPending(null)
    } finally {
      setBusy(false)
    }
  }

  const back = (
    <Link to="/business/app/employees" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-muted hover:text-ink">
      <Icon name="arrow" className="size-4 ltr:rotate-180" />
      {t('biz.employees.title')}
    </Link>
  )

  if (employee.loading && !data) return <LoadingState />
  if (employee.error && !data) {
    return (
      <>
        {back}
        <ErrorState error={employee.error} onRetry={() => employee.reload()} />
      </>
    )
  }
  if (!data) return null

  return (
    <>
      <title>{`${data.fullName ?? data.phoneNumber} · ATA`}</title>
      {back}
      <PageHeader
        title={data.fullName ?? data.phoneNumber}
        subtitle={t(`biz.role.${data.role}`)}
        actions={
          <>
            {data.status === 'invited' && (
              <>
                <Button variant="secondary" size="sm" onClick={() => setPending('resend')}>
                  <Icon name="mail" className="size-4" />
                  {t('biz.employee.resend')}
                </Button>
                <Button variant="danger" size="sm" onClick={() => setPending('delete')}>
                  <Icon name="trash" className="size-4" />
                  {t('biz.employee.cancelInvite')}
                </Button>
              </>
            )}
            {data.status === 'active' && (
              <Button variant="danger" size="sm" onClick={() => setPending('disable')}>
                {t('biz.employee.disable')}
              </Button>
            )}
            {data.status === 'disabled' && (
              <Button variant="brand" size="sm" onClick={() => setPending('enable')}>
                {t('biz.employee.enable')}
              </Button>
            )}
          </>
        }
      />

      {flash && (
        <Notice tone="success" className="mb-4">
          {flash}
        </Notice>
      )}
      {error && (
        <Notice tone="error" className="mb-4">
          {error}
        </Notice>
      )}

      <div className="grid gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
        <Card className="h-fit space-y-4">
          <div className="flex items-center justify-between gap-3">
            <span className="text-sm font-bold text-muted">{t('biz.trip.status')}</span>
            <EmployeeStatusPill status={data.status} />
          </div>
          <dl className="space-y-3 text-sm">
            <div>
              <dt className="text-xs font-bold text-muted">{t('biz.employee.phone')}</dt>
              <dd className="font-bold" dir="ltr">
                {data.phoneNumber}
              </dd>
            </div>
            <div>
              <dt className="text-xs font-bold text-muted">{t('biz.employee.costCenter')}</dt>
              <dd className="font-bold">{costCenterLabel(data.costCenter)}</dd>
            </div>
            <div>
              <dt className="text-xs font-bold text-muted">{t('biz.employee.spent')}</dt>
              <dd className="font-bold">
                {formatMoney(data.spentThisMonth, lang)}
                {data.monthlyBudget !== null && (
                  <span className="text-muted"> {t('biz.of', { total: formatMoney(data.monthlyBudget, lang) })}</span>
                )}
              </dd>
            </div>
            {data.activatedAt && (
              <div>
                <dt className="text-xs font-bold text-muted">{t('biz.employee.activatedAt')}</dt>
                <dd className="font-bold">{formatDate(data.activatedAt, lang)}</dd>
              </div>
            )}
          </dl>
        </Card>
        <Card>
          <h2 className="mb-5 text-lg font-bold">{t('biz.employee.edit')}</h2>
          {costCenters.data && policies.data ? (
            <EmployeeForm
              key={`${data.id}-${data.status}`}
              initial={data}
              costCenters={costCenters.data}
              policies={policies.data}
              submitLabel={t('action.save')}
              onSubmit={async (payload) => {
                await corporateApi.updateEmployee(id, { ...payload, phoneNumber: data.phoneNumber })
                setFlash(t('biz.saved'))
                employee.reload(true)
              }}
            />
          ) : (
            <LoadingState />
          )}
        </Card>
      </div>

      <ConfirmModal
        open={pending !== null}
        title={pending ? t(`biz.employee.confirm.${pending}.title`) : ''}
        message={pending ? t(`biz.employee.confirm.${pending}.copy`) : ''}
        confirmLabel={pending ? t(`biz.employee.confirm.${pending}.cta`) : ''}
        danger={pending === 'disable' || pending === 'delete'}
        busy={busy}
        onConfirm={() => void run()}
        onClose={() => setPending(null)}
      />
    </>
  )
}
