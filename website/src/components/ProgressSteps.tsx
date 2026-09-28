import { useI18n } from '../i18n'
import { Action } from './Button'
import { Icon } from './Icon'

export interface ProgressStep<K extends string> {
  key: K
  label: string
  done: boolean
}

interface ProgressStepsProps<K extends string> {
  steps: ProgressStep<K>[]
  active: K
  onSelect: (key: K) => void
}

/** Three numbered step cards (prototype "pending" screen) that double as tabs. */
export function ProgressSteps<K extends string>({ steps, active, onSelect }: ProgressStepsProps<K>) {
  const { t } = useI18n()
  return (
    <div className="grid gap-3 sm:grid-cols-3" role="tablist">
      {steps.map((step, index) => {
        const isActive = step.key === active
        return (
          <Action
            key={step.key}
            role="tab"
            aria-selected={isActive}
            onClick={() => onSelect(step.key)}
            className={`rounded-2xl border p-4 ${
              isActive ? 'border-brand bg-brand-soft' : 'border-line bg-white hover:bg-cloud'
            }`}
          >
            <div className="mb-5 flex items-center justify-between">
              <div
                className={`grid size-8 place-items-center rounded-full text-sm font-bold ${
                  step.done ? 'bg-brand text-white' : isActive ? 'bg-ink text-white' : 'bg-cloud text-muted'
                }`}
              >
                {step.done ? <Icon name="check" className="size-4" /> : index + 1}
              </div>
              <span className={`text-xs font-bold ${step.done ? 'text-brand' : 'text-muted'}`}>
                {step.done ? t('portal.steps.done') : t('portal.steps.pending')}
              </span>
            </div>
            <p className="font-bold">{step.label}</p>
          </Action>
        )
      })}
    </div>
  )
}
