import { useState, type FormEvent } from 'react'
import { Button } from '../../../components/Button'
import { Field, Input, Select } from '../../../components/Field'
import { Notice } from '../../../components/Notice'
import { useI18n } from '../../../i18n'
import { describeError, fieldErrorsFrom } from '../../../lib/errors'
import { isValidLocalPhone, normalizeLocalPhone } from '../../../lib/format'
import type { CorporateEmployee, CorporateEmployeePayload, CorporatePolicy, CorporateRole, CostCenter } from '../../../lib/types'

interface EmployeeFormProps {
  initial?: CorporateEmployee | null
  costCenters: CostCenter[]
  policies: CorporatePolicy[]
  submitLabel: string
  onSubmit: (payload: CorporateEmployeePayload) => Promise<void>
  onCancel?: () => void
}

function costCenterIdOf(employee: CorporateEmployee | null | undefined, costCenters: CostCenter[]): string {
  if (!employee) return ''
  if (employee.costCenterId) return employee.costCenterId
  const value = employee.costCenter
  if (value && typeof value === 'object') return value.id
  if (typeof value === 'string') return costCenters.find((item) => item.code === value || item.name === value)?.id ?? ''
  return ''
}

/** Invite / edit form for `POST|PUT /corporate/employees`. */
export function EmployeeForm({ initial, costCenters, policies, submitLabel, onSubmit, onCancel }: EmployeeFormProps) {
  const { t } = useI18n()
  const editing = Boolean(initial)
  const [phone, setPhone] = useState(() => normalizeLocalPhone(initial?.phoneNumber ?? ''))
  const [fullName, setFullName] = useState(initial?.fullName ?? '')
  const [role, setRole] = useState<CorporateRole>(initial?.role ?? 'employee')
  const [employeeNumber, setEmployeeNumber] = useState(initial?.employeeNumber ?? '')
  const [department, setDepartment] = useState(initial?.department ?? '')
  const [costCenterId, setCostCenterId] = useState(() => costCenterIdOf(initial, costCenters))
  const [policyId, setPolicyId] = useState(initial?.policyId ?? policies.find((policy) => policy.name === initial?.policyName && !policy.isDefault)?.id ?? '')
  const [budget, setBudget] = useState(initial?.monthlyBudget !== null && initial?.monthlyBudget !== undefined ? String(initial.monthlyBudget) : '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [touched, setTouched] = useState(false)

  const budgetValue = budget.trim() === '' ? null : Number(budget)
  const budgetInvalid = budgetValue !== null && (!Number.isFinite(budgetValue) || budgetValue < 0)
  const phoneInvalid = !isValidLocalPhone(phone)
  const nameInvalid = fullName.trim().length < 2

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setTouched(true)
    if (phoneInvalid || nameInvalid || budgetInvalid) return
    setBusy(true)
    setError(null)
    setFieldErrors({})
    try {
      await onSubmit({
        phoneNumber: `+966${phone}`,
        fullName: fullName.trim(),
        role,
        employeeNumber: employeeNumber.trim() || null,
        department: department.trim() || null,
        costCenterId: costCenterId || null,
        policyId: policyId || null,
        monthlyBudget: budgetValue,
      })
    } catch (caught) {
      setError(describeError(caught, t))
      setFieldErrors(fieldErrorsFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={submit} noValidate className="grid gap-4 sm:grid-cols-2">
      <Field
        label={t('biz.employee.phone')}
        htmlFor="employee-phone"
        error={(touched && phoneInvalid ? t('login.phone.invalid') : null) ?? fieldErrors.phoneNumber}
        hint={editing ? t('biz.employee.phoneLocked') : undefined}
      >
        <div className="flex items-center gap-2" dir="ltr">
          <span className="grid h-14 place-items-center rounded-2xl bg-cloud px-3 text-sm font-bold">+966</span>
          <Input
            id="employee-phone"
            type="tel"
            inputMode="numeric"
            placeholder="5X XXX XXXX"
            value={phone}
            disabled={editing}
            invalid={touched && phoneInvalid}
            onChange={(event) => setPhone(normalizeLocalPhone(event.target.value))}
          />
        </div>
      </Field>
      <Field label={t('biz.employee.fullName')} htmlFor="employee-name" error={(touched && nameInvalid ? t('form.required') : null) ?? fieldErrors.fullName}>
        <Input id="employee-name" value={fullName} invalid={touched && nameInvalid} onChange={(event) => setFullName(event.target.value)} />
      </Field>
      <Field label={t('biz.employee.role')} htmlFor="employee-role">
        <Select id="employee-role" value={role} onChange={(event) => setRole(event.target.value === 'corporate_admin' ? 'corporate_admin' : 'employee')}>
          <option value="employee">{t('biz.role.employee')}</option>
          <option value="corporate_admin">{t('biz.role.corporate_admin')}</option>
        </Select>
      </Field>
      <Field label={t('biz.employee.number')} htmlFor="employee-number" error={fieldErrors.employeeNumber}>
        <Input id="employee-number" value={employeeNumber} onChange={(event) => setEmployeeNumber(event.target.value)} />
      </Field>
      <Field label={t('biz.employee.department')} htmlFor="employee-department" error={fieldErrors.department}>
        <Input id="employee-department" value={department} onChange={(event) => setDepartment(event.target.value)} />
      </Field>
      <Field label={t('biz.employee.costCenter')} htmlFor="employee-cost-center">
        <Select id="employee-cost-center" value={costCenterId} onChange={(event) => setCostCenterId(event.target.value)}>
          <option value="">{t('biz.none')}</option>
          {costCenters
            .filter((item) => item.isActive || item.id === costCenterId)
            .map((item) => (
              <option key={item.id} value={item.id}>
                {item.code} · {item.name}
              </option>
            ))}
        </Select>
      </Field>
      <Field label={t('biz.employee.policy')} htmlFor="employee-policy">
        <Select id="employee-policy" value={policyId} onChange={(event) => setPolicyId(event.target.value)}>
          <option value="">{t('biz.employee.defaultPolicy')}</option>
          {policies
            .filter((policy) => policy.isActive || policy.id === policyId)
            .map((policy) => (
              <option key={policy.id} value={policy.id}>
                {policy.name}
              </option>
            ))}
        </Select>
      </Field>
      <Field
        label={t('biz.employee.budget')}
        htmlFor="employee-budget"
        hint={t('biz.employee.budgetHint')}
        error={(touched && budgetInvalid ? t('form.invalidAmount') : null) ?? fieldErrors.monthlyBudget}
      >
        <Input
          id="employee-budget"
          inputMode="decimal"
          dir="ltr"
          value={budget}
          invalid={touched && budgetInvalid}
          onChange={(event) => setBudget(event.target.value.replace(/[^\d.]/g, ''))}
        />
      </Field>
      {error && (
        <Notice tone="error" className="sm:col-span-2">
          {error}
        </Notice>
      )}
      <div className="flex flex-col-reverse gap-3 sm:col-span-2 sm:flex-row sm:justify-end">
        {onCancel && (
          <Button variant="secondary" onClick={onCancel} disabled={busy}>
            {t('action.cancel')}
          </Button>
        )}
        <Button type="submit" disabled={busy}>
          {busy ? t('action.saving') : submitLabel}
        </Button>
      </div>
    </form>
  )
}
