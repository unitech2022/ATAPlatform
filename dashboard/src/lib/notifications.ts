import type { Lang, TranslationKey } from '../i18n'
import type {
  CampaignAudience,
  CampaignCategory,
  CampaignChannel,
  CampaignStatus,
  DeliveryStatus,
  DeliverySkippedReason,
  NotificationCategory,
  NotificationChannel,
} from './types'

export const NOTIFICATION_CHANNELS: NotificationChannel[] = ['inapp', 'push', 'sms']
export const CAMPAIGN_CHANNELS: CampaignChannel[] = ['inapp', 'push', 'sms']
export const CAMPAIGN_CATEGORIES: CampaignCategory[] = ['promotions', 'system', 'trips', 'wallet']
export const CAMPAIGN_STATUSES: CampaignStatus[] = ['draft', 'scheduled', 'sending', 'sent', 'cancelled', 'failed']
export const DELIVERY_STATUSES: DeliveryStatus[] = ['queued', 'sent', 'failed', 'skipped']
export const DELIVERY_CHANNELS: ('push' | 'sms')[] = ['push', 'sms']

export const CHANNEL_KEY: Record<NotificationChannel, TranslationKey> = {
  inapp: 'channelInapp',
  push: 'channelPush',
  sms: 'channelSms',
}

export const CATEGORY_KEY: Record<NotificationCategory, TranslationKey> = {
  trips: 'catTrips',
  offers: 'catOffers',
  safety: 'catSafety',
  wallet: 'catWallet',
  promotions: 'catPromotions',
  system: 'catSystem',
}

export const SKIPPED_REASON_KEY: Record<DeliverySkippedReason, TranslationKey> = {
  preference_off: 'skipPreferenceOff',
  no_subscription: 'skipNoSubscription',
  template_inactive: 'skipTemplateInactive',
  no_phone: 'skipNoPhone',
  user_inactive: 'skipUserInactive',
}

/** Recipient letters of the event catalogue (§F13.2). */
export const RECIPIENT_KEY: Record<string, TranslationKey> = {
  P: 'recipientPassenger',
  D: 'recipientDriver',
  O: 'recipientOps',
  C: 'recipientContact',
  A: 'recipientCorporateAdmin',
  G: 'recipientGuest',
  passenger: 'recipientPassenger',
  driver: 'recipientDriver',
  ops: 'recipientOps',
}

const PLACEHOLDER = /\{([A-Za-z][A-Za-z0-9_]*)\}/g

/** Unique placeholder names used in `text`, in order of appearance. */
export function extractPlaceholders(text: string): string[] {
  const names = new Set<string>()
  for (const match of text.matchAll(PLACEHOLDER)) names.add(match[1])
  return [...names]
}

/** Replaces `{name}` with its value; unknown placeholders stay visible so the preview shows the gap. */
export function renderTemplate(text: string, values: Record<string, string>): string {
  return text.replace(PLACEHOLDER, (whole, name: string) => (name in values ? values[name] : whole))
}

