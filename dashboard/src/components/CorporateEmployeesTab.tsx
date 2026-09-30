import { useState } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { corporateAccounts } from '../lib/admin'
import { CORPORATE_ROLE_KEY, CORPORATE_USER_STATUSES } from '../lib/corporate'
import { parseEnum } from '../lib/finance'
import { formatDate, formatDateTime } from '../lib/format'
import { corporateUserStatusMeta } from '../lib/status'
import type { CorporateEmployee, CorporateUserStatus } from '../lib/types'
import { Badge, MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { ConfirmModal } from './ConfirmModal'
import { Money } from './Money'
import { PermissionError } from './PermissionError'
import { Pagination } from './Pagination'
import { Table, type Column } from './Table'
import { Tabs } from './Tabs'

const PAGE_SIZE = 20

type Pending = { kind: 'disable' | 'revoke'; employee: CorporateEmployee }

/**
 * Employees of a company (`GET /admin/corporate/accounts/{id}/employees`, §F19.4): status tabs, pending invitations
 * with resend/revoke, and disable/enable. The row actions use the assumed admin mirrors of the company-admin endpoints.
 */
export function CorporateEmployeesTab({ accountId, onChanged }: { accountId: string; onChanged: () => void }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, page, setPage } = useUrlState()
  const status = parseEnum(params.get('status'), CORPORATE_USER_STATUSES)
  const query = useQuery(() => corporateAccounts.employees(accountId, { status, page, pageSize: PAGE_SIZE }), `corporate-employees:${accountId}:${status}:${page}`)
  const [pending, setPending] = useState<Pending | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  // The status filter is also applied client-side in case the server ignores it.
  const rows = (query.data?.items ?? []).filter((row) => !status || row.status === status)

  const run = async (employee: CorporateEmployee, action: () => Promise<void>, done: 'coEmpDisabled' | 'coEmpEnabled' | 'coEmpResent' | 'coEmpRevoked') => {
    setBusyId(employee.id)
    try {
      await action()
      toast.success(t(done))
      query.reload()
      onChanged()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyId(null)
    }
  }

  const confirm = async () => {
    if (!pending) return
    const { kind, employee } = pending
    setPending(null)
    await run(
      employee,
      () => (kind === 'disable' ? corporateAccounts.disableEmployee(accountId, employee.id) : corporateAccounts.revokeInvitation(accountId, employee.id)),
      kind === 'disable' ? 'coEmpDisabled' : 'coEmpRevoked',
    )
  }

  const columns: Column<CorporateEmployee>[] = [
    {
      key: 'employee',
      header: t('coColEmployee'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block max-w-56 truncate font-bold">{row.fullName || t('unnamed')}</span>
          <span className="ltr-nums block text-xs text-muted">{row.phoneNumber}</span>
        </span>
      ),
    },
    { key: 'role', header: t('coColRole'), render: (row) => <Badge tone={row.role === 'corporate_admin' ? 'ink' : 'muted'}>{t(CORPORATE_ROLE_KEY[row.role] ?? 'coRoleEmployee')}</Badge> },
    {
      key: 'number',
      header: t('coEmpNumber'),
      render: (row) => <span className="ltr-nums">{row.employeeNumber ?? '—'}</span>,
    },
    {
      key: 'department',
      header: t('coDepartment'),
      render: (row) => (
        <span className="block">
          <span className="block">{row.department ?? '—'}</span>
          {row.costCenter && <span className="ltr-nums block text-xs text-muted">{row.costCenter}</span>}
        </span>
      ),
    },
    { key: 'policy', header: t('coPolicy'), render: (row) => <span className="block max-w-40 truncate">{row.policyName ?? t('coPolicyDefault')}</span> },
    {
      key: 'budget',
      header: t('coMonthlyBudget'),
      className: 'text-end',
      render: (row) => (
        <span className="block">
          {row.monthlyBudget === null ? <span className="text-muted">—</span> : <Money value={row.monthlyBudget} />}
          {row.spentThisMonth !== null && (
            <span className="block text-xs text-muted">
              {t('coSpentMonth')}: <Money value={row.spentThisMonth} />
            </span>
          )}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={corporateUserStatusMeta} value={row.status} />
          {row.status === 'invited' && row.invitationExpiresAt && (
            <span className="mt-1 block whitespace-nowrap text-xs text-muted" title={formatDateTime(row.invitationExpiresAt, lang)}>
              {t('coInviteExpires')}: {formatDate(row.invitationExpiresAt, lang)}
            </span>
          )}
          {row.status === 'active' && row.activatedAt && <span className="mt-1 block whitespace-nowrap text-xs text-muted">{formatDate(row.activatedAt, lang)}</span>}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex flex-wrap justify-end gap-2">
          {row.status === 'invited' && (
            <>
              <Button variant="secondary" size="sm" icon="send" loading={busyId === row.id} onClick={() => run(row, () => corporateAccounts.resendInvitation(accountId, row.id), 'coEmpResent')}>
                {t('coEmpResend')}
              </Button>
              <Button variant="danger-outline" size="sm" icon="x" onClick={() => setPending({ kind: 'revoke', employee: row })}>
                {t('coEmpRevoke')}
              </Button>
            </>
          )}
          {row.status === 'active' && (
            <Button variant="danger-outline" size="sm" icon="pause" onClick={() => setPending({ kind: 'disable', employee: row })}>
              {t('coEmpDisable')}
            </Button>
          )}
          {row.status === 'disabled' && (
            <Button variant="brand" size="sm" icon="play" loading={busyId === row.id} onClick={() => run(row, () => corporateAccounts.enableEmployee(accountId, row.id), 'coEmpEnabled')}>
              {t('coEmpEnable')}
            </Button>
          )}
        </span>
      ),
    },
  ]

  return (
    <>
      <Tabs
        className="mb-4"
        value={status}
        onChange={(value) => setFilter('status', value)}
        options={[{ value: '' as CorporateUserStatus | '', label: t('statusAll') }, ...CORPORATE_USER_STATUSES.map((value) => ({ value, label: t(corporateUserStatusMeta[value].key) }))]}
      />
      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="corporate.manage" onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('coEmpEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
      <ConfirmModal
        open={pending !== null}
        title={pending?.kind === 'disable' ? t('coEmpDisableTitle') : t('coEmpRevokeTitle')}
        description={pending ? `${pending.employee.fullName || pending.employee.phoneNumber} — ${pending.kind === 'disable' ? t('coEmpDisableCopy') : t('coEmpRevokeCopy')}` : undefined}
        confirmLabel={pending?.kind === 'disable' ? t('coEmpDisable') : t('coEmpRevoke')}
        onClose={() => setPending(null)}
        onConfirm={confirm}
      />
    </>
  )
}
