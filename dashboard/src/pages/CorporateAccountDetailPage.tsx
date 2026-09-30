import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { CorporateAccountFormModal } from '../components/CorporateAccountFormModal'
import { CorporateActivityTab } from '../components/CorporateActivityTab'
import { CorporateAdjustmentModal } from '../components/CorporateAdjustmentModal'
import { CorporateCostCentersTab } from '../components/CorporateCostCentersTab'
import { CorporateEmployeesTab } from '../components/CorporateEmployeesTab'
import { CorporateInviteAdminModal } from '../components/CorporateInviteAdminModal'
import { CorporateInvoicesPanel } from '../components/CorporateInvoicesPanel'
import { CorporateOverviewTab } from '../components/CorporateOverviewTab'
import { CorporatePoliciesTab } from '../components/CorporatePoliciesTab'
import { CorporateTripsTab } from '../components/CorporateTripsTab'
import { Icon } from '../components/Icon'
import { PermissionError } from '../components/PermissionError'
import { ReasonModal } from '../components/ReasonModal'
import { PageSpinner } from '../components/Spinner'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import type { TranslationKey } from '../i18n'
import { corporateAccounts, corporateReceivables } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { corporateAccountStatusMeta } from '../lib/status'

const TABS = ['overview', 'employees', 'policies', 'costCenters', 'trips', 'invoices', 'activity'] as const
type TabKey = (typeof TABS)[number]

const TAB_LABEL: Record<TabKey, TranslationKey> = {
  overview: 'coTabOverview',
  employees: 'coTabEmployees',
  policies: 'coTabPolicies',
  costCenters: 'coTabCostCenters',
  trips: 'coTabTrips',
  invoices: 'coTabInvoices',
  activity: 'coTabActivity',
}

type Confirming = 'activate' | 'suspend' | 'close' | null

/** Company detail (`/corporate/:id`, §F19.7): legal data, employees, policies (read-only), trips, invoices, adjustments and account actions. */
export function CorporateAccountDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, update } = useUrlState()
  const tab = parseEnum(params.get('tab'), TABS) || 'overview'
  const query = useQuery(() => corporateAccounts.get(id), `corporate-account:${id}`)
  // Credit used / overdue come from the receivables list (needs `payments.view` too); the page degrades without it.
  const receivables = useQuery(() => corporateReceivables.list(), 'corporate-receivables')
  const [editing, setEditing] = useState(false)
  const [inviting, setInviting] = useState(false)
  const [adjusting, setAdjusting] = useState(false)
  const [confirming, setConfirming] = useState<Confirming>(null)

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <PermissionError error={query.error} permission="corporate.manage" onRetry={query.reload} />
      </Card>
    )
  }
  const account = query.data
  const name = lang === 'en' && account.legalNameEn ? account.legalNameEn : account.legalNameAr
  const canActivate = account.status === 'pending' || account.status === 'suspended'

  const changeTab = (value: TabKey) => update((next) => {
    for (const key of Array.from(next.keys())) next.delete(key)
    if (value !== 'overview') next.set('tab', value)
  })

  const runAction = async (action: () => Promise<unknown>, done: TranslationKey) => {
    try {
      await action()
      toast.success(t(done))
      setConfirming(null)
      query.reload()
      receivables.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  return (
    <>
      <Link to="/corporate" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('coBack')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="bank" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="ltr-nums text-sm font-bold text-brand">{account.accountNumber}</p>
              <h2 className="break-words text-2xl font-bold leading-tight">{account.displayName || name}</h2>
              <p className="mt-1 break-words text-sm text-muted">{name}</p>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={corporateAccountStatusMeta} value={account.status} />
                <span className="ltr-nums">
                  {t('coCrNumber')}: {account.crNumber}
                </span>
              </div>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="edit" onClick={() => setEditing(true)}>
              {t('edit')}
            </Button>
            <Button variant="secondary" icon="user" onClick={() => setInviting(true)} disabled={account.status === 'closed'}>
              {t('coInviteAdmin')}
            </Button>
            <Button variant="secondary" icon="receipt" onClick={() => setAdjusting(true)} disabled={account.status === 'closed'}>
              {t('coAddAdjustment')}
            </Button>
            {canActivate && (
              <Button variant="brand" icon="play" onClick={() => setConfirming('activate')}>
                {t('coActivate')}
              </Button>
            )}
            {account.status === 'active' && (
              <Button variant="danger-outline" icon="pause" onClick={() => setConfirming('suspend')}>
                {t('coSuspend')}
              </Button>
            )}
            {account.status !== 'closed' && (
              <Button variant="danger-outline" icon="x" onClick={() => setConfirming('close')}>
                {t('coClose')}
              </Button>
            )}
          </div>
        </div>
        {account.status === 'pending' && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
            <Icon name="info" className="mt-0.5 size-4 shrink-0" />
            <p>{t('coPendingNote')}</p>
          </div>
        )}
      </Card>

      <Tabs className="mb-4" value={tab} onChange={changeTab} options={TABS.map((value) => ({ value, label: t(TAB_LABEL[value]) }))} />

      {tab === 'overview' && <CorporateOverviewTab account={account} receivables={receivables} />}
      {tab === 'employees' && <CorporateEmployeesTab accountId={account.id} onChanged={query.reload} />}
      {tab === 'policies' && <CorporatePoliciesTab accountId={account.id} />}
      {tab === 'costCenters' && <CorporateCostCentersTab accountId={account.id} />}
      {tab === 'trips' && <CorporateTripsTab accountId={account.id} accountNumber={account.accountNumber} />}
      {tab === 'invoices' && <CorporateInvoicesPanel accountId={account.id} />}
      {tab === 'activity' && <CorporateActivityTab accountId={account.id} />}

      <CorporateAccountFormModal
        open={editing}
        account={account}
        onClose={() => setEditing(false)}
        onSaved={() => {
          setEditing(false)
          toast.success(t('coSaved'))
          query.reload()
        }}
      />
      <CorporateInviteAdminModal
        open={inviting}
        accountId={account.id}
        onClose={() => setInviting(false)}
        onInvited={() => {
          setInviting(false)
          toast.success(t('coInviteSent'))
          query.reload()
        }}
      />
      <CorporateAdjustmentModal
        open={adjusting}
        accountId={account.id}
        onClose={() => setAdjusting(false)}
        onSaved={() => {
          setAdjusting(false)
          toast.success(t('coAdjSaved'))
          receivables.reload()
        }}
      />

      <ConfirmModal
        open={confirming === 'activate'}
        title={t('coActivateTitle')}
        description={`${account.displayName} — ${t('coActivateCopy')}`}
        confirmLabel={t('coActivate')}
        confirmVariant="brand"
        onClose={() => setConfirming(null)}
        onConfirm={() => runAction(() => corporateAccounts.activate(account.id), 'coActivated')}
      />
      <ReasonModal
        open={confirming === 'suspend'}
        title={t('coSuspendTitle')}
        description={`${account.displayName} — ${t('coSuspendCopy')}`}
        confirmLabel={t('coSuspend')}
        onClose={() => setConfirming(null)}
        onConfirm={(reason) => runAction(() => corporateAccounts.suspend(account.id, reason), 'coSuspended')}
      />
      <ReasonModal
        open={confirming === 'close'}
        title={t('coCloseTitle')}
        description={`${account.displayName} — ${t('coCloseCopy')}`}
        confirmLabel={t('coClose')}
        onClose={() => setConfirming(null)}
        onConfirm={(reason) => runAction(() => corporateAccounts.close(account.id, reason), 'coClosed')}
      />
    </>
  )
}

