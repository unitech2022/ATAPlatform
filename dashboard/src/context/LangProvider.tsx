import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { LANG_STORAGE_KEY, applyDocumentLang, dictionary, readStoredLang, type Lang, type TranslationKey } from '../i18n'
import { LangContext, type LangContextValue } from './lang'

export function LangProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>(readStoredLang)

  useEffect(() => {
    applyDocumentLang(lang)
    try {
      localStorage.setItem(LANG_STORAGE_KEY, lang)
    } catch {
      // storage unavailable — language resets to Arabic on reload
    }
  }, [lang])

  const toggleLang = useCallback(() => setLangState((current) => (current === 'ar' ? 'en' : 'ar')), [])
  const t = useCallback((key: TranslationKey) => dictionary[lang][key], [lang])

  const value = useMemo<LangContextValue>(
    () => ({ lang, dir: lang === 'ar' ? 'rtl' : 'ltr', toggleLang, t }),
    [lang, toggleLang, t],
  )

  return <LangContext value={value}>{children}</LangContext>
}
