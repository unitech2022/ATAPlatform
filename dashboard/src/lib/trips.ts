import type { TranslationKey } from '../i18n'
import type { PaymentMethod, PricingMode, TripActor, TripStatus } from './types'

/** Statuses after which no further transition is possible (cancel is not offered). */
export const TERMINAL_TRIP_STATUSES: readonly TripStatus[] = ['completed', 'cancelled', 'no_drivers']

export function isTerminalTripStatus(status: TripStatus) {
  return TERMINAL_TRIP_STATUSES.includes(status)
}

export type TripStatusGroup = 'scheduled' | 'active' | 'completed' | 'cancelled'

export const TRIP_STATUS_GROUPS: { group: TripStatusGroup; key: TranslationKey; statuses: TripStatus[] }[] = [
  { group: 'scheduled', key: 'tripGroupScheduled', statuses: ['scheduled'] },
  {
    group: 'active',
    key: 'tripGroupActive',
    statuses: ['searching', 'driver_assigned', 'driver_en_route', 'driver_arrived', 'waiting', 'pin_verified', 'in_trip'],
  },
  { group: 'completed', key: 'tripGroupCompleted', statuses: ['completed'] },
  { group: 'cancelled', key: 'tripGroupCancelled', statuses: ['cancelled', 'no_drivers'] },
]

export const ALL_TRIP_STATUSES: TripStatus[] = [
  'requested',
  ...TRIP_STATUS_GROUPS.flatMap((group) => group.statuses),
]

export function parseTripStatus(value: string | null): TripStatus | '' {
  return ALL_TRIP_STATUSES.includes(value as TripStatus) ? (value as TripStatus) : ''
}

export const PAYMENT_METHOD_KEY: Record<PaymentMethod, TranslationKey> = {
  cash: 'paymentCash',
  wallet: 'paymentWallet',
  card: 'paymentCard',
  corporate: 'paymentCorporate',
}

export const PRICING_MODE_KEY: Record<PricingMode, TranslationKey> = {
  fixed: 'pricingFixed',
  saver: 'pricingSaver',
  offer: 'pricingOffer',
}

export const TRIP_ACTOR_KEY: Record<TripActor, TranslationKey> = {
  passenger: 'actorPassenger',
  driver: 'actorDriver',
  system: 'actorSystem',
  admin: 'actorAdmin',
}
