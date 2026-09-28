import { useState } from 'react'
import { useI18n } from '../../../i18n'
import { corporateApi, downloadFile } from '../../../lib/api'
import { describeError } from '../../../lib/errors'
import type { CorporateInvoice } from '../../../lib/types'

/** PDF / CSV download buttons with their own busy + error state. */
export function useInvoiceDownloads() {
  const { t } = useI18n()
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const download = async (invoice: CorporateInvoice, kind: 'pdf' | 'csv') => {
    const key = `${invoice.id}:${kind}`
    setBusy(key)
    setError(null)
    try {
      await downloadFile(
        kind === 'pdf' ? corporateApi.invoicePdfPath(invoice.id) : corporateApi.invoiceCsvPath(invoice.id),
        `${invoice.invoiceNumber}.${kind}`,
      )
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(null)
    }
  }
  return { busy, error, download }
}
