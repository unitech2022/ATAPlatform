import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { CampaignFormModal } from '../components/CampaignFormModal'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { campaigns } from '../lib/admin'
import { lookupKey, parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { CAMPAIGN_STATUSES, CATEGORY_KEY, CHANNEL_KEY } from '../lib/notifications'
import { campaignStatusMeta } from '../lib/status'
import type { Campaign, CampaignStatus } from '../lib/types'

const PAGE_SIZE = 20

export function CampaignsPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const navigate = useNavigate()
  const { params, setFilter, page, setPage } = useUrlState()
  const status = parseEnum(params.get('status'), CAMPAIGN_STATUSES)
  const query = useQuery(() => campaigns.list({ status, page, pageSize: PAGE_SIZE }), `campaigns:${status}:${page}`)
  const [createOpen, setCreateOpen] = useState(false)

  const columns: Column<Campaign>[] = [
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block">
          <span className="block max-w-64 truncate font-bold">{row.name}</span>
          <span className="block text-xs text-muted">{t(lookupKey(CATEGORY_KEY, row.category) ?? 'catSystem')}</span>
        </span>
      ),
    },
    {
      key: 'channels',
      header: t('channels'),
      render: (row) => (
        <span className="flex flex-wrap gap-1">
          {row.channels.map((channel) => (
            <Badge key={channel} tone="muted">
              {t(lookupKey(CHANNEL_KEY, channel) ?? 'channelPush')}
            </Badge>
          ))}
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <MetaBadge record={campaignStatusMeta} value={row.status} /> },
    { key: 'target', header: t('targetCount'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.targetCount)}</span> },
    {
      key: 'sent',
      header: t('delivered'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums">{formatNumber(row.pushSent + row.smsSent + row.inappCreated)}</span>,
    },
    { key: 'opened', header: t('opened'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.openedCount)}</span> },
    {
      key: 'when',
      header: t('when'),
      render: (row) => (
        <span className="whitespace-nowrap text-sm">
          {row.completedAt
            ? formatDateTime(row.completedAt, lang)
            : row.scheduledAt
              ? `${t('scheduledFor')}: ${formatDateTime(row.scheduledAt, lang)}`
              : formatDateTime(row.createdAt, lang)}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('campaignsTitle')}
        description={t('campaignsCopy')}
        actions={
          <Button icon="plus" onClick={() => setCreateOpen(true)}>
            {t('newCampaign')}
          </Button>
        }
      />

      <Tabs
        className="mb-4"
        value={status}
        onChange={(value) => setFilter('status', value)}
        options={[{ value: '' as CampaignStatus | '', label: t('statusAll') }, ...CAMPAIGN_STATUSES.map((value) => ({ value, label: t(campaignStatusMeta[value].key) }))]}
      />

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
              onRowClick={(row) => navigate(`/notifications/campaigns/${row.id}`)}
              emptyTitle={t('noCampaigns')}
            />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <CampaignFormModal
        open={createOpen}
        campaign={null}
        onClose={() => setCreateOpen(false)}
        onDone={(campaign) => {
          toast.success(t('campaignSaved'), campaign.name)
          setCreateOpen(false)
          navigate(`/notifications/campaigns/${campaign.id}`)
        }}
      />
    </>
  )
}
