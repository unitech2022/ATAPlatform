import { useState } from 'react'
import { Link } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Select, Textarea } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { Stars } from '../components/Stars'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { ratingFlags } from '../lib/admin'
import { ROLE_KEY } from '../lib/cancellation'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatNumber } from '../lib/format'
import {
  formatAvg,
  RATING_FLAG_ACTION_KEY,
  RATING_FLAG_OUTCOME_KEY,
  RATING_FLAG_REVIEW_ACTIONS,
  RATING_FLAG_TYPE_KEY,
  RATING_FLAG_TYPES,
} from '../lib/rewards'
import { ratingFlagStatusMeta } from '../lib/status'
import type { RatingFlag, RatingFlagReviewAction } from '../lib/types'

const PAGE_SIZE = 20
const STATUS_TABS = ['open', 'actioned', 'dismissed', 'all'] as const

/** Low-rating flags queue (`GET /admin/rating-flags`, §F15.2): open first, review with dismiss / warn / suspension review. */
export function RatingFlagsPage() {
  const { t, lang } = useLang()
  const { params, setFilter, page, setPage } = useUrlState()
  const tab = parseEnum(params.get('status'), STATUS_TABS) || 'open'
  const status = tab === 'all' ? '' : tab
  const type = parseEnum(params.get('type'), RATING_FLAG_TYPES)
  const query = useQuery(() => ratingFlags.list({ status, type, page, pageSize: PAGE_SIZE }), `rating-flags:${status}:${type}:${page}`)
  const [reviewing, setReviewing] = useState<RatingFlag | null>(null)

  const rows = (query.data?.items ?? []).filter((row) => (!status || row.status === status) && (!type || row.type === type))

  const columns: Column<RatingFlag>[] = [
    {
      key: 'user',
      header: t('rtFlagUser'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.userName || t('unnamed')}</span>
          <span className="block text-xs text-muted">{t(ROLE_KEY[row.role] ?? 'actorDriver')}</span>
        </span>
      ),
    },
    {
      key: 'type',
      header: t('type'),
      render: (row) => <Badge tone={row.type === 'abusive_comment' ? 'danger' : 'warning'}>{t(RATING_FLAG_TYPE_KEY[row.type] ?? 'rtFlagTypeLowRating')}</Badge>,
    },
    {
      key: 'value',
      header: t('rtFlagValue'),
      render: (row) =>
        row.type === 'low_average' ? (
          <span className="block">
            <Stars value={row.value} low />
            {typeof row.ratingCount === 'number' && (
              <span className="ltr-nums block text-xs text-muted">
                {formatNumber(row.ratingCount)} {t('rtRatingsCount')}
              </span>
            )}
          </span>
        ) : (
          <Stars value={row.rating?.stars ?? row.value} low />
        ),
    },
    {
      key: 'rating',
      header: t('rtComment'),
      render: (row) =>
        row.rating?.comment ? (
          <span className="block max-w-64 truncate" title={row.rating.comment}>
            {row.rating.comment}
          </span>
        ) : row.rating?.tripNumber ? (
          <span className="ltr-nums text-xs text-muted">{row.rating.tripNumber}</span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap text-xs text-muted">{formatDateTime(row.createdAt, lang)}</span> },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="block">
          <MetaBadge record={ratingFlagStatusMeta} value={row.status} />
          {row.action && row.status !== 'open' && (
            <span className="mt-1 block text-xs text-muted">
              {t(RATING_FLAG_OUTCOME_KEY[row.action] ?? 'rtOutcomeNone')}
              {row.reviewedByName ? ` · ${row.reviewedByName}` : ''}
            </span>
          )}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) =>
        row.status === 'open' ? (
          <Button size="sm" icon="check" onClick={() => setReviewing(row)}>
            {t('rtReview')}
          </Button>
        ) : (
          <Button variant="ghost" size="sm" icon="eye" onClick={() => setReviewing(row)}>
            {t('showDetails')}
          </Button>
        ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('rtFlagsTitle')}
        description={t('rtFlagsCopy')}
        actions={
          <Link to="/ratings" className="inline-flex h-11 items-center gap-2 rounded-2xl border border-line bg-white px-4 text-sm font-bold transition hover:bg-cloud">
            <Icon name="star" className="size-4" />
            {t('navRatings')}
          </Link>
        }
      />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <Tabs
          value={tab}
          onChange={(value) => setFilter('status', value === 'open' ? '' : value)}
          options={STATUS_TABS.map((value) => ({ value, label: value === 'all' ? t('statusAll') : t(ratingFlagStatusMeta[value].key) }))}
        />
        <Select id="flag-type" aria-label={t('type')} value={type} onChange={(event) => setFilter('type', event.target.value)} wrapperClassName="lg:w-64">
          <option value="">{t('allTypes')}</option>
          {RATING_FLAG_TYPES.map((value) => (
            <option key={value} value={value}>
              {t(RATING_FLAG_TYPE_KEY[value])}
            </option>
          ))}
        </Select>
      </div>

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} emptyTitle={t('rtNoFlags')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      {reviewing && (
        <FlagReviewModal
          flag={reviewing}
          onClose={() => setReviewing(null)}
          onDone={() => {
            setReviewing(null)
            query.reload()
          }}
        />
      )}
    </>
  )
}

