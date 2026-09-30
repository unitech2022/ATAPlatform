import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { Input, Textarea } from '../components/Field'
import { Icon } from '../components/Icon'
import { PermissionError } from '../components/PermissionError'
import { PermissionMatrix } from '../components/PermissionMatrix'
import { PageSpinner } from '../components/Spinner'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { permissionsCatalog, roles as rolesApi } from '../lib/admin'
import { isApiError } from '../lib/api'
import { formatNumber } from '../lib/format'
import { ALL_PERMISSIONS, conflictReasonKey, PERMISSION_CATALOG, roleLabel } from '../lib/rbac'
import type { PermissionInfo, Role } from '../lib/types'

const ROLE_CODE = /^[a-z][a-z0-9_]{1,49}$/

/** Catalogue fallback (§F20.2) when `GET /admin/permissions` is unavailable: codes without server names. */
const FALLBACK_CATALOG: PermissionInfo[] = PERMISSION_CATALOG.map((item) => ({ code: item.code, module: item.module, name: item.code, description: null }))

/** Role editor (`/roles/:id`, `/roles/new`): code, names, description and the permission matrix; system roles keep their code and cannot be deleted. */
export function RoleDetailPage() {
  const { id = 'new' } = useParams()
  const { t } = useLang()
  const isNew = id === 'new'
  const role = useQuery(() => (isNew ? Promise.resolve(null) : rolesApi.get(id)), `role:${id}`)
  const catalog = useQuery(() => permissionsCatalog.list(), 'permissions-catalog')

  if ((role.loading && !role.data && !isNew) || (catalog.loading && !catalog.data && !catalog.error)) return <PageSpinner />
  if (role.error) {
    return (
      <Card>
        <PermissionError error={role.error} permission="admin.roles.manage" onRetry={role.reload} />
      </Card>
    )
  }
  const catalogRows = catalog.data && catalog.data.length > 0 ? catalog.data : FALLBACK_CATALOG
  return (
    <RoleEditor
      key={role.data ? `${role.data.id}:${role.data.updatedAt ?? ''}` : 'new'}
      role={role.data}
      catalog={catalogRows}
      catalogFallback={Boolean(catalog.error) || !catalog.data?.length}
      onSaved={role.reload}
      title={isNew ? t('roNewTitle') : undefined}
    />
  )
}

type Errors = Partial<Record<'code' | 'nameAr' | 'nameEn' | 'form', string>>

