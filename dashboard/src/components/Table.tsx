import { Fragment, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { EmptyState } from './EmptyState'
import { Spinner } from './Spinner'

export interface Column<T> {
  key: string
  header: ReactNode
  render: (row: T) => ReactNode
  /** Extra classes on both header and body cells (e.g. `hidden md:table-cell`, `text-end`). */
  className?: string
}

export interface TableProps<T> {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  loading?: boolean
  onRowClick?: (row: T) => void
  emptyTitle?: string
  emptyDescription?: string
  /** Optional row rendered right after `row` (used for expanders). */
  renderExpanded?: (row: T) => ReactNode
}

/** Card-friendly table: scrolls horizontally inside its container so the page never does. */
export function Table<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  onRowClick,
  emptyTitle,
  emptyDescription,
  renderExpanded,
}: TableProps<T>) {
  const { t } = useLang()

  if (!loading && rows.length === 0) {
    return <EmptyState title={emptyTitle ?? t('noResults')} description={emptyDescription ?? t('noResultsCopy')} />
  }

  return (
    <div className="relative overflow-x-auto">
      <table className="w-full min-w-max border-collapse text-sm">
        <thead>
          <tr className="border-b border-line bg-cloud/60 text-xs text-muted">
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className={`px-4 py-3 text-start font-bold first:ps-5 last:pe-5 sm:first:ps-6 sm:last:pe-6 ${column.className ?? ''}`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const expanded = renderExpanded?.(row)
            return (
              <Fragment key={rowKey(row)}>
                <tr
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  className={`border-b border-line last:border-0 ${onRowClick ? 'cursor-pointer transition hover:bg-brand-soft/40' : ''}`}
                >
                  {columns.map((column) => (
                    <td
                      key={column.key}
                      className={`px-4 py-3.5 align-middle first:ps-5 last:pe-5 sm:first:ps-6 sm:last:pe-6 ${column.className ?? ''}`}
                    >
                      {column.render(row)}
                    </td>
                  ))}
                </tr>
                {expanded ? (
                  <tr className="border-b border-line bg-cloud/40 last:border-0">
                    <td colSpan={columns.length} className="px-5 py-4 sm:px-6">
                      {expanded}
                    </td>
                  </tr>
                ) : null}
              </Fragment>
            )
          })}
        </tbody>
      </table>
      {loading && (
        <div className="absolute inset-0 grid min-h-32 place-items-center bg-white/70 text-brand">
          <Spinner className="size-7" />
        </div>
      )}
    </div>
  )
}
