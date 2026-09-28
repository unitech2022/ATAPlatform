import type { DriverReviewAction, DriverStatus } from './types'

/** Admin actions allowed from each application status (docs/05-api-contract.md §9). */
const ALLOWED: Record<DriverStatus, DriverReviewAction[]> = {
  draft: [],
  submitted: ['start_review', 'reject'],
  under_review: ['approve', 'reject'],
  approved: ['suspend'],
  rejected: [],
  suspended: ['reinstate'],
}

export function allowedActions(status: DriverStatus): DriverReviewAction[] {
  return ALLOWED[status] ?? []
}
