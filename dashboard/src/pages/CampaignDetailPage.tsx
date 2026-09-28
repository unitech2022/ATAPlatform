import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { CampaignFormModal } from '../components/CampaignFormModal'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { PageSpinner } from '../components/Spinner'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { campaigns } from '../lib/admin'
import { lookupKey } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import { CATEGORY_KEY, CHANNEL_KEY } from '../lib/notifications'
import { fromLocalInput } from '../lib/pricing'
import { campaignStatusMeta } from '../lib/status'
import type { Campaign } from '../lib/types'

type Pending = 'edit' | 'schedule' | 'send' | 'cancel' | 'delete' | null

const POLL_MS = 5000

export function CampaignDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const query = useQuery(() => campaigns.get(id), `campaign:${id}`)
  const [pending, setPending] = useState<Pending>(null)
  const { reload } = query
  const live = query.data?.status === 'sending'

  // Stats move while the campaign is sending; refresh until it settles.
  useEffect(() => {
    if (!live) return
    const timer = window.setTimeout(reload, POLL_MS)
    return () => window.clearTimeout(timer)
  }, [live, reload, query.data])

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const campaign = query.data

  const run = async (action: 'send' | 'cancel' | 'delete') => {
    try {
      if (action === 'send') await campaigns.sendNow(campaign.id)
      else if (action === 'cancel') await campaigns.cancel(campaign.id)
      else await campaigns.remove(campaign.id)
      toast.success(t(action === 'send' ? 'campaignSending' : action === 'cancel' ? 'campaignCancelled' : 'campaignDeleted'), campaign.name)
      setPending(null)
      if (action === 'delete') navigate('/notifications/campaigns')
      else query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const reached = campaign.inappCreated + campaign.pushSent + campaign.smsSent
  const openBase = Math.max(campaign.inappCreated, campaign.pushSent)
  const openRate = openBase > 0 ? Math.round((campaign.openedCount / openBase) * 1000) / 10 : null
  const progress = campaign.targetCount > 0 ? Math.min(100, Math.round((Math.max(campaign.inappCreated, campaign.pushSent + campaign.pushFailed + campaign.pushSkipped) / campaign.targetCount) * 100)) : 0

  const stats: { label: TranslationKey; value: number | string; tone?: 'danger' }[] = [
    { label: 'targetCount', value: formatNumber(campaign.targetCount) },
    { label: 'inappCreated', value: formatNumber(campaign.inappCreated) },
    { label: 'pushSent', value: formatNumber(campaign.pushSent) },
    { label: 'pushFailed', value: formatNumber(campaign.pushFailed), tone: campaign.pushFailed > 0 ? 'danger' : undefined },
    { label: 'pushSkipped', value: formatNumber(campaign.pushSkipped) },
    { label: 'smsSent', value: formatNumber(campaign.smsSent) },
    { label: 'smsFailed', value: formatNumber(campaign.smsFailed), tone: campaign.smsFailed > 0 ? 'danger' : undefined },
    { label: 'opened', value: formatNumber(campaign.openedCount) },
    { label: 'openRate', value: openRate === null ? '—' : `${openRate}%` },
    { label: 'delivered', value: formatNumber(reached) },
  ]

  const audience = audienceChips(campaign, t)

  return (
    <>
      <Link to="/notifications/campaigns" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('backToCampaigns')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="bell" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-brand">{t(lookupKey(CATEGORY_KEY, campaign.category) ?? 'catSystem')}</p>
              <h2 className="break-words text-2xl font-bold leading-tight">{campaign.name}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <MetaBadge record={campaignStatusMeta} value={campaign.status} />
                {campaign.channels.map((channel) => (
                  <Badge key={channel} tone="muted">
                    {t(lookupKey(CHANNEL_KEY, channel) ?? 'channelPush')}
                  </Badge>
                ))}
                {campaign.scheduledAt && (
                  <span>
                    {t('scheduledFor')}: {formatDateTime(campaign.scheduledAt, lang)}
                  </span>
                )}
              </div>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {campaign.status === 'draft' && (
              <>
                <Button variant="secondary" icon="edit" onClick={() => setPending('edit')}>
                  {t('edit')}
                </Button>
                <Button variant="secondary" icon="clock" onClick={() => setPending('schedule')}>
                  {t('schedule')}
                </Button>
                <Button variant="brand" icon="bell" onClick={() => setPending('send')}>
                  {t('sendNow')}
                </Button>
                <Button variant="danger-outline" icon="trash" onClick={() => setPending('delete')}>
                  {t('delete')}
                </Button>
              </>
            )}
            {(campaign.status === 'scheduled' || campaign.status === 'sending') && (
              <Button variant="danger-outline" icon="x" onClick={() => setPending('cancel')}>
                {t('cancelCampaign')}
              </Button>
            )}
            <Button variant="secondary" icon="list" onClick={() => navigate(`/notifications/deliveries?campaignId=${campaign.id}`)}>
              {t('viewDeliveries')}
            </Button>
          </div>
        </div>

        {campaign.targetCount > 0 && (
          <div className="mt-5">
            <div className="mb-1 flex justify-between text-xs font-bold text-muted">
              <span>{t('progress')}</span>
              <span className="ltr-nums">{progress}%</span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-cloud" dir="ltr">
              <div className="h-full rounded-full bg-brand transition-all" style={{ width: `${progress}%` }} />
            </div>
          </div>
        )}
      </Card>

      <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        {stats.map((stat) => (
          <div key={stat.label} className="min-w-0 rounded-2xl bg-white p-4 shadow-soft">
            <p className="truncate text-xs font-bold text-muted">{t(stat.label)}</p>
            <p className={`ltr-nums mt-1 text-xl font-bold ${stat.tone === 'danger' ? 'text-danger' : ''}`}>{stat.value}</p>
          </div>
        ))}
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('content')} className="lg:col-span-2">
          <div className="grid gap-4 sm:grid-cols-2">
            <div dir="rtl" className="rounded-2xl bg-cloud p-4">
              <p className="mb-1 text-xs font-bold text-muted">AR</p>
              <p className="break-words font-bold">{campaign.titleAr || '—'}</p>
              <p className="whitespace-pre-wrap break-words text-sm">{campaign.bodyAr}</p>
            </div>
            <div dir="ltr" className="rounded-2xl bg-cloud p-4">
              <p className="mb-1 text-xs font-bold text-muted">EN</p>
              <p className="break-words font-bold">{campaign.titleEn || '—'}</p>
              <p className="whitespace-pre-wrap break-words text-sm">{campaign.bodyEn}</p>
            </div>
          </div>
          {campaign.deepLink && (
            <p className="mt-4 text-sm text-muted">
              {t('deepLink')}: <span className="ltr-nums font-bold text-ink">{campaign.deepLink}</span>
            </p>
          )}
        </Card>
        <Card title={t('audience')}>
          {audience.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {audience.map((chip) => (
                <Badge key={chip} tone="ink">
                  {chip}
                </Badge>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted">{t('audienceEveryone')}</p>
          )}
          <dl className="mt-5 space-y-2 text-sm">
            {(
              [
                ['createdAt', campaign.createdAt],
                ['startedAt', campaign.startedAt],
                ['finishedAt', campaign.completedAt],
              ] as const
            ).map(([label, value]) => (
              <div key={label} className="flex justify-between gap-3">
                <dt className="text-muted">{t(label)}</dt>
                <dd className="font-bold">{formatDateTime(value, lang)}</dd>
              </div>
            ))}
            {campaign.createdByName && (
              <div className="flex justify-between gap-3">
                <dt className="text-muted">{t('createdBy')}</dt>
                <dd className="font-bold">{campaign.createdByName}</dd>
              </div>
            )}
          </dl>
        </Card>
      </div>

      <CampaignFormModal
        open={pending === 'edit'}
        campaign={campaign}
        onClose={() => setPending(null)}
        onDone={() => {
          toast.success(t('campaignSaved'), campaign.name)
          setPending(null)
          query.reload()
        }}
      />
      <ScheduleModal
        open={pending === 'schedule'}
        onClose={() => setPending(null)}
        onSubmit={async (iso) => {
          await campaigns.schedule(campaign.id, iso)
          toast.success(t('campaignScheduled'), campaign.name)
          setPending(null)
          query.reload()
        }}
      />
      <ConfirmModal
        open={pending === 'send'}
        title={t('sendNowTitle')}
        description={`${campaign.name} — ${t('sendNowCopy')}`}
        confirmLabel={t('sendNow')}
        confirmVariant="brand"
        onClose={() => setPending(null)}
        onConfirm={() => run('send')}
      />
      <ConfirmModal
        open={pending === 'cancel'}
        title={t('cancelCampaign')}
        description={`${campaign.name} — ${t('cancelCampaignCopy')}`}
        confirmLabel={t('cancelCampaign')}
        onClose={() => setPending(null)}
        onConfirm={() => run('cancel')}
      />
      <ConfirmModal
        open={pending === 'delete'}
        title={t('deleteCampaign')}
        description={`${campaign.name} — ${t('deleteCampaignCopy')}`}
        confirmLabel={t('delete')}
        onClose={() => setPending(null)}
        onConfirm={() => run('delete')}
      />
    </>
  )
}

function audienceChips(campaign: Campaign, t: (key: TranslationKey) => string): string[] {
  const audience = campaign.audience ?? {}
  if (audience.userIds?.length) return [`${t('specificUsers')}: ${formatNumber(audience.userIds.length)}`]
  const chips: string[] = []
  for (const role of audience.roles ?? []) chips.push(role === 'driver' ? t('recipientDriver') : t('recipientPassenger'))
  if (audience.cityIds?.length) chips.push(`${t('cityIds')}: ${formatNumber(audience.cityIds.length)}`)
  for (const language of audience.languages ?? []) chips.push(language === 'en' ? 'English' : 'العربية')
  for (const gender of audience.genders ?? []) chips.push(gender === 'female' ? t('genderFemale') : t('genderMale'))
  if (audience.driverTiers?.length) chips.push(`${t('driverTiers')}: ${audience.driverTiers.join(', ')}`)
  if (audience.lastActiveWithinDays) chips.push(`${t('lastActiveWithinDays')}: ${formatNumber(audience.lastActiveWithinDays)}`)
  if (typeof audience.hasCompletedTrip === 'boolean') chips.push(`${t('hasCompletedTrip')}: ${audience.hasCompletedTrip ? t('yes') : t('no')}`)
  return chips
}

function ScheduleModal({ open, ...props }: { open: boolean; onClose: () => void; onSubmit: (iso: string) => Promise<void> }) {
  return open ? <ScheduleDialog {...props} /> : null
}

function ScheduleDialog({ onClose, onSubmit }: { onClose: () => void; onSubmit: (iso: string) => Promise<void> }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [value, setValue] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    const iso = fromLocalInput(value)
    if (!iso || new Date(iso).getTime() <= Date.now()) {
      setError(t('scheduleInFuture'))
      return
    }
    setSubmitting(true)
    try {
      await onSubmit(iso)
    } catch (caught) {
      setError(describe(caught))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={t('schedule')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button icon="clock" onClick={submit} loading={submitting}>
            {t('schedule')}
          </Button>
        </>
      }
    >
      <Input id="schedule-at" type="datetime-local" dir="ltr" label={t('scheduledFor')} value={value} error={error} onChange={(event) => { setValue(event.target.value); setError(null) }} autoFocus />
    </Modal>
  )
}
