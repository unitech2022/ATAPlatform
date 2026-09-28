import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card, DarkCard } from '../components/Card'
import { ErrorState } from '../components/ErrorState'
import { Input, Textarea } from '../components/Field'
import { Icon } from '../components/Icon'
import { Modal } from '../components/Modal'
import { Money } from '../components/Money'
import { ReasonModal } from '../components/ReasonModal'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { wallets } from '../lib/admin'
import { cashDebtOf, lookupKey, MAX_CASH_DEBT, parseAmount, WALLET_KIND_KEY, WALLET_TX_KEY } from '../lib/finance'
import { formatDateTime, formatMoney } from '../lib/format'
import { walletStatusMeta } from '../lib/status'
import type { WalletAdjustmentInput, WalletDetail, WalletTransaction } from '../lib/types'

type Pending = 'adjust' | 'freeze' | 'unfreeze' | null

export function WalletDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => wallets.get(id), `wallet:${id}`)
  const [pending, setPending] = useState<Pending>(null)

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const wallet = query.data
  const frozen = wallet.status === 'frozen'
  const debt = wallet.cashDebt ?? cashDebtOf(wallet.balance)

  const adjust = async (input: WalletAdjustmentInput) => {
    await wallets.adjust(wallet.id, input)
    toast.success(t('adjustmentPosted'))
    setPending(null)
    query.reload()
  }

  const toggleFreeze = async (reason: string) => {
    try {
      if (frozen) await wallets.unfreeze(wallet.id, reason)
      else await wallets.freeze(wallet.id, reason)
      toast.success(t(frozen ? 'walletUnfrozen' : 'walletFrozen'))
      setPending(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<WalletTransaction>[] = [
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span> },
    {
      key: 'type',
      header: t('type'),
      render: (row) => {
        const key = lookupKey(WALLET_TX_KEY, row.type)
        return <Badge tone={row.type === 'adjustment' ? 'ink' : 'muted'}>{key ? t(key) : row.type}</Badge>
      },
    },
    {
      key: 'amount',
      header: t('amount'),
      className: 'text-end',
      render: (row) => <Money value={row.direction === 'debit' ? -row.amount : row.amount} signed strong />,
    },
    { key: 'balanceAfter', header: t('balanceAfter'), className: 'text-end', render: (row) => <Money value={row.balanceAfter} signed /> },
    {
      key: 'description',
      header: t('description'),
      render: (row) => (
        <span className="block max-w-72">
          <span className="block truncate">{row.description ?? '—'}</span>
          {row.createdByName && <span className="block text-xs text-muted">{t('by')} {row.createdByName}</span>}
        </span>
      ),
    },
    {
      key: 'reference',
      header: t('reference'),
      render: (row) => <span className="ltr-nums block max-w-40 truncate text-xs text-muted">{row.referenceType ? `${row.referenceType}:${row.referenceId ?? ''}` : '—'}</span>,
    },
  ]

  return (
    <>
      <Link to="/wallets" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('backToWallets')}
      </Link>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
            <div className="flex items-start gap-4">
              <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
                <Icon name="wallet" className="size-7" />
              </span>
              <div className="min-w-0">
                <p className="text-sm font-bold text-brand">{t(lookupKey(WALLET_KIND_KEY, wallet.kind) ?? 'walletKindPassenger')}</p>
                <h2 className="text-2xl font-bold leading-tight">{wallet.userName || t('unnamed')}</h2>
                <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                  <MetaBadge record={walletStatusMeta} value={wallet.status} />
                  {wallet.phone && <span className="ltr-nums">{wallet.phone}</span>}
                </div>
              </div>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button icon="edit" onClick={() => setPending('adjust')}>
                {t('manualAdjustment')}
              </Button>
              <Button variant={frozen ? 'brand' : 'danger-outline'} icon={frozen ? 'play' : 'pause'} onClick={() => setPending(frozen ? 'unfreeze' : 'freeze')}>
                {frozen ? t('unfreeze') : t('freeze')}
              </Button>
            </div>
          </div>
          {frozen && (
            <div className="mt-5 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
              <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
              <p>{t('walletFrozenCopy')}</p>
            </div>
          )}
        </Card>

        <DarkCard>
          <p className="text-sm font-bold text-white/60">{t('balance')}</p>
          <p className={`ltr-nums mt-2 text-4xl font-bold leading-tight ${wallet.balance < 0 ? 'text-red-300' : ''}`}>
            {formatMoney(wallet.balance)} <span className="text-base text-white/60">{t('sar')}</span>
          </p>
          {debt > 0 && (
            <div className="mt-4 rounded-2xl bg-white/10 px-4 py-3 text-sm">
              <p className="font-bold">{wallet.kind === 'driver' ? t('cashDebt') : t('outstandingDebt')}</p>
              <p className="ltr-nums mt-1">
                {formatMoney(debt)}
                {wallet.kind === 'driver' ? ` / ${formatMoney(MAX_CASH_DEBT)}` : ''} {t('sar')}
              </p>
            </div>
          )}
        </DarkCard>
      </div>

      <Card title={t('transactions')} description={t('recentTransactionsCopy')} flush>
        <Table columns={columns} rows={wallet.transactions ?? []} rowKey={(row) => row.id} emptyTitle={t('noTransactions')} emptyDescription="" />
      </Card>

      <AdjustmentModal open={pending === 'adjust'} wallet={wallet} onClose={() => setPending(null)} onSubmit={adjust} />
      <ReasonModal
        open={pending === 'freeze' || pending === 'unfreeze'}
        title={frozen ? t('unfreezeTitle') : t('freezeTitle')}
        description={`${wallet.userName ?? ''} — ${frozen ? t('unfreezeCopy') : t('freezeCopy')}`}
        confirmLabel={frozen ? t('unfreeze') : t('freeze')}
        confirmVariant={frozen ? 'brand' : 'danger'}
        onClose={() => setPending(null)}
        onConfirm={toggleFreeze}
      />
    </>
  )
}

