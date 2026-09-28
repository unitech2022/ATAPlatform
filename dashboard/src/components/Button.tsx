import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Icon, type IconName } from './Icon'
import { Spinner } from './Spinner'

export type ButtonVariant = 'primary' | 'brand' | 'secondary' | 'danger' | 'danger-outline' | 'ghost'
export type ButtonSize = 'sm' | 'md' | 'lg'

const variants: Record<ButtonVariant, string> = {
  primary: 'bg-ink text-white hover:bg-ink-soft shadow-button disabled:bg-line disabled:text-muted disabled:shadow-none',
  brand: 'bg-brand text-white hover:brightness-95 disabled:bg-line disabled:text-muted',
  secondary: 'border border-line bg-white text-ink hover:bg-cloud disabled:text-muted',
  danger: 'bg-danger text-white hover:brightness-95 disabled:bg-line disabled:text-muted',
  'danger-outline': 'border border-danger bg-white text-danger hover:bg-danger-soft disabled:border-line disabled:text-muted',
  ghost: 'text-ink hover:bg-cloud disabled:text-muted',
}

const sizes: Record<ButtonSize, string> = {
  sm: 'h-9 px-3 text-xs gap-1.5 rounded-xl',
  md: 'h-11 px-4 text-sm gap-2 rounded-2xl',
  lg: 'h-14 px-6 text-base gap-2 rounded-2xl',
}

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  icon?: IconName
  loading?: boolean
  children?: ReactNode
}

export function Button({
  variant = 'primary',
  size = 'md',
  icon,
  loading = false,
  className = '',
  children,
  disabled,
  type = 'button',
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={`inline-flex shrink-0 items-center justify-center font-bold transition active:scale-[0.98] disabled:cursor-not-allowed disabled:active:scale-100 ${variants[variant]} ${sizes[size]} ${className}`}
      {...rest}
    >
      {loading ? <Spinner className="size-4" /> : icon ? <Icon name={icon} className="size-4" /> : null}
      {children}
    </button>
  )
}
