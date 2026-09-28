import { useState } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { CatalogIcon } from '../../components/Icon'
import { Pagination } from '../../components/Pagination'
import { SiteLayout } from '../../components/SiteLayout'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { useI18n } from '../../i18n'
import { helpApi } from '../../lib/api'
import { parseAudience } from '../../lib/help'
import { useResource } from '../../lib/useResource'
import { ArticleList, Breadcrumbs } from './shared'
import { SupportCta } from './SupportCta'

export function HelpCategory() {
  const { categoryId = '' } = useParams()
  const [params] = useSearchParams()
  const audience = parseAudience(params.get('audience'))
  const { t, lang } = useI18n()
  const [page, setPage] = useState(1)

  const categories = useResource(() => helpApi.categories(audience), [audience, lang])
  const articles = useResource(() => helpApi.articles({ categoryId, audience, page }), [categoryId, audience, page, lang])
  const category = categories.data?.find((item) => item.id === categoryId) ?? null
  const title = category?.name ?? t('help.category')

  return (
    <SiteLayout>
      <title>{`${title} · ${t('help.pageTitle')}`}</title>
      <div className="mx-auto max-w-4xl px-4 py-10 sm:px-5 lg:py-14">
        <Breadcrumbs
          items={[
            { label: t('help.home'), to: `/help?audience=${audience}` },
            { label: title },
          ]}
        />
        <div className="mb-8 flex items-center gap-4">
          <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
            <CatalogIcon code={category?.icon ?? 'document'} className="size-7" />
          </span>
          <div className="min-w-0">
            <h1 className="text-3xl font-bold">{title}</h1>
            {category && <p className="mt-1 text-sm font-bold text-muted">{t('help.articlesCount', { n: category.articlesCount })}</p>}
          </div>
        </div>

        {articles.loading && !articles.data ? (
          <LoadingState />
        ) : articles.error && !articles.data ? (
          <ErrorState error={articles.error} onRetry={() => articles.reload()} />
        ) : articles.data && articles.data.items.length > 0 ? (
          <>
            <ArticleList articles={articles.data.items} />
            <Pagination
              className="mt-6"
              page={articles.data.page}
              pageSize={articles.data.pageSize}
              total={articles.data.total}
              onChange={setPage}
            />
          </>
        ) : (
          <EmptyState title={t('help.noArticles')} />
        )}

        <SupportCta className="mt-12" />
      </div>
    </SiteLayout>
  )
}
