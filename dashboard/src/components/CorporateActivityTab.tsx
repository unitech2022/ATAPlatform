import { useState } from 'react'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { auditLogs } from '../lib/admin'
import { formatDateTime } from '../lib/format'
import type { AuditLog } from '../lib/types'
import { Badge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { JsonView } from './JsonView'
import { PermissionError } from './PermissionError'
import { Pagination } from './Pagination'
import { Table, type Column } from './Table'

const PAGE_SIZE = 20

/** Activity of a company: its `corporate_account` audit trail (`GET /admin/audit-logs?entityType=corporate_account&entityId=…`, §F19.4 audit list). */
export function CorporateActivityTab({ accountId }: { accountId: string }) {
  const { t, lang } = useLang()
  const { page, setPage } = useUrlState()
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const query = useQuery(() => auditLogs.list({ entityType: 'corporate_account', entityId: accountId, page, pageSize: PAGE_SIZE }), `corporate-activity:${accountId}:${page}`)

  const columns: Column<AuditLog>[] = [
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span> },
    {
      key: 'actor',
      header: t('actor'),
      render: (row) => (
        <span>
          <span className="block font-bold">{row.actorName || row.actorUserId || t('actorSystem')}</span>
          {row.actorRole && <span className="block text-xs text-muted">{row.actorRole}</span>}
        </span>
      ),
    },
    { key: 'action', header: t('action'), render: (row) => <Badge tone="ink">{row.action}</Badge> },
    {
      key: 'details',
      header: t('details'),
      className: 'text-end',
      render: (row) => (
        <Button variant="secondary" size="sm" icon={expandedId === row.id ? 'x' : 'eye'} onClick={() => setExpandedId((current) => (current === row.id ? null : row.id))}>
          {expandedId === row.id ? t('hideDetails') : t('showDetails')}
        </Button>
      ),
    },
  ]

  return (
    <Card flush>
      {query.error ? (
        <PermissionError error={query.error} permission="audit.view" onRetry={query.reload} />
      ) : (
        <>
          <Table
            columns={columns}
            rows={query.data?.items ?? []}
            rowKey={(row) => row.id}
            loading={query.loading}
            emptyTitle={t('coActEmpty')}
            emptyDescription=""
            renderExpanded={(row) =>
              expandedId === row.id ? (
                <div className="grid gap-4 md:grid-cols-2">
                  <JsonView label={t('before')} value={row.before} />
                  <JsonView label={t('after')} value={row.after} />
                </div>
              ) : null
            }
          />
          {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
        </>
      )}
    </Card>
  )
}
