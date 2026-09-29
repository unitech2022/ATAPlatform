import { useMemo } from 'react'
import { useLang } from '../context/lang'
import { catalog } from '../lib/admin'
import { RATING_TAG_KEY } from '../lib/rewards'
import { useQuery } from './useQuery'

/**
 * Labels for rating tag codes: the catalog name (`GET /catalog/rating-tags`, both targets) when available,
 * else the seeded translation, else the raw code. Also returns every known code for filters.
 */
export function useRatingTags() {
  const { t, lang } = useLang()
  const query = useQuery(
    () => Promise.all([catalog.ratingTags('driver'), catalog.ratingTags('passenger')]).then(([driver, passenger]) => [...driver, ...passenger]),
    `rating-tags:${lang}`,
  )
  return useMemo(() => {
    const names = new Map((query.data ?? []).map((tag) => [tag.code, tag.name]))
    const codes = [...new Set([...Object.keys(RATING_TAG_KEY), ...names.keys()])]
    const label = (code: string) => {
      const known = RATING_TAG_KEY[code]
      return names.get(code) ?? (known ? t(known) : code)
    }
    return { label, codes }
  }, [query.data, t])
}
