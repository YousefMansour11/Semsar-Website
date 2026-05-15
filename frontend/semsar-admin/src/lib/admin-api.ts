/* eslint-disable @typescript-eslint/no-explicit-any */
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { PropertyDto, ProjectDto, UnitDto, CreatePropertyPayload, DashboardStatsDto, CreateProjectPayload, CreateUnitPayload } from './api-types';

export const API_BASE = import.meta.env.VITE_API_BASE || '';

const REQUEST_TIMEOUT = 15000;
const UPLOAD_TIMEOUT = 300000;

export class ApiError extends Error {
  status: number;
  details?: unknown;
  constructor(status: number, message: string, details?: unknown) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

let _token: string | null = null;
let _csrfToken: string | null = null;
let _onUnauthorized: (() => void) | null = null;

function generateCsrfToken(): string {
  try { return crypto.randomUUID(); } catch { return Math.random().toString(36).slice(2, 10) + Date.now().toString(36); }
}

export function setAuthToken(token: string | null) {
  _token = token;
  _csrfToken = token ? generateCsrfToken() : null;
}

export function getCsrfToken(): string | null {
  return _csrfToken;
}

export function setOnUnauthorized(cb: () => void) {
  _onUnauthorized = cb;
}

export function getAuthToken(): string | null {
  return _token;
}

async function request<T>(path: string, init?: RequestInit, timeoutMs?: number): Promise<T> {
  const MAX_RETRIES = 3;
  for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMs ?? REQUEST_TIMEOUT);
    try {
      const headers: Record<string, string> = {
        Accept: 'application/json',
        ...(init?.body && !(init.body instanceof FormData) ? { 'Content-Type': 'application/json' } : {}),
        ...(init?.headers as Record<string, string> || {}),
      };
      if (_token) {
        headers['Authorization'] = `Bearer ${_token}`;
        headers['X-CSRF-Token'] = _csrfToken || '';
      }

      const res = await fetch(`${API_BASE}${path}`, { ...init, headers, signal: controller.signal });

      if (res.status === 401 && _onUnauthorized) {
        _onUnauthorized();
      }

      const text = await res.text();
      const data = text ? safeJson(text) : null;

      if (!res.ok) {
        const msg =
          (data && typeof data === 'object' && ((data as any).title || (data as any).message)) ||
          `Request failed (${res.status})`;

        if (res.status === 429 && attempt < MAX_RETRIES) {
          const retryAfter = res.headers.get('Retry-After');
          const delayMs = retryAfter ? parseInt(retryAfter, 10) * 1000 : Math.min(1000 * Math.pow(2, attempt), 8000);
          if (import.meta.env.DEV) console.warn(`429 on ${path}, retry ${attempt + 1}/${MAX_RETRIES} after ${delayMs}ms`);
          await new Promise(r => setTimeout(r, delayMs));
          continue;
        }

        throw new ApiError(res.status, msg, data);
      }
      return data as T;
    } catch (err: unknown) {
      if (err instanceof ApiError) throw err;
      if (err instanceof DOMException && err.name === 'AbortError') {
        if (attempt < MAX_RETRIES) {
          await new Promise(r => setTimeout(r, Math.min(1000 * Math.pow(2, attempt), 8000)));
          continue;
        }
        throw new ApiError(408, 'Request timed out');
      }
      throw err;
    } finally {
      clearTimeout(timeout);
    }
  }
  throw new ApiError(429, 'Too many requests - max retries exceeded');
}

function safeJson(s: string) {
  try { return JSON.parse(s); } catch { return s; }
}

function qs(params: Record<string, unknown>) {
  const u = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') return;
    u.append(k, String(v));
  });
  const s = u.toString();
  return s ? `?${s}` : '';
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresInHours: number;
  user: { id: number; username: string; role: string };
}

export interface DashboardStats {
  totalProperties: number;
  totalProjects: number;
  rentals: number;
  resale: number;
  projectUnits: number;
  totalUnits: number;
  totalLeads: number;
  featuredProperties?: number;
}

