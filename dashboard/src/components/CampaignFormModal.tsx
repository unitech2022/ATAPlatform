import { useState, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { campaigns, notificationTemplates } from '../lib/admin'
import { formatNumber } from '../lib/format'
import { CAMPAIGN_CATEGORIES, CAMPAIGN_CHANNELS, CATEGORY_KEY, CHANNEL_KEY, cleanAudience, splitList } from '../lib/notifications'
import { fromLocalInput, toLocalInput } from '../lib/pricing'
import type { AudiencePreview, Campaign, CampaignAudience, CampaignCategory, CampaignChannel, CampaignInput } from '../lib/types'
import { Button } from './Button'
import { ChipGroup } from './ChipGroup'
import { ConfirmModal } from './ConfirmModal'
import { Input, Select, Textarea } from './Field'
import { Icon } from './Icon'
import { Modal } from './Modal'

type Delivery = 'draft' | 'schedule' | 'now'

interface FormState {
  name: string
  category: CampaignCategory
  channels: CampaignChannel[]
  roles: ('passenger' | 'driver')[]
  cityIds: string
  languages: ('ar' | 'en')[]
  genders: ('male' | 'female')[]
  driverTiers: string
  lastActiveWithinDays: string
  hasCompletedTrip: '' | 'yes' | 'no'
  userIds: string
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  deepLink: string
  delivery: Delivery
  scheduledAt: string
}

type Errors = Partial<Record<keyof FormState | 'form', string>>

function toForm(campaign: Campaign | null): FormState {
  const audience = campaign?.audience ?? {}
  return {
    name: campaign?.name ?? '',
    category: campaign?.category ?? 'promotions',
    channels: campaign?.channels ?? ['inapp', 'push'],
    roles: audience.roles ?? ['passenger'],
    cityIds: (audience.cityIds ?? []).join(', '),
    languages: audience.languages ?? [],
    genders: audience.genders ?? [],
    driverTiers: (audience.driverTiers ?? []).join(', '),
    lastActiveWithinDays: audience.lastActiveWithinDays ? String(audience.lastActiveWithinDays) : '',
    hasCompletedTrip: audience.hasCompletedTrip === true ? 'yes' : audience.hasCompletedTrip === false ? 'no' : '',
    userIds: (audience.userIds ?? []).join('\n'),
    titleAr: campaign?.titleAr ?? '',
    titleEn: campaign?.titleEn ?? '',
    bodyAr: campaign?.bodyAr ?? '',
    bodyEn: campaign?.bodyEn ?? '',
    deepLink: campaign?.deepLink ?? '',
    delivery: campaign?.scheduledAt ? 'schedule' : 'draft',
    scheduledAt: toLocalInput(campaign?.scheduledAt),
  }
}

function toAudience(form: FormState): CampaignAudience {
  const days = Number(form.lastActiveWithinDays)
  return cleanAudience({
    roles: form.roles,
    cityIds: splitList(form.cityIds),
    languages: form.languages,
    genders: form.genders,
    driverTiers: splitList(form.driverTiers),
    lastActiveWithinDays: Number.isFinite(days) && days > 0 ? Math.round(days) : undefined,
    hasCompletedTrip: form.hasCompletedTrip === '' ? undefined : form.hasCompletedTrip === 'yes',
    userIds: splitList(form.userIds),
  })
}

function toInput(form: FormState): CampaignInput {
  return {
    name: form.name.trim(),
    category: form.category,
    channels: form.channels,
    audience: toAudience(form),
    titleAr: form.titleAr.trim(),
    titleEn: form.titleEn.trim(),
    bodyAr: form.bodyAr.trim(),
    bodyEn: form.bodyEn.trim(),
    deepLink: form.deepLink.trim() || null,
  }
}

export interface CampaignFormModalProps {
  open: boolean
  /** Existing draft to edit; null creates a new campaign. */
  campaign: Campaign | null
  onClose: () => void
  onDone: (campaign: Campaign) => void
}

export function CampaignFormModal({ open, ...props }: CampaignFormModalProps) {
  return open ? <CampaignDialog {...props} /> : null
}

function CampaignDialog({ campaign, onClose, onDone }: Omit<CampaignFormModalProps, 'open'>) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(() => toForm(campaign))
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)
  const [confirmSend, setConfirmSend] = useState(false)
  // Remembers a campaign created by an earlier attempt whose schedule/send step failed, so a retry updates it.
  const [savedId, setSavedId] = useState<string | null>(campaign?.id ?? null)
  const [preview, setPreview] = useState<AudiencePreview | null>(null)
  const [previewing, setPreviewing] = useState(false)
  const templates = useQuery(() => notificationTemplates.list({ isActive: true }), 'campaign-templates')
  const usable = (templates.data ?? []).filter((template) => template.channel !== 'sms' || form.channels.includes('sms'))

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined, form: undefined }))
  }
  const setAudience = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    set(key, value)
    setPreview(null)
  }

  const applyTemplate = (id: string) => {
    const template = templates.data?.find((item) => item.id === id)
    if (!template) return
    setForm((current) => ({
      ...current,
      titleAr: template.titleAr ?? current.titleAr,
      titleEn: template.titleEn ?? current.titleEn,
      bodyAr: template.bodyAr,
      bodyEn: template.bodyEn,
    }))
  }

  const runPreview = async () => {
    setPreviewing(true)
    try {
      setPreview(await campaigns.audiencePreview(toAudience(form)))
    } catch (error) {
      setErrors((current) => ({ ...current, form: describe(error) }))
    } finally {
      setPreviewing(false)
    }
  }

  const validate = () => {
    const next: Errors = {}
    if (!form.name.trim()) next.name = t('fieldRequired')
    if (form.channels.length === 0) next.channels = t('channelsRequired')
    const needsTitle = form.channels.includes('push') || form.channels.includes('inapp')
    if (needsTitle && !form.titleAr.trim()) next.titleAr = t('fieldRequired')
    if (needsTitle && !form.titleEn.trim()) next.titleEn = t('fieldRequired')
    if (!form.bodyAr.trim()) next.bodyAr = t('fieldRequired')
    if (!form.bodyEn.trim()) next.bodyEn = t('fieldRequired')
    if (form.delivery === 'schedule') {
      const iso = fromLocalInput(form.scheduledAt)
      if (!iso || new Date(iso).getTime() <= Date.now()) next.scheduledAt = t('scheduleInFuture')
    }
    setErrors(next)
    return Object.keys(next).length === 0
  }

  const submit = async (confirmed = false) => {
    if (!validate()) return
    if (form.delivery === 'now' && !confirmed) {
      setConfirmSend(true)
      return
    }
    setSaving(true)
    try {
      const input = toInput(form)
      let saved = savedId ? await campaigns.update(savedId, input) : await campaigns.create(input)
      setSavedId(saved.id)
      if (form.delivery === 'schedule') saved = await campaigns.schedule(saved.id, fromLocalInput(form.scheduledAt) ?? '')
      else if (form.delivery === 'now') saved = await campaigns.sendNow(saved.id)
      onDone(saved)
    } catch (error) {
      setErrors({ form: describe(error) })
    } finally {
      setSaving(false)
      setConfirmSend(false)
    }
  }

  const submitLabel = form.delivery === 'now' ? t('sendNow') : form.delivery === 'schedule' ? t('schedule') : t('saveDraft')
  const byUsers = splitList(form.userIds).length > 0

  return (
    <Modal
      open
      size="xl"
      title={campaign ? t('editCampaign') : t('newCampaign')}
      description={t('campaignFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button variant={form.delivery === 'now' ? 'brand' : 'primary'} icon={form.delivery === 'now' ? 'bell' : form.delivery === 'schedule' ? 'clock' : 'check'} onClick={() => submit()} loading={saving}>
            {submitLabel}
          </Button>
        </>
      }
    >
      <div className="space-y-6">
        <Section title={t('campaignBasics')}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="campaign-name" label={t('name')} value={form.name} error={errors.name} maxLength={120} onChange={(event) => set('name', event.target.value)} autoFocus />
            <Select id="campaign-category" label={t('category')} value={form.category} onChange={(event) => set('category', event.target.value as CampaignCategory)}>
              {CAMPAIGN_CATEGORIES.map((value) => (
                <option key={value} value={value}>
                  {t(CATEGORY_KEY[value])}
                </option>
              ))}
            </Select>
          </div>
          <div className="mt-4">
            <p className="mb-2 text-sm font-bold">{t('channels')}</p>
            <ChipGroup
              options={CAMPAIGN_CHANNELS.map((value) => ({ value, label: t(CHANNEL_KEY[value]) }))}
              value={form.channels}
              onChange={(value) => set('channels', value)}
            />
            {errors.channels ? <p className="mt-1 text-xs font-bold text-danger">{errors.channels}</p> : form.channels.includes('sms') ? <p className="mt-1 text-xs text-muted">{t('smsPermissionHint')}</p> : null}
            {form.category === 'promotions' && <p className="mt-1 text-xs text-muted">{t('promotionsPreferenceHint')}</p>}
          </div>
        </Section>

        <Section title={t('audience')} description={t('audienceCopy')}>
          <div className={`grid gap-4 sm:grid-cols-2 ${byUsers ? 'opacity-50' : ''}`}>
            <div>
              <p className="mb-2 text-sm font-bold">{t('roles')}</p>
              <ChipGroup
                options={[
                  { value: 'passenger' as const, label: t('recipientPassenger') },
                  { value: 'driver' as const, label: t('recipientDriver') },
                ]}
                value={form.roles}
                onChange={(value) => setAudience('roles', value)}
              />
            </div>
            <div>
              <p className="mb-2 text-sm font-bold">{t('language')}</p>
              <ChipGroup
                options={[
                  { value: 'ar' as const, label: 'العربية' },
                  { value: 'en' as const, label: 'English' },
                ]}
                value={form.languages}
                onChange={(value) => setAudience('languages', value)}
              />
            </div>
            <div>
              <p className="mb-2 text-sm font-bold">{t('gender')}</p>
              <ChipGroup
                options={[
                  { value: 'male' as const, label: t('genderMale') },
                  { value: 'female' as const, label: t('genderFemale') },
                ]}
                value={form.genders}
                onChange={(value) => setAudience('genders', value)}
              />
            </div>
            <Select
              id="has-trip"
              label={t('hasCompletedTrip')}
              value={form.hasCompletedTrip}
              onChange={(event) => setAudience('hasCompletedTrip', event.target.value as FormState['hasCompletedTrip'])}
            >
              <option value="">{t('any')}</option>
              <option value="yes">{t('yes')}</option>
              <option value="no">{t('no')}</option>
            </Select>
            <Input id="city-ids" dir="ltr" label={t('cityIds')} hint={t('listHint')} value={form.cityIds} onChange={(event) => setAudience('cityIds', event.target.value)} />
            <Input id="driver-tiers" dir="ltr" label={t('driverTiers')} hint={t('listHint')} placeholder="gold, platinum" value={form.driverTiers} onChange={(event) => setAudience('driverTiers', event.target.value)} />
            <Input
              id="last-active"
              type="number"
              min="1"
              dir="ltr"
              label={t('lastActiveWithinDays')}
              value={form.lastActiveWithinDays}
              onChange={(event) => setAudience('lastActiveWithinDays', event.target.value)}
            />
          </div>
          <Textarea
            id="user-ids"
            dir="ltr"
            wrapperClassName="mt-4"
            className="min-h-20"
            label={t('userIdsOverride')}
            hint={t('userIdsOverrideHint')}
            value={form.userIds}
            onChange={(event) => setAudience('userIds', event.target.value)}
          />
          <div className="mt-4 flex flex-col gap-3 rounded-2xl bg-cloud p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="text-sm">
              {preview ? (
                <>
                  <p className="font-bold">
                    {t('audienceCount')}: <span className="ltr-nums">{formatNumber(preview.count)}</span>
                  </p>
                  {preview.sample.length > 0 && (
                    <p className="mt-1 text-xs text-muted">
                      {preview.sample
                        .slice(0, 5)
                        .map((user) => `${user.name ?? t('unnamed')}${user.phoneMasked ? ` (${user.phoneMasked})` : ''}`)
                        .join(' · ')}
                    </p>
                  )}
                </>
              ) : (
                <p className="text-muted">{t('audiencePreviewHint')}</p>
              )}
            </div>
            <Button variant="secondary" size="sm" icon="users" loading={previewing} onClick={runPreview}>
              {t('previewCount')}
            </Button>
          </div>
        </Section>

        <Section title={t('content')}>
          <Select id="template" label={t('startFromTemplate')} value="" onChange={(event) => applyTemplate(event.target.value)} wrapperClassName="mb-4">
            <option value="">{t('customText')}</option>
            {usable.map((template) => (
              <option key={template.id} value={template.id}>
                {template.code} · {t(CHANNEL_KEY[template.channel])}
              </option>
            ))}
          </Select>
          <div className="grid gap-4 lg:grid-cols-2">
            <div className="space-y-4">
              <Input id="title-ar" label={t('titleAr')} value={form.titleAr} error={errors.titleAr} maxLength={120} onChange={(event) => set('titleAr', event.target.value)} />
              <Textarea id="body-ar" label={t('bodyAr')} value={form.bodyAr} error={errors.bodyAr} maxLength={1000} onChange={(event) => set('bodyAr', event.target.value)} />
            </div>
            <div className="space-y-4">
              <Input id="title-en" dir="ltr" label={t('titleEn')} value={form.titleEn} error={errors.titleEn} maxLength={120} onChange={(event) => set('titleEn', event.target.value)} />
              <Textarea id="body-en" dir="ltr" label={t('bodyEn')} value={form.bodyEn} error={errors.bodyEn} maxLength={1000} onChange={(event) => set('bodyEn', event.target.value)} />
            </div>
          </div>
          <Input id="deep-link" dir="ltr" wrapperClassName="mt-4" label={t('deepLinkOptional')} placeholder="ata://promotions" value={form.deepLink} onChange={(event) => set('deepLink', event.target.value)} />
        </Section>

        <Section title={t('deliveryTiming')}>
          <div className="grid gap-2 sm:grid-cols-3" role="radiogroup" aria-label={t('deliveryTiming')}>
            {(
              [
                { value: 'draft', label: t('saveDraft'), icon: 'document' },
                { value: 'schedule', label: t('schedule'), icon: 'clock' },
                { value: 'now', label: t('sendNow'), icon: 'bell' },
              ] as const
            ).map((option) => (
              <button
                key={option.value}
                type="button"
                role="radio"
                aria-checked={form.delivery === option.value}
                onClick={() => set('delivery', option.value)}
                className={`flex items-center gap-2 rounded-2xl border-2 px-4 py-3 text-sm font-bold transition ${form.delivery === option.value ? 'border-brand bg-brand-soft text-ink' : 'border-line text-muted hover:bg-cloud'}`}
              >
                <Icon name={option.icon} className="size-4" />
                {option.label}
              </button>
            ))}
          </div>
          {form.delivery === 'schedule' && (
            <Input
              id="scheduled-at"
              type="datetime-local"
              dir="ltr"
              wrapperClassName="mt-4 sm:max-w-xs"
              label={t('scheduledFor')}
              value={form.scheduledAt}
              error={errors.scheduledAt}
              onChange={(event) => set('scheduledAt', event.target.value)}
            />
          )}
        </Section>

        {errors.form && (
          <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>{errors.form}</p>
          </div>
        )}
      </div>

      <ConfirmModal
        open={confirmSend}
        title={t('sendNowTitle')}
        description={`${form.name} — ${preview ? `${formatNumber(preview.count)} · ` : ''}${t('sendNowCopy')}`}
        confirmLabel={t('sendNow')}
        confirmVariant="brand"
        onClose={() => setConfirmSend(false)}
        onConfirm={() => submit(true)}
      />
    </Modal>
  )
}

function Section({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <section>
      <h3 className="font-bold">{title}</h3>
      {description && <p className="mb-3 text-xs text-muted">{description}</p>}
      <div className={description ? '' : 'mt-3'}>{children}</div>
    </section>
  )
}
