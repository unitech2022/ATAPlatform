/**
 * Runtime configuration. In Docker, `docker-entrypoint.d/40-config.sh` writes
 * `/config.js` with `window.__ATA_CONFIG__`; it wins over build-time
 * `import.meta.env` values so one image can serve every environment.
 */
interface RuntimeConfig {
  apiBaseUrl?: string
}

declare global {
  interface Window {
    __ATA_CONFIG__?: RuntimeConfig
  }
}

function runtimeConfig(): RuntimeConfig {
  return typeof window !== 'undefined' && window.__ATA_CONFIG__ ? window.__ATA_CONFIG__ : {}
}

export const API_BASE_URL = (
  runtimeConfig().apiBaseUrl || import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1'
).replace(/\/+$/, '')

/** The API origin (for absolute URLs such as `/api/v1/public/...` photo paths). */
export const API_ORIGIN = (() => {
  try {
    return new URL(API_BASE_URL).origin
  } catch {
    return window.location.origin
  }
})()

/** Resolve an API-supplied path (`/api/v1/...`) or absolute URL to something an <img> can load. */
export function resolveApiUrl(value: string | null | undefined): string | null {
  if (!value) return null
  if (/^https?:\/\//i.test(value)) return value
  if (value.startsWith('/')) return `${API_ORIGIN}${value}`
  return `${API_BASE_URL}/${value}`
}
