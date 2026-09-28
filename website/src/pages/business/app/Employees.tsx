import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { EmployeeStatusPill } from '../../../components/business/StatusPills'
import { Modal, PageHeader, TableWrap, Td, Th } from '../../../components/business/ui'
import { Button } from '../../../components/Button'
import { Input, Select } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { Pagination } from '../../../components/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../../components/States'
import { useI18n } from '../../../i18n'
import { useFlash } from '../../../lib/useFlash'
import { corporateApi } from '../../../lib/api'
import { costCenterLabel, EMPLOYEE_CSV_TEMPLATE } from '../../../lib/corporate'
import { describeError } from '../../../lib/errors'
import { formatMoney } from '../../../lib/format'
import type { EmployeeImportResult } from '../../../lib/types'
import { useDebounced } from '../../../lib/useDebounced'
import { useResource } from '../../../lib/useResource'
import { EmployeeForm } from './EmployeeForm'

function downloadTemplate() {
  // BOM so Excel opens the UTF-8 file correctly.
  const blob = new Blob(['﻿', EMPLOYEE_CSV_TEMPLATE], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = 'ata-employees-template.csv'
  anchor.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 10_000)
}

function ImportDialog({ open, onClose, onDone }: { open: boolean; onClose: () => void; onDone: () => void }) {
  const { t } = useI18n()
  const [file, setFile] = useState<File | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<EmployeeImportResult | null>(null)

  const close = () => {
    setFile(null)
    setError(null)
    setResult(null)
    onClose()
  }

  const upload = async () => {
    if (!file) return
    setBusy(true)
    setError(null)
    try {
      const response = await corporateApi.importEmployees(file)
      setResult(response)
      onDone()
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal open={open} title={t('biz.employees.import')} onClose={close}>
      <p className="text-sm leading-7 text-muted">{t('biz.import.copy')}</p>
      <code className="mt-3 block overflow-x-auto rounded-2xl bg-cloud p-3 text-xs" dir="ltr">
        phone_number,full_name,employee_number,department,cost_center_code,monthly_budget,role
      </code>
      <Button variant="ghost" size="sm" className="mt-2" onClick={downloadTemplate}>
        <Icon name="download" className="size-4" />
        {t('biz.import.template')}
      </Button>

      {result ? (
        <div className="mt-5 space-y-3">
          <Notice tone="success">{t('biz.import.created', { n: result.created })}</Notice>
          {result.skipped.length > 0 && (
            <div className="rounded-2xl border border-line">
              <p className="border-b border-line p-3 text-sm font-bold text-danger">{t('biz.import.skipped', { n: result.skipped.length })}</p>
              <ul className="max-h-60 divide-y divide-line overflow-y-auto text-sm">
                {result.skipped.map((item) => (
                  <li key={`${item.row}-${item.reason}`} className="flex gap-3 p-3">
                    <span className="shrink-0 font-bold">{t('biz.import.row', { n: item.row })}</span>
                    <span className="text-muted">{item.reason}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
          <div className="flex justify-end">
            <Button onClick={close}>{t('action.done')}</Button>
          </div>
        </div>
      ) : (
        <>
          <label className="mt-5 flex cursor-pointer flex-col items-center gap-2 rounded-2xl border-2 border-dashed border-line p-6 text-center hover:border-brand">
            <Icon name="upload" className="size-7 text-brand" />
            <span className="text-sm font-bold">{file ? file.name : t('biz.import.choose')}</span>
            <input
              type="file"
              accept=".csv,text/csv"
              className="sr-only"
              onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            />
          </label>
          {error && (
            <Notice tone="error" className="mt-4">
              {error}
            </Notice>
          )}
          <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <Button variant="secondary" onClick={close} disabled={busy}>
              {t('action.cancel')}
            </Button>
            <Button onClick={() => void upload()} disabled={!file || busy}>
              {busy ? t('docs.uploading') : t('biz.import.upload')}
            </Button>
          </div>
        </>
      )}
    </Modal>
  )
}

export function Employees() {
  const { t, lang } = useI18n()
  const [params, setParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [costCenterId, setCostCenterId] = useState('')
  const [page, setPage] = useState(1)
  const [importOpen, setImportOpen] = useState(false)
  const [flash, setFlash] = useFlash()
  const debounced = useDebounced(search.trim())
  const inviteOpen = params.get('invite') === '1'

  const employees = useResource(
    () => corporateApi.employees({ search: debounced || undefined, status: status || undefined, costCenterId: costCenterId || undefined, page }),
    [debounced, status, costCenterId, page, lang],
  )
  const costCenters = useResource(() => corporateApi.costCenters(), [lang])
  const policies = useResource(() => corporateApi.policies(), [lang])

  const setInviteOpen = (open: boolean) =>
    setParams((current) => {
      const next = new URLSearchParams(current)
      if (open) next.set('invite', '1')
      else next.delete('invite')
      return next
    })

  const resetPage = <T,>(setter: (value: T) => void) => (value: T) => {
    setter(value)
    setPage(1)
  }

  return (
    <>
      <title>{t('biz.nav.employees')} · ATA</title>
      <PageHeader
        title={t('biz.employees.title')}
        subtitle={t('biz.employees.subtitle')}
        actions={
          <>
            <Button variant="secondary" size="sm" onClick={() => setImportOpen(true)}>
              <Icon name="upload" className="size-4" />
              {t('biz.employees.import')}
            </Button>
            <Button size="sm" onClick={() => setInviteOpen(true)}>
              <Icon name="plus" className="size-4" />
              {t('biz.employees.invite')}
            </Button>
          </>
        }
      />

      {flash && (
        <Notice tone="success" className="mb-4">
          {flash}
        </Notice>
      )}

      <div className="mb-4 grid gap-3 sm:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1fr)]">
        <Input
          type="search"
          aria-label={t('biz.search')}
          placeholder={t('biz.employees.searchPlaceholder')}
          value={search}
          onChange={(event) => resetPage(setSearch)(event.target.value)}
        />
        <Select aria-label={t('biz.trip.status')} value={status} onChange={(event) => resetPage(setStatus)(event.target.value)}>
          <option value="">{t('biz.allStatuses')}</option>
          {(['active', 'invited', 'disabled'] as const).map((value) => (
            <option key={value} value={value}>
              {t(`employee.status.${value}`)}
            </option>
          ))}
        </Select>
        <Select aria-label={t('biz.employee.costCenter')} value={costCenterId} onChange={(event) => resetPage(setCostCenterId)(event.target.value)}>
          <option value="">{t('biz.allCostCenters')}</option>
          {(costCenters.data ?? []).map((item) => (
            <option key={item.id} value={item.id}>
              {item.code} · {item.name}
            </option>
          ))}
        </Select>
      </div>

      {employees.loading && !employees.data ? (
        <LoadingState />
      ) : employees.error && !employees.data ? (
        <ErrorState error={employees.error} onRetry={() => employees.reload()} />
      ) : employees.data && employees.data.items.length > 0 ? (
        <>
          <TableWrap>
            <thead>
              <tr>
                <Th>{t('biz.employee.fullName')}</Th>
                <Th>{t('biz.employee.role')}</Th>
                <Th>{t('biz.employee.department')}</Th>
                <Th>{t('biz.employee.costCenter')}</Th>
                <Th>{t('biz.employee.policy')}</Th>
                <Th>{t('biz.employee.spent')}</Th>
                <Th>{t('biz.trip.status')}</Th>
              </tr>
            </thead>
            <tbody>
              {employees.data.items.map((employee) => (
                <tr key={employee.id} className="hover:bg-cloud">
                  <Td>
                    <Link to={`/business/app/employees/${employee.id}`} className="block font-bold text-ink hover:text-brand">
                      {employee.fullName ?? '—'}
                    </Link>
                    <span className="block text-xs text-muted" dir="ltr">
                      {employee.phoneNumber}
                      {employee.employeeNumber ? ` · ${employee.employeeNumber}` : ''}
                    </span>
                  </Td>
                  <Td className="whitespace-nowrap">{t(`biz.role.${employee.role}`)}</Td>
                  <Td>{employee.department ?? '—'}</Td>
                  <Td>{costCenterLabel(employee.costCenter)}</Td>
                  <Td>{employee.policyName ?? t('biz.employee.defaultPolicy')}</Td>
                  <Td className="whitespace-nowrap">
                    <span className="font-bold">{formatMoney(employee.spentThisMonth, lang)}</span>
                    {employee.monthlyBudget !== null && (
                      <span className="block text-xs text-muted">{t('biz.of', { total: formatMoney(employee.monthlyBudget, lang) })}</span>
                    )}
                  </Td>
                  <Td>
                    <EmployeeStatusPill status={employee.status} />
                  </Td>
                </tr>
              ))}
            </tbody>
          </TableWrap>
          <Pagination
            className="mt-4"
            page={employees.data.page}
            pageSize={employees.data.pageSize}
            total={employees.data.total}
            onChange={setPage}
          />
        </>
      ) : (
        <EmptyState icon="users" title={t('biz.employees.empty')}>
          {t('biz.employees.emptyHint')}
        </EmptyState>
      )}

      <Modal open={inviteOpen} title={t('biz.employees.invite')} onClose={() => setInviteOpen(false)} wide>
        <p className="mb-5 text-sm leading-7 text-muted">{t('biz.employees.inviteCopy')}</p>
        <EmployeeForm
          costCenters={costCenters.data ?? []}
          policies={policies.data ?? []}
          submitLabel={t('biz.employees.sendInvite')}
          onCancel={() => setInviteOpen(false)}
          onSubmit={async (payload) => {
            await corporateApi.inviteEmployee(payload)
            setInviteOpen(false)
            setFlash(t('biz.employees.invited', { name: payload.fullName }))
            employees.reload(true)
          }}
        />
      </Modal>

      <ImportDialog open={importOpen} onClose={() => setImportOpen(false)} onDone={() => employees.reload(true)} />
    </>
  )
}
