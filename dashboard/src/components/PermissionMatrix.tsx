import { useState } from 'react'
import { useLang } from '../context/lang'
import { groupByModule, MODULE_KEY } from '../lib/rbac'
import type { PermissionInfo } from '../lib/types'
import { SearchInput } from './Field'
import { Icon } from './Icon'

function Checkbox({
  id,
  checked,
  indeterminate = false,
  disabled = false,
  onChange,
  label,
}: {
  id: string
  checked: boolean
  indeterminate?: boolean
  disabled?: boolean
  onChange: (checked: boolean) => void
  label?: string
}) {
  return (
    <input
      id={id}
      type="checkbox"
      aria-label={label}
      className="size-4 shrink-0 cursor-pointer accent-[var(--color-brand)] disabled:cursor-not-allowed"
      ref={(element) => {
        if (element) element.indeterminate = indeterminate && !checked
      }}
      checked={checked}
      disabled={disabled}
      onChange={(event) => onChange(event.target.checked)}
    />
  )
}

/**
 * Role permission matrix (docs/12 §F20.9): catalogue grouped by module, a tri-state "select all" per module,
 * and a text filter. `readOnly` locks it (e.g. a `*` role).
 */
export function PermissionMatrix({
  catalog,
  value,
  onChange,
  readOnly = false,
}: {
  catalog: PermissionInfo[]
  value: string[]
  onChange: (value: string[]) => void
  readOnly?: boolean
}) {
  const { t } = useLang()
  const [filter, setFilter] = useState('')
  const selected = new Set(value)
  const needle = filter.trim().toLowerCase()
  const visible = needle
    ? catalog.filter((item) => item.code.toLowerCase().includes(needle) || item.name.toLowerCase().includes(needle) || item.module.toLowerCase().includes(needle))
    : catalog
  const groups = groupByModule(visible)

  const setMany = (codes: string[], on: boolean) => {
    const next = new Set(value)
    for (const code of codes) {
      if (on) next.add(code)
      else next.delete(code)
    }
    // Keep catalogue order so saved payloads are stable.
    onChange(catalog.map((item) => item.code).filter((code) => next.has(code)).concat(value.filter((code) => !catalog.some((item) => item.code === code))))
  }

  const total = catalog.filter((item) => selected.has(item.code)).length

  return (
    <div>
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <SearchInput
          wrapperClassName="sm:w-80"
          placeholder={t('roMatrixFilter')}
          aria-label={t('roMatrixFilter')}
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
        />
        <div className="flex items-center gap-3 text-sm">
          <span className="text-muted">
            {t('roSelected')}: <span className="ltr-nums font-bold text-ink">{total} / {catalog.length}</span>
          </span>
          {!readOnly && (
            <>
              <button type="button" className="font-bold text-brand" onClick={() => setMany(catalog.map((item) => item.code), true)}>
                {t('roSelectAll')}
              </button>
              <button type="button" className="font-bold text-muted" onClick={() => setMany(catalog.map((item) => item.code), false)}>
                {t('roClearAll')}
              </button>
            </>
          )}
        </div>
      </div>

      {groups.length === 0 && <p className="py-6 text-center text-sm text-muted">{t('noResults')}</p>}

      <div className="grid gap-4 lg:grid-cols-2" data-testid="permission-matrix">
        {groups.map((group) => {
          const codes = group.items.map((item) => item.code)
          const count = codes.filter((code) => selected.has(code)).length
          const all = count === codes.length
          const moduleKey = MODULE_KEY[group.module]
          const moduleLabel = moduleKey ? t(moduleKey) : group.module
          return (
            <fieldset key={group.module} className="rounded-2xl border border-line p-4" data-module={group.module}>
              <legend className="sr-only">{moduleLabel}</legend>
              <div className="mb-3 flex items-center justify-between gap-3 border-b border-line pb-3">
                <label htmlFor={`module-${group.module}`} className="flex cursor-pointer items-center gap-3 font-bold">
                  <Checkbox
                    id={`module-${group.module}`}
                    checked={all}
                    indeterminate={count > 0}
                    disabled={readOnly}
                    label={`${t('roSelectModule')} ${moduleLabel}`}
                    onChange={(on) => setMany(codes, on)}
                  />
                  <span>{moduleLabel}</span>
                </label>
                <span className="ltr-nums rounded-full bg-cloud px-2 text-xs font-bold text-muted">
                  {count}/{codes.length}
                </span>
              </div>
              <ul className="space-y-2.5">
                {group.items.map((item) => (
                  <li key={item.code}>
                    <label htmlFor={`perm-${item.code}`} className="flex cursor-pointer items-start gap-3">
                      <span className="pt-1">
                        <Checkbox id={`perm-${item.code}`} checked={selected.has(item.code)} disabled={readOnly} onChange={(on) => setMany([item.code], on)} />
                      </span>
                      <span className="min-w-0">
                        <span className="block text-sm font-bold">{item.name || item.code}</span>
                        <span className="ltr-nums block text-xs text-muted">{item.code}</span>
                        {item.description && <span className="block text-xs text-muted">{item.description}</span>}
                      </span>
                    </label>
                  </li>
                ))}
              </ul>
            </fieldset>
          )
        })}
      </div>
      {readOnly && (
        <p className="mt-3 flex items-center gap-2 text-xs text-muted">
          <Icon name="lock" className="size-3.5" />
          {t('roMatrixReadOnly')}
        </p>
      )}
    </div>
  )
}
