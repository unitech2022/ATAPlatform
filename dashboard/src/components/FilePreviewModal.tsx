import { useEffect, useState } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { files } from '../lib/admin'
import { Button } from './Button'
import { EmptyState } from './EmptyState'
import { Modal } from './Modal'
import { PageSpinner } from './Spinner'

export interface FilePreviewTarget {
  fileId: string
  fileName: string
  title: string
}

type PreviewState = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready'; url: string; mime: string }

/** Loads a protected file (Bearer) into an object URL and shows it inline (image or PDF). */
export function FilePreviewModal({ target, onClose }: { target: FilePreviewTarget | null; onClose: () => void }) {
  // Keyed by file so every preview starts from a fresh loading state.
  return target ? <PreviewDialog key={target.fileId} target={target} onClose={onClose} /> : null
}

function PreviewDialog({ target, onClose }: { target: FilePreviewTarget; onClose: () => void }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [state, setState] = useState<PreviewState>({ kind: 'loading' })

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null
    files
      .blob(target.fileId)
      .then((blob) => {
        if (!active) return
        objectUrl = URL.createObjectURL(blob)
        setState({ kind: 'ready', url: objectUrl, mime: blob.type || guessMime(target.fileName) })
      })
      .catch((error: unknown) => {
        if (active) setState({ kind: 'error', message: describe(error) })
      })
    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [target, describe])

  const download =
    state.kind === 'ready' ? (
      <Button variant="secondary" icon="download" onClick={() => triggerDownload(state.url, target.fileName)}>
        {t('download')}
      </Button>
    ) : null

  return (
    <Modal open title={target.title} description={target.fileName} onClose={onClose} size="xl" footer={download}>
      {state.kind === 'loading' && <PageSpinner />}
      {state.kind === 'error' && <EmptyState tone="danger" icon="alert" title={t('errorTitle')} description={state.message} />}
      {state.kind === 'ready' && <PreviewBody url={state.url} mime={state.mime} unavailable={t('previewUnavailable')} />}
    </Modal>
  )
}

function PreviewBody({ url, mime, unavailable }: { url: string; mime: string; unavailable: string }) {
  if (mime.startsWith('image/')) {
    return (
      <div className="grid place-items-center rounded-2xl bg-cloud p-3">
        <img src={url} alt="" className="max-h-[70vh] max-w-full rounded-xl object-contain" />
      </div>
    )
  }
  if (mime === 'application/pdf') {
    return <iframe title="pdf" src={url} className="h-[70vh] w-full rounded-2xl border border-line bg-cloud" />
  }
  return <EmptyState icon="document" title={unavailable} />
}

function guessMime(fileName: string) {
  const extension = fileName.split('.').pop()?.toLowerCase()
  if (extension === 'pdf') return 'application/pdf'
  if (extension === 'png') return 'image/png'
  if (extension === 'jpg' || extension === 'jpeg') return 'image/jpeg'
  return 'application/octet-stream'
}

function triggerDownload(url: string, fileName: string) {
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
}
