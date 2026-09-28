import { useState } from 'react'
import { Link } from 'react-router'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { SearchInput, Select, Textarea } from '../components/Field'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { lostItems } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDateTime } from '../lib/format'
import { LOST_CATEGORY_KEY, LOST_ITEM_STATUSES, telHref } from '../lib/safety'
import { lostItemStatusMeta } from '../lib/status'
import type { LostItemReport, LostItemStatus } from '../lib/types'

const PAGE_SIZE = 20

/** Next steps suggested per status (§F12.6: ops complete `driver_contacted` → `returned` → `closed`). */
const NEXT_STATUSES: Record<LostItemStatus, LostItemStatus[]> = {
  open: ['driver_contacted', 'found', 'not_found', 'closed'],
  driver_contacted: ['found', 'not_found', 'returned', 'closed'],
  found: ['returned', 'closed'],
  returned: ['closed'],
  not_found: ['driver_contacted', 'closed'],
  closed: ['open'],
}

export function LostItemsPage() {
  const { t, lang } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const [editing, setEditing] = useState<LostItemReport | null>(null)

  const status = parseEnum(params.get('status'), LOST_ITEM_STATUSES)
  const query = useQuery(() => lostItems.list({ status, search: search.value, page, pageSize: PAGE_SIZE }), `lost-items:${status}:${search.value}:${page}`)

  const columns: Column<LostItemReport>[] = [
    {
      key: 'number',
      header: t('liReportNumber'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold">{row.reportNumber}</span>
          <span className="block text-xs text-muted">{formatDateTime(row.createdAt, lang)}</span>
        </span>
      ),
    },
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (
        <Link to={`/trips/${row.tripId}`} className="ltr-nums font-bold text-brand hover:underline">
          {row.tripNumber ?? t('viewTrip')}
        </Link>
      ),
    },
    {
      key: 'item',
      header: t('liItem'),
      render: (row) => (
        <span className="block max-w-72">
          <span className="block font-bold">{t(LOST_CATEGORY_KEY[row.itemCategory] ?? 'liCatOther')}</span>
          <span className="line-clamp-2 block whitespace-normal text-xs text-muted">{row.description}</span>
        </span>
      ),
    },
    {
      key: 'reporter',
      header: t('passenger'),
      render: (row) => {
        const phone = row.contactPhone ?? row.reporterPhone ?? null
        return (
          <span className="block">
            <span className="block font-bold">{row.reporterName || t('unnamed')}</span>
            {phone && (
              <a href={telHref(phone)} className="ltr-nums block text-xs font-bold text-brand hover:underline">
                {phone}
              </a>
            )}
          </span>
        )
      },
    },
    {
      key: 'driver',
      header: t('driver'),
      render: (row) => (
        <span className="block">
          {row.driverId ? (
            <Link to={`/drivers/${row.driverId}`} className="block font-bold text-brand hover:underline">
              {row.driverName || t('unnamed')}
            </Link>
          ) : (
            <span className="block text-muted">—</span>
          )}
          <span className="block text-xs text-muted">
            {row.driverResponse ? (row.driverResponse === 'found' ? t('liDriverFound') : t('liDriverNotFound')) : t('liNoDriverResponse')}
          </span>
          {row.driverNote && <span className="block max-w-56 truncate text-xs text-muted">{row.driverNote}</span>}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={lostItemStatusMeta} value={row.status} />
          {row.supportTicketId && <span className="ltr-nums mt-1 block max-w-40 truncate text-[11px] text-muted">#{row.supportTicketId}</span>}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <Button variant="secondary" size="sm" icon="edit" onClick={() => setEditing(row)}>
          {t('liUpdateStatus')}
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title={t('liTitle')} description={t('liCopy')} />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={status}
          onChange={(value) => setFilter('status', value)}
          options={[
            { value: '' as LostItemStatus | '', label: t('statusAll') },
            ...LOST_ITEM_STATUSES.map((value) => ({ value, label: t(lostItemStatusMeta[value].key) })),
          ]}
        />
        <SearchInput wrapperClassName="lg:w-80" className="bg-white shadow-soft" placeholder={t('liSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('liEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <LostItemStatusModal
        report={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          query.reload()
        }}
      />
    </>
  )
}

function LostItemStatusModal({ report, onClose, onSaved }: { report: LostItemReport | null; onClose: () => void; onSaved: () => void }) {
  return report ? <StatusDialog key={report.id} report={report} onClose={onClose} onSaved={onSaved} /> : null
}

function StatusDialog({ report, onClose, onSaved }: { report: LostItemReport; onClose: () => void; onSaved: () => void }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const options = NEXT_STATUSES[report.status] ?? LOST_ITEM_STATUSES
  const [status, setStatus] = useState<LostItemStatus>(options[0] ?? 'closed')
  const [note, setNote] = useState('')
  const [saving, setSaving] = useState(false)

  const submit = async () => {
    setSaving(true)
    try {
      await lostItems.update(report.id, status, note.trim())
      toast.success(t('liUpdated'))
      onSaved()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      title={t('liUpdateStatus')}
      description={`${report.reportNumber} · ${t(LOST_CATEGORY_KEY[report.itemCategory] ?? 'liCatOther')}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button onClick={submit} loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4">
        <div className="flex items-center gap-2 text-sm text-muted">
          {t('status')}: <MetaBadge record={lostItemStatusMeta} value={report.status} />
        </div>
        <Select id="lost-status" label={t('liNewStatus')} value={status} onChange={(event) => setStatus(event.target.value as LostItemStatus)}>
          {options.map((value) => (
            <option key={value} value={value}>
              {t(lostItemStatusMeta[value].key)}
            </option>
          ))}
        </Select>
        <Textarea id="lost-note" label={t('noteOptional')} maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
      </div>
    </Modal>
  )
}
