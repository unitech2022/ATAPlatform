import { useMemo, useState, type FormEvent } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Input, Select, Toggle } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { Money } from '../components/Money'
import { PageHeader } from '../components/PageHeader'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import type { TranslationKey } from '../i18n'
import { cancellationRules, rideCategories, zones } from '../lib/admin'
import {
  ACTOR_KEY,
  BOOKING_TYPE_KEY,
  BOOKING_TYPES,
  FEE_TYPE_KEY,
  FEE_TYPES,
  formatWindow,
  optionalNumber,
  RULE_ACTORS,
  RULE_STAGES,
  STAGE_KEY,
  validateRule,
  type RuleErrorField,
} from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { formatMoney, formatNumber } from '../lib/format'
import type { BookingType, CancellationFeeType, CancellationRule, CancellationRuleInput, CancellationSimulateResult, RuleActor, RuleStage } from '../lib/types'

type FormState = {
  name: string
  actor: RuleActor
  stage: RuleStage
  bookingType: BookingType | ''
  rideCategoryId: string
  zoneId: string
  freeWindowSeconds: string
  feeType: CancellationFeeType
  feeAmount: string
  feePercent: string
  minFee: string
  maxFee: string
  driverCompensationPercent: string
  penaltyPoints: string
  priority: string
  isActive: boolean
}

const EMPTY: FormState = {
  name: '',
  actor: 'passenger',
  stage: 'after_accept',
  bookingType: '',
  rideCategoryId: '',
  zoneId: '',
  freeWindowSeconds: '120',
  feeType: 'fixed',
  feeAmount: '5',
  feePercent: '',
  minFee: '',
  maxFee: '',
  driverCompensationPercent: '50',
  penaltyPoints: '1',
  priority: '0',
  isActive: true,
}

const str = (value: number | null) => (value === null ? '' : String(value))

function toForm(rule: CancellationRule): FormState {
  return {
    name: rule.name,
    actor: rule.actor,
    stage: rule.stage,
    bookingType: rule.bookingType ?? '',
    rideCategoryId: rule.rideCategoryId ?? '',
    zoneId: rule.zoneId ?? '',
    freeWindowSeconds: String(rule.freeWindowSeconds),
    feeType: rule.feeType,
    feeAmount: str(rule.feeAmount),
    feePercent: str(rule.feePercent),
    minFee: str(rule.minFee),
    maxFee: str(rule.maxFee),
    driverCompensationPercent: String(rule.driverCompensationPercent),
    penaltyPoints: String(rule.penaltyPoints),
    priority: String(rule.priority),
    isActive: rule.isActive,
  }
}

function toInput(form: FormState): CancellationRuleInput {
  const passengerFee = form.feeType !== 'none'
  return {
    name: form.name.trim(),
    actor: form.actor,
    stage: form.stage,
    bookingType: form.bookingType || null,
    rideCategoryId: form.rideCategoryId || null,
    zoneId: form.zoneId || null,
    freeWindowSeconds: Number(form.freeWindowSeconds || 0),
    feeType: form.feeType,
    feeAmount: form.feeType === 'fixed' ? optionalNumber(form.feeAmount) : null,
    feePercent: form.feeType === 'percent' ? optionalNumber(form.feePercent) : null,
    minFee: passengerFee ? optionalNumber(form.minFee) : null,
    maxFee: passengerFee ? optionalNumber(form.maxFee) : null,
    driverCompensationPercent: form.actor === 'passenger' ? Number(form.driverCompensationPercent || 0) : 0,
    penaltyPoints: Number(form.penaltyPoints || 0),
    priority: Number(form.priority || 0),
    isActive: form.isActive,
  }
}

type Editing = { mode: 'create' } | { mode: 'edit'; rule: CancellationRule } | null

