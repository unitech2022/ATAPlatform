import { useState } from 'react'
import { useNavigate } from 'react-router'
import { AdminUserBadges } from '../components/AdminUserBadges'
import { AdminUserFormModal } from '../components/AdminUserFormModal'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { SearchInput, Select } from '../components/Field'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { PermissionError } from '../components/PermissionError'
import { SecretRevealModal } from '../components/SecretRevealModal'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { adminUsers, roles as rolesApi } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { roleLabel } from '../lib/rbac'
import { formatDateTime } from '../lib/format'
import type { AdminUser, AdminUserCreateResult } from '../lib/types'

const PAGE_SIZE = 20
const STATUS_FILTERS = ['active', 'disabled'] as const

/** Admin users (`/admin-users`, docs/12 §F20.5, `admin.users.manage`): search, role and status filters, create. */
export function AdminUsersPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const status = parseEnum(params.get('status'), STATUS_FILTERS)
  const roleId = params.get('roleId') ?? ''
  const [creating, setCreating] = useState(false)
  const [created, setCreated] = useState<AdminUserCreateResult | null>(null)

  const query = useQuery(
    () => adminUsers.list({ search: search.value, roleId, isActive: status === '' ? '' : status === 'active', page, pageSize: PAGE_SIZE }),
    `admin-users:${search.value}:${roleId}:${status}:${page}`,
  )
  // Roles need `admin.roles.manage`; without it the role filter and picker fall back to what the rows carry.
  const roleList = useQuery(() => rolesApi.list(), 'roles-list')

  const columns: Column<AdminUser>[] = [
    {
      key: 'user',
      header: t('auColUser'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block max-w-56 truncate font-bold">{row.fullName || row.username}</span>
          <span className="ltr-nums block text-xs text-muted">{row.username}</span>
        </span>
      ),
    },
    { key: 'phone', header: t('phoneNumber'), className: 'hidden md:table-cell', render: (row) => <span className="ltr-nums text-sm">{row.phoneNumber ?? '—'}</span> },
    {
      key: 'roles',
      header: t('auRoles'),
      render: (row) => (
        <span className="flex max-w-72 flex-wrap gap-1">
          {row.roles.length === 0 ? <span className="text-muted">—</span> : row.roles.map((role) => (
            <Badge key={role.id} tone="muted">
              {role.name || role.code}
            </Badge>
          ))}
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <AdminUserBadges user={row} /> },
    { key: 'lastLogin', header: t('auLastLogin'), className: 'hidden lg:table-cell', render: (row) => <span className="whitespace-nowrap text-xs text-muted">{formatDateTime(row.lastLoginAt, lang)}</span> },
  ]

  return (
    <>
      <PageHeader
        title={t('auTitle')}
        description={t('auCopy')}
        actions={
          <Button icon="plus" onClick={() => setCreating(true)}>
            {t('auNew')}
          </Button>
        }
      />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status}
          onChange={(value) => setFilter('status', value)}
          options={[
            { value: '' as (typeof STATUS_FILTERS)[number] | '', label: t('statusAll') },
            { value: 'active', label: t('auActive') },
            { value: 'disabled', label: t('auDisabled') },
          ]}
        />
        <div className="grid gap-3 sm:grid-cols-2 lg:w-[34rem]">
          <SearchInput className="bg-white shadow-soft" placeholder={t('auSearch')} aria-label={t('auSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
          {roleList.data && (
            <Select id="au-role-filter" aria-label={t('auRoleFilter')} value={roleId} onChange={(event) => setFilter('roleId', event.target.value)} className="bg-white shadow-soft">
              <option value="">{t('auAllRoles')}</option>
              {roleList.data.map((role) => (
                <option key={role.id} value={role.id}>
                  {roleLabel(role, lang)}
                </option>
              ))}
            </Select>
          )}
        </div>
      </div>

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="admin.users.manage" onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.loading} onRowClick={(row) => navigate(`/admin-users/${row.id}`)} emptyTitle={t('auEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <AdminUserFormModal
        open={creating}
        user={null}
        roles={roleList.data ?? []}
        rolesError={roleList.error ? describe(roleList.error) : null}
        onClose={() => setCreating(false)}
        onCreated={(result) => {
          setCreating(false)
          setCreated(result)
          toast.success(t('auCreated'))
          query.reload()
        }}
      />

      <SecretRevealModal
        open={created !== null}
        title={t('auCreatedTitle')}
        description={created ? `${created.adminUser.fullName ?? ''} · ${created.adminUser.username}` : undefined}
        secret={created?.temporaryPassword ?? ''}
        onClose={() => {
          const id = created?.adminUser.id
          setCreated(null)
          if (id) navigate(`/admin-users/${id}`)
        }}
      />
    </>
  )
}