/** Sample values used by the live preview (editable in the editor). */
const SAMPLES: Record<string, { ar: string; en: string }> = {
  driverName: { ar: 'محمد', en: 'Mohammed' },
  passengerName: { ar: 'سارة', en: 'Sara' },
  senderName: { ar: 'محمد', en: 'Mohammed' },
  userName: { ar: 'سارة', en: 'Sara' },
  vehicle: { ar: 'تويوتا كامري', en: 'Toyota Camry' },
  plateNumber: { ar: 'أ ب ج 2841', en: 'ABJ 2841' },
  etaMinutes: { ar: '4', en: '4' },
  freeWaitingMinutes: { ar: '3', en: '3' },
  dropoffName: { ar: 'مطار الملك خالد', en: 'King Khalid Airport' },
  pickupName: { ar: 'حي العليا', en: 'Olaya' },
  fare: { ar: '46.00 ر.س', en: 'SAR 46.00' },
  fee: { ar: '10.00 ر.س', en: 'SAR 10.00' },
  amount: { ar: '50.00 ر.س', en: 'SAR 50.00' },
  balance: { ar: '125.00 ر.س', en: 'SAR 125.00' },
  netAmount: { ar: '640.00 ر.س', en: 'SAR 640.00' },
  driverNet: { ar: '38.00 ر.س', en: 'SAR 38.00' },
  reward: { ar: '150.00 ر.س', en: 'SAR 150.00' },
  total: { ar: '1,150.00 ر.س', en: 'SAR 1,150.00' },
  tripNumber: { ar: 'T-20260928-00042', en: 'T-20260928-00042' },
  payoutNumber: { ar: 'PO-20260928-00012', en: 'PO-20260928-00012' },
  invoiceNumber: { ar: 'INV-2026-0091', en: 'INV-2026-0091' },
  applicationNumber: { ar: 'DA-20260928-0007', en: 'DA-20260928-0007' },
  ticketNumber: { ar: 'S-10442', en: 'S-10442' },
  caseNumber: { ar: 'SC-2031', en: 'SC-2031' },
  scheduledAt: { ar: '18:30 28/09', en: '18:30 28/09' },
  pickupTime: { ar: '18:30 28/09', en: '18:30 28/09' },
  expiresAt: { ar: '05/10/2026', en: '05/10/2026' },
  restrictedUntil: { ar: '05/10/2026', en: '05/10/2026' },
  scheduledFor: { ar: '28/10/2026', en: '28/10/2026' },
  minutesBefore: { ar: '30', en: '30' },
  deadlineMinutes: { ar: '15', en: '15' },
  daysLeft: { ar: '7', en: '7' },
  documentName: { ar: 'رخصة القيادة', en: 'Driving licence' },
  categoryName: { ar: 'اقتصادي', en: 'Economy' },
  tierName: { ar: 'ذهبي', en: 'Gold' },
  incentiveName: { ar: 'تحدي الأسبوع', en: 'Weekly challenge' },
  periodLabel: { ar: '21–28 سبتمبر', en: '21–28 Sep' },
  companyName: { ar: 'شركة المثال', en: 'Example Co.' },
  code: { ar: 'ATA10', en: 'ATA10' },
  title: { ar: 'عرض خاص', en: 'Special offer' },
  body: { ar: 'خصم 10% على رحلتك القادمة', en: '10% off your next ride' },
  preview: { ar: 'أنا عند البوابة', en: "I'm at the gate" },
  reason: { ar: 'انتهاء المستند', en: 'Document expired' },
  status: { ar: 'مدفوع', en: 'Paid' },
  cancelledBy: { ar: 'الراكب', en: 'Passenger' },
  purpose: { ar: 'شحن المحفظة', en: 'Wallet top-up' },
  level: { ar: 'تحذير', en: 'Warning' },
  cancellationRate: { ar: '18%', en: '18%' },
  shareUrl: { ar: 'https://ata.sa/t/abc', en: 'https://ata.sa/t/abc' },
  mapsUrl: { ar: 'https://maps.google.com/?q=24.7,46.6', en: 'https://maps.google.com/?q=24.7,46.6' },
  pin: { ar: '4821', en: '4821' },
  alertType: { ar: 'توقف طويل', en: 'Long stop' },
  type: { ar: 'SOS', en: 'SOS' },
  priority: { ar: 'عالية', en: 'High' },
  itemCategory: { ar: 'هاتف', en: 'Phone' },
}

export function sampleValue(name: string, lang: Lang): string {
  return SAMPLES[name]?.[lang] ?? name
}

/** GSM-7 basic set (approximation: printable ASCII plus newlines). */
const GSM7 = /^[\n\r\x20-\x7E£¥èéùìòÇØøÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ¤¡ÄÖÑÜ§¿äöñüà]*$/

/** Characters and SMS segments (GSM-7: 160/153, UCS-2 — e.g. Arabic: 70/67). */
export function smsInfo(text: string) {
  const length = [...text].length
  const unicode = !GSM7.test(text)
  const single = unicode ? 70 : 160
  const multi = unicode ? 67 : 153
  const segments = length === 0 ? 0 : length <= single ? 1 : Math.ceil(length / multi)
  return { length, segments, unicode }
}

/** Comma/space/newline separated list → trimmed unique values. */
export function splitList(text: string): string[] {
  return [...new Set(text.split(/[\s,،]+/).map((value) => value.trim()).filter(Boolean))]
}

/** Drops empty keys so the stored audience JSON only carries what was chosen (§F13.5: keys are ANDed). */
export function cleanAudience(audience: CampaignAudience): CampaignAudience {
  const result: CampaignAudience = {}
  if (audience.userIds?.length) return { userIds: audience.userIds }
  if (audience.roles?.length) result.roles = audience.roles
  if (audience.cityIds?.length) result.cityIds = audience.cityIds
  if (audience.languages?.length) result.languages = audience.languages
  if (audience.genders?.length) result.genders = audience.genders
  if (audience.driverTiers?.length) result.driverTiers = audience.driverTiers
  if (typeof audience.lastActiveWithinDays === 'number' && audience.lastActiveWithinDays > 0) result.lastActiveWithinDays = audience.lastActiveWithinDays
  if (typeof audience.hasCompletedTrip === 'boolean') result.hasCompletedTrip = audience.hasCompletedTrip
  return result
}
