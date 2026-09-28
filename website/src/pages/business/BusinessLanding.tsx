import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Field, Input } from '../../components/Field'
import { Icon, type IconName } from '../../components/Icon'
import { SiteLayout } from '../../components/SiteLayout'
import { useI18n, type TranslationKey } from '../../i18n'
import { BUSINESS_EMAIL, BUSINESS_WHATSAPP } from '../../lib/catalog'

const benefits: { icon: IconName; title: TranslationKey; copy: TranslationKey }[] = [
  { icon: 'receipt', title: 'bizLanding.benefit.invoice.title', copy: 'bizLanding.benefit.invoice.copy' },
  { icon: 'shield', title: 'bizLanding.benefit.policies.title', copy: 'bizLanding.benefit.policies.copy' },
  { icon: 'wallet', title: 'bizLanding.benefit.budgets.title', copy: 'bizLanding.benefit.budgets.copy' },
  { icon: 'user', title: 'bizLanding.benefit.guests.title', copy: 'bizLanding.benefit.guests.copy' },
  { icon: 'chart', title: 'bizLanding.benefit.reports.title', copy: 'bizLanding.benefit.reports.copy' },
  { icon: 'pin', title: 'bizLanding.benefit.safety.title', copy: 'bizLanding.benefit.safety.copy' },
]

const steps: { title: TranslationKey; copy: TranslationKey }[] = [
  { title: 'bizLanding.step1.title', copy: 'bizLanding.step1.copy' },
  { title: 'bizLanding.step2.title', copy: 'bizLanding.step2.copy' },
  { title: 'bizLanding.step3.title', copy: 'bizLanding.step3.copy' },
  { title: 'bizLanding.step4.title', copy: 'bizLanding.step4.copy' },
]

interface SalesForm {
  company: string
  name: string
  phone: string
  employees: string
  message: string
}

const emptyForm: SalesForm = { company: '', name: '', phone: '', employees: '', message: '' }

