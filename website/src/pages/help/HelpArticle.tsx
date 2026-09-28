import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { Action } from '../../components/Button'
import { Card } from '../../components/Card'
import { Icon } from '../../components/Icon'
import { Markdown } from '../../components/Markdown'
import { Notice } from '../../components/Notice'
import { SiteLayout } from '../../components/SiteLayout'
import { ErrorState, LoadingState } from '../../components/States'
import { useI18n } from '../../i18n'
import { helpApi, isApiError } from '../../lib/api'
import { formatDate } from '../../lib/format'
import { useResource } from '../../lib/useResource'
import { Breadcrumbs } from './shared'
import { SupportCta } from './SupportCta'

const FEEDBACK_KEY = 'ata-help-feedback'

function readFeedback(): Record<string, boolean> {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(FEEDBACK_KEY) ?? '{}')
    return typeof parsed === 'object' && parsed !== null ? (parsed as Record<string, boolean>) : {}
  } catch {
    return {}
  }
}

function rememberFeedback(id: string, helpful: boolean) {
  try {
    localStorage.setItem(FEEDBACK_KEY, JSON.stringify({ ...readFeedback(), [id]: helpful }))
  } catch {
    // Not critical: the API also rate-limits per IP/article/day.
  }
}

/** Plain-text summary of a Markdown body for the meta description. */
function plainExcerpt(markdown: string): string {
  return markdown
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/!\[[^\]]*\]\([^)]*\)/g, ' ')
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/[#>*_`~|-]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 160)
}

function Feedback({ articleId }: { articleId: string }) {
  const { t } = useI18n()
  const [sent, setSent] = useState<boolean | null>(() => readFeedback()[articleId] ?? null)
  const [busy, setBusy] = useState(false)
  const [failed, setFailed] = useState(false)

  const send = async (helpful: boolean) => {
    setBusy(true)
    setFailed(false)
    try {
      await helpApi.feedback(articleId, helpful)
      rememberFeedback(articleId, helpful)
      setSent(helpful)
    } catch (error) {
      // Already voted today (rate limited) still counts as recorded.
      if (isApiError(error) && error.status === 429) {
        rememberFeedback(articleId, helpful)
        setSent(helpful)
      } else setFailed(true)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
      {sent === null ? (
        <>
          <p className="font-bold">{t('help.feedback.question')}</p>
          <div className="flex gap-3">
            {([true, false] as const).map((helpful) => (
              <Action
                key={String(helpful)}
                disabled={busy}
                onClick={() => void send(helpful)}
                className="rounded-2xl border border-line px-5 py-2.5 text-sm font-bold hover:border-brand hover:text-brand"
              >
                {t(helpful ? 'help.feedback.yes' : 'help.feedback.no')}
              </Action>
            ))}
          </div>
        </>
      ) : (
        <p className="flex items-center gap-2 font-bold text-brand" role="status">
          <Icon name="check" className="size-5" />
          {t('help.feedback.thanks')}
        </p>
      )}
      {failed && (
        <Notice tone="error" className="w-full">
          {t('error.generic')}
        </Notice>
      )}
    </Card>
  )
}

export function HelpArticle() {
  const { slug = '' } = useParams()
  const { t, lang } = useI18n()
  const article = useResource(() => helpApi.article(slug), [slug, lang])
  const data = article.data
  const notFound = isApiError(article.error) && article.error.status === 404

  return (
    <SiteLayout>
      <title>{data ? `${data.title} · ${t('help.pageTitle')}` : t('help.pageTitle')}</title>
      {data && <meta name="description" content={plainExcerpt(data.body)} />}
      <div className="mx-auto max-w-4xl px-4 py-10 sm:px-5 lg:py-14">
        {article.loading && !data ? (
          <LoadingState />
        ) : notFound ? (
          <div className="py-10 text-center">
            <h1 className="text-2xl font-bold">{t('help.articleNotFound')}</h1>
            <Link to="/help" className="mt-6 inline-block font-bold text-brand">
              {t('help.backToHelp')}
            </Link>
          </div>
        ) : article.error && !data ? (
          <ErrorState error={article.error} onRetry={() => article.reload()} />
        ) : data ? (
          <>
            <Breadcrumbs
              items={[
                { label: t('help.home'), to: '/help' },
                { label: data.category.name, to: `/help/categories/${data.category.id}` },
                { label: data.title },
              ]}
            />
            <article className="rounded-3xl bg-white p-6 shadow-soft sm:p-10">
              <h1 className="text-3xl font-bold leading-snug">{data.title}</h1>
              <p className="mt-3 text-sm font-bold text-muted">{t('help.updated', { date: formatDate(data.updatedAt, lang) })}</p>
              <hr className="my-6 border-line" />
              <Markdown>{data.body}</Markdown>
              {data.tags && data.tags.length > 0 && (
                <ul className="mt-8 flex flex-wrap gap-2">
                  {data.tags.map((tag) => (
                    <li key={tag} className="rounded-full bg-cloud px-3 py-1 text-xs font-bold text-muted">
                      #{tag}
                    </li>
                  ))}
                </ul>
              )}
            </article>

            <div className="mt-6">
              <Feedback articleId={data.id} />
            </div>

            {data.related.length > 0 && (
              <section className="mt-10">
                <h2 className="mb-4 text-xl font-bold">{t('help.related')}</h2>
                <ul className="grid gap-3 sm:grid-cols-2">
                  {data.related.map((item) => (
                    <li key={item.slug}>
                      <Link
                        to={`/help/${item.slug}`}
                        className="flex items-center gap-3 rounded-2xl bg-white p-4 font-bold shadow-soft hover:text-brand"
                      >
                        <Icon name="document" className="size-5 shrink-0 text-brand" />
                        <span className="min-w-0 flex-1">{item.title}</span>
                      </Link>
                    </li>
                  ))}
                </ul>
              </section>
            )}
          </>
        ) : null}

        <SupportCta className="mt-12" />
      </div>
    </SiteLayout>
  )
}
