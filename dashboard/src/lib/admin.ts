import { api } from './api'
import type {
  AuditLog,
  AuthResponse,
  DashboardSummary,
  DriverDetail,
  DriverDocument,
  DriverListItem,
  DriverStatus,
  Paginated,
  PassengerListItem,
  RideCategory,
  RideCategoryInput,
} from './types'

export const auth = {
  login: (username: string, password: string) =>
    api.post<AuthResponse>('/auth/admin/login', { username, password }, { anonymous: true }),
  logout: (refreshToken: string) => api.post<void>('/auth/logout', { refreshToken }),
}

export const dashboard = {
  summary: () => api.get<DashboardSummary>('/admin/dashboard/summary'),
}

export type DriverListQuery = {
  status?: DriverStatus | ''
  search?: string
  page?: number
  pageSize?: number
}

export const drivers = {
  list: (query: DriverListQuery) => api.get<Paginated<DriverListItem>>('/admin/drivers', query),
  get: (id: string) => api.get<DriverDetail>(`/admin/drivers/${id}`),
  startReview: (id: string) => api.post<DriverDetail>(`/admin/drivers/${id}/review`, { action: 'start_review' }),
  approve: (id: string) => api.post<DriverDetail>(`/admin/drivers/${id}/approve`),
  reject: (id: string, reason: string) => api.post<DriverDetail>(`/admin/drivers/${id}/reject`, { reason }),
  suspend: (id: string, reason: string) => api.post<DriverDetail>(`/admin/drivers/${id}/suspend`, { reason }),
  reinstate: (id: string) => api.post<DriverDetail>(`/admin/drivers/${id}/reinstate`),
}

export const documents = {
  review: (id: string, status: 'verified' | 'rejected', note?: string) =>
    api.post<DriverDocument>(`/admin/documents/${id}/verify`, { status, note: note || undefined }),
}

export const files = {
  blob: (fileId: string) => api.blob(`/files/${fileId}`),
}

export type PassengerListQuery = {
  search?: string
  page?: number
  pageSize?: number
}

export const passengers = {
  list: (query: PassengerListQuery) => api.get<Paginated<PassengerListItem>>('/admin/passengers', query),
}

export const users = {
  suspend: (userId: string, reason: string) => api.post<void>(`/admin/users/${userId}/suspend`, { reason }),
  reinstate: (userId: string) => api.post<void>(`/admin/users/${userId}/reinstate`),
}

export const rideCategories = {
  list: () => api.get<RideCategory[]>('/admin/ride-categories'),
  create: (input: RideCategoryInput) => api.post<RideCategory>('/admin/ride-categories', input),
  update: (id: string, input: RideCategoryInput) => api.put<RideCategory>(`/admin/ride-categories/${id}`, input),
  remove: (id: string) => api.delete<void>(`/admin/ride-categories/${id}`),
}

export type AuditLogQuery = {
  entityType?: string
  entityId?: string
  page?: number
  pageSize?: number
}

export const auditLogs = {
  list: (query: AuditLogQuery) => api.get<Paginated<AuditLog>>('/admin/audit-logs', query),
}