function FlagReviewModal({ flag, onClose, onDone }: { flag: RatingFlag; onClose: () => void; onDone: () => void }) {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const readOnly = flag.status !== 'open'
  const [action, setAction] = useState<RatingFlagReviewAction>('dismiss')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const submit = async () => {
    const trimmed = note.trim()
    if (action !== 'dismiss' && !trimmed) {
      setError(t('reasonRequired'))
      return
    }
    setSaving(true)
    try {
      await ratingFlags.review(flag.id, action, trimmed)
      toast.success(t('rtFlagReviewed'))
      onDone()
    } catch (caught) {
      toast.error(t('errorTitle'), describe(caught))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      size="lg"
      title={readOnly ? t('rtFlagDetail') : t('rtReviewTitle')}
      description={`${flag.userName || t('unnamed')} · ${t(RATING_FLAG_TYPE_KEY[flag.type] ?? 'rtFlagTypeLowRating')}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={saving}>
            {readOnly ? t('close') : t('cancel')}
          </Button>
          {!readOnly && (
            <Button variant={action === 'dismiss' ? 'primary' : 'danger'} onClick={submit} loading={saving}>
              {t(RATING_FLAG_ACTION_KEY[action])}
            </Button>
          )}
        </>
      }
    >
      <div className="space-y-5">
        {flag.rating && (
          <div className="rounded-2xl bg-cloud p-4 text-sm">
            <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
              <Stars value={flag.rating.stars} low />
              {flag.rating.tripId ? (
                <Link to={`/trips/${flag.rating.tripId}`} className="ltr-nums text-xs font-bold text-brand hover:underline">
                  {flag.rating.tripNumber ?? t('viewTrip')}
                </Link>
              ) : (
                flag.rating.tripNumber && <span className="ltr-nums text-xs text-muted">{flag.rating.tripNumber}</span>
              )}
            </div>
            <p className="whitespace-pre-wrap break-words">{flag.rating.comment || t('rtNoComment')}</p>
          </div>
        )}
        {flag.type === 'low_average' && (
          <p className="rounded-2xl bg-amber-50 px-4 py-3 text-sm text-amber-700">
            {t('rtLowAverageCopy')} <span className="ltr-nums font-bold">{formatAvg(flag.value)}</span>
          </p>
        )}
        <div className="flex flex-wrap gap-3 text-sm">
          {flag.driverId && flag.role === 'driver' && (
            <Link to={`/drivers/${flag.driverId}`} className="inline-flex items-center gap-1 font-bold text-brand">
              {t('rtOpenDriver')}
              <Icon name="chevron" className="size-4 rtl:rotate-180" />
            </Link>
          )}
          <Link to={`/ratings?userId=${flag.userId}`} className="inline-flex items-center gap-1 font-bold text-brand">
            {t('rtUserRatings')}
            <Icon name="chevron" className="size-4 rtl:rotate-180" />
          </Link>
        </div>

        {readOnly ? (
          <div className="rounded-2xl border border-line p-4 text-sm">
            <p className="font-bold">
              <MetaBadge record={ratingFlagStatusMeta} value={flag.status} /> {flag.action ? t(RATING_FLAG_OUTCOME_KEY[flag.action] ?? 'rtOutcomeNone') : ''}
            </p>
            {flag.note && <p className="mt-2 whitespace-pre-wrap">{flag.note}</p>}
            <p className="mt-2 text-xs text-muted">
              {flag.reviewedByName ? `${t('by')} ${flag.reviewedByName} · ` : ''}
              {formatDateTime(flag.reviewedAt, lang)}
            </p>
          </div>
        ) : (
          <>
            <div className="grid gap-2 sm:grid-cols-3">
              {RATING_FLAG_REVIEW_ACTIONS.map((value) => (
                <button
                  key={value}
                  type="button"
                  aria-pressed={action === value}
                  onClick={() => {
                    setAction(value)
                    setError(null)
                  }}
                  className={`rounded-2xl border-2 px-3 py-3 text-start text-sm transition ${action === value ? 'border-brand bg-brand-soft' : 'border-line hover:bg-cloud'}`}
                >
                  <span className="block font-bold">{t(RATING_FLAG_ACTION_KEY[value])}</span>
                  <span className="mt-1 block text-xs text-muted">
                    {t(value === 'dismiss' ? 'rtActionDismissCopy' : value === 'warn' ? 'rtActionWarnCopy' : 'rtActionSuspensionReviewCopy')}
                  </span>
                </button>
              ))}
            </div>
            <Textarea
              id="flag-note"
              label={action === 'dismiss' ? t('noteOptional') : t('note')}
              placeholder={t('reasonPlaceholder')}
              value={note}
              error={error}
              onChange={(event) => {
                setNote(event.target.value)
                setError(null)
              }}
            />
            <p className="flex items-start gap-2 text-xs text-muted">
              <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
              {t('rtReviewAuditNote')}
            </p>
          </>
        )}
      </div>
    </Modal>
  )
}