export function BusinessLanding() {
  const { t } = useI18n()
  const [form, setForm] = useState<SalesForm>(emptyForm)
  const [submitted, setSubmitted] = useState(false)
  const set = (key: keyof SalesForm) => (value: string) => setForm((current) => ({ ...current, [key]: value }))

  const body = [
    `${t('bizLanding.form.company')}: ${form.company}`,
    `${t('bizLanding.form.name')}: ${form.name}`,
    `${t('bizLanding.form.phone')}: ${form.phone}`,
    `${t('bizLanding.form.employees')}: ${form.employees}`,
    '',
    form.message,
  ].join('\n')
  const valid = form.company.trim() !== '' && form.name.trim() !== '' && form.phone.trim() !== ''

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!valid) {
      setSubmitted(true)
      return
    }
    const subject = t('bizLanding.form.subject', { company: form.company })
    window.location.href = `mailto:${BUSINESS_EMAIL}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`
  }

  return (
    <SiteLayout
      actions={
        <Link
          to="/business/login"
          className="inline-flex items-center gap-2 rounded-full bg-ink px-4 py-2.5 text-sm font-bold text-white shadow-soft hover:bg-ink-soft"
        >
          <Icon name="building" className="size-4" />
          <span className="hidden sm:inline">{t('bizLanding.login')}</span>
        </Link>
      }
    >
      <title>{t('bizLanding.pageTitle')}</title>
      <meta name="description" content={t('bizLanding.copy')} />

      <section className="relative overflow-hidden">
        <div className="pointer-events-none absolute -end-28 -top-28 size-96 rounded-full bg-brand/10" />
        <div className="relative mx-auto grid max-w-7xl items-center gap-10 px-4 py-14 sm:px-5 lg:grid-cols-[1.1fr_0.9fr] lg:px-10 lg:py-24">
          <div>
            <p className="mb-3 text-sm font-bold text-brand">{t('bizLanding.eyebrow')}</p>
            <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">{t('bizLanding.title')}</h1>
            <p className="mt-5 max-w-xl text-lg leading-8 text-muted">{t('bizLanding.copy')}</p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Link
                to="/business/login"
                className="inline-flex items-center justify-center gap-3 rounded-2xl bg-ink px-6 py-4 font-bold text-white shadow-button hover:bg-ink-soft"
              >
                <Icon name="building" className="size-5" />
                {t('bizLanding.login')}
              </Link>
              <a
                href="#contact"
                className="inline-flex items-center justify-center gap-3 rounded-2xl border border-line bg-white px-6 py-4 font-bold text-ink hover:bg-cloud"
              >
                {t('bizLanding.contactSales')}
                <Icon name="arrow" className="size-5 ltr:rotate-180" />
              </a>
            </div>
          </div>
          <Card tone="dark" className="sm:p-8">
            <p className="text-sm font-bold text-brand">{t('bizLanding.card.eyebrow')}</p>
            <ul className="mt-5 space-y-4">
              {(['bizLanding.card.point1', 'bizLanding.card.point2', 'bizLanding.card.point3', 'bizLanding.card.point4'] as const).map(
                (key) => (
                  <li key={key} className="flex items-start gap-3">
                    <span className="mt-0.5 grid size-6 shrink-0 place-items-center rounded-full bg-brand text-white">
                      <Icon name="check" className="size-4" />
                    </span>
                    <span className="leading-7 text-white/85">{t(key)}</span>
                  </li>
                ),
              )}
            </ul>
          </Card>
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 py-14 sm:px-5 lg:px-10">
        <p className="mb-2 text-sm font-bold text-brand">{t('bizLanding.benefits.eyebrow')}</p>
        <h2 className="mb-8 text-3xl font-bold sm:text-4xl">{t('bizLanding.benefits.title')}</h2>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {benefits.map((item) => (
            <Card key={item.title}>
              <div className="mb-6 grid size-12 place-items-center rounded-2xl bg-brand-soft text-brand">
                <Icon name={item.icon} className="size-6" />
              </div>
              <p className="text-lg font-bold">{t(item.title)}</p>
              <p className="mt-2 text-sm leading-7 text-muted">{t(item.copy)}</p>
            </Card>
          ))}
        </div>
      </section>

      <section className="bg-white/60 py-14">
        <div className="mx-auto max-w-7xl px-4 sm:px-5 lg:px-10">
          <h2 className="mb-8 text-3xl font-bold sm:text-4xl">{t('bizLanding.how.title')}</h2>
          <ol className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {steps.map((step, index) => (
              <li key={step.title} className="rounded-3xl bg-white p-5 shadow-soft">
                <span className="mb-5 grid size-9 place-items-center rounded-full bg-ink text-sm font-bold text-white">{index + 1}</span>
                <p className="font-bold">{t(step.title)}</p>
                <p className="mt-2 text-sm leading-7 text-muted">{t(step.copy)}</p>
              </li>
            ))}
          </ol>
        </div>
      </section>

      <section id="contact" className="mx-auto max-w-7xl scroll-mt-24 px-4 py-14 sm:px-5 lg:px-10">
        <Card tone="panel" className="grid gap-8 sm:p-10 lg:grid-cols-[0.8fr_1.2fr]">
          <div>
            <p className="mb-2 text-sm font-bold text-brand">{t('bizLanding.contactSales')}</p>
            <h2 className="text-3xl font-bold">{t('bizLanding.form.title')}</h2>
            <p className="mt-3 leading-7 text-muted">{t('bizLanding.form.copy')}</p>
            <div className="mt-6 space-y-3">
              <a href={`mailto:${BUSINESS_EMAIL}`} className="flex items-center gap-3 font-bold hover:text-brand">
                <Icon name="mail" className="size-5 text-brand" />
                <span dir="ltr">{BUSINESS_EMAIL}</span>
              </a>
              {BUSINESS_WHATSAPP && (
                <a
                  href={`https://wa.me/${BUSINESS_WHATSAPP}?text=${encodeURIComponent(body)}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-3 font-bold hover:text-brand"
                >
                  <Icon name="phone" className="size-5 text-brand" />
                  {t('bizLanding.form.whatsapp')}
                </a>
              )}
            </div>
          </div>
          <form className="grid gap-4 sm:grid-cols-2" onSubmit={submit} noValidate>
            <Field label={t('bizLanding.form.company')} htmlFor="sales-company" error={submitted && !form.company.trim() ? t('form.required') : null}>
              <Input id="sales-company" value={form.company} onChange={(e) => set('company')(e.target.value)} autoComplete="organization" />
            </Field>
            <Field label={t('bizLanding.form.name')} htmlFor="sales-name" error={submitted && !form.name.trim() ? t('form.required') : null}>
              <Input id="sales-name" value={form.name} onChange={(e) => set('name')(e.target.value)} autoComplete="name" />
            </Field>
            <Field label={t('bizLanding.form.phone')} htmlFor="sales-phone" error={submitted && !form.phone.trim() ? t('form.required') : null}>
              <Input id="sales-phone" type="tel" dir="ltr" value={form.phone} onChange={(e) => set('phone')(e.target.value)} autoComplete="tel" />
            </Field>
            <Field label={t('bizLanding.form.employees')} htmlFor="sales-employees">
              <Input id="sales-employees" inputMode="numeric" value={form.employees} onChange={(e) => set('employees')(e.target.value)} />
            </Field>
            <Field label={t('bizLanding.form.message')} htmlFor="sales-message" className="sm:col-span-2">
              <textarea
                id="sales-message"
                rows={4}
                value={form.message}
                onChange={(e) => set('message')(e.target.value)}
                className="field-control h-auto py-3"
              />
            </Field>
            <div className="sm:col-span-2">
              <Button type="submit" block>
                <Icon name="mail" className="size-5" />
                {t('bizLanding.form.send')}
              </Button>
              <p className="mt-3 text-center text-xs text-muted">{t('bizLanding.form.note')}</p>
            </div>
          </form>
        </Card>
      </section>
    </SiteLayout>
  )
}
