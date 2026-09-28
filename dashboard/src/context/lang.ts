import { createContext, useContext } from 'react'
import type { Lang, TranslationKey } from '../i18n'

export interface LangContextValue {
  lang: Lang
  dir: 'rtl' | 'ltr'
  toggleLang: () => void
  t: (key: TranslationKey) => string
}

export const LangContext = createContext<LangContextValue | null>(null)

export function useLang(): LangContextValue {
  const value = useContext(LangContext)
  if (!value) throw new Error('useLang must be used inside <LangProvider>')
  return value
}
