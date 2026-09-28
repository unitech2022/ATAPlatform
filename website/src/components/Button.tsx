import type { ButtonHTMLAttributes, ReactNode } from 'react'

type ActionProps = ButtonHTMLAttributes<HTMLButtonElement> & { children: ReactNode }

/** Unstyled pressable used for cards, keypad keys and list rows (prototype `Action`). */
export function Action({ children, className = '', type = 'button', ...rest }: ActionProps) {
  return (
    <button
      type={type}
      className={`cursor-pointer select-none text-start transition active:scale-[0.98] disabled:cursor-not-allowed disabled:active:scale-100 ${className}`}
      {...rest}
    >
      {children}
    </button>
  )
}

type Variant = 'primary' | 'secondary' | 'brand' | 'danger' | 'ghost'

const variants: Record<Variant, string> = {
  primary: 'bg-ink text-white shadow-button hover:bg-ink-soft disabled:bg-line disabled:text-white disabled:shadow-none',
  secondary: 'border border-line bg-white text-ink hover:bg-cloud disabled:text-muted disabled:hover:bg-white',
  brand: 'bg-brand text-white hover:brightness-95 disabled:bg-line',
  danger: 'border border-danger bg-white text-danger hover:bg-danger-soft disabled:border-line disabled:text-muted',
  ghost: 'text-brand hover:bg-brand-soft disabled:text-muted',
}

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: Variant
  block?: boolean
  size?: 'md' | 'sm'
  children: ReactNode
}

export function Button({
  variant = 'primary',
  block = false,
  size = 'md',
  className = '',
  type = 'button',
  children,
  ...rest
}: ButtonProps) {
  const sizing = size === 'sm' ? 'px-4 py-2.5 text-sm' : 'px-6 py-4'
  return (
    <button
      type={type}
      className={`inline-flex items-center justify-center gap-2 rounded-2xl font-bold transition active:scale-[0.98] disabled:cursor-not-allowed disabled:active:scale-100 ${sizing} ${variants[variant]} ${block ? 'w-full' : ''} ${className}`}
      {...rest}
    >
      {children}
    </button>
  )
}
