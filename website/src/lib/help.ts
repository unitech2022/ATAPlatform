import type { HelpAudience } from './types'

export const AUDIENCES: HelpAudience[] = ['passenger', 'driver']

/** `?audience=` → passenger (default) or driver. */
export function parseAudience(value: string | null): HelpAudience {
  return value === 'driver' ? 'driver' : 'passenger'
}
