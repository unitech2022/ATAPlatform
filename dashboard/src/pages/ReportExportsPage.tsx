import { useState } from 'react'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { ReportFilters, type ReportFilterValue } from '../components/ReportFilters'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { reports } from '../lib/admin'
import { isApiError } from '../lib/api'
import { parseEnum, saveBlob } from '../lib/finance'
import { formatNumber } from '../lib/format'
import { defaultRange, rangeError, REPORT_DATASET_VALUES, REPORT_DATASETS, REPORT_MAX_EXPORT_ROWS, REPORT_MAX_RANGE_DAYS } from '../lib/reports'

/**
 * CSV exports (`/reports/exports`, docs/12 §F20.7, `reports.export`): dataset picker with its columns, the shared
 * filter row, an authenticated blob download named from `Content-Disposition`, and the row/range limit errors.
 */
export function ReportExportsPage() {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, update } = useUrlState()
  const fallback = defaultRange()
  const filters: ReportFilterValue = {
    from: parseIsoDate(params.get('from')) || fallback.from,
    to: parseIsoDate(params.get('to')) || fallback.to,
    cityId: params.get('cityId') ?? '',
    zoneId: params.get('zoneId') ?? '',
    rideCategoryId: params.get('rideCategoryId') ?? '',
  }
  const dataset = parseEnum(params.get('dataset'), REPORT_DATASET_VALUES) || 'trips'
  const selected = REPORT_DATASETS.find((item) => item.value === dataset) ?? REPORT_DATASETS[0]
  const invalid = rangeError(filters.from, filters.to)
  const [downloading, setDownloading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [rebuilding, setRebuilding] = useState(false)

  const setParams = (patch: Record<string, string | undefined>) => {
    setError(null)
    update((next) => {
      for (const [key, value] of Object.entries(patch)) {
        if (value) next.set(key, value)
        else next.delete(key)
      }
    }, true)
  }

  const limitMessage = (caught: unknown) => {
    if (!isApiError(caught) || caught.code !== 'report_range_too_large') return null
    const details = caught.details ?? {}
    const rowCount = [details.rows, details.rowCount].find((value) => typeof value === 'number')
    if (typeof details.maxRows === 'number' || typeof rowCount === 'number' || typeof details.maxExportRows === 'number') {
      const max = [details.maxRows, details.maxExportRows].find((value) => typeof value === 'number') ?? REPORT_MAX_EXPORT_ROWS
      const rows = typeof rowCount === 'number' ? ` · ${t('rxRowCount')}: ${formatNumber(rowCount)}` : ''
      return `${t('rxErrRowLimit')} (${t('rxMaxRows')}: ${formatNumber(Number(max))}${rows})`
    }
    if (typeof details.maxDays === 'number') return `${t('rpErrRangeTooLarge')} (${t('rpMaxDays')}: ${details.maxDays})`
    return `${t('rxErrRowLimit')} (${t('rxMaxRows')}: ${formatNumber(REPORT_MAX_EXPORT_ROWS)}) — ${t('rpErrRangeTooLarge')} (${t('rpMaxDays')}: ${REPORT_MAX_RANGE_DAYS})`
  }

  const download = async () => {
    if (invalid) return
    setDownloading(true)
    setError(null)
    try {
      const { blob, fileName } = await reports.export({
        dataset,
        from: filters.from,
        to: filters.to,
        cityId: filters.cityId,
        zoneId: filters.zoneId,
        rideCategoryId: filters.rideCategoryId,
      })
      saveBlob(blob, fileName ?? `ata-${dataset}-${filters.from}-${filters.to}.csv`)
      toast.success(t('rxDownloaded'))
    } catch (caught) {
      setError(limitMessage(caught) ?? describe(caught))
    } finally {
      setDownloading(false)
    }
  }

  return (
    <>
      <PageHeader title={t('rxTitle')} description={t('rxCopy')} />

      <ReportFilters value={filters} onChange={setParams} error={invalid ? t(invalid) : null} />

      <Card className="mb-6" title={t('rxDatasetTitle')} description={t('rxDatasetCopy')}>
        <div role="radiogroup" aria-label={t('rxDatasetTitle')} className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
          {REPORT_DATASETS.map((item) => {
            const active = item.value === dataset
            return (
              <button
                key={item.value}
                type="button"
                role="radio"
                aria-checked={active}
                data-dataset={item.value}
                onClick={() => setParams({ dataset: item.value })}
                className={`flex items-center justify-between gap-3 rounded-2xl border px-4 py-3 text-start transition ${active ? 'border-brand bg-brand-soft' : 'border-line bg-white hover:bg-cloud'}`}
              >
                <span className="min-w-0">
                  <span className="block font-bold">{t(item.key)}</span>
                  <span className="ltr-nums block text-xs text-muted">{item.value}</span>
                </span>
                <span className="ltr-nums shrink-0 rounded-full bg-white px-2 text-xs font-bold text-muted">
                  {item.columns.length} {t('rxColumns')}
                </span>
              </button>
            )
          })}
        </div>

        <div className="mt-5">
          <p className="mb-2 text-sm font-bold">
            {t('rxColumnsOf')} {t(selected.key)}
          </p>
          <ul className="flex flex-wrap gap-1.5" dir="ltr">
            {selected.columns.map((column) => (
              <li key={column} className="rounded-full bg-cloud px-2.5 py-1 font-mono text-xs text-ink">
                {column}
              </li>
            ))}
          </ul>
          <p className="mt-3 flex items-start gap-2 text-xs text-muted">
            <Icon name="shield" className="mt-0.5 size-3.5 shrink-0" />
            {t('rxPrivacyNote')}
          </p>
        </div>

        {error && (
          <div role="alert" className="mt-5 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger" data-testid="export-error">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <div className="mt-5 flex flex-wrap items-center gap-3">
          <Button icon="download" loading={downloading} disabled={Boolean(invalid)} onClick={download}>
            {t('rxDownload')}
          </Button>
          <p className="text-xs text-muted">
            {t('rxLimitHint')} <span className="ltr-nums font-bold">{formatNumber(REPORT_MAX_EXPORT_ROWS)}</span>
          </p>
        </div>
      </Card>

      <Card title={t('rxRebuildTitle')} description={t('rxRebuildCopy')}>
        <Button variant="secondary" icon="refresh" disabled={Boolean(invalid)} onClick={() => setRebuilding(true)}>
          {t('rxRebuild')}
        </Button>
      </Card>

      <ConfirmModal
        open={rebuilding}
        title={t('rxRebuildTitle')}
        description={`${t('rxRebuildConfirm')} ${filters.from} → ${filters.to}`}
        confirmLabel={t('rxRebuild')}
        confirmVariant="primary"
        onClose={() => setRebuilding(false)}
        onConfirm={async () => {
          try {
            await reports.rebuildSnapshots({ from: filters.from, to: filters.to })
            toast.success(t('rxRebuildQueued'))
            setRebuilding(false)
          } catch (caught) {
            toast.error(t('errorTitle'), limitMessage(caught) ?? describe(caught))
          }
        }}
      />
    </>
  )
}
