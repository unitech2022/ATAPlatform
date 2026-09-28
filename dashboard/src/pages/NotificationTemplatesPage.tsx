import { useMemo, useRef, useState } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput, Select, Textarea, Toggle } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import type { Lang } from '../i18n'
import { notificationEvents, notificationTemplates } from '../lib/admin'
import { isApiError } from '../lib/api'
import { formatDateTime, formatNumber } from '../lib/format'
import { lookupKey } from '../lib/finance'
import { CATEGORY_KEY, CHANNEL_KEY, extractPlaceholders, NOTIFICATION_CHANNELS, RECIPIENT_KEY, renderTemplate, sampleValue, smsInfo } from '../lib/notifications'
import type { NotificationChannel, NotificationEvent, NotificationTemplate } from '../lib/types'

interface EventRow {
  code: string
  event: NotificationEvent | null
  templates: Partial<Record<NotificationChannel, NotificationTemplate>>
}

type Editing = { row: EventRow; channel: NotificationChannel } | null

export function NotificationTemplatesPage() {
  const { t } = useLang()
  const { params, setFilter } = useUrlState()
  const events = useQuery(() => notificationEvents.list(), 'notification-events')
  const templates = useQuery(() => notificationTemplates.list(), 'notification-templates')
  const [editing, setEditing] = useState<Editing>(null)
  const search = params.get('q') ?? ''
  const channelFilter = (NOTIFICATION_CHANNELS as string[]).includes(params.get('channel') ?? '') ? (params.get('channel') as NotificationChannel) : ''
  const category = params.get('category') ?? ''

  const rows = useMemo<EventRow[]>(() => {
    const byCode = new Map<string, EventRow>()
    for (const event of events.data ?? []) byCode.set(event.code, { code: event.code, event, templates: {} })
    for (const template of templates.data ?? []) {
      const row = byCode.get(template.code) ?? { code: template.code, event: null, templates: {} }
      row.templates[template.channel] = template
      byCode.set(template.code, row)
    }
    return [...byCode.values()].sort((a, b) => a.code.localeCompare(b.code))
  }, [events.data, templates.data])

  const categories = [...new Set(rows.map((row) => row.event?.category).filter((value): value is string => Boolean(value)))]
  const filtered = rows.filter((row) => {
    if (search && !row.code.toLowerCase().includes(search.toLowerCase())) return false
    if (category && row.event?.category !== category) return false
    if (channelFilter && !channelsOf(row).includes(channelFilter)) return false
    return true
  })

  const columns: Column<EventRow>[] = [
    {
      key: 'code',
      header: t('eventCode'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold">{row.code}</span>
          {row.event?.deepLink && <span className="ltr-nums block max-w-64 truncate text-xs text-muted">{row.event.deepLink}</span>}
        </span>
      ),
    },
    {
      key: 'category',
      header: t('category'),
      render: (row) => {
        const key = lookupKey(CATEGORY_KEY, row.event?.category)
        return (
          <span className="flex flex-wrap items-center gap-1.5">
            <Badge tone="muted">{key ? t(key) : (row.event?.category ?? '—')}</Badge>
            {row.event?.isCritical && <Badge tone="danger">{t('critical')}</Badge>}
          </span>
        )
      },
    },
    { key: 'recipients', header: t('recipients'), render: (row) => <span className="text-sm">{recipientsLabel(row.event, t)}</span> },
    {
      key: 'channels',
      header: t('channels'),
      render: (row) => (
        <span className="flex flex-wrap gap-1.5">
          {channelsOf(row).map((channel) => {
            const template = row.templates[channel]
            const tone = !template ? 'border-dashed border-line text-muted' : template.isActive ? 'border-brand bg-brand-soft text-brand' : 'border-line bg-cloud text-muted line-through'
            return (
              <button
                key={channel}
                type="button"
                onClick={() => setEditing({ row, channel })}
                title={template ? (template.isActive ? t('templateActive') : t('templateInactive')) : t('templateMissing')}
                className={`inline-flex items-center gap-1 rounded-full border px-3 py-1 text-xs font-bold transition hover:shadow-soft ${tone}`}
              >
                <Icon name={template ? 'edit' : 'plus'} className="size-3" />
                {t(CHANNEL_KEY[channel])}
              </button>
            )
          })}
        </span>
      ),
    },
  ]

  const error = events.error ?? templates.error

  return (
    <>
      <PageHeader title={t('templatesTitle')} description={t('templatesCopy')} />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto]">
        <SearchInput placeholder={t('searchEventCode')} value={search} onChange={(event) => setFilter('q', event.target.value)} wrapperClassName="sm:col-span-2 lg:col-span-1" />
        <Select id="category" aria-label={t('category')} value={category} onChange={(event) => setFilter('category', event.target.value)} wrapperClassName="lg:w-44">
          <option value="">{t('allCategories')}</option>
          {categories.map((value) => {
            const key = lookupKey(CATEGORY_KEY, value)
            return (
              <option key={value} value={value}>
                {key ? t(key) : value}
              </option>
            )
          })}
        </Select>
        <Select id="channel" aria-label={t('channel')} value={channelFilter} onChange={(event) => setFilter('channel', event.target.value)} wrapperClassName="lg:w-40">
          <option value="">{t('allChannels')}</option>
          {NOTIFICATION_CHANNELS.map((value) => (
            <option key={value} value={value}>
              {t(CHANNEL_KEY[value])}
            </option>
          ))}
        </Select>
      </div>

      <Card flush>
        {error ? (
          <ErrorState
            error={error}
            onRetry={() => {
              events.reload()
              templates.reload()
            }}
          />
        ) : (
          <Table columns={columns} rows={filtered} rowKey={(row) => row.code} loading={events.loading || templates.loading} emptyTitle={t('noTemplates')} />
        )}
      </Card>

      {editing && (
        <TemplateEditor
          key={`${editing.row.code}:${editing.channel}`}
          row={editing.row}
          channel={editing.channel}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            templates.reload()
          }}
        />
      )}
    </>
  )
}

