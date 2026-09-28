import { useI18n } from '../i18n'
import { Action } from './Button'

const digits = ['1', '2', '3', '4', '5', '6', '7', '8', '9']

interface KeypadProps {
  onDigit: (digit: string) => void
  onDelete: () => void
  disabled?: boolean
}

export function Keypad({ onDigit, onDelete, disabled = false }: KeypadProps) {
  const { t } = useI18n()
  const key = 'grid h-13 place-items-center rounded-2xl bg-cloud text-xl font-bold hover:bg-brand-soft disabled:opacity-50'
  return (
    <div className="mx-auto grid max-w-xs grid-cols-3 gap-3" dir="ltr">
      {digits.map((digit) => (
        <Action key={digit} disabled={disabled} onClick={() => onDigit(digit)} className={key}>
          {digit}
        </Action>
      ))}
      <div />
      <Action disabled={disabled} onClick={() => onDigit('0')} className={key}>
        0
      </Action>
      <Action
        disabled={disabled}
        onClick={onDelete}
        className="grid h-13 place-items-center rounded-2xl text-sm font-bold text-muted hover:bg-cloud disabled:opacity-50"
      >
        {t('action.delete')}
      </Action>
    </div>
  )
}
