import { formatJson } from '../lib/format'

export function JsonView({ label, value }: { label: string; value: unknown }) {
  return (
    <div className="min-w-0">
      <p className="mb-2 text-xs font-bold text-muted">{label}</p>
      <pre dir="ltr" className="max-h-72 overflow-auto rounded-2xl bg-ink p-4 text-start text-xs leading-relaxed text-white/90">
        {formatJson(value)}
      </pre>
    </div>
  )
}
