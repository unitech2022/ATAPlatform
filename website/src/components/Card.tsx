import type { HTMLAttributes, ReactNode } from 'react'

type Tone = 'white' | 'dark' | 'panel'

const tones: Record<Tone, string> = {
  white: 'bg-white shadow-soft',
  dark: 'bg-ink text-white shadow-button',
  panel: 'bg-white shadow-panel',
}

type CardProps = HTMLAttributes<HTMLDivElement> & { tone?: Tone; children: ReactNode }

export function Card({ tone = 'white', className = '', children, ...rest }: CardProps) {
  return (
    <div className={`rounded-3xl p-5 sm:p-6 ${tones[tone]} ${className}`} {...rest}>
      {children}
    </div>
  )
}
