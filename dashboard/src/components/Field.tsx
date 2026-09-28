import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'
import { Icon } from './Icon'

interface FieldShellProps {
  label?: ReactNode
  hint?: ReactNode
  error?: ReactNode
  htmlFor?: string
  className?: string
  children: ReactNode
}

function FieldShell({ label, hint, error, htmlFor, className = '', children }: FieldShellProps) {
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      {label && (
        <label htmlFor={htmlFor} className="text-sm font-bold">
          {label}
        </label>
      )}
      {children}
      {error ? <p className="text-xs font-bold text-danger">{error}</p> : hint ? <p className="text-xs text-muted">{hint}</p> : null}
    </div>
  )
}

const controlBase =
  'w-full rounded-2xl border-2 bg-cloud px-4 text-base text-ink placeholder:text-muted/70 outline-none transition focus:border-brand focus:bg-white focus:shadow-brand disabled:cursor-not-allowed disabled:opacity-60'

function borderFor(error: unknown) {
  return error ? 'border-danger' : 'border-transparent'
}

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: ReactNode
  hint?: ReactNode
  error?: ReactNode
  wrapperClassName?: string
}

export function Input({ label, hint, error, id, className = '', wrapperClassName, ...rest }: InputProps) {
  return (
    <FieldShell label={label} hint={hint} error={error} htmlFor={id} className={wrapperClassName}>
      <input id={id} className={`${controlBase} h-12 ${borderFor(error)} ${className}`} {...rest} />
    </FieldShell>
  )
}

export interface SearchInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  wrapperClassName?: string
}

export function SearchInput({ className = '', wrapperClassName = '', ...rest }: SearchInputProps) {
  return (
    <div className={`relative ${wrapperClassName}`}>
      <span className="pointer-events-none absolute inset-y-0 start-4 grid place-items-center text-muted">
        <Icon name="search" className="size-5" />
      </span>
      <input type="search" className={`${controlBase} h-12 border-transparent ps-12 ${className}`} {...rest} />
    </div>
  )
}

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: ReactNode
  hint?: ReactNode
  error?: ReactNode
  wrapperClassName?: string
}

export function Select({ label, hint, error, id, className = '', wrapperClassName, children, ...rest }: SelectProps) {
  return (
    <FieldShell label={label} hint={hint} error={error} htmlFor={id} className={wrapperClassName}>
      <select id={id} className={`${controlBase} h-12 ${borderFor(error)} ${className}`} {...rest}>
        {children}
      </select>
    </FieldShell>
  )
}

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: ReactNode
  hint?: ReactNode
  error?: ReactNode
  wrapperClassName?: string
}

export function Textarea({ label, hint, error, id, className = '', wrapperClassName, ...rest }: TextareaProps) {
  return (
    <FieldShell label={label} hint={hint} error={error} htmlFor={id} className={wrapperClassName}>
      <textarea id={id} className={`${controlBase} min-h-28 py-3 ${borderFor(error)} ${className}`} {...rest} />
    </FieldShell>
  )
}

export interface ToggleProps {
  checked: boolean
  onChange: (checked: boolean) => void
  label: ReactNode
  description?: ReactNode
  id?: string
}

/** 48×28 toggle from the design system (§3). */
export function Toggle({ checked, onChange, label, description, id }: ToggleProps) {
  return (
    <button
      type="button"
      id={id}
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
      className="flex w-full items-center justify-between gap-4 rounded-2xl border border-line px-4 py-3 text-start transition hover:bg-cloud"
    >
      <span className="min-w-0">
        <span className="block text-sm font-bold">{label}</span>
        {description && <span className="block text-xs text-muted">{description}</span>}
      </span>
      <span className={`flex h-7 w-12 shrink-0 items-center rounded-full p-1 transition ${checked ? 'justify-end bg-brand' : 'justify-start bg-line'}`}>
        <span className="size-5 rounded-full bg-white shadow-soft" />
      </span>
    </button>
  )
}