function AdjustmentModal({ open, ...props }: { open: boolean; wallet: WalletDetail; onClose: () => void; onSubmit: (input: WalletAdjustmentInput) => Promise<void> }) {
  return open ? <AdjustmentDialog {...props} /> : null
}

function AdjustmentDialog({ wallet, onClose, onSubmit }: { wallet: WalletDetail; onClose: () => void; onSubmit: (input: WalletAdjustmentInput) => Promise<void> }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [direction, setDirection] = useState<'credit' | 'debit'>('credit')
  const [amount, setAmount] = useState('')
  const [reason, setReason] = useState('')
  const [errors, setErrors] = useState<{ amount?: string; reason?: string; form?: string }>({})
  const [submitting, setSubmitting] = useState(false)
  const parsed = parseAmount(amount)
  const after = parsed === null ? null : Math.round((wallet.balance + (direction === 'credit' ? parsed : -parsed)) * 100) / 100

  const submit = async () => {
    const next: typeof errors = {}
    if (parsed === null) next.amount = t('invalidAmount')
    if (!reason.trim()) next.reason = t('reasonRequired')
    setErrors(next)
    if (Object.keys(next).length > 0 || parsed === null) return
    setSubmitting(true)
    try {
      await onSubmit({ direction, amount: parsed, reason: reason.trim() })
    } catch (error) {
      setErrors({ form: describe(error) })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={t('manualAdjustment')}
      description={t('manualAdjustmentCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button icon="check" onClick={submit} loading={submitting}>
            {t('postAdjustment')}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-2 rounded-2xl bg-cloud p-1.5" role="radiogroup" aria-label={t('direction')}>
          {(['credit', 'debit'] as const).map((value) => (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={direction === value}
              onClick={() => setDirection(value)}
              className={`rounded-xl px-3 py-2.5 text-sm font-bold transition ${direction === value ? (value === 'credit' ? 'bg-brand text-white' : 'bg-danger text-white') : 'text-muted hover:bg-white'}`}
            >
              {value === 'credit' ? t('credit') : t('debit')}
            </button>
          ))}
        </div>
        <Input
          id="adjust-amount"
          type="number"
          inputMode="decimal"
          min="0.01"
          step="0.01"
          dir="ltr"
          label={`${t('amount')} (${t('sar')})`}
          value={amount}
          error={errors.amount}
          hint={after !== null ? `${t('balanceAfter')}: ${formatMoney(after)} ${t('sar')}` : undefined}
          onChange={(event) => {
            setAmount(event.target.value)
            setErrors((current) => ({ ...current, amount: undefined }))
          }}
          autoFocus
        />
        <Textarea
          id="adjust-reason"
          label={t('reason')}
          placeholder={t('reasonPlaceholder')}
          value={reason}
          error={errors.reason}
          maxLength={500}
          onChange={(event) => {
            setReason(event.target.value)
            setErrors((current) => ({ ...current, reason: undefined }))
          }}
        />
        <p className="flex items-start gap-2 text-xs text-muted">
          <Icon name="list" className="mt-0.5 size-4 shrink-0" />
          {t('auditedAction')}
        </p>
        {errors.form && (
          <div className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>{errors.form}</p>
          </div>
        )}
      </div>
    </Modal>
  )
}
