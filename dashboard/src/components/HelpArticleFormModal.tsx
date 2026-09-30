import { useRef, useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { Lang } from '../i18n'
import { helpArticles } from '../lib/admin'
import { formatNumber } from '../lib/format'
import { formatRatio } from '../lib/rewards'
import { AUDIENCE_KEY, helpfulRate, HELP_AUDIENCES, parseTags, SLUG_PATTERN, slugify } from '../lib/support'
import type { HelpArticle, HelpArticleInput, HelpAudience, HelpCategory } from '../lib/types'
import { Button } from './Button'
import { ErrorState } from './ErrorState'
import { Input, Select, Textarea, Toggle } from './Field'
import { HelpMarkdown } from './HelpMarkdown'
import { Modal } from './Modal'
import { PageSpinner } from './Spinner'

export type ArticleTarget = { mode: 'new' } | { mode: 'edit'; id: string }

type FormState = {
  categoryId: string
  slug: string
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  audience: HelpAudience
  tags: string
  sortOrder: string
  isPublished: boolean
}
type Errors = Partial<Record<'categoryId' | 'slug' | 'titleAr' | 'titleEn' | 'bodyAr' | 'bodyEn' | 'sortOrder', string>>

const EMPTY: FormState = { categoryId: '', slug: '', titleAr: '', titleEn: '', bodyAr: '', bodyEn: '', audience: 'all', tags: '', sortOrder: '10', isPublished: false }

/** Markdown snippets of the editor toolbar: `before`/`after` wrap the selection (or a placeholder when nothing is selected). */
const TOOLS: { id: string; label: string; before: string; after: string; placeholder: string }[] = [
  { id: 'bold', label: 'B', before: '**', after: '**', placeholder: 'text' },
  { id: 'italic', label: 'I', before: '_', after: '_', placeholder: 'text' },
  { id: 'heading', label: 'H2', before: '\n## ', after: '\n', placeholder: 'Heading' },
  { id: 'list', label: '•', before: '\n- ', after: '\n', placeholder: 'item' },
  { id: 'quote', label: '❝', before: '\n> ', after: '\n', placeholder: 'quote' },
  { id: 'link', label: '🔗', before: '[', after: '](https://)', placeholder: 'link' },
]

function fromArticle(article: HelpArticle): FormState {
  return {
    categoryId: article.categoryId,
    slug: article.slug,
    titleAr: article.titleAr,
    titleEn: article.titleEn,
    bodyAr: article.bodyAr,
    bodyEn: article.bodyEn,
    audience: article.audience,
    tags: (article.tags ?? []).join(', '),
    sortOrder: String(article.sortOrder),
    isPublished: article.isPublished,
  }
}

/**
 * Help-article editor (`/admin/help/articles`): Arabic / English title and Markdown body with a live sanitized preview, slug, category,
 * audience, tags, order and the publish toggle (saved through `/publish` · `/unpublish`), plus the view / helpful statistics.
 */
export function HelpArticleFormModal({ target, categories, onClose, onSaved }: { target: ArticleTarget | null; categories: HelpCategory[]; onClose: () => void; onSaved: () => void }) {
  return target ? <ArticleLoader key={target.mode === 'new' ? 'new' : target.id} target={target} categories={categories} onClose={onClose} onSaved={onSaved} /> : null
}

function ArticleLoader({ target, categories, onClose, onSaved }: { target: ArticleTarget; categories: HelpCategory[]; onClose: () => void; onSaved: () => void }) {
  const { t } = useLang()
  const id = target.mode === 'edit' ? target.id : ''
  const query = useQuery(async () => (id ? await helpArticles.get(id) : null), `help-article:${id}`)

  if (id && query.data === null) {
    return (
      <Modal open title={t('hcArticleEdit')} onClose={onClose}>
        {query.error ? <ErrorState error={query.error} onRetry={query.reload} /> : <PageSpinner />}
      </Modal>
    )
  }
  return <ArticleForm article={query.data} categories={categories} onClose={onClose} onSaved={onSaved} />
}

function ArticleForm({ article, categories, onClose, onSaved }: { article: HelpArticle | null; categories: HelpCategory[]; onClose: () => void; onSaved: () => void }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<FormState>(article ? fromArticle(article) : { ...EMPTY, categoryId: categories[0]?.id ?? '' })
  const [slugTouched, setSlugTouched] = useState(Boolean(article))
  const [editLang, setEditLang] = useState<Lang>(lang)
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)
  const bodyRef = useRef<HTMLTextAreaElement>(null)

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors((current) => ({ ...current, [key]: undefined }))
  }

  const setTitleEn = (value: string) => {
    setForm((current) => ({ ...current, titleEn: value, slug: slugTouched ? current.slug : slugify(value) }))
    setErrors((current) => ({ ...current, titleEn: undefined, slug: slugTouched ? current.slug : undefined }))
  }

  const bodyKey = editLang === 'ar' ? 'bodyAr' : 'bodyEn'
  const titleKey = editLang === 'ar' ? 'titleAr' : 'titleEn'

  const applyTool = (tool: (typeof TOOLS)[number]) => {
    const element = bodyRef.current
    const text = form[bodyKey]
    const start = element?.selectionStart ?? text.length
    const end = element?.selectionEnd ?? text.length
    const selected = text.slice(start, end) || tool.placeholder
    set(bodyKey, `${text.slice(0, start)}${tool.before}${selected}${tool.after}${text.slice(end)}`)
    requestAnimationFrame(() => {
      element?.focus()
      const from = start + tool.before.length
      element?.setSelectionRange(from, from + selected.length)
    })
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const next: Errors = {}
    if (!form.categoryId) next.categoryId = t('fieldRequired')
    const slug = form.slug.trim()
    if (!slug || slug.length > 120 || !SLUG_PATTERN.test(slug)) next.slug = t('hcErrSlug')
    if (!form.titleAr.trim()) next.titleAr = t('fieldRequired')
    if (!form.titleEn.trim()) next.titleEn = t('fieldRequired')
    if (!form.bodyAr.trim()) next.bodyAr = t('fieldRequired')
    if (!form.bodyEn.trim()) next.bodyEn = t('fieldRequired')
    if (form.sortOrder.trim() === '' || !Number.isInteger(Number(form.sortOrder))) next.sortOrder = t('invalidNumber')
    setErrors(next)
    if (Object.keys(next).length > 0) {
      // Jump to the language tab that holds the first missing field.
      if (next.titleAr || next.bodyAr) setEditLang('ar')
      else if (next.titleEn || next.bodyEn) setEditLang('en')
      return
    }

    const input: HelpArticleInput = {
      categoryId: form.categoryId,
      slug,
      titleAr: form.titleAr.trim(),
      titleEn: form.titleEn.trim(),
      bodyAr: form.bodyAr,
      bodyEn: form.bodyEn,
      audience: form.audience,
      tags: parseTags(form.tags),
      sortOrder: Number(form.sortOrder),
    }
    setSaving(true)
    try {
      const saved = article ? await helpArticles.update(article.id, input) : await helpArticles.create(input)
      const id = saved?.id ?? article?.id
      const wasPublished = article?.isPublished ?? false
      if (id && form.isPublished !== wasPublished) {
        if (form.isPublished) await helpArticles.publish(id)
        else await helpArticles.unpublish(id)
      }
      toast.success(t('hcArticleSaved'))
      onSaved()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const rate = article ? helpfulRate(article.helpfulYes, article.helpfulNo) : null

  return (
    <Modal
      open
      size="xl"
      title={article ? t('hcArticleEdit') : t('hcArticleNew')}
      description={t('hcArticleFormCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {t('cancel')}
          </Button>
          <Button type="submit" form="help-article-form" loading={saving}>
            {t('save')}
          </Button>
        </>
      }
    >
      <form id="help-article-form" onSubmit={save} noValidate className="space-y-4">
        {article && (
          <dl className="grid grid-cols-3 gap-3 text-center text-sm" data-testid="article-stats">
            <div className="rounded-2xl bg-cloud px-3 py-2">
              <dt className="text-xs font-bold text-muted">{t('hcViews')}</dt>
              <dd className="ltr-nums font-bold">{formatNumber(article.viewCount)}</dd>
            </div>
            <div className="rounded-2xl bg-cloud px-3 py-2">
              <dt className="text-xs font-bold text-muted">{t('hcHelpful')}</dt>
              <dd className="ltr-nums font-bold">
                {formatNumber(article.helpfulYes)} / {formatNumber(article.helpfulNo)}
              </dd>
            </div>
            <div className="rounded-2xl bg-cloud px-3 py-2">
              <dt className="text-xs font-bold text-muted">{t('hcHelpfulRate')}</dt>
              <dd className="ltr-nums font-bold">{rate === null ? '—' : formatRatio(rate)}</dd>
            </div>
          </dl>
        )}

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Select id="ha-category" label={t('category')} value={form.categoryId} error={errors.categoryId} onChange={(event) => set('categoryId', event.target.value)}>
            <option value="" disabled>
              {t('hcSelectCategory')}
            </option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {lang === 'ar' ? category.nameAr : category.nameEn}
              </option>
            ))}
          </Select>
          <Select id="ha-audience" label={t('hcAudience')} value={form.audience} onChange={(event) => set('audience', event.target.value as HelpAudience)}>
            {HELP_AUDIENCES.map((value) => (
              <option key={value} value={value}>
                {t(AUDIENCE_KEY[value])}
              </option>
            ))}
          </Select>
          <Input id="ha-sort" type="number" min={0} label={t('sortOrder')} dir="ltr" value={form.sortOrder} error={errors.sortOrder} onChange={(event) => set('sortOrder', event.target.value)} />
          <Input
            id="ha-slug"
            label={t('hcSlug')}
            hint={t('hcSlugHint')}
            dir="ltr"
            value={form.slug}
            error={errors.slug}
            maxLength={120}
            onChange={(event) => {
              setSlugTouched(true)
              set('slug', event.target.value)
            }}
            wrapperClassName="lg:col-span-2"
          />
          <Input id="ha-tags" label={t('hcTags')} hint={t('hcTagsHint')} value={form.tags} onChange={(event) => set('tags', event.target.value)} />
        </div>

        <Toggle checked={form.isPublished} onChange={(value) => set('isPublished', value)} label={t('hcPublished')} description={t('hcPublishedCopy')} />

        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex gap-1 rounded-2xl bg-cloud p-1" role="group" aria-label={t('hcEditLanguage')}>
            {(['ar', 'en'] as const).map((value) => (
              <button
                key={value}
                type="button"
                aria-pressed={editLang === value}
                onClick={() => setEditLang(value)}
                className={`rounded-xl px-3.5 py-1.5 text-sm font-bold transition ${editLang === value ? 'bg-ink text-white' : 'text-muted hover:bg-line'}`}
              >
                {value === 'ar' ? 'العربية' : 'English'}
                {(value === 'ar' ? errors.titleAr || errors.bodyAr : errors.titleEn || errors.bodyEn) ? ' •' : ''}
              </button>
            ))}
          </div>
          <div className="flex flex-wrap gap-1" role="toolbar" aria-label={t('hcToolbar')}>
            {TOOLS.map((tool) => (
              <button
                key={tool.id}
                type="button"
                onClick={() => applyTool(tool)}
                aria-label={t('hcToolbar')}
                title={tool.id}
                className="grid size-9 place-items-center rounded-xl bg-cloud text-sm font-bold text-ink transition hover:bg-line"
              >
                {tool.label}
              </button>
            ))}
          </div>
        </div>

        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-3">
            <Input
              id="ha-title"
             
              label={editLang === 'ar' ? t('hcTitleAr') : t('hcTitleEn')}
              dir={editLang === 'ar' ? 'rtl' : 'ltr'}
              value={form[titleKey]}
              error={errors[titleKey]}
              onChange={(event) => (editLang === 'en' ? setTitleEn(event.target.value) : set('titleAr', event.target.value))}
            />
            <Textarea
              id="ha-body"
             
              ref={bodyRef}
              label={editLang === 'ar' ? t('hcBodyAr') : t('hcBodyEn')}
              hint={t('hcMarkdownHint')}
              dir={editLang === 'ar' ? 'rtl' : 'ltr'}
              className="min-h-72 font-mono text-sm"
              value={form[bodyKey]}
              error={errors[bodyKey]}
              onChange={(event) => set(bodyKey, event.target.value)}
            />
          </div>
          <div className="min-w-0 rounded-2xl border border-line p-4" data-testid="article-preview">
            <p className="mb-3 text-xs font-bold text-muted">{t('hcPreview')}</p>
            <h3 dir={editLang === 'ar' ? 'rtl' : 'ltr'} className="mb-3 break-words text-xl font-bold">
              {form[titleKey] || '—'}
            </h3>
            <HelpMarkdown dir={editLang === 'ar' ? 'rtl' : 'ltr'}>{form[bodyKey] || t('hcPreviewEmpty')}</HelpMarkdown>
          </div>
        </div>
      </form>
    </Modal>
  )
}
