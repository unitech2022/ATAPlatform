import { useState, type FormEvent } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { isApiError } from '../lib/api'
import { me as meApi } from '../lib/admin'
import { PASSWORD_RULES, passwordMeetsPolicy, serverRuleKey, violatedRules } from '../lib/rbac'
import { Button } from './Button'
import { Input } from './Field'
import { Icon } from './Icon'

/** Live checklist of the server password policy (§F20.4: ≥ 12 chars, upper, lower, digit, symbol). */
export function PasswordPolicyHints({ value, serverRules = [] }: { value: string; serverRules?: string[] }) {
  const { t } = useLang()
  const knownKeys = new Set(serverRules.map(serverRuleKey).filter(Boolean))
  const unknown = serverRules.filter((rule) => !serverRuleKey(rule))
  return (
    <div className="rounded-2xl bg-cloud p-4">
      <p className="mb-2 text-xs font-bold text-muted">{t('pwPolicyTitle')}</p>
      <ul className="grid gap-1.5 text-sm sm:grid-cols-2" aria-live="polite">
        {PASSWORD_RULES.map((rule) => {
          const ok = rule.test(value)
          const flagged = knownKeys.has(rule.key)
          return (
            <li key={rule.rule} className={`flex items-center gap-2 ${ok && !flagged ? 'text-brand' : flagged ? 'font-bold text-danger' : 'text-muted'}`}>
              <Icon name={ok && !flagged ? 'check' : 'x'} className="size-4 shrink-0" />
              <span>{t(rule.key)}</span>
              <span className="sr-only">{ok ? t('pwRuleMet') : t('pwRuleNotMet')}</span>
            </li>
          )
        })}
        {serverRules
          .map(serverRuleKey)
          .filter((key): key is NonNullable<typeof key> => Boolean(key) && !PASSWORD_RULES.some((rule) => rule.key === key))
          .map((key) => (
            <li key={key} className="flex items-center gap-2 font-bold text-danger">
              <Icon name="x" className="size-4 shrink-0" />
              <span>{t(key)}</span>
            </li>
          ))}
        {unknown.map((rule) => (
          <li key={rule} className="flex items-center gap-2 font-bold text-danger">
            <Icon name="x" className="size-4 shrink-0" />
            <span className="ltr-nums">{rule}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

/**
 * `POST /admin/me/password` (§F20.5): current + new + confirmation with the policy checklist; surfaces
 * `422 password_policy_violation { rules }` next to the rules. The server revokes the other sessions on success.
 */
export function PasswordChangeForm({ onChanged, submitLabel }: { onChanged: () => void | Promise<void>; submitLabel?: string }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [errors, setErrors] = useState<{ current?: string; next?: string; confirm?: string; form?: string }>({})
  const [serverRules, setServerRules] = useState<string[]>([])
  const [submitting, setSubmitting] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const found: typeof errors = {}
    if (!current) found.current = t('pwErrCurrentRequired')
    if (!passwordMeetsPolicy(next)) found.next = t('pwErrPolicy')
    else if (next === current) found.next = t('pwErrSame')
    if (confirm !== next) found.confirm = t('pwErrMismatch')
    setErrors(found)
    setServerRules([])
    if (Object.keys(found).length > 0) return
    setSubmitting(true)
    try {
      await meApi.changePassword(current, next)
      setCurrent('')
      setNext('')
      setConfirm('')
      await onChanged()
    } catch (error) {
      if (isApiError(error) && error.code === 'password_policy_violation') {
        setServerRules(violatedRules(error.details))
        setErrors({ next: t('pwErrPolicyServer') })
      } else if (isApiError(error) && (error.code === 'invalid_credentials' || error.code === 'invalid_password' || error.status === 400)) {
        setErrors({ current: error.code === 'validation_error' ? describe(error) : t('pwErrCurrentWrong') })
      } else {
        setErrors({ form: describe(error) })
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <Input
        id="pw-current"
        type="password"
        label={t('pwCurrent')}
        autoComplete="current-password"
        dir="ltr"
        value={current}
        error={errors.current}
        onChange={(event) => setCurrent(event.target.value)}
      />
      <Input
        id="pw-new"
        type="password"
        label={t('pwNew')}
        autoComplete="new-password"
        dir="ltr"
        value={next}
        error={errors.next}
        onChange={(event) => setNext(event.target.value)}
      />
      <PasswordPolicyHints value={next} serverRules={serverRules} />
      <Input
        id="pw-confirm"
        type="password"
        label={t('pwConfirm')}
        autoComplete="new-password"
        dir="ltr"
        value={confirm}
        error={errors.confirm}
        onChange={(event) => setConfirm(event.target.value)}
      />
      {errors.form && (
        <div role="alert" className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger">
          <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
          <span>{errors.form}</span>
        </div>
      )}
      <Button type="submit" className="w-full sm:w-auto" loading={submitting} icon="lock">
        {submitLabel ?? t('pwSubmit')}
      </Button>
    </form>
  )
}
