export interface DefinitionItem {
  label: string
  value: string
  /** Numbers, phones and identifiers are always rendered left-to-right. */
  ltr?: boolean
}

export function DefinitionList({ items, columns = 2 }: { items: DefinitionItem[]; columns?: 1 | 2 }) {
  return (
    <dl className={`grid gap-3 ${columns === 2 ? 'sm:grid-cols-2' : ''}`}>
      {items.map((item) => (
        <div key={item.label} className="min-w-0 rounded-2xl bg-cloud px-4 py-3">
          <dt className="text-xs font-bold text-muted">{item.label}</dt>
          <dd className={`mt-1 truncate font-bold ${item.ltr ? 'ltr-nums' : ''}`}>{item.value}</dd>
        </div>
      ))}
    </dl>
  )
}
