import { useLang } from '../context/lang'
import type { QueryState } from '../hooks/useQuery'
import { creditShare, creditUsedOf, receivableOf, usageTone } from '../lib/corporate'
import { formatDate, formatMoney, formatNumber } from '../lib/format'
import type { CorporateAccount, CorporateReceivable } from '../lib/types'
import { Card } from './Card'
import { DefinitionList } from './DefinitionList'
import { Money } from './Money'
import { PermissionError } from './PermissionError'
import { UsageBar } from './UsageBar'

const dash = '—'

/** Company overview: spend vs credit limit (receivables, `payments.view`), budget utilization and the legal data. */
export function CorporateOverviewTab({ account, receivables }: { account: CorporateAccount; receivables: QueryState<CorporateReceivable[]> }) {
  const { t, lang } = useLang()
  const receivable = receivableOf(receivables.data, account.id)
  const summary = account.summary ?? null
  const used = creditUsedOf(receivable) ?? summary?.creditUsed ?? null
  const share = creditShare(used, account.creditLimit)
  const available = used === null ? null : account.creditLimit - used
  const budget = summary?.budgetUtilizationPercent ?? null
  const budgetShare = typeof budget === 'number' ? Math.min(1, Math.max(0, budget / 100)) : null
  const address = account.billingAddress
  const addressText = address ? [address.buildingNumber, address.street, address.district, address.city, address.postalCode, address.additionalNumber].filter(Boolean).join('، ') : ''

  const tile = (label: string, value: number | null | undefined, danger = false) => (
    <div className="rounded-2xl bg-cloud px-4 py-3">
      <p className="text-xs font-bold text-muted">{label}</p>
      <p className={`mt-1 font-bold ${danger && (value ?? 0) > 0 ? 'text-danger' : ''}`}>{typeof value === 'number' ? <Money value={value} strong /> : dash}</p>
    </div>
  )

  return (
    <div className="space-y-6">
      <div className="grid gap-6 lg:grid-cols-2">
        <Card title={t('coSpendTitle')} description={t('coSpendCopy')}>
          <div className="mb-3 flex flex-wrap items-end justify-between gap-2">
            <p className="ltr-nums text-2xl font-bold">
              {used === null ? dash : formatMoney(used)} <span className="text-sm text-muted">/ {formatMoney(account.creditLimit)} {t('sar')}</span>
            </p>
            {share !== null && <span className={`ltr-nums text-sm font-bold ${share >= 1 ? 'text-danger' : 'text-muted'}`}>{Math.round(share * 100)}%</span>}
          </div>
          <UsageBar share={share} tone={usageTone(share)} />
          {available !== null && available < 0 && <p className="mt-2 text-xs font-bold text-danger">{t('coOverCredit')}</p>}
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            {tile(t('coCreditAvailable'), available)}
            {tile(t('coUnbilled'), receivable?.unbilled)}
            {tile(t('coUnpaidInvoices'), receivable?.unpaidInvoices)}
            {tile(t('coOverdueAmount'), receivable?.overdueAmount, true)}
          </div>
          {receivables.error && (
            <div className="mt-4">
              <PermissionError error={receivables.error} permission="payments.view" onRetry={receivables.reload} />
            </div>
          )}
        </Card>

        <Card title={t('coBudgetTitle')} description={t('coBudgetCopy')}>
          <div className="mb-3 flex flex-wrap items-end justify-between gap-2">
            <p className="text-sm font-bold text-muted">{t('coBudgetUtilization')}</p>
            <span className="ltr-nums text-2xl font-bold">{typeof budget === 'number' ? `${formatNumber(Math.round(budget))}%` : dash}</span>
          </div>
          <UsageBar share={budgetShare} tone={usageTone(budgetShare)} />
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            {tile(t('coMtdSpend'), summary?.monthToDate?.spend)}
            <div className="rounded-2xl bg-cloud px-4 py-3">
              <p className="text-xs font-bold text-muted">{t('coMtdTrips')}</p>
              <p className="ltr-nums mt-1 font-bold">{formatNumber(summary?.monthToDate?.trips)}</p>
            </div>
            <div className="rounded-2xl bg-cloud px-4 py-3">
              <p className="text-xs font-bold text-muted">{t('coActiveEmployees')}</p>
              <p className="ltr-nums mt-1 font-bold">{formatNumber(summary?.activeEmployees)}</p>
            </div>
            <div className="rounded-2xl bg-cloud px-4 py-3">
              <p className="text-xs font-bold text-muted">{t('coInvitedEmployees')}</p>
              <p className="ltr-nums mt-1 font-bold">{formatNumber(summary?.invitedEmployees)}</p>
            </div>
          </div>
        </Card>
      </div>

      <Card title={t('coSecLegal')}>
        <DefinitionList
          items={[
            { label: t('coLegalNameAr'), value: account.legalNameAr || dash },
            { label: t('coLegalNameEn'), value: account.legalNameEn || dash, ltr: true },
            { label: t('coCrNumber'), value: account.crNumber, ltr: true },
            { label: t('coVatNumber'), value: account.vatNumber ?? dash, ltr: true },
            { label: t('coAccountNumber'), value: account.accountNumber, ltr: true },
            { label: t('city'), value: account.cityName ?? account.cityId ?? dash },
            { label: t('coBillingEmail'), value: account.billingEmail || dash, ltr: true },
            { label: t('coBillingCycle'), value: t('coBillingMonthly') },
            { label: t('coPaymentTerms'), value: `${formatNumber(account.paymentTermsDays)} ${t('coDays')}` },
            { label: t('coCreditLimit'), value: `${formatMoney(account.creditLimit)} ${t('sar')}`, ltr: true },
            { label: t('coContactName'), value: account.contactName || dash },
            { label: t('coContactPhone'), value: account.contactPhone || dash, ltr: true },
            { label: t('createdAt'), value: formatDate(account.createdAt, lang) },
          ]}
        />
        <div className="mt-3 grid gap-3 sm:grid-cols-2">
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <p className="text-xs font-bold text-muted">{t('coSecAddress')}</p>
            <p className="mt-1 break-words font-bold">{addressText || dash}</p>
          </div>
          <div className="rounded-2xl bg-cloud px-4 py-3">
            <p className="text-xs font-bold text-muted">{t('coNotes')}</p>
            <p className="mt-1 break-words font-bold">{account.notes || dash}</p>
          </div>
        </div>
      </Card>
    </div>
  )
}
