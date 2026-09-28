import { useState } from 'react'
import { Link } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput, Select } from '../components/Field'
import { Icon } from '../components/Icon'
import { JsonView } from '../components/JsonView'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { deliveries } from '../lib/admin'
import { lookupKey, parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { CHANNEL_KEY, DELIVERY_CHANNELS, DELIVERY_STATUSES, SKIPPED_REASON_KEY } from '../lib/notifications'
import { deliveryStatusMeta } from '../lib/status'
import type { NotificationDelivery } from '../lib/types'

const PAGE_SIZE = 25

export function NotificationDeliveriesPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, page, setPage } = useUrlState()
  const eventCode = useUrlSearch('eventCode')
  const userId = useUrlSearch('userId')
  const channel = parseEnum(params.get('channel'), DELIVERY_CHANNELS)
  const status = parseEnum(params.get('status'), DELIVERY_STATUSES)
  const campaignId = params.get('campaignId') ?? ''
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))
  const [expanded, setExpanded] = useState<string | null>(null)
  const [retrying, setRetrying] = useState<string | null>(null)

  const query = useQuery(
    () => deliveries.list({ eventCode: eventCode.value, userId: userId.value, channel, status, campaignId, from, to, page, pageSize: PAGE_SIZE }),
    `deliveries:${eventCode.value}:${userId.value}:${channel}:${status}:${campaignId}:${from}:${to}:${page}`,
  )

  const retry = async (row: NotificationDelivery) => {
    setRetrying(row.id)
    try {
      await deliveries.retry(row.id)
      toast.success(t('deliveryRetried'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setRetrying(null)
    }
  }

  const columns: Column<NotificationDelivery>[] = [
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span> },
    { key: 'event', header: t('eventCode'), render: (row) => <span className="ltr-nums font-bold">{row.eventCode}</span> },
    { key: 'channel', header: t('channel'), render: (row) => <Badge tone="muted">{t(lookupKey(CHANNEL_KEY, row.channel) ?? 'channelPush')}</Badge> },
    {
      key: 'user',
      header: t('recipient'),
      render: (row) => (
        <span className="block min-w-0">
          <span className="block truncate font-bold">{row.userName || (row.userId ? t('unnamed') : '—')}</span>
          {row.phoneMasked && <span className="ltr-nums block text-xs text-muted">{row.phoneMasked}</span>}
        </span>
      ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => {
        const reason = lookupKey(SKIPPED_REASON_KEY, row.skippedReason)
        return (
          <span className="block">
            <MetaBadge record={deliveryStatusMeta} value={row.status} />
            {row.skippedReason && <span className="mt-1 block text-xs text-muted">{reason ? t(reason) : row.skippedReason}</span>}
            {row.status === 'failed' && row.errorCode && <span className="ltr-nums mt-1 block max-w-48 truncate text-xs text-danger">{row.errorCode}</span>}
          </span>
        )
      },
    },
    { key: 'attempts', header: t('attempts'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.attempts)}</span> },
    {
      key: 'provider',
      header: t('provider'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block">{row.provider ?? '—'}</span>
          {row.providerMessageId && <span className="ltr-nums block max-w-40 truncate text-xs text-muted">{row.providerMessageId}</span>}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon={expanded === row.id ? 'x' : 'eye'} onClick={() => setExpanded((current) => (current === row.id ? null : row.id))}>
            {expanded === row.id ? t('hideDetails') : t('showDetails')}
          </Button>
          {row.status === 'failed' && (
            <Button variant="secondary" size="sm" icon="refresh" loading={retrying === row.id} onClick={() => retry(row)}>
              {t('retry')}
            </Button>
          )}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader title={t('deliveriesTitle')} description={t('deliveriesCopy')} />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-4">
        <SearchInput placeholder={t('eventCode')} dir="ltr" value={eventCode.input} onChange={(event) => eventCode.setInput(event.target.value)} />
        <SearchInput placeholder={t('userId')} dir="ltr" value={userId.input} onChange={(event) => userId.setInput(event.target.value)} />
        <Select id="channel" aria-label={t('channel')} value={channel} onChange={(event) => setFilter('channel', event.target.value)}>
          <option value="">{t('allChannels')}</option>
          {DELIVERY_CHANNELS.map((value) => (
            <option key={value} value={value}>
              {t(CHANNEL_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="status" aria-label={t('status')} value={status} onChange={(event) => setFilter('status', event.target.value)}>
          <option value="">{t('allStatuses')}</option>
          {DELIVERY_STATUSES.map((value) => (
            <option key={value} value={value}>
              {t(deliveryStatusMeta[value].key)}
            </option>
          ))}
        </Select>
        <Input id="from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
      </div>

      {campaignId && (
        <div className="mb-4 flex flex-wrap items-center gap-3 rounded-2xl bg-white p-4 text-sm shadow-soft">
          <Icon name="bell" className="size-4 text-brand" />
          <span className="text-muted">{t('filteredByCampaign')}</span>
          <Link to={`/notifications/campaigns/${campaignId}`} className="font-bold text-brand hover:underline">
            {t('viewCampaign')}
          </Link>
          <Button variant="ghost" size="sm" icon="x" onClick={() => setFilter('campaignId', '')}>
            {t('clearFilter')}
          </Button>
        </div>
      )}

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
              emptyTitle={t('noDeliveries')}
              renderExpanded={(row) => (expanded === row.id ? <DeliveryDetails row={row} /> : null)}
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>
    </>
  )
}

function DeliveryDetails({ row }: { row: NotificationDelivery }) {
  const { t, lang } = useLang()
  return (
    <div className="space-y-4">
      {(row.errorCode || row.errorMessage) && (
        <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
          <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
          <p className="break-words">
            {row.errorCode && <span className="ltr-nums font-bold">{row.errorCode}</span>}
            {row.errorCode && row.errorMessage ? ' — ' : ''}
            {row.errorMessage}
          </p>
        </div>
      )}
      <DefinitionList
        items={[
          { label: t('sentAt'), value: formatDateTime(row.sentAt, lang) },
          { label: t('openedAt'), value: formatDateTime(row.openedAt, lang) },
          { label: t('userId'), value: row.userId ?? '—', ltr: true },
          { label: t('deliveryId'), value: row.id, ltr: true },
        ]}
      />
      {row.payload !== undefined && row.payload !== null && <JsonView label={t('payload')} value={row.payload} />}
    </div>
  )
}
