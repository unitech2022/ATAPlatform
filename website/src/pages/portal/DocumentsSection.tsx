import { Card } from '../../components/Card'
import { Icon } from '../../components/Icon'
import { useI18n } from '../../i18n'
import type { DriverApplication } from '../../lib/types'
import { DocumentRow } from './DocumentRow'

/** Which documents may be (re)uploaded: everything, only rejected ones, or nothing (read-only). */
export type UploadPolicy = 'all' | 'rejected' | 'none'

interface DocumentsSectionProps {
  application: DriverApplication
  uploadPolicy: UploadPolicy
  onChanged: (message: string) => void
}

export function DocumentsSection({ application, uploadPolicy, onChanged }: DocumentsSectionProps) {
  const { t } = useI18n()
  const { requiredDocuments, documents } = application

  const required = requiredDocuments.filter((item) => item.isRequired)
  const done = required.filter((item) => {
    const document = documents.find((candidate) => candidate.documentTypeId === item.documentTypeId)
    return document !== undefined && document.status !== 'rejected'
  }).length

  return (
    <Card>
      <div className="mb-2 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="grid size-11 place-items-center rounded-xl bg-brand-soft text-brand">
            <Icon name="document" />
          </div>
          <p className="text-lg font-bold">{t('docs.title')}</p>
        </div>
        <span className={`rounded-full px-3 py-1 text-xs font-bold ${done === required.length ? 'bg-brand-soft text-brand' : 'bg-cloud text-muted'}`}>
          {t('docs.summary', { done, total: required.length })}
        </span>
      </div>
      <p className="mb-4 text-sm leading-7 text-muted">{t('docs.copy')}</p>

      <div>
        {requiredDocuments.map((item) => {
          const document = documents.find((candidate) => candidate.documentTypeId === item.documentTypeId)
          const canUpload =
            uploadPolicy === 'all'
              ? document?.status !== 'verified'
              : uploadPolicy === 'rejected'
                ? document?.status === 'rejected'
                : false
          return (
            <DocumentRow
              key={item.documentTypeId}
              required={item}
              document={document}
              canUpload={canUpload}
              onChanged={onChanged}
            />
          )
        })}
      </div>
    </Card>
  )
}
