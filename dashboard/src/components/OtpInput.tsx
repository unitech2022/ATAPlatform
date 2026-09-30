import { useRef, type ClipboardEvent, type KeyboardEvent } from 'react'
import { useLang } from '../context/lang'
import { TOTP_LENGTH } from '../lib/rbac'

/**
 * Six single-digit boxes for a TOTP code (§F20.4): auto-advance, backspace to the previous box and paste of the
 * whole code. Always rendered LTR (numbers, design system §2).
 */
export function OtpInput({
  value,
  onChange,
  onComplete,
  disabled = false,
  invalid = false,
  autoFocus = false,
  idPrefix = 'otp',
}: {
  value: string
  onChange: (value: string) => void
  onComplete?: (value: string) => void
  disabled?: boolean
  invalid?: boolean
  autoFocus?: boolean
  idPrefix?: string
}) {
  const { t } = useLang()
  const refs = useRef<(HTMLInputElement | null)[]>([])
  const digits = Array.from({ length: TOTP_LENGTH }, (_, index) => value[index] ?? '')

  const focus = (index: number) => refs.current[Math.max(0, Math.min(TOTP_LENGTH - 1, index))]?.focus()

  const commit = (next: string) => {
    const clean = next.replace(/\D/g, '').slice(0, TOTP_LENGTH)
    onChange(clean)
    if (clean.length === TOTP_LENGTH) onComplete?.(clean)
  }

  const setDigit = (index: number, digit: string) => {
    const chars = digits.slice()
    chars[index] = digit
    // Keep the code contiguous: typing into a later empty box fills the first gap.
    commit(chars.join(''))
  }

  const onKeyDown = (index: number, event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Backspace' && !digits[index] && index > 0) {
      event.preventDefault()
      const chars = digits.slice()
      chars[index - 1] = ''
      commit(chars.join(''))
      focus(index - 1)
    } else if (event.key === 'ArrowLeft') {
      event.preventDefault()
      focus(index - 1)
    } else if (event.key === 'ArrowRight') {
      event.preventDefault()
      focus(index + 1)
    }
  }

  const onPaste = (event: ClipboardEvent<HTMLInputElement>) => {
    const pasted = event.clipboardData.getData('text').replace(/\D/g, '')
    if (!pasted) return
    event.preventDefault()
    commit(pasted)
    focus(Math.min(pasted.length, TOTP_LENGTH - 1))
  }

  return (
    <div role="group" aria-label={t('mfCodeLabel')} dir="ltr" className="flex justify-center gap-2 sm:gap-3">
      {digits.map((digit, index) => (
        <input
          key={index}
          id={`${idPrefix}-${index}`}
          ref={(element) => {
            refs.current[index] = element
          }}
          value={digit}
          disabled={disabled}
          inputMode="numeric"
          autoComplete={index === 0 ? 'one-time-code' : 'off'}
          pattern="[0-9]*"
          maxLength={1}
          autoFocus={autoFocus && index === 0}
          aria-label={`${t('mfDigit')} ${index + 1} / ${TOTP_LENGTH}`}
          aria-invalid={invalid || undefined}
          onPaste={onPaste}
          onKeyDown={(event) => onKeyDown(index, event)}
          onFocus={(event) => event.currentTarget.select()}
          onChange={(event) => {
            const typed = event.target.value.replace(/\D/g, '')
            if (typed.length > 1) {
              commit(digits.slice(0, index).join('') + typed)
              focus(index + typed.length)
              return
            }
            setDigit(index, typed)
            if (typed) focus(index + 1)
          }}
          className={`ltr-nums size-11 rounded-2xl border-2 bg-cloud text-center text-xl font-bold outline-none transition focus:border-brand focus:bg-white focus:shadow-brand disabled:opacity-60 sm:size-12 ${invalid ? 'border-danger' : 'border-transparent'}`}
        />
      ))}
    </div>
  )
}
