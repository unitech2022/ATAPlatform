import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { UserStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { SearchInput } from '../components/Field'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { ReasonModal } from '../components/ReasonModal'
import { ReliabilityCard } from '../components/ReliabilityCard'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useQuery } from '../hooks/useQuery'
import { passengers, users } from '../lib/admin'
import { formatDate, formatNumber } from '../lib/format'
import type { PassengerListItem } from '../lib/types'

const PAGE_SIZE = 20

export function PassengersPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const page = Math.max(1, Number(params.get('page')) || 1)

  const [searchInput, setSearchInput] = useState(search)
  const debouncedSearch = useDebouncedValue(searchInput.trim())
  const [suspendTarget, setSuspendTarget] = useState<PassengerListItem | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [expandedId, setExpandedId] = useState<string | null>(null)

  useEffect(() => {
    if (debouncedSearch === search) return
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        if (debouncedSearch) next.set('q', debouncedSearch)
        else next.delete('q')
        next.delete('page')
        return next
      },
      { replace: true },
    )
  }, [debouncedSearch, search, setParams])

  const query = useQuery(() => passengers.list({ search, page, pageSize: PAGE_SIZE }), `passengers:${search}:${page}`)

  const userIdOf = (row: PassengerListItem) => row.userId ?? row.id

  const suspend = async (reason: string) => {
    if (!suspendTarget) return
    try {
      await users.suspend(userIdOf(suspendTarget), reason)
      toast.success(t('userSuspended'))
      setSuspendTarget(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const reinstate = async (row: PassengerListItem) => {
    setBusyId(row.id)
    try {
      await users.reinstate(userIdOf(row))
      toast.success(t('userReinstated'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyId(null)
    }
  }

  const columns: Column<PassengerListItem>[] = [
    { key: 'fullName', header: t('fullName'), render: (row) => <span className="font-bold">{row.fullName || t('unnamed')}</span> },
    { key: 'phoneNumber', header: t('phoneNumber'), render: (row) => <span className="ltr-nums">{row.phoneNumber}</span> },
    { key: 'status', header: t('status'), render: (row) => <UserStatusBadge status={row.status} /> },
    {
      key: 'tripsCount',
      header: t('tripsCount'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums">{formatNumber(row.tripsCount)}</span>,
    },
    { key: 'createdAt', header: t('createdAt'), render: (row) => formatDate(row.createdAt, lang) },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2">
          <Button
            variant="secondary"
            size="sm"
            icon="gauge"
            aria-expanded={expandedId === row.id}
            onClick={() => setExpandedId((current) => (current === row.id ? null : row.id))}
          >
            {t('rlCardTitle')}
          </Button>
          {row.status === 'suspended' ? (
            <Button variant="brand" size="sm" icon="play" loading={busyId === row.id} onClick={() => reinstate(row)}>
              {t('reinstate')}
            </Button>
          ) : row.status === 'active' ? (
            <Button variant="danger-outline" size="sm" icon="pause" onClick={() => setSuspendTarget(row)}>
              {t('suspend')}
            </Button>
          ) : null}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader title={t('passengersTitle')} description={t('passengersCopy')} />

      <div className="mb-4">
        <SearchInput
          wrapperClassName="lg:w-96"
          className="bg-white shadow-soft"
          placeholder={t('searchPassengers')}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={query.data?.items ?? []}
              rowKey={(row) => row.id}
              loading={query.loading}
              renderExpanded={(row) => (expandedId === row.id ? <ReliabilityCard userId={userIdOf(row)} role="passenger" bare /> : null)}
            />
            {query.data && (
              <Pagination
                page={query.data.page}
                pageSize={query.data.pageSize}
                total={query.data.total}
                onChange={(nextPage) =>
                  setParams((current) => {
                    const next = new URLSearchParams(current)
                    next.set('page', String(nextPage))
                    return next
                  })
                }
              />
            )}
          </>
        )}
      </Card>

      <ReasonModal
        open={suspendTarget !== null}
        title={t('suspendUserTitle')}
        description={
          suspendTarget ? `${suspendTarget.fullName || t('unnamed')} · ${suspendTarget.phoneNumber} — ${t('suspendUserCopy')}` : undefined
        }
        confirmLabel={t('suspend')}
        onClose={() => setSuspendTarget(null)}
        onConfirm={suspend}
      />
    </>
  )
}
