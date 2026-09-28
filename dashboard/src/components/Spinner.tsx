export function Spinner({ className = 'size-5' }: { className?: string }) {
  return (
    <svg aria-hidden="true" className={`${className} animate-spin`} viewBox="0 0 24 24" fill="none">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeOpacity="0.2" strokeWidth="3" />
      <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
    </svg>
  )
}

export function PageSpinner() {
  return (
    <div className="grid min-h-48 place-items-center text-brand">
      <Spinner className="size-8" />
    </div>
  )
}
