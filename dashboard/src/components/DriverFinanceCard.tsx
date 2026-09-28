import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { payouts, wallets } from '../lib/admin'
import { cashDebtOf, MAX_CASH_DEBT } from '../lib/finance'
import { formatDate, formatMoney } from '../lib/format'
import { payoutStatusMeta } from '../lib/status'
import type { DriverDetail } from '../lib/types'
import { MetaBadge } from './Badge'
import { Card } from './Card'
import { EmptyState } from './EmptyState'
import { ErrorState } from './ErrorState'
import { Icon } from './Icon'
import { Money } from './Money'
import { PageSpinner } from './Spinner'

/** Wallet balance, cash debt against the limit (docs/08 §F11.3) and the latest payouts of a driver. */
export function DriverFinanceCard({ driver }: { driver: DriverDetail }) {
  const { t, lang } = useLang()
  const phone = driver.user.phoneNumber ?? ''
  const wallet = useQuery(async () => {
    // The wallets endpoint searches by user/phone; pick the driver wallet of this user.
    const page = await wallets.list({ kind: 'driver', search: phone || driver.user.id, page: 1, pageSize: 10 })
    return page.items.find((item) => item.userId === driver.user.id) ?? page.items[0] ?? null
  }, `driver-wallet:${driver.id}:${phone}`)
  const recent = useQuery(() => payouts.list({ driverId: driver.id, status: '', page: 1, pageSize: 5 }), `driver-payouts:${driver.id}`)

  const balance = wallet.data?.balance ?? null
  const debt = balance === null ? 0 : cashDebtOf(balance)
  const ratio = Math.min(100, Math.round((debt / MAX_CASH_DEBT) * 100))
  const overLimit = debt >= MAX_CASH_DEBT

  return (
    <Card
      title={t('finance')}
      className="mb-6"
      action={
        wallet.data ? (
          <Link to={`/wallets/${wallet.data.id}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
            {t('openWallet')}
            <Icon name="chevron" className="size-4 rtl:rotate-180" />
          </Link>
        ) : undefined
      }
    >
      <div className="grid gap-6 lg:grid-cols-2">
        <div className="space-y-3">
          {wallet.loading && !wallet.data ? (
            <PageSpinner />
          ) : wallet.error ? (
            <ErrorState error={wallet.error} onRetry={wallet.reload} />
          ) : !wallet.data ? (
            <EmptyState icon="wallet" title={t('noWallet')} />
          ) : (
            <>
              <div className="rounded-2xl bg-cloud px-4 py-3">
                <p className="text-xs font-bold text-muted">{t('balance')}</p>
                <p className="mt-1 text-2xl font-bold">
                  <Money value={balance} signed strong />
                </p>
              </div>
              <div className={`rounded-2xl px-4 py-3 ${overLimit ? 'bg-danger-soft' : 'bg-cloud'}`}>
                <div className="flex items-center justify-between gap-3">
                  <p className={`text-xs font-bold ${overLimit ? 'text-danger' : 'text-muted'}`}>{t('cashDebt')}</p>
                  <p className="ltr-nums text-sm font-bold">
                    {formatMoney(debt)} / {formatMoney(MAX_CASH_DEBT)} {t('sar')}
                  </p>
                </div>
                <div className="mt-2 h-2 overflow-hidden rounded-full bg-white" dir="ltr">
                  <div className={`h-full rounded-full ${overLimit ? 'bg-danger' : ratio >= 70 ? 'bg-amber-500' : 'bg-brand'}`} style={{ width: `${ratio}%` }} />
                </div>
                {overLimit && <p className="mt-2 text-xs font-bold text-danger">{t('cashDebtOverLimit')}</p>}
              </div>
            </>
          )}
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between gap-3">
            <p className="text-sm font-bold">{t('recentPayouts')}</p>
            <Link to={`/payouts?status=all&driverId=${driver.id}`} className="text-xs font-bold text-brand">
              {t('viewAll')}
            </Link>
          </div>
          {recent.loading && !recent.data ? (
            <PageSpinner />
          ) : recent.error ? (
            <ErrorState error={recent.error} onRetry={recent.reload} />
          ) : (recent.data?.items.length ?? 0) === 0 ? (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('noPayouts')}</p>
          ) : (
            <ul className="divide-y divide-line rounded-2xl border border-line">
              {recent.data?.items.map((payout) => (
                <li key={payout.id} className="flex items-center justify-between gap-3 px-4 py-3">
                  <span className="min-w-0">
                    <span className="ltr-nums block truncate text-sm font-bold">{payout.payoutNumber}</span>
                    <span className="block text-xs text-muted">{formatDate(payout.requestedAt, lang)}</span>
                  </span>
                  <span className="flex shrink-0 flex-col items-end gap-1">
                    <Money value={payout.amount} strong />
                    <MetaBadge record={payoutStatusMeta} value={payout.status} />
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </Card>
  )
}
