import { useState } from 'react'
import { Link } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { DefinitionList } from '../components/DefinitionList'
import { ErrorState } from '../components/ErrorState'
import { Input, SearchInput, Select } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { ReasonModal } from '../components/ReasonModal'
import { StarRow, Stars } from '../components/Stars'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useRatingTags } from '../hooks/useRatingTags'
import { parseIsoDate, useUrlSearch, useUrlState } from '../hooks/useUrlState'
import { ratings } from '../lib/admin'
import { parseEnum } from '../lib/finance'
import { formatDateTime } from '../lib/format'
import { LOW_RATING_THRESHOLD, RATER_ROLES, RATING_DIRECTION_KEY, RATING_STATUSES } from '../lib/rewards'
import { ratingStatusMeta } from '../lib/status'
import type { AdminRating } from '../lib/types'

const PAGE_SIZE = 20
const STAR_OPTIONS = ['1', '2', '3', '4', '5'] as const
const FLAGGED = ['true', 'false'] as const

/** Ratings (`GET /admin/ratings`, §F15.3): direction tabs, stars/tag/flag/status/date filters, detail drawer, hide/restore. */
export function RatingsPage() {
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, page, setPage } = useUrlState()
  const search = useUrlSearch()
  const tags = useRatingTags()
  const raterRole = parseEnum(params.get('raterRole'), RATER_ROLES)
  const stars = parseEnum(params.get('stars'), STAR_OPTIONS)
  const flagged = parseEnum(params.get('flagged'), FLAGGED)
  const status = parseEnum(params.get('status'), RATING_STATUSES)
  const tag = params.get('tag') ?? ''
  const userId = params.get('userId') ?? ''
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))

  const query = useQuery(
    () =>
      ratings.list({
        raterRole,
        stars: stars ? Number(stars) : '',
        flagged: flagged === '' ? '' : flagged === 'true',
        status,
        tag: tag || undefined,
        userId: userId || undefined,
        from,
        to,
        search: search.value,
        page,
        pageSize: PAGE_SIZE,
      }),
    `ratings:${raterRole}:${stars}:${flagged}:${status}:${tag}:${userId}:${from}:${to}:${search.value}:${page}`,
  )

  const [selected, setSelected] = useState<AdminRating | null>(null)
  const [hiding, setHiding] = useState<AdminRating | null>(null)
  const [restoring, setRestoring] = useState<AdminRating | null>(null)

  // Tag / status filters are sent to the API and re-applied on the current page in case the server ignores them.
  const rows = (query.data?.items ?? []).filter((row) => (!tag || row.tags.includes(tag)) && (!status || row.status === status))

  const afterChange = (updated: AdminRating | undefined, fallback: AdminRating, nextStatus: AdminRating['status']) => {
    setSelected((current) => (current && current.id === fallback.id ? (updated ?? { ...fallback, status: nextStatus }) : current))
    query.reload()
  }

  const hide = async (reason: string) => {
    if (!hiding) return
    try {
      const updated = await ratings.hide(hiding.id, reason)
      toast.success(t('rtHidden'))
      afterChange(updated, { ...hiding, hiddenReason: reason }, 'hidden')
      setHiding(null)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const restore = async () => {
    if (!restoring) return
    try {
      const updated = await ratings.unhide(restoring.id)
      toast.success(t('rtRestored'))
      afterChange(updated, { ...restoring, hiddenReason: null }, 'visible')
      setRestoring(null)
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<AdminRating>[] = [
    {
      key: 'trip',
      header: t('tripNumber'),
      render: (row) => (
        <span className="block">
          <span className="ltr-nums block font-bold">{row.tripNumber}</span>
          <span className="block whitespace-nowrap text-xs text-muted">{formatDateTime(row.createdAt, lang)}</span>
        </span>
      ),
    },
    {
      key: 'parties',
      header: t('rtParties'),
      render: (row) => (
        <span className="block">
          <span className="block font-bold">{row.raterName || t('unnamed')}</span>
          <span className="block text-xs text-muted">
            {t(RATING_DIRECTION_KEY[row.raterRole] ?? 'rtDirPassengerToDriver')} · {row.rateeName || t('unnamed')}
          </span>
        </span>
      ),
    },
    { key: 'stars', header: t('rtStars'), render: (row) => <Stars value={row.stars} low={row.stars <= LOW_RATING_THRESHOLD} /> },
    {
      key: 'tags',
      header: t('rtTags'),
      render: (row) =>
        row.tags.length > 0 ? (
          <span className="flex max-w-60 flex-wrap gap-1">
            {row.tags.map((code) => (
              <Badge key={code} tone={row.stars >= 4 ? 'brand' : 'danger'}>
                {tags.label(code)}
              </Badge>
            ))}
          </span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'comment',
      header: t('rtComment'),
      render: (row) =>
        row.comment ? (
          <span className={`block max-w-64 truncate ${row.commentHidden ? 'text-muted line-through' : ''}`} title={row.comment}>
            {row.comment}
          </span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
    {
      key: 'status',
      header: t('status'),
      render: (row) => (
        <span className="flex flex-wrap gap-1">
          <MetaBadge record={ratingStatusMeta} value={row.status} />
          {row.flagged && (
            <Badge tone="warning">
              <Icon name="flag" className="me-1 size-3" />
              {t('rtFlagged')}
            </Badge>
          )}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2" onClick={(event) => event.stopPropagation()}>
          {row.status === 'hidden' ? (
            <Button variant="secondary" size="sm" icon="eye" onClick={() => setRestoring(row)}>
              {t('rtRestore')}
            </Button>
          ) : (
            <Button variant="danger-outline" size="sm" icon="x" onClick={() => setHiding(row)}>
              {t('rtHide')}
            </Button>
          )}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('rtTitle')}
        description={t('rtCopy')}
        actions={
          <Link to="/ratings/flags" className="inline-flex h-11 items-center gap-2 rounded-2xl border border-line bg-white px-4 text-sm font-bold transition hover:bg-cloud">
            <Icon name="flag" className="size-4" />
            {t('navRatingFlags')}
          </Link>
        }
      />

      <Tabs
        className="mb-4"
        value={raterRole}
        onChange={(value) => setFilter('raterRole', value)}
        options={[
          { value: '', label: t('rtAllDirections') },
          ...RATER_ROLES.map((role) => ({ value: role, label: t(RATING_DIRECTION_KEY[role]) })),
        ]}
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-4">
        <SearchInput placeholder={t('rtSearch')} value={search.input} onChange={(event) => search.setInput(event.target.value)} wrapperClassName="sm:col-span-2" />
        <Input id="rt-from" type="date" aria-label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="rt-to" type="date" aria-label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
        <Select id="rt-stars" aria-label={t('rtStars')} value={stars} onChange={(event) => setFilter('stars', event.target.value)}>
          <option value="">{t('rtAnyStars')}</option>
          {STAR_OPTIONS.map((value) => (
            <option key={value} value={value}>
              {'★'.repeat(Number(value))} ({value})
            </option>
          ))}
        </Select>
        <Select id="rt-tag" aria-label={t('rtTags')} value={tag} onChange={(event) => setFilter('tag', event.target.value)}>
          <option value="">{t('rtAnyTag')}</option>
          {tags.codes.map((code) => (
            <option key={code} value={code}>
              {tags.label(code)}
            </option>
          ))}
        </Select>
        <Select id="rt-flagged" aria-label={t('rtFlagged')} value={flagged} onChange={(event) => setFilter('flagged', event.target.value)}>
          <option value="">{t('rtAnyFlag')}</option>
          <option value="true">{t('rtFlaggedOnly')}</option>
          <option value="false">{t('rtNotFlagged')}</option>
        </Select>
        <Select id="rt-status" aria-label={t('status')} value={status} onChange={(event) => setFilter('status', event.target.value)}>
          <option value="">{t('allStatuses')}</option>
          {RATING_STATUSES.map((value) => (
            <option key={value} value={value}>
              {t(ratingStatusMeta[value].key)}
            </option>
          ))}
        </Select>
      </div>

      {userId && (
        <div className="mb-4 flex flex-wrap items-center gap-2 text-sm">
          <Badge tone="ink">
            {t('userId')}: <span className="ltr-nums ms-1">{userId}</span>
          </Badge>
          <Button variant="ghost" size="sm" icon="x" onClick={() => setFilter('userId', '')}>
            {t('clearFilter')}
          </Button>
        </div>
      )}

      <Card flush>
        {query.error ? (
          <ErrorState error={query.error} onRetry={query.reload} />
        ) : (
          <>
            <Table columns={columns} rows={rows} rowKey={(row) => row.id} loading={query.loading} onRowClick={setSelected} emptyTitle={t('rtEmpty')} emptyDescription="" />
            {query.data && <Pagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.total} onChange={setPage} />}
          </>
        )}
      </Card>

      <Modal
        open={selected !== null}
        size="lg"
        title={selected ? `${t('rtDetailTitle')} · ${selected.tripNumber}` : ''}
        onClose={() => setSelected(null)}
        footer={
          selected && (
            <>
              <Button variant="secondary" onClick={() => setSelected(null)}>
                {t('close')}
              </Button>
              {selected.status === 'hidden' ? (
                <Button icon="eye" onClick={() => setRestoring(selected)}>
                  {t('rtRestore')}
                </Button>
              ) : (
                <Button variant="danger" icon="x" onClick={() => setHiding(selected)}>
                  {t('rtHide')}
                </Button>
              )}
            </>
          )
        }
      >
        {selected && <RatingDetail rating={selected} tagLabel={tags.label} />}
      </Modal>

      <ReasonModal
        open={hiding !== null}
        title={t('rtHideTitle')}
        description={t('rtHideCopy')}
        confirmLabel={t('rtHide')}
        onClose={() => setHiding(null)}
        onConfirm={hide}
      />
      <ConfirmModal
        open={restoring !== null}
        title={t('rtRestoreTitle')}
        description={t('rtRestoreCopy')}
        confirmLabel={t('rtRestore')}
        confirmVariant="primary"
        onClose={() => setRestoring(null)}
        onConfirm={restore}
      />
    </>
  )
}

function RatingDetail({ rating, tagLabel }: { rating: AdminRating; tagLabel: (code: string) => string }) {
  const { t, lang } = useLang()
  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-cloud px-4 py-3">
        <StarRow value={rating.stars} />
        <span className="flex flex-wrap gap-1">
          <MetaBadge record={ratingStatusMeta} value={rating.status} />
          {rating.flagged && <Badge tone="warning">{t('rtFlagged')}</Badge>}
        </span>
      </div>
      <DefinitionList
        items={[
          { label: t('rtDirection'), value: t(RATING_DIRECTION_KEY[rating.raterRole] ?? 'rtDirPassengerToDriver') },
          { label: t('createdAt'), value: formatDateTime(rating.createdAt, lang) },
          { label: t('rtRater'), value: rating.raterName || t('unnamed') },
          { label: t('rtRatee'), value: rating.rateeName || t('unnamed') },
        ]}
      />
      <div>
        <p className="mb-2 text-sm font-bold">{rating.stars >= 4 ? t('rtTagsLiked') : t('rtTagsDisliked')}</p>
        {rating.tags.length > 0 ? (
          <div className="flex flex-wrap gap-1.5">
            {rating.tags.map((code) => (
              <Badge key={code} tone={rating.stars >= 4 ? 'brand' : 'danger'}>
                {tagLabel(code)}
              </Badge>
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted">—</p>
        )}
      </div>
      <div>
        <p className="mb-2 text-sm font-bold">{t('rtComment')}</p>
        {rating.comment ? (
          <p className={`whitespace-pre-wrap break-words rounded-2xl border border-line px-4 py-3 text-sm ${rating.commentHidden ? 'text-muted' : ''}`}>{rating.comment}</p>
        ) : (
          <p className="text-sm text-muted">{t('rtNoComment')}</p>
        )}
        {rating.commentHidden && (
          <p className="mt-2 flex items-center gap-2 text-xs text-danger">
            <Icon name="alert" className="size-3.5" />
            {t('rtCommentAutoHidden')}
          </p>
        )}
      </div>
      {rating.status === 'hidden' && (
        <div className="rounded-2xl bg-danger-soft p-4 text-sm text-danger">
          <p className="font-bold">{t('rtHiddenBecause')}</p>
          <p className="mt-1">{rating.hiddenReason || '—'}</p>
          {(rating.hiddenByName || rating.hiddenAt) && (
            <p className="mt-1 text-xs">
              {rating.hiddenByName ? `${t('by')} ${rating.hiddenByName}` : ''} {rating.hiddenAt ? `· ${formatDateTime(rating.hiddenAt, lang)}` : ''}
            </p>
          )}
        </div>
      )}
      <p className="flex items-start gap-2 text-xs text-muted">
        <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
        {t('rtAuditNote')}
      </p>
      {rating.tripId && (
        <Link to={`/trips/${rating.tripId}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
          {t('viewTrip')}
          <Icon name="chevron" className="size-4 rtl:rotate-180" />
        </Link>
      )}
    </div>
  )
}
