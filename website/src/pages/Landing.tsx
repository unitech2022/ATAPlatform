import { useMemo } from 'react'
import { Link } from 'react-router'
import logo from '../assets/logo.png'
import { Card } from '../components/Card'
import { Footer } from '../components/Footer'
import { Header } from '../components/Header'
import { CatalogIcon, Icon, type IconName } from '../components/Icon'
import { MapArt } from '../components/MapArt'
import { Notice } from '../components/Notice'
import { StoreLinks } from '../components/StoreLinks'
import { useI18n, type TranslationKey } from '../i18n'
import { catalogApi } from '../lib/api'
import { fallbackRideCategories, SUPPORT_EMAIL } from '../lib/catalog'
import { formatMinutes, formatPrice } from '../lib/format'
import { useResource } from '../lib/useResource'

const safetyCards: { title: TranslationKey; copy: TranslationKey; icon: IconName }[] = [
  { title: 'safety.share.title', copy: 'safety.share.copy', icon: 'pin' },
  { title: 'safety.help.title', copy: 'safety.help.copy', icon: 'shield' },
  { title: 'safety.trusted.title', copy: 'safety.trusted.copy', icon: 'user' },
]

const joinSteps: { title: TranslationKey; copy: TranslationKey }[] = [
  { title: 'join.step1.title', copy: 'join.step1.copy' },
  { title: 'join.step2.title', copy: 'join.step2.copy' },
  { title: 'join.step3.title', copy: 'join.step3.copy' },
]

const joinDocs: TranslationKey[] = ['join.doc1', 'join.doc2', 'join.doc3', 'join.doc4', 'join.doc5']

function SectionTitle({ eyebrow, title, copy }: { eyebrow: string; title: string; copy: string }) {
  return (
    <div className="mb-8">
      <p className="mb-2 text-sm font-bold text-brand">{eyebrow}</p>
      <h2 className="text-3xl font-bold tracking-tight sm:text-4xl">{title}</h2>
      <p className="mt-3 max-w-2xl leading-7 text-muted">{copy}</p>
    </div>
  )
}