export interface PropertyListItem {
  id: number;
  publicKey?: string;
  titleEn: string;
  titleAr: string;
  price: number;
  rentPerMonth?: number;
  location: string;
  type: string;
  listingType: string;
  size: number;
  isFeatured: boolean;
  isRecommended?: boolean;
  images: string[];
  features: string[];
  propertyCode: string;
  slug?: string;
  deliveryText?: string;
  constructionStatus?: string;
  availabilityStatus?: string;
  ownershipType?: string;
  viewCount?: number;
  inquiryCount?: number;
  favoriteCount?: number;
  virtualTourUrl?: string;
  highlightsAr?: string[];
  nearbyPlaces?: string[];
  nearbyPlacesAr?: string[];
  createdAt: string;
  contact?: { name: string; phone: string; type: string };
  installments: {
    downPaymentPercent: number;
    years: number;
    monthlyAmount?: number;
    isEnabled: boolean;
  }[];
}

export interface ProjectCardDto {
  id: number;
  publicKey?: string;
  nameEn: string;
  nameAr?: string;
  location: string;
  locationAr?: string;
  developer: string;
  image: string;
  slug: string;
  unitCount: number;
  highlights?: string[];
  highlightsAr?: string[];
  startingPrice?: number;
  nearbyPlaces?: string[];
  nearbyPlacesAr?: string[];
  propertyTypes?: string[];
  totalArea?: number;
  ownershipType?: string;
  deliveryText?: string;
  isRecommended?: boolean;
  constructionStatus?: string;
  availabilityStatus?: string;
  viewCount?: number;
  inquiryCount?: number;
  favoriteCount?: number;
  virtualTourUrl?: string;
  descriptionEn?: string;
  descriptionAr?: string;
}

export interface ContactDto {
  id: number;
  name: string;
  phone: string;
  type: string | number;
}

export interface LeadDto {
  id: number;
  name: string;
  phone: string;
  message: string;
  createdAt: string;
  source: string;
  isPaid?: boolean;
  propertyId?: number;
  propertyCode?: string;
  medium?: string;
  campaign?: string;
  term?: string;
  content?: string;
  landingPage?: string;
  firstVisitAt?: string;
  currentPage?: string;
  referrer?: string;
  userAgent?: string;
  pageViews: number;
  sessionDuration?: number;
  lastReferrer?: string;
  visitHistory?: string;
}

export interface BookingDto {
  id: number;
  propertyCode: string;
  propertyTitle: string;
  propertyLocation: string;
  name: string;
  phone: string;
  message: string;
  preferredDate: string;
  source: string;
  propertyId?: number;
  medium?: string;
  campaign?: string;
  term?: string;
  content?: string;
  landingPage?: string;
  firstVisitAt?: string;
  currentPage?: string;
  referrer?: string;
  userAgent?: string;
  pageViews: number;
  sessionDuration?: number;
  lastReferrer?: string;
  visitHistory?: string;
  createdAt: string;
}

export interface LandRequestDto {
  id: number;
  name: string;
  phone: string;
  location: string;
  minPrice: number;
  maxPrice: number;
  minArea: number;
  maxArea: number;
  notes: string;
  source: string;
  medium?: string;
  campaign?: string;
  term?: string;
  content?: string;
  landingPage?: string;
  firstVisitAt?: string;
  currentPage?: string;
  referrer?: string;
  userAgent?: string;
  pageViews: number;
  sessionDuration?: number;
  lastReferrer?: string;
  visitHistory?: string;
  createdAt: string;
}

export interface SettingsDto {
  whatsappNumber: string;
  phoneNumber: string;
  companyName: string;
  socialLinks: { facebook: string; instagram: string; tiktok: string };
}

export interface UploadResponse {
  url: string;
  publicId: string;
}