function channelsOf(row: EventRow): NotificationChannel[] {
  const allowed = row.event?.allowedChannels ?? []
  const existing = Object.keys(row.templates) as NotificationChannel[]
  return NOTIFICATION_CHANNELS.filter((channel) => allowed.includes(channel) || existing.includes(channel))
}

function recipientsLabel(event: NotificationEvent | null, t: ReturnType<typeof useLang>['t']) {
  if (!event) return '—'
  const list = Array.isArray(event.recipients) ? event.recipients : String(event.recipients).split(/[/,\s]+/).filter(Boolean)
  return list.map((value) => (RECIPIENT_KEY[value] ? t(RECIPIENT_KEY[value]) : value)).join(' · ') || '—'
}

type FieldKey = 'titleAr' | 'titleEn' | 'bodyAr' | 'bodyEn'

interface EditorForm {
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  isActive: boolean
}

function TemplateEditor({ row, channel, onClose, onSaved }: { row: EventRow; channel: NotificationChannel; onClose: () => void; onSaved: () => void }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const template = row.templates[channel] ?? null
  const hasTitle = channel !== 'sms'
  const [form, setForm] = useState<EditorForm>({
    titleAr: template?.titleAr ?? '',
    titleEn: template?.titleEn ?? '',
    bodyAr: template?.bodyAr ?? '',
    bodyEn: template?.bodyEn ?? '',
    isActive: template?.isActive ?? true,
  })
  const [errors, setErrors] = useState<Partial<Record<FieldKey | 'form', string>>>({})
  const [saving, setSaving] = useState(false)
  const [testUserId, setTestUserId] = useState('')
  const [testing, setTesting] = useState(false)
  const refs = useRef<Partial<Record<FieldKey, HTMLInputElement | HTMLTextAreaElement | null>>>({})
  const [focused, setFocused] = useState<FieldKey>('bodyAr')

  const allowed = row.event?.placeholders?.map((value) => value.replace(/^\{|\}$/g, '')) ?? []
  const used = extractPlaceholders([form.titleAr, form.titleEn, form.bodyAr, form.bodyEn].join(' '))
  const names = [...new Set([...allowed, ...used])]
  const unknown = row.event ? used.filter((name) => !allowed.includes(name)) : []
  const [samples, setSamples] = useState<Record<Lang, Record<string, string>>>(() => ({
    ar: Object.fromEntries(names.map((name) => [name, sampleValue(name, 'ar')])),
    en: Object.fromEntries(names.map((name) => [name, sampleValue(name, 'en')])),
  }))
  const sampleFor = (lang: Lang) => Object.fromEntries(names.map((name) => [name, samples[lang][name] ?? sampleValue(name, lang)]))

  const setField = (key: FieldKey, value: string) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined, form: undefined }))
  }

  const insert = (name: string) => {
    const key = !hasTitle && focused.startsWith('title') ? 'bodyAr' : focused
    const element = refs.current[key]
    const token = `{${name}}`
    const value = form[key]
    const start = element?.selectionStart ?? value.length
    const end = element?.selectionEnd ?? value.length
    setField(key, value.slice(0, start) + token + value.slice(end))
    window.requestAnimationFrame(() => {
      element?.focus()
      element?.setSelectionRange(start + token.length, start + token.length)
    })
  }

  const save = async () => {
    const next: typeof errors = {}
    if (!form.bodyAr.trim()) next.bodyAr = t('fieldRequired')
    if (!form.bodyEn.trim()) next.bodyEn = t('fieldRequired')
    if (hasTitle && channel === 'push' && !form.titleEn.trim()) next.titleEn = t('titleEnRequired')
    setErrors(next)
    if (Object.keys(next).length > 0) return
    setSaving(true)
    const input = {
      titleAr: hasTitle ? form.titleAr.trim() || null : null,
      titleEn: hasTitle ? form.titleEn.trim() || null : null,
      bodyAr: form.bodyAr.trim(),
      bodyEn: form.bodyEn.trim(),
      isActive: form.isActive,
    }
    try {
      if (template) await notificationTemplates.update(template.id, input)
      else await notificationTemplates.create({ code: row.code, channel, ...input })
      toast.success(t('templateSaved'))
      onSaved()
    } catch (error) {
      if (isApiError(error) && error.code === 'template_placeholder_invalid') {
        const list = error.details?.unknownPlaceholders
        setErrors({ form: `${t('errPlaceholderInvalid')}${Array.isArray(list) ? `: ${list.map(String).join(', ')}` : ''}` })
      } else {
        setErrors({ form: describe(error) })
      }
    } finally {
      setSaving(false)
    }
  }

  const sendTest = async () => {
    if (!template || !testUserId.trim()) return
    setTesting(true)
    try {
      await notificationTemplates.test(template.id, testUserId.trim())
      toast.success(t('testSent'))
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setTesting(false)
    }
  }

  const field = (key: FieldKey, fieldLang: Lang) => {
    const isTitle = key.startsWith('title')
    const common = {
      id: `tpl-${key}`,
      dir: fieldLang === 'ar' ? 'rtl' : 'ltr',
      value: form[key],
      error: errors[key],
      onFocus: () => setFocused(key),
    } as const
    if (isTitle) {
      return (
        <Input
          {...common}
          ref={(element: HTMLInputElement | null) => {
            refs.current[key] = element
          }}
          label={fieldLang === 'ar' ? t('titleAr') : t('titleEn')}
          maxLength={120}
          onChange={(event) => setField(key, event.target.value)}
        />
      )
    }
    const info = smsInfo(form[key])
    return (
      <Textarea
        {...common}
        ref={(element: HTMLTextAreaElement | null) => {
          refs.current[key] = element
        }}
        label={fieldLang === 'ar' ? t('bodyAr') : t('bodyEn')}
        maxLength={1000}
        hint={
          <span className="ltr-nums">
            {formatNumber(info.length)} / 1000
            {channel === 'sms' ? ` · ${formatNumber(info.segments)} ${t('smsSegments')}${info.unicode ? ' (UCS-2)' : ''}` : ''}
          </span>
        }
        onChange={(event) => setField(key, event.target.value)}
      />
    )
  }

  return (
    <Modal
      open
      size="xl"
      title={
        <span className="flex flex-wrap items-center gap-2">
          <span className="ltr-nums">{row.code}</span>
          <Badge tone="ink">{t(CHANNEL_KEY[channel])}</Badge>
          {!template && <Badge tone="warning">{t('newTemplate')}</Badge>}
        </span>
      }
      description={template?.updatedAt ? `${t('lastUpdated')}: ${formatDateTime(template.updatedAt, lang)}${template.updatedByName ? ` · ${template.updatedByName}` : ''}` : t('templateEditorCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button icon="check" onClick={save} loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <div className="space-y-5">
        {names.length > 0 && (
          <div>
            <p className="mb-2 text-sm font-bold">{t('placeholders')}</p>
            <p className="mb-2 text-xs text-muted">{t('placeholdersHint')}</p>
            <div className="flex flex-wrap gap-1.5">
              {names.map((name) => (
                <button
                  key={name}
                  type="button"
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => insert(name)}
                  className={`ltr-nums rounded-full border px-3 py-1 text-xs font-bold transition hover:bg-brand-soft ${unknown.includes(name) ? 'border-danger text-danger' : 'border-brand/40 text-brand'}`}
                >
                  {`{${name}}`}
                </button>
              ))}
            </div>
          </div>
        )}

        {unknown.length > 0 && (
          <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>
              {t('errPlaceholderInvalid')}: <span className="ltr-nums font-bold">{unknown.map((name) => `{${name}}`).join(' ')}</span>
            </p>
          </div>
        )}

        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-4">
            {hasTitle && field('titleAr', 'ar')}
            {field('bodyAr', 'ar')}
          </div>
          <div className="space-y-4">
            {hasTitle && field('titleEn', 'en')}
            {field('bodyEn', 'en')}
          </div>
        </div>

        <Toggle checked={form.isActive} onChange={(value) => setForm((current) => ({ ...current, isActive: value }))} label={t('templateActive')} description={t('templateActiveCopy')} />

        <div>
          <p className="mb-2 text-sm font-bold">{t('livePreview')}</p>
          <div className="grid gap-4 lg:grid-cols-2">
            <PreviewBubble channel={channel} lang="ar" title={renderTemplate(form.titleAr, sampleFor('ar'))} body={renderTemplate(form.bodyAr, sampleFor('ar'))} />
            <PreviewBubble channel={channel} lang="en" title={renderTemplate(form.titleEn, sampleFor('en'))} body={renderTemplate(form.bodyEn, sampleFor('en'))} />
          </div>
        </div>

        {names.length > 0 && (
          <details className="rounded-2xl border border-line p-4">
            <summary className="cursor-pointer text-sm font-bold">{t('sampleData')}</summary>
            <div className="mt-4 grid gap-3 sm:grid-cols-2">
              {names.map((name) => (
                <div key={name} className="grid grid-cols-2 gap-2">
                  <Input
                    id={`sample-ar-${name}`}
                    aria-label={`${name} (ar)`}
                    label={<span className="ltr-nums text-xs">{`{${name}}`} · AR</span>}
                    value={samples.ar[name] ?? ''}
                    onChange={(event) => setSamples((current) => ({ ...current, ar: { ...current.ar, [name]: event.target.value } }))}
                  />
                  <Input
                    id={`sample-en-${name}`}
                    aria-label={`${name} (en)`}
                    dir="ltr"
                    label={<span className="ltr-nums text-xs">EN</span>}
                    value={samples.en[name] ?? ''}
                    onChange={(event) => setSamples((current) => ({ ...current, en: { ...current.en, [name]: event.target.value } }))}
                  />
                </div>
              ))}
            </div>
          </details>
        )}

        {template && (
          <div className="rounded-2xl border border-line p-4">
            <p className="text-sm font-bold">{t('sendTest')}</p>
            <p className="mb-3 text-xs text-muted">{t('sendTestCopy')}</p>
            <div className="flex flex-col gap-2 sm:flex-row sm:items-start">
              <Input id="test-user" dir="ltr" aria-label={t('userId')} placeholder={t('userId')} value={testUserId} onChange={(event) => setTestUserId(event.target.value)} wrapperClassName="flex-1" />
              <Button variant="secondary" icon="bell" loading={testing} disabled={!testUserId.trim()} onClick={sendTest} className="sm:mt-0.5">
                {t('sendTest')}
              </Button>
            </div>
          </div>
        )}

        {errors.form && (
          <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>{errors.form}</p>
          </div>
        )}
      </div>
    </Modal>
  )
}

