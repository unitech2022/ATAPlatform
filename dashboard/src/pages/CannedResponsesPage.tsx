import { useState } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { CannedResponseFormModal } from '../components/CannedResponseFormModal'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { PageHeader } from '../components/PageHeader'
import { PermissionError } from '../components/PermissionError'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { cannedResponses } from '../lib/admin'
import { TICKET_TYPE_KEY } from '../lib/support'
import type { CannedResponse } from '../lib/types'

/** Canned responses (`/support/canned-responses`, `support.manage`): CRUD of the bilingual templates agents insert into replies. */
export function CannedResponsesPage() {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => cannedResponses.list(), 'canned-responses-admin')
  const [editing, setEditing] = useState<CannedResponse | 'new' | null>(null)
  const [deleting, setDeleting] = useState<CannedResponse | null>(null)

  const remove = async () => {
    if (!deleting) return
    try {
      await cannedResponses.remove(deleting.id)
      toast.success(t('spCannedDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<CannedResponse>[] = [
    {
      key: 'title',
      header: t('spCannedTitle'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.title}</span>
          <span className="ltr-nums block text-xs text-muted">{row.code}</span>
        </span>
      ),
    },
    {
      key: 'type',
      header: t('type'),
      render: (row) => (row.ticketType ? <Badge tone="ink">{t(TICKET_TYPE_KEY[row.ticketType])}</Badge> : <Badge tone="muted">{t('spAllTypes')}</Badge>),
    },
    {
      key: 'body',
      header: t('spCannedBodies'),
      render: (row) => (
        <span className="block max-w-md whitespace-normal">
          <span dir="rtl" className="line-clamp-2 block text-xs text-muted">
            {row.bodyAr}
          </span>
          <span dir="ltr" className="line-clamp-2 mt-1 block text-xs text-muted">
            {row.bodyEn}
          </span>
        </span>
      ),
    },
    { key: 'active', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => setEditing(row)}>
            {t('edit')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('spCannedTitlePage')}
        description={t('spCannedCopy')}
        actions={
          <Button icon="plus" onClick={() => setEditing('new')}>
            {t('spCannedNew')}
          </Button>
        }
      />

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="support.manage" onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={query.data ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('spCannedEmpty')} emptyDescription="" />
        )}
      </Card>

      <CannedResponseFormModal
        target={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          query.reload()
        }}
      />
      <ConfirmModal
        open={deleting !== null}
        title={t('spCannedDelete')}
        description={deleting ? `${deleting.title} (${deleting.code}) — ${t('spCannedDeleteCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