export const adminApi = {
  login: (username: string, password: string) =>
    request<AuthResponse>(`/api/auth/login`, {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),

  getStats: () =>
    request<DashboardStatsDto>(`/api/stats`),

  getProperties: (params?: { page?: number; pageSize?: number; listingType?: string }) =>
    request<{ data: PropertyListItem[]; total: number; page: number; pageSize: number }>(
      `/api/properties/filter${qs({ ...params, pageSize: params?.pageSize || 100, _t: Date.now() })}`
    ),

  getProperty: (id: number) =>
    request<PropertyDto>(`/api/properties/admin/${id}`),

  createProperty: (dto: CreatePropertyPayload) =>
    request<PropertyDto>(`/api/properties`, { method: 'POST', body: JSON.stringify(dto) }),

  updateProperty: (id: number, dto: Partial<CreatePropertyPayload>) =>
    request<PropertyDto>(`/api/properties/${id}`, { method: 'PATCH', body: JSON.stringify(dto) }),

  deleteProperty: (id: number) =>
    request<void>(`/api/properties/${id}`, { method: 'DELETE' }),

  getProjects: (params?: { page?: number; pageSize?: number }) =>
    request<{ data: ProjectCardDto[]; total: number; page: number; pageSize: number }>(
      `/api/projects${qs({ ...params, pageSize: params?.pageSize || 100, _t: Date.now() })}`
    ),

  getProject: (id: number) =>
    request<ProjectDto>(`/api/projects/${id}`),

  createProject: (dto: CreateProjectPayload) =>
    request<{ id: number; name: string; location: string }>(`/api/projects`, { method: 'POST', body: JSON.stringify(dto) }),

  updateProject: (id: number, dto: Partial<CreateProjectPayload>) =>
    request<ProjectDto>(`/api/projects/${id}`, { method: 'PATCH', body: JSON.stringify(dto) }),

  deleteProject: (id: number) =>
    request<void>(`/api/projects/${id}`, { method: 'DELETE' }),

  getContacts: (params?: { page?: number; pageSize?: number }) =>
    request<{ data: ContactDto[]; total: number; page: number; pageSize: number; totalPages: number }>(
      `/api/contacts${qs({ ...params, pageSize: params?.pageSize || 100 })}`
    ),

  getLeads: (params?: { page?: number; pageSize?: number; type?: string }) =>
    request<{ data: LeadDto[]; total: number; page: number; pageSize: number }>(
      `/api/leads${qs({ ...params, pageSize: params?.pageSize || 100 })}`
    ),

  deleteLead: (id: number) =>
    request<void>(`/api/leads/${id}`, { method: 'DELETE' }),

  getBookings: (params?: { page?: number; pageSize?: number }) =>
    request<{ data: BookingDto[]; total: number; page: number; pageSize: number }>(
      `/api/bookings${qs({ ...params, pageSize: params?.pageSize || 100 })}`
    ),

  deleteBooking: (id: number) =>
    request<void>(`/api/bookings/${id}`, { method: 'DELETE' }),

  getLandRequests: (params?: { page?: number; pageSize?: number }) =>
    request<{ data: LandRequestDto[]; total: number; page: number; pageSize: number; totalPages: number }>(
      `/api/land-requests${qs({ ...params, pageSize: params?.pageSize || 100 })}`
    ),

  createLandRequest: (dto: { name: string; phone: string; location: string; minPrice?: number; maxPrice?: number; minArea?: number; maxArea?: number; notes?: string }) =>
    request<any>(`/api/land-requests`, { method: 'POST', body: JSON.stringify(dto) }),

  deleteLandRequest: (id: number) =>
    request<void>(`/api/land-requests/${id}`, { method: 'DELETE' }),

  getSettings: () =>
    request<SettingsDto>(`/api/settings`),

  updateSettings: (dto: { companyName: string; whatsappNumber: string; phoneNumber: string; socialLinks?: { facebook?: string; instagram?: string; tiktok?: string } }) =>
    request<{ message: string }>(`/api/settings`, { method: 'PUT', body: JSON.stringify(dto) }),

  getUnit: (id: number) =>
    request<any>(`/api/units/admin/${id}`),

  getUnits: (params?: { projectId?: number; page?: number; pageSize?: number }) =>
    request<{ data: any[]; total: number; page: number; pageSize: number }>(
      `/api/units${qs({ ...params, pageSize: params?.pageSize || 100, _t: Date.now() })}`
    ),

  createUnit: (dto: CreateUnitPayload) =>
    request<UnitDto>(`/api/units`, { method: 'POST', body: JSON.stringify(dto) }),

  updateUnit: (id: number, dto: Partial<CreateUnitPayload>) =>
    request<UnitDto>(`/api/units/${id}`, { method: 'PATCH', body: JSON.stringify(dto) }),

  deleteUnit: (id: number) =>
    request<void>(`/api/units/${id}`, { method: 'DELETE' }),

  uploadImage: (file: File, folder: string = 'properties') => {
    const fd = new FormData();
    fd.append('file', file);
    return request<UploadResponse>(`/api/upload?folder=${folder}`, { method: 'POST', body: fd }, UPLOAD_TIMEOUT);
  },

  deletePropertyImage: (id: number, imageId: number) =>
    request<void>(`/api/properties/${id}/images/${imageId}`, { method: 'DELETE' }),

  uploadPropertyImages: async (id: number, files: File[]) => {
    const results: any[] = [];
    for (const file of files) {
      const fd = new FormData();
      fd.append('images', file);
      const result = await request<any>(`/api/properties/${id}/images`, { method: 'POST', body: fd }, UPLOAD_TIMEOUT);
      results.push(result);
    }
    return results;
  },

  deleteUnitImage: (id: number, imageId: number) =>
    request<void>(`/api/units/${id}/images/${imageId}`, { method: 'DELETE' }),

  deleteProjectImage: (id: number, imageId: number) =>
    request<void>(`/api/projects/${id}/images/${imageId}`, { method: 'DELETE' }),

  uploadUnitImages: async (id: number, files: File[]) => {
    const results: any[] = [];
    for (const file of files) {
      const fd = new FormData();
      fd.append('images', file);
      const result = await request<any>(`/api/units/${id}/images`, { method: 'POST', body: fd }, UPLOAD_TIMEOUT);
      results.push(result);
    }
    return results;
  },

  uploadPropertyVideos: async (id: number, files: File[]) => {
    const results: any[] = [];
    for (const file of files) {
      const fd = new FormData();
      fd.append('videos', file);
      const result = await request<any>(`/api/properties/${id}/videos`, { method: 'POST', body: fd }, UPLOAD_TIMEOUT);
      results.push(result);
    }
    return results;
  },

  deletePropertyVideo: (id: number, videoId: number) =>
    request<void>(`/api/properties/${id}/videos/${videoId}`, { method: 'DELETE' }),

  uploadProjectVideos: async (id: number, files: File[]) => {
    const results: any[] = [];
    for (const file of files) {
      const fd = new FormData();
      fd.append('videos', file);
      const result = await request<any>(`/api/projects/${id}/videos`, { method: 'POST', body: fd }, UPLOAD_TIMEOUT);
      results.push(result);
    }
    return results;
  },

  deleteProjectVideo: (id: number, videoId: number) =>
    request<void>(`/api/projects/${id}/videos/${videoId}`, { method: 'DELETE' }),

  uploadUnitVideos: async (id: number, files: File[]) => {
    const results: any[] = [];
    for (const file of files) {
      const fd = new FormData();
      fd.append('videos', file);
      const result = await request<any>(`/api/units/${id}/videos`, { method: 'POST', body: fd }, UPLOAD_TIMEOUT);
      results.push(result);
    }
    return results;
  },

  deleteUnitVideo: (id: number, videoId: number) =>
    request<void>(`/api/units/${id}/videos/${videoId}`, { method: 'DELETE' }),

  // Video library (deduplicated)
  getVideoLibrary: () =>
    request<any[]>(`/api/videos/library`, { method: 'GET' }),

  getVideoLibraryByProject: (projectId: number) =>
    request<any[]>(`/api/videos/library/project/${projectId}`, { method: 'GET' }),

  attachLibraryVideoToProperty: (propertyId: number, publicId: string) =>
    request<any>(`/api/videos/attach/property/${propertyId}`, { method: 'POST', body: JSON.stringify({ publicId }) }),

  getVideoUploadSignature: (folder: string = 'properties', publicId?: string) =>
    request<{ signature: string; timestamp: number; apiKey: string; cloudName: string; folder: string; publicId: string | null; overwrite: boolean | null }>(
      `/api/videos/upload-signature?folder=${folder}${publicId ? `&publicId=${publicId}` : ''}`
    ),

  confirmPropertyVideo: (propertyId: number, data: { url: string; publicId: string; thumbnailUrl?: string; fileName?: string }) =>
    request<any>(`/api/properties/${propertyId}/videos/confirm`, { method: 'POST', body: JSON.stringify(data) }),

  confirmProjectVideo: (projectId: number, data: { url: string; publicId: string; thumbnailUrl?: string; fileName?: string }) =>
    request<any>(`/api/projects/${projectId}/videos/confirm`, { method: 'POST', body: JSON.stringify(data) }),

  confirmUnitVideo: (unitId: number, data: { url: string; publicId: string; thumbnailUrl?: string; fileName?: string }) =>
    request<any>(`/api/units/${unitId}/videos/confirm`, { method: 'POST', body: JSON.stringify(data) }),

  attachLibraryVideoToUnit: (unitId: number, publicId: string) =>
    request<any>(`/api/videos/attach/unit/${unitId}`, { method: 'POST', body: JSON.stringify({ publicId }) }),

  attachLibraryVideoToProject: (projectId: number, publicId: string) =>
    request<any>(`/api/videos/attach/project/${projectId}`, { method: 'POST', body: JSON.stringify({ publicId }) }),

  refreshAuth: (refreshToken: string) =>
    request<AuthResponse>(`/api/auth/refresh`, { method: 'POST', body: JSON.stringify({ refreshToken }) }),
};

export const useAuthStore = create<{
  token: string | null;
  refreshToken: string | null;
  user: { id: number; username: string; role: string } | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  setToken: (token: string | null) => void;
  tryRefresh: () => Promise<boolean>;
}>()(
  persist(
    (set, get) => ({
      token: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      login: async (username, password) => {
        try {
          const res = await adminApi.login(username, password);
          setAuthToken(res.token);
          set({
            token: res.token,
            refreshToken: res.refreshToken,
            user: res.user,
            isAuthenticated: true,
          });
          scheduleTokenRefresh(res.expiresInHours);
          return true;
        } catch {
          return false;
        }
      },
      logout: () => {
        setAuthToken(null);
        set({ token: null, refreshToken: null, user: null, isAuthenticated: false });
        useAuthStore.persist.clearStorage();
        if (_refreshTimer) { clearTimeout(_refreshTimer); _refreshTimer = null; }
      },
      setToken: (token) => {
        setAuthToken(token);
        set({ token, isAuthenticated: !!token });
      },
      tryRefresh: async () => {
        const { refreshToken: rt } = get();
        if (!rt) return false;
        try {
          const res = await adminApi.refreshAuth(rt);
          setAuthToken(res.token);
          set({ token: res.token, refreshToken: res.refreshToken, user: res.user });
          scheduleTokenRefresh(res.expiresInHours);
          return true;
        } catch {
          get().logout();
          return false;
        }
      },
    }),
    { name: 'semsar-admin-auth' }
  )
);

let _refreshTimer: ReturnType<typeof setTimeout> | null = null;
function scheduleTokenRefresh(expiresInHours: number) {
  if (_refreshTimer) clearTimeout(_refreshTimer);
  const ms = Math.max((expiresInHours * 60 * 60 * 1000) / 2, 5 * 60 * 1000);
  _refreshTimer = setTimeout(() => {
    useAuthStore.getState().tryRefresh();
  }, ms);
}

setOnUnauthorized(() => {
  const store = useAuthStore.getState();
  if (store.isAuthenticated) {
    store.logout();
    window.location.href = '/admin/login';
  }
});

const storedToken = useAuthStore.getState().token;
if (storedToken) setAuthToken(storedToken);
