import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { Icon } from '../../components/Icon'
import { SiteLayout } from '../../components/SiteLayout'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { useI18n } from '../../i18n'
import { helpApi } from '../../lib/api'
import type { HelpAudience } from '../../lib/types'
import { useDebounced } from '../../lib/useDebounced'
import { parseAudience } from '../../lib/help'
import { useResource } from '../../lib/useResource'
import { ArticleList, AudienceTabs, CategoryCard } from './shared'
import { SupportCta } from './SupportCta'

export function HelpHome() {
  const { t, lang } = useI18n()
  const [params, setParams] = useSearchParams()
  const audience = parseAudience(params.get('audience'))
  const [query, setQuery] = useState(params.get('q') ?? '')
  const debounced = useDebounced(query.trim())

  // Mirror the debounced search into the URL so results can be shared/bookmarked.
  const urlQuery = params.get('q') ?? ''
  useEffect(() => {
    if (urlQuery === debounced) return
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        if (debounced) next.set('q', debounced)
        else next.delete('q')
        return next
      },
      { replace: true },
    )
  }, [debounced, urlQuery, setParams])

  const setAudience = (next: HelpAudience) =>
    setParams((current) => {
      const updated = new URLSearchParams(current)
      updated.set('audience', next)
      return updated
    })

  const categories = useResource(() => helpApi.categories(audience), [audience, lang])
  const articles = useResource(() => helpApi.articles({ audience, q: debounced || undefined, page: 1 }), [audience, debounced, lang])
  const searching = debounced.length > 0

  return (
    <SiteLayout>
      <title>{t('help.pageTitle')}</title>
      <meta name="description" content={t('help.metaDescription')} />
      <section className="relative overflow-hidden bg-ink text-white">
        <div className="pointer-events-none absolute -end-24 -top-24 size-80 rounded-full bg-brand/20" />
        <div className="relative mx-auto max-w-4xl px-4 py-14 text-center sm:px-5 lg:py-20">
          <p className="mb-3 text-sm font-bold text-brand">{t('help.eyebrow')}</p>
          <h1 className="text-3xl font-bold sm:text-4xl">{t('help.title')}</h1>
          <p className="mx-auto mt-3 max-w-xl leading-7 text-white/70">{t('help.copy')}</p>
          <form
            role="search"
            className="mx-auto mt-8 flex h-16 max-w-2xl items-center gap-3 rounded-2xl bg-white px-4 text-ink shadow-float"
            onSubmit={(event) => event.preventDefault()}
          >
            <Icon name="search" className="size-6 shrink-0 text-muted" />
            <input
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={t('help.searchPlaceholder')}
              aria-label={t('help.searchLabel')}
              className="h-full min-w-0 flex-1 bg-transparent font-bold outline-none placeholder:font-medium placeholder:text-muted"
            />
          </form>
        </div>
      </section>

      <div className="mx-auto max-w-5xl px-4 py-10 sm:px-5 lg:py-14">
        <div className="mb-8 flex justify-center">
          <AudienceTabs value={audience} onChange={setAudience} />
        </div>

        {!searching && (
          <section className="mb-12">
            <h2 className="mb-5 text-2xl font-bold">{t('help.categories')}</h2>
            {categories.loading && !categories.data ? (
              <LoadingState />
            ) : categories.error && !categories.data ? (
              <ErrorState error={categories.error} onRetry={() => categories.reload()} />
            ) : categories.data && categories.data.length > 0 ? (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {categories.data.map((category) => (
                  <CategoryCard key={category.id} category={category} audience={audience} />
                ))}
              </div>
            ) : (
              <EmptyState title={t('help.noCategories')} />
            )}
          </section>
        )}

        <section>
          <h2 className="mb-5 text-2xl font-bold">
            {searching ? t('help.resultsFor', { q: debounced }) : t('help.popular')}
          </h2>
          {articles.loading && !articles.data ? (
            <LoadingState />
          ) : articles.error && !articles.data ? (
            <ErrorState error={articles.error} onRetry={() => articles.reload()} />
          ) : articles.data && articles.data.items.length > 0 ? (
            <ArticleList articles={articles.data.items} />
          ) : (
            <EmptyState title={searching ? t('help.noResults') : t('help.noArticles')}>
              {searching && t('help.noResultsHint')}
            </EmptyState>
          )}
        </section>

        <SupportCta className="mt-12" />
      </div>
    </SiteLayout>
  )
}
