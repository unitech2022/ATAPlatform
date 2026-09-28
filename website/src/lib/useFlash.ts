import { useEffect, useState } from 'react'

/** Auto-dismissing success message. */
export function useFlash(timeout = 5000): [string | null, (message: string | null) => void] {
  const [message, setMessage] = useState<string | null>(null)
  useEffect(() => {
    if (!message) return
    const timer = window.setTimeout(() => setMessage(null), timeout)
    return () => window.clearTimeout(timer)
  }, [message, timeout])
  return [message, setMessage]
}
