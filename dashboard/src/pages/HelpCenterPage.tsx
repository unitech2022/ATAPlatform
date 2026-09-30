import { useState } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { SearchInput, Select } from '../components/Field'
import { HelpArticleFormModal, type ArticleTarget } from '../components/HelpArticleFormModal'
import { HelpCategoryFormModal } from '../components/HelpCategoryFormModal'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { PermissionError } from '../components/PermissionError'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery, type QueryState } from '../hooks/useQuery'
import { useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { helpArticles, helpCategories } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDate, formatNumber } from '../lib/format'
import { formatRatio } from '../lib/rewards'
import { AUDIENCE_KEY, helpfulRate, HELP_AUDIENCES } from '../lib/support'
import type { HelpArticle, HelpCategory } from '../lib/types'

const PAGE_SIZE = 20
const TABS = ['articles', 'categories'] as const
type HelpTab = (typeof TABS)[number]
const PUBLISHED_FILTERS = ['true', 'false'] as const

/** Help-center CMS (`/help-center`, `help.manage`): articles (bilingual Markdown editor with preview, publish toggle, stats) and categories. */
export function HelpCenterPage() {
  const { t } = useLang()
  const { params, setFilter } = useUrlState()
  const tab = parseEnum(params.get('tab'), TABS) || 'articles'
  const categories = useQuery(() => helpCategories.list(), 'help-categories')

  return (
    <>
      <PageHeader title={t('hcTitle')} description={t('hcCopy')} />
      <Tabs
        className="mb-4"
        value={tab}
        onChange={(value: HelpTab) => setFilter('tab', value === 'articles' ? '' : value)}
        options={[
          { value: 'articles' as HelpTab, label: t('hcArticles') },
          { value: 'categories' as HelpTab, label: t('hcCategories'), count: categories.data ? categories.data.length : null },
        ]}
      />
      {tab === 'articles' ? <ArticlesTab categories={categories.data ?? []} categoriesReady={!categories.loading || categories.data !== null} /> : <CategoriesTab query={categories} />}
    </>
  )
}

