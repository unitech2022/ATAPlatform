import { useI18n } from '../i18n'

interface OtpBoxesProps {
  value: string
  onChange: (value: string) => void
  length?: number
  disabled?: boolean
  autoFocus?: boolean
  onComplete?: () => void
}

/**
 * Four 56×56 boxes (prototype) backed by an invisible input so the code can be
 * typed, pasted or auto-filled from SMS as well as entered on the keypad.
 */
export function OtpBoxes({ value, onChange, length = 4, disabled = false, autoFocus = false, onComplete }: OtpBoxesProps) {
  const { t } = useI18n()
  const slots = Array.from({ length }, (_, index) => index)

  return (
    <div className="relative mx-auto w-fit" dir="ltr">
      <div className="flex justify-center gap-3">
        {slots.map((index) => {
          const filled = Boolean(value[index])
          const active = index === value.length
          return (
            <div
              key={index}
              className={`grid size-14 place-items-center rounded-2xl border-2 text-xl font-bold transition ${
                filled ? 'border-brand bg-brand-soft' : active ? 'border-brand' : 'border-line'
              }`}
            >
              {value[index] ?? ''}
            </div>
          )
        })}
      </div>
      <input
        aria-label={t('login.otp.label')}
        autoComplete="one-time-code"
        autoFocus={autoFocus}
        className="absolute inset-0 size-full cursor-default opacity-0"
        disabled={disabled}
        inputMode="numeric"
        maxLength={length}
        onChange={(event) => onChange(event.target.value.replace(/\D/g, '').slice(0, length))}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && value.length === length) onComplete?.()
        }}
        pattern="[0-9]*"
        type="text"
        value={value}
      />
    </div>
  )
}
