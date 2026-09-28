import { useI18n, type Lang } from '../i18n'
import { Action } from './Button'

const options: { id: Lang; tag: string }[] = [
  { id: 'ar', tag: 'AR' },
  { id: 'en', tag: 'EN' },
]

export function LanguageToggle({ className = '' }: { className?: string }) {
  const { lang, setLang, t } = useI18n()
  return (
    <div
      role="group"
      aria-label={t('lang.label')}
      className={`flex items-center gap-1 rounded-full bg-cloud p-1 ${className}`}
      dir="ltr"
    >
      {options.map((option) => (
        <Action
          key={option.id}
          aria-pressed={lang === option.id}
          aria-label={t(`lang.${option.id}`)}
          onClick={() => setLang(option.id)}
          className={`grid h-9 w-11 place-items-center rounded-full text-xs font-bold transition ${
            lang === option.id ? 'bg-ink text-white shadow-soft' : 'text-muted hover:text-ink'
          }`}
        >
          {option.tag}
        </Action>
      ))}
    </div>
  )
}