export function Landing() {
  const { t, lang } = useI18n()
  const categories = useResource(() => catalogApi.rideCategories(), [lang])
  const usingFallback = categories.error !== null || (!categories.loading && (categories.data?.length ?? 0) === 0)

  const items = useMemo(() => {
    const list = usingFallback || !categories.data ? fallbackRideCategories(lang) : categories.data
    return [...list].sort((a, b) => a.sortOrder - b.sortOrder)
  }, [categories.data, lang, usingFallback])

  const featured = items.find((item) => item.code === 'economy') ?? items[0]
  const heroCaption = featured
    ? `${featured.name} · ${featured.estimate ? `${formatMinutes(featured.estimate.etaMinutes, lang)} · ${formatPrice(featured.estimate.price, lang)}` : ''}`
    : 'ATA'

  const nav = [
    { label: t('nav.categories'), to: '#categories' },
    { label: t('nav.safety'), to: '#safety' },
    { label: t('nav.join'), to: '#join' },
    { label: t('nav.help'), to: '/help' },
    { label: t('nav.business'), to: '/business' },
  ]

  return (
    <div className="min-h-screen bg-canvas text-ink">
      <title>{t('landing.pageTitle')}</title>
      <meta name="description" content={t('landing.metaDescription')} />
      <Header
        nav={nav}
        actions={
          <Link
            to="/driver"
            className="inline-flex items-center gap-2 rounded-full bg-ink px-4 py-2.5 text-sm font-bold text-white shadow-soft hover:bg-ink-soft"
          >
            <Icon name="car" className="size-4" />
            <span className="hidden sm:inline">{t('nav.driverPortal')}</span>
          </Link>
        }
      />

      <main>
        {/* Hero */}
        <section className="relative overflow-hidden">
          <div className="pointer-events-none absolute -end-28 -top-28 size-96 rounded-full bg-brand/10" />
          <div className="pointer-events-none absolute -bottom-32 -start-24 size-96 rounded-full bg-ink/5" />
          <div className="relative mx-auto grid max-w-7xl items-center gap-10 px-4 py-14 sm:px-5 lg:grid-cols-2 lg:px-10 lg:py-24">
            <div>
              <img src={logo} alt="ATA" className="mb-8 h-14 w-32 object-contain" />
              <p className="mb-3 text-sm font-bold text-brand">{t('hero.eyebrow')}</p>
              <h1 className="text-4xl font-bold tracking-tight sm:text-5xl lg:text-6xl">{t('hero.title')}</h1>
              <p className="mt-5 max-w-xl text-lg leading-8 text-muted">{t('hero.copy')}</p>
              <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                <Link
                  to="/driver"
                  className="inline-flex items-center justify-center gap-3 rounded-2xl bg-ink px-6 py-4 font-bold text-white shadow-button hover:bg-ink-soft"
                >
                  <Icon name="car" className="size-5" />
                  {t('hero.ctaDriver')}
                </Link>
                <a
                  href="#stores"
                  className="inline-flex items-center justify-center gap-3 rounded-2xl border border-line bg-white px-6 py-4 font-bold text-ink hover:bg-cloud"
                >
                  {t('hero.ctaRider')}
                  <Icon name="arrow" className="size-5 ltr:rotate-180" />
                </a>
              </div>
              <div className="mt-6 flex items-center gap-2 text-xs font-bold text-muted">
                <Icon name="shield" className="size-4 text-brand" />
                {t('hero.safeNote')}
              </div>
              <div className="mt-10 grid grid-cols-3 gap-3">
                {[
                  [String(items.length), t('hero.stat.categories')],
                  ['24/7', t('hero.stat.support')],
                  [t('hero.stat.city'), t('hero.stat.cityNote')],
                ].map(([value, label]) => (
                  <div key={label} className="rounded-2xl bg-white p-4 shadow-soft">
                    <p className="text-2xl font-bold text-ink">{value}</p>
                    <p className="mt-1 text-xs font-bold text-muted">{label}</p>
                  </div>
                ))}
              </div>
            </div>
            <MapArt caption={heroCaption} />
          </div>
        </section>

        {/* Ride categories */}
        <section id="categories" className="mx-auto max-w-7xl scroll-mt-24 px-4 py-14 sm:px-5 lg:px-10">
          <SectionTitle eyebrow={t('categories.eyebrow')} title={t('categories.title')} copy={t('categories.copy')} />
          {usingFallback && (
            <Notice tone="info" className="mb-6" onRetry={() => categories.reload()}>
              {t('error.sampleData')}
            </Notice>
          )}
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {items.map((category) => (
              <Card key={category.id} className="flex flex-col">
                <div className="mb-6 flex items-start justify-between">
                  <div className="grid size-12 place-items-center rounded-xl bg-brand-soft text-brand">
                    <CatalogIcon code={category.icon} className="size-7" />
                  </div>
                  {category.estimate && (
                    <span className="rounded-full bg-cloud px-3 py-1.5 text-xs font-bold text-brand">
                      {formatMinutes(category.estimate.etaMinutes, lang)}
                    </span>
                  )}
                </div>
                <p className="text-xl font-bold">{category.name}</p>
                <p className="mt-2 text-sm leading-7 text-muted">{category.description}</p>
                <div className="mt-6 flex items-end justify-between gap-3">
                  <div className="text-xs font-bold text-muted">
                    <p>{t('categories.seats', { n: category.seats })}</p>
                    {category.maxStops > 0 && <p className="mt-1">{t('categories.stops', { n: category.maxStops })}</p>}
                  </div>
                  {category.estimate && <p className="text-lg font-bold">{formatPrice(category.estimate.price, lang)}</p>}
                </div>
              </Card>
            ))}
          </div>
          <p className="mt-4 text-xs font-bold text-muted">{t('categories.pricesNote')}</p>
        </section>

        {/* Safety */}
        <section id="safety" className="scroll-mt-24 bg-white/60 py-14">
          <div className="mx-auto max-w-7xl px-4 sm:px-5 lg:px-10">
            <SectionTitle eyebrow={t('safety.eyebrow')} title={t('safety.title')} copy={t('safety.copy')} />
            <div className="grid gap-4 md:grid-cols-3">
              {safetyCards.map((card) => (
                <Card key={card.title} className="group transition hover:-translate-y-1 hover:shadow-float">
                  <div className="mb-8 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand transition group-hover:bg-brand group-hover:text-white">
                    <Icon name={card.icon} className="size-7" />
                  </div>
                  <p className="text-xl font-bold">{t(card.title)}</p>
                  <p className="mt-3 text-sm leading-7 text-muted">{t(card.copy)}</p>
                  <div className="mt-6 flex items-center gap-2 text-sm font-bold text-brand">
                    {t('safety.learnMore')} <Icon name="arrow" className="size-4 ltr:rotate-180" />
                  </div>
                </Card>
              ))}
            </div>
            <div className="mt-6 flex flex-col items-start justify-between gap-5 rounded-3xl bg-ink p-7 text-white sm:flex-row sm:items-center">
              <div className="flex items-center gap-4">
                <div className="grid size-14 shrink-0 place-items-center rounded-full bg-brand text-white">
                  <Icon name="shield" className="size-7" />
                </div>
                <div>
                  <p className="text-lg font-bold">{t('safety.urgent.title')}</p>
                  <p className="mt-1 text-sm text-white/60">{t('safety.urgent.copy')}</p>
                </div>
              </div>
              <a
                href={`mailto:${SUPPORT_EMAIL}`}
                className="w-full rounded-2xl bg-white px-6 py-3 text-center font-bold text-ink hover:bg-cloud sm:w-auto"
              >
                {t('safety.urgent.cta')}
              </a>
            </div>
          </div>
        </section>

        {/* Join as a driver */}
        <section id="join" className="mx-auto max-w-7xl scroll-mt-24 px-4 py-14 sm:px-5 lg:px-10">
          <Card tone="panel" className="sm:p-10">
            <div className="mb-8 flex flex-col items-start justify-between gap-5 lg:flex-row lg:items-center">
              <div>
                <p className="mb-2 text-sm font-bold text-brand">{t('join.eyebrow')}</p>
                <h2 className="text-3xl font-bold sm:text-4xl">{t('join.title')}</h2>
                <p className="mt-3 max-w-xl leading-7 text-muted">{t('join.copy')}</p>
              </div>
              <Link
                to="/driver"
                className="inline-flex w-full items-center justify-center gap-3 rounded-2xl bg-ink px-6 py-4 font-bold text-white shadow-button hover:bg-ink-soft lg:w-auto"
              >
                <Icon name="upload" className="size-5" />
                {t('join.cta')}
              </Link>
            </div>

            <div className="mb-7 grid gap-3 sm:grid-cols-3">
              {joinSteps.map((step, index) => (
                <div key={step.title} className={`rounded-2xl border p-4 ${index === 0 ? 'border-brand bg-brand-soft' : 'border-line'}`}>
                  <div
                    className={`mb-5 grid size-8 place-items-center rounded-full text-sm font-bold ${
                      index === 0 ? 'bg-brand text-white' : 'bg-cloud text-muted'
                    }`}
                  >
                    {index + 1}
                  </div>
                  <p className="font-bold">{t(step.title)}</p>
                  <p className="mt-1 text-xs text-muted">{t(step.copy)}</p>
                </div>
              ))}
            </div>

            <div className="rounded-2xl border border-line p-5">
              <div className="mb-4 flex items-center gap-3">
                <Icon name="document" className="size-6 text-brand" />
                <p className="font-bold">{t('join.docsTitle')}</p>
              </div>
              <div className="grid gap-3 text-sm text-muted sm:grid-cols-2">
                {joinDocs.map((doc) => (
                  <div key={doc} className="flex items-center gap-2">
                    <Icon name="check" className="size-4 shrink-0 text-brand" />
                    {t(doc)}
                  </div>
                ))}
              </div>
            </div>
          </Card>
        </section>

        {/* Store links */}
        <section id="stores" className="mx-auto max-w-7xl scroll-mt-24 px-4 pb-16 sm:px-5 lg:px-10">
          <Card tone="dark" className="flex flex-col items-start justify-between gap-6 sm:p-8 lg:flex-row lg:items-center">
            <div>
              <p className="text-2xl font-bold">{t('stores.title')}</p>
              <p className="mt-2 text-sm leading-7 text-white/70">{t('stores.copy')}</p>
            </div>
            <StoreLinks />
          </Card>
        </section>
      </main>

      <Footer />
    </div>
  )
}
