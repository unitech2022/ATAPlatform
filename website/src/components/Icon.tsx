import type { ReactNode } from 'react'

export type IconName =
  | 'arrow'
  | 'bell'
  | 'building'
  | 'calendar'
  | 'chart'
  | 'close'
  | 'download'
  | 'edit'
  | 'grid'
  | 'help'
  | 'lock'
  | 'mail'
  | 'receipt'
  | 'settings'
  | 'star'
  | 'trash'
  | 'users'
  | 'car'
  | 'check'
  | 'chevron'
  | 'clock'
  | 'document'
  | 'home'
  | 'location'
  | 'menu'
  | 'pin'
  | 'plus'
  | 'phone'
  | 'search'
  | 'shield'
  | 'upload'
  | 'user'
  | 'wallet'

// Line icons from the design prototype (viewBox 24, stroke 1.8, round caps).
const paths: Record<IconName, ReactNode> = {
  building: (
    <>
      <path d="M4 21V5l8-3v19M12 8h8v13M2 21h20" />
      <path d="M8 8h.01M8 12h.01M8 16h.01M16 12h.01M16 16h.01" />
    </>
  ),
  calendar: (
    <>
      <path d="M4 6h16v15H4zM4 10h16" />
      <path d="M8 3v4M16 3v4" />
    </>
  ),
  chart: <path d="M4 20V10M10 20V4M16 20v-7M22 20H2" />,
  close: <path d="M6 6l12 12M18 6 6 18" />,
  download: (
    <>
      <path d="M12 4v12M7 11l5 5 5-5" />
      <path d="M5 20h14" />
    </>
  ),
  edit: (
    <>
      <path d="M4 20h4L19 9l-4-4L4 16z" />
      <path d="m13 7 4 4" />
    </>
  ),
  grid: <path d="M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4zM13 13h7v7h-7z" />,
  help: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M9.5 9a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .9-1 1.6V14M12 17h.01" />
    </>
  ),
  lock: (
    <>
      <path d="M5 11h14v10H5z" />
      <path d="M8 11V7a4 4 0 0 1 8 0v4" />
    </>
  ),
  mail: (
    <>
      <path d="M3 5h18v14H3z" />
      <path d="m3 7 9 6 9-6" />
    </>
  ),
  receipt: (
    <>
      <path d="M6 2h12v20l-3-2-3 2-3-2-3 2z" />
      <path d="M9 7h6M9 11h6M9 15h4" />
    </>
  ),
  settings: (
    <>
      <circle cx="12" cy="12" r="3" />
      <path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1" />
    </>
  ),
  star: <path d="m12 3 2.8 5.7 6.2.9-4.5 4.4 1 6.2-5.5-2.9-5.5 2.9 1-6.2L3 9.6l6.2-.9z" />,
  trash: (
    <>
      <path d="M4 7h16M9 7V4h6v3M6 7l1 14h10l1-14" />
    </>
  ),
  users: (
    <>
      <circle cx="9" cy="8" r="3.5" />
      <path d="M2 21c.8-4 3.4-6 7-6s6.2 2 7 6" />
      <path d="M16 4.5a3.5 3.5 0 0 1 0 7M18 15c2 .6 3.4 2.6 4 6" />
    </>
  ),
  arrow: <path d="m15 18-6-6 6-6" />,
  bell: (
    <>
      <path d="M6 9a6 6 0 0 1 12 0c0 7 3 7 3 7H3s3 0 3-7" />
      <path d="M10 20h4" />
    </>
  ),
  car: (
    <>
      <path d="M5 17H3v-5l2-5h14l2 5v5h-2" />
      <path d="M5 12h14M7 17h10M7.5 7 9 4h6l1.5 3" />
      <circle cx="7" cy="15" r="1" />
      <circle cx="17" cy="15" r="1" />
    </>
  ),
  check: <path d="m5 12 4 4L19 6" />,
  chevron: <path d="m9 18 6-6-6-6" />,
  clock: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </>
  ),
  document: (
    <>
      <path d="M6 2h8l4 4v16H6z" />
      <path d="M14 2v5h5M9 12h6M9 16h6" />
    </>
  ),
  home: (
    <>
      <path d="m4 11 8-7 8 7v9H4z" />
      <path d="M9 20v-6h6v6" />
    </>
  ),
  location: (
    <>
      <circle cx="12" cy="12" r="8" />
      <circle cx="12" cy="12" r="2" />
      <path d="M12 2V0M12 24v-2M2 12H0M24 12h-2" />
    </>
  ),
  menu: <path d="M4 7h16M4 12h16M4 17h16" />,
  pin: (
    <>
      <path d="M20 10c0 5-8 12-8 12S4 15 4 10a8 8 0 1 1 16 0Z" />
      <circle cx="12" cy="10" r="2.5" />
    </>
  ),
  plus: <path d="M12 5v14M5 12h14" />,
  phone: <path d="M7 3h3l1 5-2 1c1 3 3 5 6 6l1-2 5 1v3c0 2-2 4-4 4C9 20 4 15 3 7c0-2 2-4 4-4Z" />,
  search: (
    <>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-4-4" />
    </>
  ),
  shield: (
    <>
      <path d="M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  upload: (
    <>
      <path d="M12 16V4M7 9l5-5 5 5" />
      <path d="M5 14v6h14v-6" />
    </>
  ),
  user: (
    <>
      <circle cx="12" cy="8" r="4" />
      <path d="M4 21c1-5 4-7 8-7s7 2 8 7" />
    </>
  ),
  wallet: (
    <>
      <path d="M3 6h17v13H3zM3 9h17" />
      <path d="M15 13h5v3h-5z" />
    </>
  ),
}

export function Icon({ name, className = 'size-5' }: { name: IconName; className?: string }) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 24 24"
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="1.8"
    >
      {paths[name]}
    </svg>
  )
}

/** Map a catalog icon code (e.g. "car") to a known icon, defaulting to the car. */
export function CatalogIcon({ code, className }: { code: string; className?: string }) {
  const name: IconName = Object.hasOwn(paths, code) ? (code as IconName) : 'car'
  return <Icon name={name} className={className} />
}
