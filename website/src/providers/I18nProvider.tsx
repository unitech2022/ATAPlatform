import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  directionOf,
  I18nContext,
  LANGUAGE_STORAGE_KEY,
  readStoredLanguage,
  translate,
  type Lang,
  type TranslationKey,
  type TranslationParams,
} from '../i18n'

export function I18nProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>(() => readStoredLanguage())

  useEffect(() => {
    try {
      localStorage.setItem(LANGUAGE_STORAGE_KEY, lang)
    } catch {
      // Storage unavailable; the choice still applies for this visit.
    }
    document.documentElement.lang = lang
    document.documentElement.dir = directionOf(lang)
  }, [lang])

  const setLang = useCallback((next: Lang) => setLangState(next), [])
  const t = useCallback(
    (key: TranslationKey, params?: TranslationParams) => translate(lang, key, params),
    [lang],
  )

  const value = useMemo(() => ({ lang, dir: directionOf(lang), setLang, t }), [lang, setLang, t])

  return <I18nContext value={value}>{children}</I18nContext>
}
