import { Link } from 'react-router'
import { Action } from '../../components/Button'
import { CatalogIcon, Icon } from '../../components/Icon'
import { useI18n } from '../../i18n'
import { formatDate } from '../../lib/format'
import { AUDIENCES } from '../../lib/help'
import type { HelpArticleSummary, HelpAudience, HelpCategory } from '../../lib/types'

export function AudienceTabs({ value, onChange }: { value: HelpAudience; onChange: (next: HelpAudience) => void }) {
  const { t } = useI18n()
  return (
    <div role="tablist" aria-label={t('help.audience')} className="inline-flex rounded-full bg-white p-1 shadow-soft">
      {AUDIENCES.map((audience) => (
        <Action
          key={audience}
          role="tab"
          aria-selected={value === audience}
          onClick={() => onChange(audience)}
          className={`rounded-full px-5 py-2.5 text-sm font-bold transition ${
            value === audience ? 'bg-ink text-white' : 'text-muted hover:text-ink'
          }`}
        >
          {t(`help.audience.${audience}`)}
        </Action>
      ))}
    </div>
  )
}

export function CategoryCard({ category, audience }: { category: HelpCategory; audience: HelpAudience }) {
  const { t } = useI18n()
  return (
    <Link
      to={`/help/categories/${category.id}?audience=${audience}`}
      className="group flex items-center gap-4 rounded-3xl bg-white p-5 shadow-soft transition hover:-translate-y-0.5 hover:shadow-float"
    >
      <span className="grid size-12 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand transition group-hover:bg-brand group-hover:text-white">
        <CatalogIcon code={category.icon} className="size-6" />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate font-bold">{category.name}</span>
        <span className="block text-xs font-bold text-muted">{t('help.articlesCount', { n: category.articlesCount })}</span>
      </span>
      <Icon name="chevron" className="size-5 shrink-0 text-muted rtl:rotate-180" />
    </Link>
  )
}

export function ArticleList({ articles }: { articles: HelpArticleSummary[] }) {
  const { lang } = useI18n()
  return (
    <ul className="divide-y divide-line overflow-hidden rounded-3xl bg-white shadow-soft">
      {articles.map((article) => (
        <li key={article.id}>
          <Link to={`/help/${article.slug}`} className="flex items-start gap-4 p-5 transition hover:bg-cloud">
            <Icon name="document" className="mt-1 size-5 shrink-0 text-brand" />
            <span className="min-w-0 flex-1">
              <span className="block font-bold">{article.title}</span>
              {article.excerpt && <span className="mt-1 line-clamp-2 block text-sm leading-7 text-muted">{article.excerpt}</span>}
              <span className="mt-1 block text-xs text-muted">{formatDate(article.updatedAt, lang)}</span>
            </span>
            <Icon name="chevron" className="mt-1 size-5 shrink-0 text-muted rtl:rotate-180" />
          </Link>
        </li>
      ))}
    </ul>
  )
}

export function Breadcrumbs({ items }: { items: { label: string; to?: string }[] }) {
  const { t } = useI18n()
  return (
    <nav aria-label={t('help.breadcrumbs')} className="mb-6 flex flex-wrap items-center gap-2 text-sm font-bold text-muted">
      {items.map((item, index) => (
        <span key={`${item.label}-${index}`} className="flex min-w-0 items-center gap-2">
          {index > 0 && <Icon name="chevron" className="size-4 shrink-0 rtl:rotate-180" />}
          {item.to ? (
            <Link to={item.to} className="truncate hover:text-brand">
              {item.label}
            </Link>
          ) : (
            <span className="truncate text-ink">{item.label}</span>
          )}
        </span>
      ))}
    </nav>
  )
}
