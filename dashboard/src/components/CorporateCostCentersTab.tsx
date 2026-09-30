import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { corporateAccounts } from '../lib/admin'
import type { CorporateCostCenter } from '../lib/types'
import { Badge } from './Badge'
import { Card } from './Card'
import { PermissionError } from './PermissionError'
import { Table, type Column } from './Table'

/** Read-only cost centers of a company (assumed `GET /admin/corporate/accounts/{id}/cost-centers`). */
export function CorporateCostCentersTab({ accountId }: { accountId: string }) {
  const { t } = useLang()
  const query = useQuery(() => corporateAccounts.costCenters(accountId), `corporate-cost-centers:${accountId}`)

  const columns: Column<CorporateCostCenter>[] = [
    { key: 'code', header: t('coCcCode'), render: (row) => <span className="ltr-nums font-bold">{row.code}</span> },
    { key: 'name', header: t('coCcName'), render: (row) => row.name },
    { key: 'active', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('statusActive') : t('coInactive')}</Badge> },
  ]

  return (
    <Card flush>
      {query.error ? (
        <PermissionError error={query.error} permission="corporate.manage" onRetry={query.reload} />
      ) : (
        <Table columns={columns} rows={query.data ?? []} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('coCcEmpty')} emptyDescription="" />
      )}
    </Card>
  )
}
