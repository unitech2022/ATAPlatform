import { PageHeader } from '../components/PageHeader'
import { CorporateInvoicesPanel } from '../components/CorporateInvoicesPanel'
import { useLang } from '../context/lang'

/** Invoices of every company (`GET /admin/corporate/invoices`, §F19.7): status tabs, totals incl. VAT, manual generation, issue, record payment, void, PDF. */
export function CorporateInvoicesPage() {
  const { t } = useLang()
  return (
    <>
      <PageHeader title={t('coInvoicesTitle')} description={t('coInvoicesCopy')} />
      <CorporateInvoicesPanel />
    </>
  )
}