/** Cancellation rules (`/admin/cancellation-rules`, §F14.3) with filters, a full form and the fee simulator. */
export function CancellationRulesPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter } = useUrlState()
  const actor = parseEnum(params.get('actor'), RULE_ACTORS)
  const stage = parseEnum(params.get('stage'), RULE_STAGES)
  const bookingType = parseEnum(params.get('bookingType'), BOOKING_TYPES)

  const query = useQuery(() => cancellationRules.list({ actor, stage, bookingType }), `cancellation-rules:${actor}:${stage}:${bookingType}`)
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data])
  const categoryName = (id: string | null) => {
    const category = id ? categories.find((item) => item.id === id) : null
    return category ? (lang === 'ar' ? category.nameAr : category.nameEn) : null
  }
  const zoneName = (id: string | null) => {
    const zone = id ? zoneList.find((item) => item.id === id) : null
    return zone ? (lang === 'ar' ? zone.nameAr : zone.nameEn) : null
  }

  // Filters are sent to the API and applied again client-side in case the server ignores them.
  const rows = (query.data ?? [])
    .filter((rule) => (!actor || rule.actor === actor) && (!stage || rule.stage === stage) && (!bookingType || rule.bookingType === bookingType || rule.bookingType === null))
    .sort((a, b) => a.actor.localeCompare(b.actor) || RULE_STAGES.indexOf(a.stage) - RULE_STAGES.indexOf(b.stage) || b.priority - a.priority)

  const [editing, setEditing] = useState<Editing>(null)
  const [form, setForm] = useState<FormState>(EMPTY)
  const [errors, setErrors] = useState<Partial<Record<RuleErrorField, TranslationKey>>>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<CancellationRule | null>(null)

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => {
      const next = { ...current, [key]: value }
      // §F14.3: no passenger fee before a driver accepts.
      if (next.actor === 'passenger' && next.stage === 'before_accept') next.feeType = 'none'
      if (next.actor === 'driver' && next.stage === 'no_show') next.stage = 'after_accept'
      return next
    })
    setErrors({})
  }

  const openForm = (next: Editing) => {
    setForm(next?.mode === 'edit' ? toForm(next.rule) : { ...EMPTY, actor: actor || 'passenger', stage: stage || 'after_accept' })
    setErrors({})
    setEditing(next)
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!editing) return
    const input = toInput(form)
    const found = validateRule(input)
    for (const key of ['feeAmount', 'feePercent', 'minFee', 'maxFee'] as const) {
      const value = input[key]
      if (value !== null && Number.isNaN(value)) found[key] = 'invalidNumber'
    }
    setErrors(found)
    if (Object.keys(found).length > 0) return
    setSaving(true)
    try {
      if (editing.mode === 'create') await cancellationRules.create(input)
      else await cancellationRules.update(editing.rule.id, input)
      toast.success(t('ruleSaved'))
      setEditing(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setSaving(false)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await cancellationRules.remove(deleting.id)
      toast.success(t('ruleDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const feeText = (rule: CancellationRule) => {
    const bounds = [rule.minFee !== null ? `≥ ${formatMoney(rule.minFee)}` : '', rule.maxFee !== null ? `≤ ${formatMoney(rule.maxFee)}` : ''].filter(Boolean).join(' · ')
    switch (rule.feeType) {
      case 'fixed':
        return { main: `${formatMoney(rule.feeAmount)} ${t('sar')}`, sub: bounds }
      case 'percent':
        return { main: `${formatNumber(rule.feePercent)}%`, sub: bounds }
      case 'pricing_rule':
        return { main: t('cxFeeTypePricingRule'), sub: bounds }
      default:
        return { main: t('cxFeeTypeNone'), sub: '' }
    }
  }

  const columns: Column<CancellationRule>[] = [
    {
      key: 'name',
      header: t('ruleName'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.name}</span>
          <span className="block text-xs text-muted">
            {t(ACTOR_KEY[row.actor])} · {t(STAGE_KEY[row.stage] ?? 'cxStageAfterAccept')}
          </span>
        </span>
      ),
    },
    {
      key: 'scope',
      header: t('scope'),
      render: (row) => (
        <span className="flex max-w-60 flex-wrap gap-1">
          <Badge tone="muted">{row.bookingType ? t(BOOKING_TYPE_KEY[row.bookingType]) : t('cxAnyBooking')}</Badge>
          <Badge tone="muted">{categoryName(row.rideCategoryId) ?? t('allCategories')}</Badge>
          <Badge tone="muted">{zoneName(row.zoneId) ?? t('wholeCity')}</Badge>
        </span>
      ),
    },
    { key: 'window', header: t('cxFreeWindow'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatWindow(row.freeWindowSeconds)}</span> },
    {
      key: 'fee',
      header: t('cxFee'),
      render: (row) => {
        const fee = feeText(row)
        return (
          <span className="block">
            <span className="ltr-nums block font-bold">{fee.main}</span>
            {fee.sub && <span className="ltr-nums block text-xs text-muted">{fee.sub}</span>}
          </span>
        )
      },
    },
    {
      key: 'compensation',
      header: t('cxCompensation'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums">{row.actor === 'passenger' ? `${formatNumber(row.driverCompensationPercent)}%` : '—'}</span>,
    },
    { key: 'points', header: t('cxPoints'), className: 'text-center', render: (row) => <span className="ltr-nums font-bold">{formatNumber(row.penaltyPoints)}</span> },
    { key: 'priority', header: t('priority'), className: 'text-center', render: (row) => <span className="ltr-nums text-muted">{formatNumber(row.priority)}</span> },
    { key: 'active', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => openForm({ mode: 'edit', rule: row })}>
            {t('edit')}
          </Button>
          <Button variant="danger-outline" size="sm" icon="trash" onClick={() => setDeleting(row)} aria-label={t('delete')} />
        </span>
      ),
    },
  ]

  const feeLocked = form.actor === 'passenger' && form.stage === 'before_accept'
  const err = (key: RuleErrorField) => (errors[key] ? t(errors[key]) : undefined)

  return (
    <>
      <PageHeader
        title={t('cxRulesTitle')}
        description={t('cxRulesCopy')}
        actions={
          <Button icon="plus" onClick={() => openForm({ mode: 'create' })}>
            {t('addRule')}
          </Button>
        }
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-3">
        <Select id="rule-actor" aria-label={t('actor')} value={actor} onChange={(event) => setFilter('actor', event.target.value)}>
          <option value="">{t('cxAllActors')}</option>
          {RULE_ACTORS.map((value) => (
            <option key={value} value={value}>
              {t(ACTOR_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="rule-stage" aria-label={t('cxStage')} value={stage} onChange={(event) => setFilter('stage', event.target.value)}>
          <option value="">{t('cxAllStages')}</option>
          {RULE_STAGES.map((value) => (
            <option key={value} value={value}>
              {t(STAGE_KEY[value])}
            </option>
          ))}
        </Select>
        <Select id="rule-booking" aria-label={t('bookingType')} value={bookingType} onChange={(event) => setFilter('bookingType', event.target.value)}>
          <option value="">{t('cxAnyBooking')}</option>
          {BOOKING_TYPES.map((value) => (
            <option key={value} value={value}>
              {t(BOOKING_TYPE_KEY[value])}
            </option>
          ))}
        </Select>
      </div>

      <Card flush className="mb-6">
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('cxNoRules')} emptyDescription="" />
        )}
      </Card>

      <FeeSimulator categories={categories.map((item) => ({ id: item.id, name: lang === 'ar' ? item.nameAr : item.nameEn }))} zones={zoneList.map((item) => ({ id: item.id, name: lang === 'ar' ? item.nameAr : item.nameEn }))} />

      <Modal
        open={editing !== null}
        size="lg"
        title={editing?.mode === 'edit' ? t('editRule') : t('newRule')}
        onClose={() => setEditing(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditing(null)} disabled={saving}>
              {t('cancel')}
            </Button>
            <Button type="submit" form="cancellation-rule-form" loading={saving}>
              {t('save')}
            </Button>
          </>
        }
      >
        <form id="cancellation-rule-form" onSubmit={save} noValidate className="grid gap-4 sm:grid-cols-2">
          <Input id="cr-name" label={t('ruleName')} value={form.name} error={err('name')} onChange={(event) => set('name', event.target.value)} wrapperClassName="sm:col-span-2" />
          <Select id="cr-actor" label={t('actor')} value={form.actor} onChange={(event) => set('actor', event.target.value as RuleActor)}>
            {RULE_ACTORS.map((value) => (
              <option key={value} value={value}>
                {t(ACTOR_KEY[value])}
              </option>
            ))}
          </Select>
          <Select id="cr-stage" label={t('cxStage')} value={form.stage} error={err('stage')} onChange={(event) => set('stage', event.target.value as RuleStage)}>
            {RULE_STAGES.filter((value) => form.actor === 'passenger' || value !== 'no_show').map((value) => (
              <option key={value} value={value}>
                {t(STAGE_KEY[value])}
              </option>
            ))}
          </Select>
          <Select id="cr-booking" label={t('bookingType')} value={form.bookingType} onChange={(event) => set('bookingType', event.target.value as BookingType | '')}>
            <option value="">{t('cxAnyBooking')}</option>
            {BOOKING_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(BOOKING_TYPE_KEY[value])}
              </option>
            ))}
          </Select>
          <Select id="cr-category" label={t('rideCategory')} value={form.rideCategoryId} onChange={(event) => set('rideCategoryId', event.target.value)}>
            <option value="">{t('allCategories')}</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {lang === 'ar' ? category.nameAr : category.nameEn}
              </option>
            ))}
          </Select>
          <Select id="cr-zone" label={t('pickupZone')} value={form.zoneId} onChange={(event) => set('zoneId', event.target.value)}>
            <option value="">{t('wholeCity')}</option>
            {zoneList.map((zone) => (
              <option key={zone.id} value={zone.id}>
                {lang === 'ar' ? zone.nameAr : zone.nameEn}
              </option>
            ))}
          </Select>
          <Input
            id="cr-window"
            type="number"
            min={0}
            step={10}
            dir="ltr"
            label={t('cxFreeWindowSeconds')}
            hint={`${formatWindow(Number(form.freeWindowSeconds) || 0)} ${t('min')}`}
            value={form.freeWindowSeconds}
            error={err('freeWindowSeconds')}
            onChange={(event) => set('freeWindowSeconds', event.target.value)}
          />

          <div className="rounded-2xl border border-line p-4 sm:col-span-2">
            <div className="grid gap-4 sm:grid-cols-2">
              <Select
                id="cr-fee-type"
                label={t('cxFeeType')}
                value={form.feeType}
                error={err('feeType')}
                disabled={feeLocked}
                hint={feeLocked ? t('cxErrNoFeeBeforeAccept') : undefined}
                onChange={(event) => set('feeType', event.target.value as CancellationFeeType)}
              >
                {FEE_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {t(FEE_TYPE_KEY[value])}
                  </option>
                ))}
              </Select>
              {form.feeType === 'fixed' && (
                <Input id="cr-fee-amount" type="number" min={0} step="0.01" dir="ltr" label={`${t('amount')} (${t('sar')})`} value={form.feeAmount} error={err('feeAmount')} onChange={(event) => set('feeAmount', event.target.value)} />
              )}
              {form.feeType === 'percent' && (
                <Input id="cr-fee-percent" type="number" min={0} max={100} step="0.5" dir="ltr" label={t('cxFeePercent')} hint={t('cxFeePercentHint')} value={form.feePercent} error={err('feePercent')} onChange={(event) => set('feePercent', event.target.value)} />
              )}
              {form.feeType === 'pricing_rule' && <p className="self-end rounded-2xl bg-cloud px-4 py-3 text-xs text-muted">{t('cxPricingRuleHint')}</p>}
              {form.feeType !== 'none' && (
                <>
                  <Input id="cr-min-fee" type="number" min={0} step="0.01" dir="ltr" label={t('cxMinFee')} value={form.minFee} error={err('minFee')} onChange={(event) => set('minFee', event.target.value)} />
                  <Input id="cr-max-fee" type="number" min={0} step="0.01" dir="ltr" label={t('cxMaxFee')} value={form.maxFee} error={err('maxFee')} onChange={(event) => set('maxFee', event.target.value)} />
                </>
              )}
            </div>
          </div>

          {form.actor === 'passenger' && (
            <Input
              id="cr-compensation"
              type="number"
              min={0}
              max={100}
              dir="ltr"
              label={t('cxCompensationPercent')}
              hint={t('cxCompensationHint')}
              value={form.driverCompensationPercent}
              error={err('driverCompensationPercent')}
              onChange={(event) => set('driverCompensationPercent', event.target.value)}
            />
          )}
          <Input id="cr-points" type="number" min={0} step={1} dir="ltr" label={t('cxPenaltyPoints')} value={form.penaltyPoints} error={err('penaltyPoints')} onChange={(event) => set('penaltyPoints', event.target.value)} />
          <Input id="cr-priority" type="number" step={1} dir="ltr" label={t('priority')} hint={t('cxPriorityHint')} value={form.priority} onChange={(event) => set('priority', event.target.value)} />
          <div className="self-end">
            <Toggle checked={form.isActive} onChange={(value) => set('isActive', value)} label={t('isActive')} />
          </div>
        </form>
      </Modal>

      <ConfirmModal
        open={deleting !== null}
        title={t('deleteRule')}
        description={deleting ? `${deleting.name} — ${t('cxDeleteRuleCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}

function FeeSimulator({ categories, zones: zoneOptions }: { categories: { id: string; name: string }[]; zones: { id: string; name: string }[] }) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [actor, setActor] = useState<RuleActor>('passenger')
  const [stage, setStage] = useState<RuleStage>('en_route')
  const [bookingType, setBookingType] = useState<BookingType>('now')
  const [rideCategoryId, setRideCategoryId] = useState('')
  const [zoneId, setZoneId] = useState('')
  const [seconds, setSeconds] = useState('180')
  const [fare, setFare] = useState('35')
  const [result, setResult] = useState<CancellationSimulateResult | null>(null)
  const [running, setRunning] = useState(false)

  const run = async (event: FormEvent) => {
    event.preventDefault()
    const secondsSinceAnchor = Number(seconds)
    const estimatedFare = Number(fare)
    if (!Number.isFinite(secondsSinceAnchor) || secondsSinceAnchor < 0 || !Number.isFinite(estimatedFare) || estimatedFare < 0) {
      toast.error(t('invalidNumber'))
      return
    }
    setRunning(true)
    try {
      setResult(
        await cancellationRules.simulate({
          actor,
          stage,
          bookingType,
          rideCategoryId: rideCategoryId || undefined,
          zoneId: zoneId || undefined,
          secondsSinceAnchor,
          estimatedFare,
        }),
      )
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setRunning(false)
    }
  }

  return (
    <Card title={t('cxSimulatorTitle')} description={t('cxSimulatorCopy')}>
      <div className="grid gap-6 lg:grid-cols-3">
        <form onSubmit={run} noValidate className="grid gap-3 sm:grid-cols-2 lg:col-span-2">
          <Select id="sim-actor" label={t('actor')} value={actor} onChange={(event) => setActor(event.target.value as RuleActor)}>
            {RULE_ACTORS.map((value) => (
              <option key={value} value={value}>
                {t(ACTOR_KEY[value])}
              </option>
            ))}
          </Select>
          <Select id="sim-stage" label={t('cxStage')} value={stage} onChange={(event) => setStage(event.target.value as RuleStage)}>
            {RULE_STAGES.map((value) => (
              <option key={value} value={value}>
                {t(STAGE_KEY[value])}
              </option>
            ))}
          </Select>
          <Select id="sim-booking" label={t('bookingType')} value={bookingType} onChange={(event) => setBookingType(event.target.value as BookingType)}>
            {BOOKING_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(BOOKING_TYPE_KEY[value])}
              </option>
            ))}
          </Select>
          <Select id="sim-category" label={t('rideCategory')} value={rideCategoryId} onChange={(event) => setRideCategoryId(event.target.value)}>
            <option value="">{t('allCategories')}</option>
            {categories.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </Select>
          <Select id="sim-zone" label={t('pickupZone')} value={zoneId} onChange={(event) => setZoneId(event.target.value)}>
            <option value="">{t('wholeCity')}</option>
            {zoneOptions.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </Select>
          <Input id="sim-seconds" type="number" min={0} dir="ltr" label={t('cxSecondsSinceAnchor')} hint={`${formatWindow(Number(seconds) || 0)} ${t('min')}`} value={seconds} onChange={(event) => setSeconds(event.target.value)} />
          <Input id="sim-fare" type="number" min={0} step="0.01" dir="ltr" label={`${t('estimatedFare')} (${t('sar')})`} value={fare} onChange={(event) => setFare(event.target.value)} />
          <div className="self-end">
            <Button type="submit" icon="play" loading={running} className="w-full">
              {t('cxRunSimulation')}
            </Button>
          </div>
        </form>
        <div className="rounded-2xl bg-cloud p-4">
          {result ? (
            <div className="space-y-3">
              <p className="flex items-center gap-2 text-sm font-bold">
                <Icon name={result.isFree ? 'check' : 'tag'} className={`size-4 ${result.isFree ? 'text-brand' : 'text-danger'}`} />
                {result.isFree ? t('cxSimFree') : t('cxSimCharged')}
              </p>
              <dl className="space-y-2 text-sm">
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t('cxMatchedRule')}</dt>
                  <dd className="text-end font-bold">{result.ruleName ?? t('cxNoRuleMatched')}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t('cxFee')}</dt>
                  <dd>
                    <Money value={result.fee} strong />
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t('cxCompensation')}</dt>
                  <dd>
                    <Money value={result.compensation} />
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t('cxPoints')}</dt>
                  <dd className="ltr-nums font-bold">{formatNumber(result.penaltyPoints)}</dd>
                </div>
              </dl>
            </div>
          ) : (
            <p className="text-sm text-muted">{t('cxSimEmpty')}</p>
          )}
        </div>
      </div>
    </Card>
  )
}