/** Device-like preview for push (notification card), sms (bubble) and inapp (inbox row). */
function PreviewBubble({ channel, lang, title, body }: { channel: NotificationChannel; lang: Lang; title: string; body: string }) {
  const { t } = useLang()
  const dir = lang === 'ar' ? 'rtl' : 'ltr'
  const empty = <span className="text-muted">{t('previewEmpty')}</span>
  if (channel === 'sms') {
    return (
      <div dir={dir} className="rounded-3xl bg-cloud p-4">
        <p className="mb-2 text-xs font-bold text-muted">ATA · SMS · {lang.toUpperCase()}</p>
        <p className="max-w-[85%] whitespace-pre-wrap break-words rounded-2xl rounded-ss-sm bg-white px-4 py-3 text-sm shadow-soft">{body || empty}</p>
      </div>
    )
  }
  if (channel === 'inapp') {
    return (
      <div dir={dir} className="flex items-start gap-3 rounded-3xl border border-line bg-white p-4">
        <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand">
          <Icon name="bell" className="size-5" />
        </span>
        <span className="min-w-0">
          <span className="block break-words font-bold">{title || empty}</span>
          <span className="block whitespace-pre-wrap break-words text-sm text-muted">{body || empty}</span>
          <span className="mt-1 block text-xs text-muted">{lang.toUpperCase()}</span>
        </span>
      </div>
    )
  }
  return (
    <div dir={dir} className="rounded-3xl bg-ink p-3">
      <div className="rounded-2xl bg-white/95 p-3">
        <p className="mb-1 flex items-center gap-2 text-[11px] font-bold text-muted">
          <span className="grid size-4 place-items-center rounded bg-brand text-[9px] text-white">A</span>
          ATA · {lang.toUpperCase()}
        </p>
        <p className="break-words text-sm font-bold">{title || empty}</p>
        <p className="whitespace-pre-wrap break-words text-sm">{body || empty}</p>
      </div>
    </div>
  )
}
