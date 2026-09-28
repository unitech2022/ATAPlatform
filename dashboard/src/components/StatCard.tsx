import { Icon, type IconName } from './Icon'

export interface StatCardProps {
  title: string
  value: string
  icon: IconName
  meta?: string
  tone?: 'brand' | 'danger'
}

export function StatCard({ title, value, icon, meta, tone = 'brand' }: StatCardProps) {
  const iconTone = tone === 'danger' ? 'bg-danger-soft text-danger' : 'bg-brand-soft text-brand'
  return (
    <div className="rounded-3xl bg-white p-5 shadow-soft">
      <div className="mb-5 flex items-center justify-between">
        <div className={`grid size-11 place-items-center rounded-xl ${iconTone}`}>
          <Icon name={icon} />
        </div>
        {meta && <span className="text-xs font-bold text-muted">{meta}</span>}
      </div>
      <p className="text-sm font-bold text-muted">{title}</p>
      <p className="ltr-nums mt-2 text-3xl font-bold leading-tight">{value}</p>
    </div>
  )
}
