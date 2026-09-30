import { useNavigate } from 'react-router'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { PageHeader } from '../components/PageHeader'
import { PermissionError } from '../components/PermissionError'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { roles as rolesApi } from '../lib/admin'
import { formatNumber } from '../lib/format'
import { ALL_PERMISSIONS, roleLabel } from '../lib/rbac'
import type { Role } from '../lib/types'

/** Roles (`/roles`, docs/12 §F20.5, `admin.roles.manage`): names, system flag, user and permission counts. */
export function RolesPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const query = useQuery(() => rolesApi.list(), 'roles-list')

  const columns: Column<Role>[] = [
    {
      key: 'name',
      header: t('roColRole'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block max-w-64 truncate font-bold">{roleLabel(row, lang)}</span>
          <span className="ltr-nums block text-xs text-muted">{row.code}</span>
        </span>
      ),
    },
    {
      key: 'description',
      header: t('roDescription'),
      className: 'hidden md:table-cell',
      render: (row) => <span className="block max-w-80 truncate text-sm text-muted">{row.description || '—'}</span>,
    },
    {
      key: 'type',
      header: t('roType'),
      render: (row) => (row.isSystem ? <Badge tone="ink">{t('roSystem')}</Badge> : <Badge tone="muted">{t('roCustom')}</Badge>),
    },
    {
      key: 'users',
      header: t('roUsers'),
      className: 'text-end',
      render: (row) => <span className="ltr-nums font-bold">{typeof row.userCount === 'number' ? formatNumber(row.userCount) : '—'}</span>,
    },
    {
      key: 'permissions',
      header: t('roPermissions'),
      className: 'text-end',
      render: (row) =>
        row.permissionCodes?.includes(ALL_PERMISSIONS) ? (
          <Badge tone="brand">{t('asFullAccess')}</Badge>
        ) : (
          <span className="ltr-nums">{row.permissionCodes ? formatNumber(row.permissionCodes.length) : '—'}</span>
        ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('roTitle')}
        description={t('roCopy')}
        actions={
          <Button icon="plus" onClick={() => navigate('/roles/new')}>
            {t('roNew')}
          </Button>
        }
      />
      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="admin.roles.manage" onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={query.data ?? []} rowKey={(row) => row.id} loading={query.loading} onRowClick={(row) => navigate(`/roles/${row.id}`)} emptyTitle={t('roEmpty')} emptyDescription="" />
        )}
      </Card>
    </>
  )
}
