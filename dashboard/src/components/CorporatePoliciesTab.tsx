import { useMemo, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { corporateAccounts, rideCategories, zones } from '../lib/admin'
import { CORPORATE_ZONE_MATCH_KEY } from '../lib/corporate'
import { localName, weekdayKey } from '../lib/pricing'
import type { CorporatePolicy } from '../lib/types'
import { Badge } from './Badge'
import { Card } from './Card'
import { EmptyState } from './EmptyState'
import { Money } from './Money'
import { PermissionError } from './PermissionError'
import { PageSpinner } from './Spinner'

function Chips({ items, empty }: { items: string[] | null; empty: string }) {
  if (items === null || items.length === 0) return <p className="text-sm text-muted">{empty}</p>
  return (
    <div className="flex flex-wrap gap-1.5">
      {items.map((item) => (
        <span key={item} className="rounded-full bg-cloud px-3 py-1 text-xs font-bold">
          {item}
        </span>
      ))}
    </div>
  )
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="min-w-0">
      <p className="mb-1.5 text-xs font-bold text-muted">{label}</p>
      {children}
    </div>
  )
}

/** Read-only view of the company policies (§F19.7 "السياسات (للقراءة)"); categories and zones are resolved to names when readable. */
export function CorporatePoliciesTab({ accountId }: { accountId: string }) {
  const { t, lang } = useLang()
  const policies = useQuery(() => corporateAccounts.policies(accountId), `corporate-policies:${accountId}`)
  const categories = useQuery(() => rideCategories.list(), 'ride-categories')
  const zoneList = useQuery(() => zones.list(), 'zones')
  const categoryNames = useMemo(() => new Map((categories.data ?? []).map((category) => [category.id, localName(category, lang)])), [categories.data, lang])
  const zoneNames = useMemo(() => new Map((zoneList.data ?? []).map((zone) => [zone.id, localName(zone, lang)])), [zoneList.data, lang])

  if (policies.loading && !policies.data) return <PageSpinner />
  if (policies.error) {
    return (
      <Card>
        <PermissionError error={policies.error} permission="corporate.manage" onRetry={policies.reload} />
      </Card>
    )
  }
  const list = policies.data ?? []
  if (list.length === 0) return <Card><EmptyState icon="shield" title={t('coPolEmpty')} /></Card>

  const flag = (value: boolean) => <Badge tone={value ? 'brand' : 'muted'}>{value ? t('yes') : t('no')}</Badge>

  return (
    <div className="space-y-4">
      <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('coPolReadOnly')}</p>
      {list.map((policy: CorporatePolicy) => (
        <Card
          key={policy.id}
          title={policy.name}
          action={
            <span className="flex flex-wrap gap-2">
              {policy.isDefault && <Badge tone="brand">{t('coPolDefault')}</Badge>}
              {!policy.isActive && <Badge tone="muted">{t('coInactive')}</Badge>}
            </span>
          }
        >
          <div className="grid gap-5 md:grid-cols-2">
            <Row label={t('coPolCategories')}>
              <Chips items={policy.allowedRideCategoryIds?.map((id) => categoryNames.get(id) ?? id) ?? null} empty={t('coPolNoLimit')} />
            </Row>
            <Row label={t('coPolDays')}>
              <Chips items={policy.allowedDays ? [...policy.allowedDays].sort((a, b) => a - b).map((day) => t(weekdayKey(day))) : null} empty={t('everyDay')} />
            </Row>
            <Row label={t('coPolWindows')}>
              <Chips items={policy.timeWindows?.map((window) => `${window.from} – ${window.to}`) ?? null} empty={t('coPolNoLimit')} />
            </Row>
            <Row label={`${t('coPolZones')} · ${t(CORPORATE_ZONE_MATCH_KEY[policy.zoneMatch] ?? 'coPolZoneAnd')}`}>
              <Chips items={policy.allowedZoneIds?.map((id) => zoneNames.get(id) ?? id) ?? null} empty={t('allZones')} />
            </Row>
            <Row label={t('coPolMaxFare')}>{policy.maxFarePerTrip === null ? <p className="text-sm text-muted">{t('coPolNoLimit')}</p> : <Money value={policy.maxFarePerTrip} strong />}</Row>
            <Row label={t('coPolBudget')}>{policy.monthlyBudgetPerEmployee === null ? <p className="text-sm text-muted">{t('coPolNoLimit')}</p> : <Money value={policy.monthlyBudgetPerEmployee} strong />}</Row>
          </div>
          <div className="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {[
              { label: t('coPolRequirePurpose'), value: policy.requirePurpose },
              { label: t('coPolRequireCostCenter'), value: policy.requireCostCenter },
              { label: t('coPolAllowScheduled'), value: policy.allowScheduled },
              { label: t('coPolAllowGuest'), value: policy.allowGuestBooking },
            ].map((item) => (
              <div key={item.label} className="flex items-center justify-between gap-2 rounded-2xl bg-cloud px-4 py-3">
                <span className="text-xs font-bold text-muted">{item.label}</span>
                {flag(item.value)}
              </div>
            ))}
          </div>
        </Card>
      ))}
    </div>
  )
}
