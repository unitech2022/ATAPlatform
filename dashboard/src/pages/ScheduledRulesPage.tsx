import { useMemo, useState } from 'react'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { PermissionError } from '../components/PermissionError'
import { ScheduledRuleFormModal } from '../components/ScheduledRuleFormModal'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { catalog, rideCategories, scheduledRules } from '../lib/admin'
import { formatMoney, formatNumber } from '../lib/format'
import { localName } from '../lib/pricing'
import { formatOffsetLabel, ruleScopeRank, SCHEDULED_FEE_TYPE_KEY, scheduledRuleInputOf } from '../lib/scheduling'
import type { ScheduledRule } from '../lib/types'

/** Scheduled ride rules (§F17.2 `scheduled_ride_rules`): general / city / category / city + category, CRUD (`scheduling.manage`). */
export function ScheduledRulesPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => scheduledRules.list(), 'scheduled-rules')
  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const citiesQuery = useQuery(() => catalog.cities(), 'catalog-cities')
  const [editing, setEditing] = useState<ScheduledRule | 'new' | null>(null)
  const [deleting, setDeleting] = useState<ScheduledRule | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const rows = useMemo(
    () => [...(query.data ?? [])].sort((a, b) => ruleScopeRank(a) - ruleScopeRank(b) || Number(b.isActive) - Number(a.isActive)),
    [query.data],
  )
  const categoryNames = useMemo(() => new Map((categoriesQuery.data ?? []).map((category) => [category.id, localName(category, lang)])), [categoriesQuery.data, lang])
  const cityNames = useMemo(() => new Map((citiesQuery.data ?? []).map((city) => [city.id, city.name])), [citiesQuery.data])
  const hasGeneral = rows.some((row) => row.isActive && !row.cityId && !row.rideCategoryId)

  const setActive = async (row: ScheduledRule, isActive: boolean) => {
    setBusyId(row.id)
    try {
      await scheduledRules.update(row.id, scheduledRuleInputOf(row, isActive))
      toast.success(t(isActive ? 'sdRuleActivated' : 'sdRuleDeactivated'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyId(null)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await scheduledRules.remove(deleting.id)
      toast.success(t('sdRuleDeleted'))
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const scopeLabel = (row: ScheduledRule) => {
    if (!row.cityId && !row.rideCategoryId) return t('sdScopeGeneral')
    return [row.cityId ? (cityNames.get(row.cityId) ?? row.cityId) : t('allCities'), row.rideCategoryId ? (categoryNames.get(row.rideCategoryId) ?? row.rideCategoryId) : t('allCategories')].join(' · ')
  }

  const columns: Column<ScheduledRule>[] = [
    {
      key: 'scope',
      header: t('sdScope'),
      render: (row) => (
        <span className="block min-w-36">
          <span className="block font-bold">{scopeLabel(row)}</span>
          {!row.cityId && !row.rideCategoryId && <Badge tone="ink">{t('sdScopeGeneralBadge')}</Badge>}
        </span>
      ),
    },
    {
      key: 'window',
      header: t('sdSectionWindow'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">
            {t('sdMaxDaysAhead')}: <span className="ltr-nums font-bold">{formatNumber(row.maxDaysAhead)}</span> {t('sdDays')}
          </span>
          <span className="block text-muted">
            {t('sdMinLead')}: <span className="ltr-nums font-bold text-ink">{formatNumber(row.minLeadMinutes)}</span> {t('min')}
          </span>
          <span className="block text-muted">
            {t('sdMaxOpen')}: <span className="ltr-nums font-bold text-ink">{formatNumber(row.maxOpenPerPassenger)}</span>
          </span>
        </span>
      ),
    },
    {
      key: 'timeline',
      header: t('sdSectionConfirmations'),
      render: (row) => (
        <span className="ltr-nums block whitespace-nowrap text-xs">
          <span className="block">
            <span className="text-muted">{t('sdStepFirstConfirm')}:</span> <b>T−{formatOffsetLabel(row.driverAssignmentLeadMinutes)}</b>
          </span>
          <span className="block">
            <span className="text-muted">{t('sdStepFinalConfirm')}:</span> <b>T−{formatOffsetLabel(row.finalConfirmationMinutesBefore)}</b>
          </span>
          <span className="block">
            <span className="text-muted">{t('sdStepSearchStart')}:</span> <b>T−{formatOffsetLabel(row.searchStartMinutesBefore)}</b>
          </span>
        </span>
      ),
    },
    {
      key: 'reminders',
      header: t('sdSectionReminders'),
      render: (row) => (
        <span className="ltr-nums block whitespace-nowrap text-xs">
          <span className="block">
            <span className="text-muted">{t('sdRiderReminders')}:</span> {row.riderReminderOffsets.map(formatOffsetLabel).join(' · ') || '—'}
          </span>
          <span className="block">
            <span className="text-muted">{t('sdDriverReminders')}:</span> {row.driverReminderOffsets.map(formatOffsetLabel).join(' · ') || '—'}
          </span>
        </span>
      ),
    },
    {
      key: 'cancel',
      header: t('sdSectionCancellation'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">
            {t('sdFreeCancel')}: <span className="ltr-nums font-bold">{formatNumber(row.freeCancelMinutesBefore)}</span> {t('min')}
          </span>
          <span className="block text-muted">
            {t(SCHEDULED_FEE_TYPE_KEY[row.lateCancelFeeType])}
            {row.lateCancelFeeType === 'fixed' && row.lateCancelFeeAmount !== null && (
              <>
                {': '}
                <span className="ltr-nums font-bold text-ink">{formatMoney(row.lateCancelFeeAmount)}</span> {t('sar')}
              </>
            )}
            {row.lateCancelFeeType === 'percent' && row.lateCancelFeePercent !== null && (
              <>
                {': '}
                <span className="ltr-nums font-bold text-ink">{formatNumber(row.lateCancelFeePercent)}%</span>
              </>
            )}
          </span>
        </span>
      ),
    },
    {
      key: 'penalties',
      header: t('sdSectionPenalties'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs text-muted">
          <span className="block">
            {t('sdNoShowPoints')}: <span className="ltr-nums font-bold text-ink">{formatNumber(row.driverNoShowPenaltyPoints)}</span>
          </span>
          <span className="block">
            {t('sdLateReleasePoints')}: <span className="ltr-nums font-bold text-ink">{formatNumber(row.driverLateReleasePenaltyPoints)}</span>
          </span>
        </span>
      ),
    },
    { key: 'status', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button variant="secondary" size="sm" icon="edit" onClick={() => setEditing(row)}>
            {t('edit')}
          </Button>
          {row.isActive ? (
            <Button variant="danger-outline" size="sm" icon="pause" loading={busyId === row.id} onClick={() => setActive(row, false)}>
              {t('prDeactivate')}
            </Button>
          ) : (
            <Button variant="brand" size="sm" icon="play" loading={busyId === row.id} onClick={() => setActive(row, true)}>
              {t('prActivate')}
            </Button>
          )}
          <Button variant="danger-outline" size="sm" icon="trash" aria-label={t('delete')} onClick={() => setDeleting(row)}>
            {t('delete')}
          </Button>
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('sdRulesTitle')}
        description={t('sdRulesCopy')}
        actions={
          <Button icon="plus" onClick={() => setEditing('new')}>
            {t('sdRuleNew')}
          </Button>
        }
      />

      {query.data && !hasGeneral && (
        <p className="mb-4 flex items-start gap-2 rounded-2xl bg-amber-50 px-4 py-3 text-sm font-bold text-amber-800">
          <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
          {t('sdNoGeneralRule')}
        </p>
      )}

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="scheduling.manage" onRetry={query.reload} />
        ) : (
          <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading && !query.data} emptyTitle={t('sdRulesEmpty')} emptyDescription="" />
        )}
      </Card>

      <ScheduledRuleFormModal
        open={editing !== null}
        rule={editing === 'new' ? null : editing}
        rules={rows}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          toast.success(t('sdRuleSaved'))
          query.reload()
        }}
      />

      <ConfirmModal
        open={deleting !== null}
        title={t('sdRuleDeleteTitle')}
        description={deleting ? `${scopeLabel(deleting)} — ${t('sdRuleDeleteCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
