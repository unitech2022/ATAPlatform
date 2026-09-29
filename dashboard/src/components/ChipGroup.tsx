import { Icon } from './Icon'

export interface ChipOption<T extends string> {
  value: T
  label: string
}

/** Multi-select pill group (campaign audience, promotion/incentive restrictions). */
export function ChipGroup<T extends string>({
  options,
  value,
  onChange,
  disabled = false,
}: {
  options: ChipOption<T>[]
  value: T[]
  onChange: (value: T[]) => void
  disabled?: boolean
}) {
  return (
    <div className="flex flex-wrap gap-2">
      {options.map((option) => {
        const active = value.includes(option.value)
        return (
          <button
            key={option.value}
            type="button"
            aria-pressed={active}
            disabled={disabled}
            onClick={() => onChange(active ? value.filter((item) => item !== option.value) : [...value, option.value])}
            className={`inline-flex items-center gap-1.5 rounded-full border px-3.5 py-1.5 text-sm font-bold transition disabled:cursor-not-allowed disabled:opacity-60 ${active ? 'border-brand bg-brand text-white' : 'border-line bg-white text-muted hover:bg-cloud'}`}
          >
            {active && <Icon name="check" className="size-3.5" />}
            {option.label}
          </button>
        )
      })}
    </div>
  )
}
