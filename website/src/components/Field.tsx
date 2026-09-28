import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes } from 'react'

interface FieldProps {
  label: string
  htmlFor: string
  hint?: string
  error?: string | null
  children: ReactNode
  className?: string
}

export function Field({ label, htmlFor, hint, error, children, className = '' }: FieldProps) {
  return (
    <div className={className}>
      <label htmlFor={htmlFor} className="mb-2 block text-sm font-bold text-ink">
        {label}
      </label>
      {children}
      {error ? (
        <p className="mt-2 text-xs font-bold text-danger" role="alert">
          {error}
        </p>
      ) : hint ? (
        <p className="mt-2 text-xs text-muted">{hint}</p>
      ) : null}
    </div>
  )
}

type InputProps = InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }

export function Input({ invalid = false, className = '', ...rest }: InputProps) {
  return <input aria-invalid={invalid || undefined} className={`field-control ${className}`} {...rest} />
}

type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & { invalid?: boolean; children: ReactNode }

export function Select({ invalid = false, className = '', children, ...rest }: SelectProps) {
  return (
    <select aria-invalid={invalid || undefined} className={`field-control ${className}`} {...rest}>
      {children}
    </select>
  )
}
