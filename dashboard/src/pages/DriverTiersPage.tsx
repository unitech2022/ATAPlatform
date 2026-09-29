import { useState, type FormEvent } from 'react'
import { MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input, Textarea } from '../components/Field'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { PageSpinner } from '../components/Spinner'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { driverTiers } from '../lib/admin'
import { formatDateTime, formatNumber } from '../lib/format'
import { DRIVER_TIERS, effectiveDriverShare, formatRatio, TIER_PERIOD_DAYS, validateTierRule, type TierErrorField } from '../lib/rewards'
import { driverTierMeta } from '../lib/status'
import type { DriverTierRule, DriverTierRuleInput } from '../lib/types'

const TIER_BAR: Record<string, string> = { bronze: 'bg-muted', silver: 'bg-ink', gold: 'bg-amber-500', platinum: 'bg-brand' }

/** Driver tiers (§F15.7): the four rule rows as an editable ladder, recalculation trigger and tier distribution. */
export function DriverTiersPage() {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => driverTiers.rules(), 'driver-tier-rules')
  const [confirming, setConfirming] = useState(false)
  const rows = [...(query.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || DRIVER_TIERS.indexOf(a.tier) - DRIVER_TIERS.indexOf(b.tier))
  const counted = rows.filter((row) => typeof row.driversCount === 'number')
  const totalDrivers = counted.reduce((sum, row) => sum + (row.driversCount ?? 0), 0)

  const recalculate = async () => {
    try {
      await driverTiers.recalculate()
      toast.success(t('tierRecalcQueued'), t('tierRecalcQueuedCopy'))
      setConfirming(false)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  return (
    <>
      <PageHeader
        title={t('tierTitle')}
        description={t('tierCopy')}
        actions={
          <Button icon="refresh" onClick={() => setConfirming(true)}>
            {t('tierRecalculate')}
          </Button>
        }
      />

      {counted.length > 0 && totalDrivers > 0 && (
        <Card title={t('tierDistribution')} className="mb-6">
          <div className="flex h-3 w-full overflow-hidden rounded-full bg-line" role="presentation">
            {counted.map((row) => (
              <span key={row.tier} className={`h-full ${TIER_BAR[row.tier] ?? 'bg-muted'}`} style={{ width: `${((row.driversCount ?? 0) / totalDrivers) * 100}%` }} />
            ))}
          </div>
          <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
            {counted.map((row) => (
              <div key={row.tier} className="rounded-2xl bg-cloud px-4 py-3">
                <MetaBadge record={driverTierMeta} value={row.tier} />
                <p className="ltr-nums mt-2 text-xl font-bold">{formatNumber(row.driversCount)}</p>
                <p className="ltr-nums text-xs text-muted">{formatRatio((row.driversCount ?? 0) / totalDrivers)}</p>
              </div>
            ))}
          </div>
        </Card>
      )}

      <p className="mb-4 flex items-start gap-2 text-sm text-muted">
        <Icon name="info" className="mt-0.5 size-4 shrink-0" />
        {t('tierLadderHint').replace('{days}', String(TIER_PERIOD_DAYS))}
      </p>

      {query.loading && !query.data ? (
        <PageSpinner />
      ) : query.error ? (
        <Card>
          <ErrorState error={query.error} onRetry={query.reload} />
        </Card>
      ) : rows.length === 0 ? (
        <Card>
          <p className="text-sm text-muted">{t('tierNoRules')}</p>
        </Card>
      ) : (
        <ol className="space-y-4">
          {rows.map((row, index) => (
            <TierRuleRow key={`${row.id}:${row.updatedAt ?? ''}`} rule={row} previous={rows[index - 1] ?? null} onSaved={query.reload} />
          ))}
        </ol>
      )}

      <ConfirmModal
        open={confirming}
        title={t('tierRecalcTitle')}
        description={t('tierRecalcCopy')}
        confirmLabel={t('tierRecalculate')}
        confirmVariant="primary"
        onClose={() => setConfirming(false)}
        onConfirm={recalculate}
      />
    </>
  )
}

type TierForm = Record<'minCompletedTrips' | 'minRatingAvg' | 'minAcceptancePercent' | 'maxCancellationPercent' | 'commissionDiscountPercent' | 'matchingNorm' | 'benefitsAr' | 'benefitsEn' | 'sortOrder', string>

const pct = (value: number) => String(Math.round(value * 10000) / 100)

function toForm(rule: DriverTierRule): TierForm {
  return {
    minCompletedTrips: String(rule.minCompletedTrips),
    minRatingAvg: String(rule.minRatingAvg),
    minAcceptancePercent: pct(rule.minAcceptanceRate),
    maxCancellationPercent: pct(rule.maxCancellationRate),
    commissionDiscountPercent: String(rule.commissionDiscountPercent),
    matchingNorm: String(rule.matchingNorm),
    benefitsAr: rule.benefitsAr ?? '',
    benefitsEn: rule.benefitsEn ?? '',
    sortOrder: String(rule.sortOrder),
  }
}

function toInput(form: TierForm): DriverTierRuleInput {
  const num = (value: string) => (value.trim() === '' ? Number.NaN : Number(value))
  return {
    minCompletedTrips: num(form.minCompletedTrips),
    minRatingAvg: num(form.minRatingAvg),
    minAcceptanceRate: Math.round(num(form.minAcceptancePercent) * 100) / 10000,
    maxCancellationRate: Math.round(num(form.maxCancellationPercent) * 100) / 10000,
    commissionDiscountPercent: num(form.commissionDiscountPercent),
    matchingNorm: num(form.matchingNorm),
    benefitsAr: form.benefitsAr.trim() || null,
    benefitsEn: form.benefitsEn.trim() || null,
    sortOrder: Number(form.sortOrder || 0),
  }
}

/** A higher tier should never be easier to reach than the one below it. */
function weakerThanPrevious(input: DriverTierRuleInput, previous: DriverTierRule | null) {
  if (!previous) return false
  return (
    input.minCompletedTrips < previous.minCompletedTrips ||
    input.minRatingAvg < previous.minRatingAvg ||
    input.minAcceptanceRate < previous.minAcceptanceRate ||
    input.maxCancellationRate > previous.maxCancellationRate
  )
}

function TierRuleRow({ rule, previous, onSaved }: { rule: DriverTierRule; previous: DriverTierRule | null; onSaved: () => void }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [form, setForm] = useState<TierForm>(() => toForm(rule))
  const [errors, setErrors] = useState<Partial<Record<TierErrorField, TranslationKey>>>({})
  const [saving, setSaving] = useState(false)
  const input = toInput(form)
  const dirty = JSON.stringify(form) !== JSON.stringify(toForm(rule))
  const weaker = weakerThanPrevious(input, previous)

  const set = (key: keyof TierForm, value: string) => {
    setForm((current) => ({ ...current, [key]: value }))
    setErrors({})
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const found = validateTierRule(input)
    setErrors(found)
    if (Object.keys(found).length > 0) return
    setSaving(true)
    try {
      await driverTiers.updateRule(rule.id, input)
      toast.success(t('tierSaved'))
      onSaved()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const err = (key: TierErrorField) => (errors[key] ? t(errors[key]) : undefined)
  const share = Number.isFinite(input.commissionDiscountPercent) ? effectiveDriverShare(80, input.commissionDiscountPercent) : null

  return (
    <li>
      <Card>
        <form onSubmit={save} noValidate>
          <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <span className="grid size-11 place-items-center rounded-xl bg-cloud text-ink">
                <Icon name="trophy" className="size-5" />
              </span>
              <div>
                <MetaBadge record={driverTierMeta} value={rule.tier} />
                {rule.updatedAt && (
                  <p className="mt-1 text-xs text-muted">
                    {t('tierUpdatedAt')}: {formatDateTime(rule.updatedAt, lang)}
                  </p>
                )}
              </div>
            </div>
            <div className="flex items-center gap-2">
              {dirty && (
                <Button variant="ghost" size="sm" onClick={() => setForm(toForm(rule))} disabled={saving}>
                  {t('reset')}
                </Button>
              )}
              <Button type="submit" size="sm" icon="check" loading={saving} disabled={!dirty}>
                {t('save')}
              </Button>
            </div>
          </div>
          <p className="mb-3 text-xs font-bold text-muted">{t('tierCriteria')}</p>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Input id={`${rule.id}-trips`} type="number" min={0} step={1} dir="ltr" label={t('tierMinTrips')} value={form.minCompletedTrips} error={err('minCompletedTrips')} onChange={(event) => set('minCompletedTrips', event.target.value)} />
            <Input id={`${rule.id}-rating`} type="number" min={0} max={5} step="0.01" dir="ltr" label={t('tierMinRating')} value={form.minRatingAvg} error={err('minRatingAvg')} onChange={(event) => set('minRatingAvg', event.target.value)} />
            <Input id={`${rule.id}-accept`} type="number" min={0} max={100} step="0.5" dir="ltr" label={t('tierMinAcceptance')} value={form.minAcceptancePercent} error={err('minAcceptanceRate')} onChange={(event) => set('minAcceptancePercent', event.target.value)} />
            <Input id={`${rule.id}-cancel`} type="number" min={0} max={100} step="0.5" dir="ltr" label={t('tierMaxCancellation')} value={form.maxCancellationPercent} error={err('maxCancellationRate')} onChange={(event) => set('maxCancellationPercent', event.target.value)} />
          </div>
          <p className="mb-3 mt-5 text-xs font-bold text-muted">{t('tierBenefits')}</p>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Input
              id={`${rule.id}-commission`}
              type="number"
              min={0}
              max={100}
              step="0.5"
              dir="ltr"
              label={t('tierCommissionDiscount')}
              hint={share !== null ? t('tierShareExample').replace('{share}', String(Math.round(share * 100) / 100)) : undefined}
              value={form.commissionDiscountPercent}
              error={err('commissionDiscountPercent')}
              onChange={(event) => set('commissionDiscountPercent', event.target.value)}
            />
            <Input id={`${rule.id}-norm`} type="number" min={0} max={1} step="0.05" dir="ltr" label={t('tierMatchingNorm')} hint={t('tierMatchingNormHint')} value={form.matchingNorm} error={err('matchingNorm')} onChange={(event) => set('matchingNorm', event.target.value)} />
            <Input id={`${rule.id}-sort`} type="number" step={1} dir="ltr" label={t('sortOrder')} value={form.sortOrder} onChange={(event) => set('sortOrder', event.target.value)} wrapperClassName="lg:col-span-2" />
            <Textarea id={`${rule.id}-benefits-ar`} label={t('tierBenefitsAr')} value={form.benefitsAr} maxLength={500} onChange={(event) => set('benefitsAr', event.target.value)} className="min-h-20" wrapperClassName="sm:col-span-2" />
            <Textarea id={`${rule.id}-benefits-en`} dir="ltr" label={t('tierBenefitsEn')} value={form.benefitsEn} maxLength={500} onChange={(event) => set('benefitsEn', event.target.value)} className="min-h-20" wrapperClassName="sm:col-span-2" />
          </div>
          {weaker && (
            <p className="mt-4 flex items-start gap-2 rounded-2xl bg-amber-50 px-4 py-3 text-sm text-amber-700">
              <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
              {t('tierWeakerThanPrevious')}
            </p>
          )}
        </form>
      </Card>
    </li>
  )
}
