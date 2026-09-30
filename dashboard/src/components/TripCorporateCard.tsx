import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { formatDateTime } from '../lib/format'
import type { TripDetail } from '../lib/types'
import { Badge } from './Badge'
import { Card } from './Card'
import { DefinitionList } from './DefinitionList'
import { Icon } from './Icon'

/** Trip event recorded when the final fare or the budget exceeds the policy (§F19.2). */
const POLICY_EXCEEDED_EVENT = 'corporate_policy_exceeded'

/**
 * Corporate booking block on the trip page (`Trip.corporate`, §F19.4): company, beneficiary (employee or guest), purpose,
 * cost center and the policy applied. Renders nothing for non-corporate trips.
 */
export function TripCorporateCard({ trip }: { trip: TripDetail }) {
  const { t, lang } = useLang()
  const corporate = trip.corporate ?? null
  if (!corporate && trip.paymentMethod !== 'corporate') return null

  const exceeded = trip.events.find((event) => event.type === POLICY_EXCEEDED_EVENT)
  const dash = '—'
  const company = corporate?.companyName ?? dash
  const beneficiary = corporate?.isGuest ? (corporate.guestName ?? dash) : (corporate?.employeeName ?? trip.passenger?.fullName ?? dash)

  return (
    <Card
      title={t('coTripCardTitle')}
      description={t('coTripCardCopy')}
      className="mb-6"
      action={
        corporate?.accountId ? (
          <Link to={`/corporate/${corporate.accountId}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
            {t('coOpenCompany')}
            <Icon name="chevron" className="size-4 rtl:rotate-180" />
          </Link>
        ) : corporate ? (
          <Link to={`/corporate?q=${encodeURIComponent(corporate.companyName)}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
            {t('coOpenCompany')}
            <Icon name="chevron" className="size-4 rtl:rotate-180" />
          </Link>
        ) : undefined
      }
    >
      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Badge tone="ink">{t('paymentCorporate')}</Badge>
        <Badge tone={corporate?.isGuest ? 'warning' : 'brand'}>{corporate?.isGuest ? t('coTripGuest') : t('coTripTypeEmployee')}</Badge>
        {exceeded && <Badge tone="danger">{`${t('coTcPolicyExceeded')} · ${formatDateTime(exceeded.createdAt, lang)}`}</Badge>}
      </div>
      <DefinitionList
        items={[
          { label: t('coColCompany'), value: company },
          { label: corporate?.isGuest ? t('coTcGuest') : t('coColEmployee'), value: beneficiary },
          ...(corporate?.isGuest ? [{ label: t('coTcGuestPhone'), value: corporate.guestPhone ?? dash, ltr: true }] : [{ label: t('coEmpNumber'), value: corporate?.employeeNumber ?? dash, ltr: true }]),
          { label: t('coDepartment'), value: corporate?.department ?? dash },
          { label: t('purpose'), value: corporate?.purpose ?? dash },
          { label: t('coCostCenter'), value: corporate?.costCenter ?? dash, ltr: true },
          { label: t('coTcPolicy'), value: corporate?.policyName ?? t('coPolicyDefault') },
        ]}
      />
    </Card>
  )
}