function RoleEditor({
  role,
  catalog,
  catalogFallback,
  onSaved,
  title,
}: {
  role: Role | null
  catalog: PermissionInfo[]
  catalogFallback: boolean
  onSaved: () => void
  title?: string
}) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const [code, setCode] = useState(role?.code ?? '')
  const [nameAr, setNameAr] = useState(role?.nameAr ?? '')
  const [nameEn, setNameEn] = useState(role?.nameEn ?? '')
  const [description, setDescription] = useState(role?.description ?? '')
  const [permissionCodes, setPermissionCodes] = useState<string[]>(role?.permissionCodes ?? [])
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState(false)

  const isSystem = role?.isSystem ?? false
  const fullAccess = permissionCodes.includes(ALL_PERMISSIONS)

  const save = async () => {
    const found: Errors = {}
    if (!ROLE_CODE.test(code.trim())) found.code = t('roErrCode')
    if (!nameAr.trim()) found.nameAr = t('roErrName')
    if (!nameEn.trim()) found.nameEn = t('roErrName')
    setErrors(found)
    if (Object.keys(found).length > 0) return
    setSaving(true)
    const input = { code: code.trim(), nameAr: nameAr.trim(), nameEn: nameEn.trim(), description: description.trim() || null, permissionCodes }
    try {
      if (role) {
        await rolesApi.update(role.id, input)
        toast.success(t('roSaved'), t('roSavedCopy'))
        onSaved()
      } else {
        const created = await rolesApi.create(input)
        toast.success(t('roCreated'))
        navigate(created?.id ? `/roles/${created.id}` : '/roles', { replace: true })
      }
    } catch (error) {
      const reason = isApiError(error) ? conflictReasonKey(error.details) : null
      if (reason) setErrors({ form: t(reason) })
      else if (isApiError(error) && error.status === 409) setErrors({ code: t('roErrCodeTaken') })
      else setErrors({ form: describe(error) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <Link to="/roles" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('roBack')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="key" className="size-7" />
            </span>
            <div className="min-w-0">
              {role && <p className="ltr-nums text-sm font-bold text-brand">{role.code}</p>}
              <h2 className="break-words text-2xl font-bold leading-tight">{title ?? (role ? roleLabel(role, lang) : '')}</h2>
              <div className="mt-2 flex flex-wrap gap-2">
                {role && (isSystem ? <Badge tone="ink">{t('roSystem')}</Badge> : <Badge tone="muted">{t('roCustom')}</Badge>)}
                {typeof role?.userCount === 'number' && (
                  <Badge tone="muted">
                    {t('roUsers')}: <span className="ltr-nums ms-1">{formatNumber(role.userCount)}</span>
                  </Badge>
                )}
              </div>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {role && !isSystem && (
              <Button variant="danger-outline" icon="trash" onClick={() => setDeleting(true)}>
                {t('delete')}
              </Button>
            )}
            <Button icon="check" loading={saving} onClick={save}>
              {role ? t('save') : t('roCreate')}
            </Button>
          </div>
        </div>
        {isSystem && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-cloud p-4 text-sm text-muted">
            <Icon name="lock" className="mt-0.5 size-4 shrink-0" />
            <p>{t('roSystemNote')}</p>
          </div>
        )}
      </Card>

      <Card className="mb-6" title={t('roDetailsTitle')}>
        <div className="grid gap-4 md:grid-cols-2">
          <Input
            id="role-code"
            label={t('roCode')}
            dir="ltr"
            value={code}
            disabled={isSystem}
            error={errors.code}
            hint={isSystem ? t('roCodeLocked') : t('roCodeHint')}
            onChange={(event) => setCode(event.target.value.toLowerCase())}
          />
          <div className="hidden md:block" />
          <Input id="role-name-ar" label={t('roNameAr')} dir="rtl" value={nameAr} error={errors.nameAr} onChange={(event) => setNameAr(event.target.value)} />
          <Input id="role-name-en" label={t('roNameEn')} dir="ltr" value={nameEn} error={errors.nameEn} onChange={(event) => setNameEn(event.target.value)} />
          <Textarea
            id="role-description"
            label={t('roDescription')}
            wrapperClassName="md:col-span-2"
            maxLength={255}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </div>
        {errors.form && (
          <div role="alert" className="mt-4 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <span>{errors.form}</span>
          </div>
        )}
      </Card>

      <Card title={t('roMatrixTitle')} description={t('roMatrixCopy')}>
        {fullAccess && (
          <div className="mb-4 flex items-start gap-3 rounded-2xl bg-brand-soft p-4 text-sm text-ink">
            <Icon name="shield" className="mt-0.5 size-4 shrink-0 text-brand" />
            <p>{t('roFullAccessNote')}</p>
          </div>
        )}
        {catalogFallback && <p className="mb-4 text-xs text-muted">{t('roCatalogFallback')}</p>}
        <PermissionMatrix catalog={catalog} value={fullAccess ? catalog.map((item) => item.code) : permissionCodes} onChange={setPermissionCodes} readOnly={fullAccess} />
      </Card>

      <ConfirmModal
        open={deleting}
        title={t('roDeleteTitle')}
        description={t('roDeleteCopy')}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(false)}
        onConfirm={async () => {
          if (!role) return
          try {
            await rolesApi.remove(role.id)
            toast.success(t('roDeleted'))
            navigate('/roles', { replace: true })
          } catch (error) {
            const reason = isApiError(error) ? conflictReasonKey(error.details) : null
            toast.error(t('errorTitle'), reason ? t(reason) : describe(error))
          }
        }}
      />
    </>
  )
}