function ArticlesTab({ categories, categoriesReady }: { categories: HelpCategory[]; categoriesReady: boolean }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const [target, setTarget] = useState<ArticleTarget | null>(null)
  const [deleting, setDeleting] = useState<HelpArticle | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const categoryId = params.get('categoryId') ?? ''
  const audience = parseEnum(params.get('audience'), HELP_AUDIENCES)
  const published = parseEnum(params.get('published'), PUBLISHED_FILTERS)

  const query = useQuery(
    () => helpArticles.list({ categoryId, audience, published, q: search.value, page, pageSize: PAGE_SIZE }),
    `help-articles:${categoryId}:${audience}:${published}:${search.value}:${page}`,
  )

  const categoryName = (article: HelpArticle) => {
    const category = categories.find((item) => item.id === article.categoryId)
    return category ? (lang === 'ar' ? category.nameAr : category.nameEn) : (article.categoryName ?? '—')
  }

  const togglePublish = async (article: HelpArticle) => {
    setBusyId(article.id)
    try {
      if (article.isPublished) await helpArticles.unpublish(article.id)
      else await helpArticles.publish(article.id)
      toast.success(t(article.isPublished ? 'hcUnpublished' : 'hcPublishedToast'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyId(null)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await helpArticles.remove(deleting.id)
      toast.success(t('hcArticleDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<HelpArticle>[] = [
    {
      key: 'title',
      header: t('hcArticle'),
      render: (row) => (
        <span className="block max-w-80">
          <span className="block truncate font-bold">{lang === 'ar' ? row.titleAr : row.titleEn}</span>
          <span className="block truncate text-xs text-muted">{lang === 'ar' ? row.titleEn : row.titleAr}</span>
          <span className="ltr-nums block truncate text-[11px] text-muted">/{row.slug}</span>
        </span>
      ),
    },
    { key: 'category', header: t('category'), render: (row) => categoryName(row) },
    { key: 'audience', header: t('hcAudience'), render: (row) => <Badge tone="ink">{t(AUDIENCE_KEY[row.audience] ?? 'hcAudienceAll')}</Badge> },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <Badge tone={row.isPublished ? 'brand' : 'muted'}>{row.isPublished ? t('hcPublished') : t('hcDraft')}</Badge>
          {row.publishedAt && <span className="block text-[11px] text-muted">{formatDate(row.publishedAt, lang)}</span>}
        </span>
      ),
    },
    {
      key: 'stats',
      header: t('hcStats'),
      render: (row) => {
        const rate = helpfulRate(row.helpfulYes, row.helpfulNo)
        return (
          <span className="block whitespace-nowrap text-xs">
            <span className="ltr-nums block">
              {t('hcViews')}: <span className="font-bold">{formatNumber(row.viewCount)}</span>
            </span>
            <span className="ltr-nums block text-muted">
              {t('hcHelpful')}: {formatNumber(row.helpfulYes)} / {formatNumber(row.helpfulNo)}
              {rate !== null ? ` · ${formatRatio(rate)}` : ''}
            </span>
          </span>
        )
      },
    },
    { key: 'updated', header: t('hcUpdatedAt'), render: (row) => <span className="whitespace-nowrap text-xs text-muted">{formatDate(row.updatedAt, lang)}</span> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => setTarget({ mode: 'edit', id: row.id })}>
            {t('edit')}
          </Button>
          <Button variant={row.isPublished ? 'secondary' : 'brand'} size="sm" icon={row.isPublished ? 'pause' : 'globe'} loading={busyId === row.id} onClick={() => togglePublish(row)}>
            {row.isPublished ? t('hcUnpublish') : t('hcPublish')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  return (
    <>
      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto_auto_auto]">
        <SearchInput placeholder={t('hcSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} wrapperClassName="sm:col-span-2 lg:col-span-1" />
        <Select id="category" aria-label={t('category')} value={categoryId} onChange={(event) => setFilter('categoryId', event.target.value)} wrapperClassName="lg:w-48">
          <option value="">{t('hcAllCategories')}</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {lang === 'ar' ? category.nameAr : category.nameEn}
            </option>
          ))}
        </Select>
        <Select id="audience" aria-label={t('hcAudience')} value={audience} onChange={(event) => setFilter('audience', event.target.value)} wrapperClassName="lg:w-40">
          <option value="">{t('hcAllAudiences')}</option>
          {HELP_AUDIENCES.map((value) => (
            <option key={value} value={value}>
              {t(AUDIENCE_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="published" aria-label={t('status')} value={published} onChange={(event) => setFilter('published', event.target.value)} wrapperClassName="lg:w-40">
          <option value="">{t('spAllStatuses')}</option>
          <option value="true">{t('hcPublished')}</option>
          <option value="false">{t('hcDraft')}</option>
        </Select>
        <Button icon="plus" disabled={!categoriesReady || categories.length === 0} onClick={() => setTarget({ mode: 'new' })} title={categories.length === 0 ? t('hcNeedCategory') : undefined}>
          {t('hcArticleNew')}
        </Button>
      </div>

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="help.manage" onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('hcNoArticles')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <HelpArticleFormModal
        target={target}
        categories={categories}
        onClose={() => setTarget(null)}
        onSaved={() => {
          setTarget(null)
          query.reload()
        }}
      />
      <ConfirmModal
        open={deleting !== null}
        title={t('hcArticleDelete')}
        description={deleting ? `${lang === 'ar' ? deleting.titleAr : deleting.titleEn} — ${t('hcArticleDeleteCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}

function CategoriesTab({ query }: { query: QueryState<HelpCategory[]> }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [editing, setEditing] = useState<HelpCategory | 'new' | null>(null)
  const [deleting, setDeleting] = useState<HelpCategory | null>(null)

  const remove = async () => {
    if (!deleting) return
    try {
      await helpCategories.remove(deleting.id)
      toast.success(t('hcCategoryDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const rows = [...(query.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder)

  const columns: Column<HelpCategory>[] = [
    { key: 'sort', header: t('sortOrder'), className: 'text-center', render: (row) => <span className="ltr-nums text-muted">{formatNumber(row.sortOrder)}</span> },
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{lang === 'ar' ? row.nameAr : row.nameEn}</span>
          <span className="block text-xs text-muted">{lang === 'ar' ? row.nameEn : row.nameAr}</span>
        </span>
      ),
    },
    { key: 'code', header: t('code'), render: (row) => <span className="ltr-nums text-xs text-muted">{row.code}</span> },
    { key: 'icon', header: t('icon'), render: (row) => (row.icon ? <span className="ltr-nums text-xs">{row.icon}</span> : <span className="text-muted">—</span>) },
    { key: 'audience', header: t('hcAudience'), render: (row) => <Badge tone="ink">{t(AUDIENCE_KEY[row.audience] ?? 'hcAudienceAll')}</Badge> },
    {
      key: 'articles',
      header: t('hcArticles'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums">{typeof row.articlesCount === 'number' ? formatNumber(row.articlesCount) : '—'}</span>,
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
      <div className="mb-4 flex justify-end">
        <Button icon="plus" onClick={() => setEditing('new')}>
          {t('hcCategoryNew')}
        </Button>
      </div>
      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="help.manage" onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('hcNoCategories')} emptyDescription="" />
        )}
      </Card>
      <HelpCategoryFormModal
        target={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          query.reload()
        }}
      />
      <ConfirmModal
        open={deleting !== null}
        title={t('hcCategoryDelete')}
        description={deleting ? `${lang === 'ar' ? deleting.nameAr : deleting.nameEn} (${deleting.code}) — ${t('hcCategoryDeleteCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
