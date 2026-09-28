import { Link } from 'react-router'
import { SiteLayout } from '../../components/SiteLayout'
import { LEGAL_UPDATED_AT, privacyPolicy, termsOfUse, type LegalDocument } from '../../content/legal'
import { useI18n, type Lang } from '../../i18n'
import { formatDate } from '../../lib/format'

function LegalDocumentView({ documents, other }: { documents: Record<Lang, LegalDocument>; other: { to: string; label: string } }) {
  const { t, lang } = useI18n()
  const doc = documents[lang]
  return (
    <SiteLayout>
      <title>{`${doc.title} · ATA`}</title>
      <meta name="description" content={doc.intro} />
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-10 sm:px-5 lg:grid-cols-[16rem_minmax(0,1fr)] lg:py-14">
        <aside className="lg:sticky lg:top-6 lg:self-start">
          <nav aria-label={t('legal.contents')} className="rounded-3xl bg-white p-5 shadow-soft">
            <p className="mb-3 text-sm font-bold text-brand">{t('legal.contents')}</p>
            <ol className="space-y-2 text-sm font-bold">
              {doc.sections.map((section) => (
                <li key={section.id}>
                  <a href={`#${section.id}`} className="text-muted hover:text-ink">
                    {section.heading}
                  </a>
                </li>
              ))}
            </ol>
            <Link to={other.to} className="mt-5 block border-t border-line pt-4 text-sm font-bold text-brand">
              {other.label}
            </Link>
          </nav>
        </aside>
        <article className="rounded-3xl bg-white p-6 shadow-soft sm:p-10">
          <h1 className="text-3xl font-bold">{doc.title}</h1>
          <p className="mt-2 text-sm font-bold text-muted">{t('legal.updated', { date: formatDate(LEGAL_UPDATED_AT, lang) })}</p>
          <p className="mt-6 leading-8 text-ink">{doc.intro}</p>
          {doc.sections.map((section) => (
            <section key={section.id} id={section.id} className="mt-8 scroll-mt-6">
              <h2 className="text-xl font-bold">{section.heading}</h2>
              {section.paragraphs.map((paragraph) => (
                <p key={paragraph} className="mt-3 leading-8 text-muted">
                  {paragraph}
                </p>
              ))}
              {section.bullets && (
                <ul className="mt-3 list-disc space-y-2 ps-6 leading-8 text-muted">
                  {section.bullets.map((bullet) => (
                    <li key={bullet}>{bullet}</li>
                  ))}
                </ul>
              )}
            </section>
          ))}
        </article>
      </div>
    </SiteLayout>
  )
}

export function PrivacyPolicy() {
  const { t } = useI18n()
  return <LegalDocumentView documents={privacyPolicy} other={{ to: '/terms', label: t('footer.terms') }} />
}

export function TermsOfUse() {
  const { t } = useI18n()
  return <LegalDocumentView documents={termsOfUse} other={{ to: '/privacy', label: t('footer.privacy') }} />
}
