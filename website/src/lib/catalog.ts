import type { Lang } from '../i18n'
import type { RideCategory } from './types'

interface StaticCategory {
  code: string
  nameAr: string
  nameEn: string
  descriptionAr: string
  descriptionEn: string
  icon: string
  seats: number
  maxStops: number
  etaMinutes: number
  price: number
}

// Mirrors the seed in docs/04-database-schema.md; prices/ETAs follow the prototype.
const staticCategories: StaticCategory[] = [
  { code: 'saver', nameAr: 'توفير', nameEn: 'Saver', descriptionAr: 'الخيار الأوفر لرحلاتك اليومية', descriptionEn: 'The most affordable option for daily trips', icon: 'car', seats: 4, maxStops: 1, etaMinutes: 3, price: 32 },
  { code: 'economy', nameAr: 'اقتصادي', nameEn: 'Economy', descriptionAr: 'سيارة مريحة', descriptionEn: 'A comfortable car', icon: 'car', seats: 4, maxStops: 2, etaMinutes: 2, price: 38 },
  { code: 'comfort', nameAr: 'مريح', nameEn: 'Comfort', descriptionAr: 'سيارات أحدث ومساحة أوسع', descriptionEn: 'Newer cars with more room', icon: 'car', seats: 4, maxStops: 2, etaMinutes: 4, price: 46 },
  { code: 'family', nameAr: 'عائلي', nameEn: 'Family', descriptionAr: 'حتى 6 ركاب', descriptionEn: 'Up to 6 passengers', icon: 'car', seats: 6, maxStops: 2, etaMinutes: 4, price: 54 },
  { code: 'premium', nameAr: 'ATA بلس', nameEn: 'ATA Plus', descriptionAr: 'رحلة أكثر تميزاً', descriptionEn: 'A more distinguished ride', icon: 'shield', seats: 4, maxStops: 2, etaMinutes: 6, price: 72 },
  { code: 'airport', nameAr: 'المطار', nameEn: 'Airport', descriptionAr: 'رحلات المطار بسعر ثابت', descriptionEn: 'Airport trips at a fixed price', icon: 'pin', seats: 4, maxStops: 0, etaMinutes: 8, price: 95 },
]

export function fallbackRideCategories(lang: Lang): RideCategory[] {
  return staticCategories.map((category, index) => ({
    id: category.code,
    code: category.code,
    name: lang === 'ar' ? category.nameAr : category.nameEn,
    description: lang === 'ar' ? category.descriptionAr : category.descriptionEn,
    icon: category.icon,
    seats: category.seats,
    maxStops: category.maxStops,
    sortOrder: index + 1,
    estimate: { etaMinutes: category.etaMinutes, price: category.price },
  }))
}

export const SUPPORT_PHONE = '9200 123 45'
export const SUPPORT_PHONE_HREF = 'tel:920012345'
export const SUPPORT_EMAIL = 'help@ata.sa'

/** Corporate sales contact for the `/business` "talk to sales" form (mailto / WhatsApp, no API). */
export const BUSINESS_EMAIL: string = import.meta.env.VITE_BUSINESS_EMAIL || 'business@ata.sa'
/** WhatsApp number in international format without "+" (hidden when empty). */
export const BUSINESS_WHATSAPP: string = (import.meta.env.VITE_BUSINESS_WHATSAPP ?? '').replace(/\D/g, '')
