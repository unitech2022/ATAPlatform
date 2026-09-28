import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input } from '../components/Field'
import { JsonView } from '../components/JsonView'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { auditLogs } from '../lib/admin'
import { formatDateTime } from '../lib/format'
import type { AuditLog } from '../lib/types'

const PAGE_SIZE = 20

const ENTITY_SUGGESTIONS: { value: string; key: TranslationKey }[] = [
  { value: 'driver', key: 'entityDriver' },
  { value: 'driver_document', key: 'entityDocument' },
  { value: 'user', key: 'entityUser' },
  { value: 'ride_category', key: 'entityRideCategory' },
  { value: 'vehicle', key: 'entityVehicle' },
]

export function AuditLogsPage() {
  const { t, lang } = useLang()
  const [params, setParams] = useSearchParams()
  const entityType = params.get('entityType') ?? ''
  const entityId = params.get('entityId') ?? ''
  const page = Math.max(1, Number(params.get('page')) || 1)

  const [typeInput, setTypeInput] = useState(entityType)
  const [idInput, setIdInput] = useState(entityId)
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const query = useQuery(
    () => auditLogs.list({ entityType, entityId, page, pageSize: PAGE_SIZE }),
    `audit:${entityType}:${entityId}:${page}`,
  )

  const applyFilters = (event: FormEvent) => {
    event.preventDefault()
    const next = new URLSearchParams()
    if (typeInput.trim()) next.set('entityType', typeInput.trim())
    if (idInput.trim()) next.set('entityId', idInput.trim())
    setParams(next)
    setExpandedId(null)
  }

  const resetFilters = () => {
    setTypeInput('')
    setIdInput('')
    setParams(new URLSearchParams())
    setExpandedId(null)
  }

  const columns: Column<AuditLog>[] = [
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span> },
    {
      key: 'actor',
      header: t('actor'),
      render: (row) => (
        <span>
          <span className="block font-bold">{row.actorName || row.actorUserId || '—'}</span>
          {row.actorRole && <span className="block text-xs text-muted">{row.actorRole}</span>}
        </span>
      ),
    },
    { key: 'action', header: t('action'), render: (row) => <Badge tone="ink">{row.action}</Badge> },
    { key: 'entityType', header: t('entityType'), render: (row) => <span className="ltr-nums">{row.entityType}</span> },
    {
      key: 'entityId',
      header: t('entityId'),
      render: (row) => <span className="ltr-nums block max-w-48 truncate text-xs text-muted">{row.entityId ?? '—'}</span>,
    },
    {
      key: 'details',
      header: t('details'),
      className: 'text-end',
      render: (row) => (
        <Button
          variant="secondary"
          size="sm"
          icon={expandedId === row.id ? 'x' : 'eye'}
          onClick={() => setExpandedId((current) => (current === row.id ? null : row.id))}
        >
          {expandedId === row.id ? t('hideDetails') : t('showDetails')}
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title={t('auditLogsTitle')} description={t('auditLogsCopy')} />

      <form onSubmit={applyFilters} className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-[1fr_1fr_auto]">
        <Input
          id="entityType"
          label={t('entityType')}
          list="entity-types"
          placeholder={t('allEntities')}
          dir="ltr"
          value={typeInput}
          onChange={(event) => setTypeInput(event.target.value)}
        />
        <datalist id="entity-types">
          {ENTITY_SUGGESTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {t(option.key)}
            </option>
          ))}
        </datalist>
        <Input id="entityId" label={t('entityId')} dir="ltr" value={idInput} onChange={(event) => setIdInput(event.target.value)} />
        <div className="flex items-end gap-2">
          <Button type="submit" icon="search" className="h-12">
            {t('apply')}
          </Button>
          <Button variant="secondary" onClick={resetFilters} className="h-12">
            {t('reset')}
          </Button>
        </div>
      </form>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={query.data?.items ?? []}
              rowKey={(row) => row.id}
              loading={query.loading}
              emptyTitle={t('noAuditLogs')}
              renderExpanded={(row) =>
                expandedId === row.id ? (
                  <div className="grid gap-4 lg:grid-cols-2">
                    <JsonView label={t('before')} value={row.before} />
                    <JsonView label={t('after')} value={row.after} />
                    {row.ipAddress && (
                      <p className="text-xs text-muted lg:col-span-2">
                        {t('ipAddress')}: <span className="ltr-nums">{row.ipAddress}</span>
                      </p>
                    )}
                  </div>
                ) : null
              }
            />
            {query.data && (
              <Pagination
                page={query.data.page}
                pageSize={query.data.pageSize}
                total={query.data.total}
                onChange={(nextPage) =>
                  setParams((current) => {
                    const next = new URLSearchParams(current)
                    next.set('page', String(nextPage))
                    return next
                  })
                }
              />
            )}
          </>
        )}
      </Card>
    </>
  )
}
