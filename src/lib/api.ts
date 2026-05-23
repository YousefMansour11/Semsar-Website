/* eslint-disable @typescript-eslint/no-explicit-any */
// =====================================================================
// Semsar backend API client.
// Base: http://semsar-hub.runasp.net  (HTTP only — HTTPS handled at edge)
// Generated to mirror swagger /api/* endpoints, with adapters that
// normalize the raw DTOs into our existing Property / Project shapes.
// =====================================================================

import type {
  Property,
  Project,
  PropertyType,
  PropertyCategory,
  PropertyListingType,
  PropertyStatus,
  ListingType,
  InstallmentPlan,
  SiteSettings,
  BookingPayload,
  LandRequestPayload,
} from '../types/property';

export const API_BASE = import.meta.env.VITE_API_BASE || 'https://semsar-hub.runasp.net';

const FALLBACK_IMG = '/placeholder.svg';

// --------------------- low-level fetch ---------------------
const REQUEST_TIMEOUT = 15000;

export class ApiError extends Error {
  status: number;
  details?: unknown;
  constructor(status: number, message: string, details?: unknown) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

function sanitize(str: string): string {
  return str.replace(/<[^>]*>/g, '').replace(/[<>]/g, '').trim();
}

function truncate(str: string | undefined, maxLen: number): string | undefined {
  if (!str) return str;
  return str.length > maxLen ? str.slice(0, maxLen) : str;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);
  try {
    const res = await fetch(`${API_BASE}${path}`, {
      ...init,
      signal: controller.signal,
      headers: {
        Accept: 'application/json',
        ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
        ...(init?.headers || {}),
      },
    });
    const text = await res.text();
    const data = text ? safeJson(text) : null;
    if (!res.ok) {
      let msg = `Request failed (${res.status})`;
      if (data && typeof data === 'object') {
        const d = data as Record<string, unknown>;
        msg = (d.title as string) || (d.message as string) || msg;
      }
      throw new ApiError(res.status, msg, data);
    }
    return data as T;
  } catch (err: unknown) {
    if (err instanceof ApiError) throw err;
    if (err instanceof DOMException && err.name === 'AbortError') {
      throw new ApiError(408, 'Request timed out');
    }
    throw err;
  } finally {
    clearTimeout(timeout);
  }
}

function safeJson(s: string) {
  try { return JSON.parse(s); } catch { return s; }
}

function qs(params: Record<string, unknown>) {
  const u = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') return;
    if (Array.isArray(v)) v.forEach(x => u.append(k, String(x)));
    else u.append(k, String(v));
  });
  const s = u.toString();
  return s ? `?${s}` : '';
}

// --------------------- adapters ---------------------
const LISTING_BACKEND_TO_UI: Record<string, ListingType> = {
  Resale: 'sale',
  Project: 'sale',
  Rental: 'rent',
};

function pickStr(o: any, ...keys: string[]): string {
  for (const k of keys) if (o?.[k] != null && o[k] !== '') return String(o[k]);
  return '';
}

function adaptImages(raw: any): string[] {
  const arr =
    raw?.images || raw?.imageUrls || raw?.gallery || raw?.photos || [];
  if (!Array.isArray(arr)) return [];
  return arr
    .map((x: any) => (typeof x === 'string' ? x : x?.url || x?.imageUrl || x?.path))
    .filter(Boolean);
}

function adaptInstallments(raw: any): InstallmentPlan[] {
  const arr = raw?.installments || raw?.installmentPlans || [];
  if (!Array.isArray(arr)) return [];
  return arr
    .filter((p: any) => p && p.isEnabled !== false && !p.isDeleted)
    .map((p: any) => ({
      downPaymentPercent: Number(p.downPaymentPercent ?? 0),
      years: Number(p.years ?? 0),
      monthlyAmount: p.monthlyAmount != null ? Number(p.monthlyAmount) || undefined : undefined,
      isEnabled: p.isEnabled !== false,
      isDeleted: !!p.isDeleted,
    }));
}

export function adaptProperty(raw: any): Property {
  const titleEn = pickStr(raw, 'titleEn', 'title');
  const titleAr = pickStr(raw, 'titleAr') || titleEn;
  const descriptionEn = pickStr(raw, 'descriptionEn', 'description');
  const descriptionAr = pickStr(raw, 'descriptionAr') || descriptionEn;

  const images = adaptImages(raw);
  const primary = raw?.primaryImage || raw?.image || images[0] || FALLBACK_IMG;

  const installments = adaptInstallments(raw);

  const listingBackend = (raw?.listingType ?? 'Resale') as PropertyListingType;
  const listingUi: ListingType = LISTING_BACKEND_TO_UI[listingBackend] || 'sale';

  const numericId = typeof raw?.id === 'number' ? raw.id : undefined;
  const isRawUnit = typeof raw?.publicKey === 'string' && raw.publicKey.startsWith('unt_');

  return {
    id: String(raw?.id ?? raw?.publicKey ?? raw?.slug ?? ''),
    rawId: numericId !== undefined && !isRawUnit ? numericId : undefined,
    rawUnitId: numericId !== undefined && isRawUnit ? numericId : undefined,
    publicKey: raw?.publicKey,
    slug: pickStr(raw, 'slug') || String(raw?.id ?? ''),
    propertyCode: pickStr(raw, 'propertyCode', 'code') || '—',

    titleEn,
    titleAr,
    descriptionEn,
    descriptionAr,

    title: titleEn,
    description: descriptionEn,

    type: (raw?.propertyType || raw?.type || raw?.unitType || raw?.bedroomType || 'Studio') as PropertyType,
    propertyType: (raw?.propertyType ?? 'Apartment') as PropertyCategory,
    listingType: listingUi,
    listingTypeBackend: listingBackend,

    price: Number(raw?.price ?? 0) || 0,
    rentPerMonth: raw?.rentPerMonth != null ? Number(raw.rentPerMonth) : undefined,
    currency: pickStr(raw, 'currency') || 'EGP',

    location: pickStr(raw, 'location') || '—',
    locationAr: raw?.locationAr || undefined,
    size: Number(raw?.size ?? 0) || 0,
    status: (raw?.status ?? 'Available') as PropertyStatus,
    features: Array.isArray(raw?.features) ? raw.features : [],
    featuresAr: Array.isArray(raw?.featuresAr) ? raw.featuresAr : undefined,
    bedrooms: raw?.bedrooms != null ? Number(raw.bedrooms) || undefined : undefined,
    bathrooms: raw?.bathrooms != null ? Number(raw.bathrooms) || undefined : undefined,
    floor: raw?.floor != null ? Number(raw.floor) || undefined : undefined,
    totalFloors: raw?.totalFloors != null ? Number(raw.totalFloors) : undefined,
    isFurnished: raw?.isFurnished != null ? !!raw.isFurnished : undefined,
    view: raw?.view || undefined,
    unitNumber: raw?.unitNumber || undefined,
    buildingNumber: raw?.buildingNumber || undefined,
    deliveryDate: raw?.deliveryDate || undefined,
    finishingType: raw?.finishingType || undefined,
    hasBalcony: raw?.hasBalcony != null ? !!raw.hasBalcony : undefined,
    hasParking: raw?.hasParking != null ? !!raw.hasParking : undefined,

    image: primary,
    images: images.length ? images : [primary],

    projectId: raw?.projectId != null ? String(raw.projectId) : null,

    installments,
    installment: installments[0],

    isFeatured: !!raw?.isFeatured,
  };
}

export function adaptProject(raw: any, units: Property[] = []): Project {
  const nameEn = pickStr(raw, 'nameEn', 'name');
  const nameAr = pickStr(raw, 'nameAr') || nameEn;
  const descriptionEn = pickStr(raw, 'descriptionEn', 'description');
  const descriptionAr = pickStr(raw, 'descriptionAr') || descriptionEn;
  const images = adaptImages(raw);
  const primary = raw?.primaryImage || raw?.image || images[0] || FALLBACK_IMG;

  return {
    id: String(raw?.id ?? raw?.publicKey ?? ''),
    publicKey: raw?.publicKey,
    slug: pickStr(raw, 'slug') || String(raw?.id ?? ''),

    nameEn,
    nameAr,
    descriptionEn,
    descriptionAr,
    name: nameEn,
    description: descriptionEn,

    location: pickStr(raw, 'location') || '—',
    locationAr: raw?.locationAr || undefined,
    developer: raw?.developer || undefined,

    image: primary,
    images: images.length ? images : [primary],
    highlights: Array.isArray(raw?.highlights) ? raw.highlights : [],
    highlightsAr: Array.isArray(raw?.highlightsAr) ? raw.highlightsAr : undefined,
    unitCount: raw?.unitCount ?? units.length,
    units,
  };
}

// --------------------- endpoints ---------------------
function toArray<T>(r: any): T[] {
  if (Array.isArray(r)) return r as T[];
  if (Array.isArray(r?.data)) return r.data as T[];
  if (Array.isArray(r?.items)) return r.items as T[];
  return [];
}

// ---- Properties ----
export interface PropertyFilterParams {
  minPrice?: number;
  maxPrice?: number;
  minSize?: number;
  maxSize?: number;
  location?: string;
  locations?: string[];
  propertyType?: PropertyCategory;
  types?: string[];
  listingType?: PropertyListingType;
  projectId?: number;
  isFeatured?: boolean;
  hasInstallment?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export async function fetchProperties(params: PropertyFilterParams = {}): Promise<Property[]> {
  const raw = await request<any>(`/api/Properties/filter${qs(params as any)}`);
  return toArray(raw).map(adaptProperty);
}

export async function fetchPropertyBySlug(slug: string): Promise<Property | null> {
  try {
    const raw = await request<any>(`/api/Properties/slug/${encodeURIComponent(slug)}`);
    return raw ? adaptProperty(raw) : null;
  } catch (e) {
    if (!(e instanceof ApiError && e.status === 404)) throw e;
  }
  try {
    const raw = await request<any>(`/api/Units/slug/${encodeURIComponent(slug)}`);
    return raw ? adaptProperty(raw) : null;
  } catch (e2) {
    if (e2 instanceof ApiError && e2.status === 404) return null;
    throw e2;
  }
}

export async function fetchFilterMetadata() {
  try {
    return await request<{
      locations: string[];
      locationsAr: string[];
      propertyTypes: { value: string; name: string }[];
      listingTypes: { value: string; name: string }[];
    }>(`/api/Properties/filter/metadata`);
  } catch {
    return { locations: [], locationsAr: [], propertyTypes: [], listingTypes: [] };
  }
}

// ---- Projects ----
export async function fetchProjects(): Promise<Project[]> {
  try {
    const raw = await request<any>(`/api/Projects?page=1&pageSize=50`);
    const list = toArray<any>(raw);
    return list.map(p => adaptProject(p));
  } catch {
    return [];
  }
}

export async function fetchProjectBySlug(slug: string): Promise<Project | null> {
  try {
    const raw = await request<any>(`/api/Projects/slug/${encodeURIComponent(slug)}`);
    if (!raw) return null;
    const projectId = raw?.id;
    let units: Property[] = [];
    if (projectId != null) {
      try {
        const unitsRaw = await request<any>(`/api/Units${qs({ projectId, page: 1, pageSize: 100 })}`);
        units = toArray<any>(unitsRaw).map(adaptProperty);
      } catch { /* ignore unit fetch errors */ }
    }
    return adaptProject(raw, units);
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }
}

// ---- Units ----
export async function fetchUnitBySlug(slug: string): Promise<Property | null> {
  try {
    const raw = await request<any>(`/api/Units/slug/${encodeURIComponent(slug)}`);
    return raw ? adaptProperty(raw) : null;
  } catch (e) {
    if (!(e instanceof ApiError && e.status === 404)) throw e;
  }
  try {
    const raw = await request<any>(`/api/Properties/slug/${encodeURIComponent(slug)}`);
    return raw ? adaptProperty(raw) : null;
  } catch (e2) {
    if (e2 instanceof ApiError && e2.status === 404) return null;
    throw e2;
  }
}

// ---- Settings ----
export async function fetchSettings(): Promise<SiteSettings> {
  let raw: any;
  try {
    raw = await request<any>(`/api/settings`);
  } catch {
    raw = {};
  }
  return {
    whatsappNumber: raw?.whatsappNumber || '',
    phoneNumber: raw?.phoneNumber || raw?.whatsappNumber || '',
    companyName: raw?.companyName || 'Semsar',
    email: raw?.email || undefined,
    socialLinks: {
      facebook: raw?.socialLinks?.facebook || undefined,
      instagram: raw?.socialLinks?.instagram || undefined,
      tiktok: raw?.socialLinks?.tiktok || undefined,
    },
  };
}

function getHoneypotAndTimestamp(submittedAt?: string) {
  return {
    honeypot: '',
    submittedAt: submittedAt || new Date().toISOString(),
  };
}

// ---- Submissions ----
export async function submitBooking(p: BookingPayload, submittedAt?: string) {
  return request<unknown>(`/api/bookings`, {
    method: 'POST',
    body: JSON.stringify({
      ...p,
      ...getHoneypotAndTimestamp(submittedAt),
      name: sanitize(p.name),
      phone: sanitize(p.phone),
      message: p.message ? sanitize(p.message) : undefined,
      userAgent: truncate(p.userAgent, 500),
      landingPage: truncate(p.landingPage, 500),
      currentPage: truncate(p.currentPage, 500),
      referrer: truncate(p.referrer, 500),
      lastReferrer: truncate(p.lastReferrer, 500),
      visitHistory: truncate(p.visitHistory, 8000),
    }),
  });
}

export async function submitLandRequest(p: LandRequestPayload) {
  return request<unknown>(`/api/land-requests`, {
    method: 'POST',
    body: JSON.stringify({
      ...p,
      ...getHoneypotAndTimestamp(),
      name: sanitize(p.name),
      phone: sanitize(p.phone),
      location: sanitize(p.location),
      notes: p.notes ? sanitize(p.notes) : undefined,
    }),
  });
}

export async function submitLead(p: {
  propertyId?: number | null;
  unitId?: number | null;
  name: string;
  phone: string;
  message?: string;
  source?: string;
  medium?: string;
  campaign?: string;
  term?: string;
  content?: string;
  landingPage?: string;
  firstVisitAt?: string;
  currentPage?: string;
  referrer?: string;
  userAgent?: string;
  pageViews?: number;
  sessionDuration?: number;
  lastReferrer?: string;
  visitHistory?: string;
}) {
  return request<unknown>(`/api/leads`, {
    method: 'POST',
    body: JSON.stringify({
      ...p,
      ...getHoneypotAndTimestamp(),
      name: sanitize(p.name),
      phone: sanitize(p.phone),
      message: p.message ? sanitize(p.message) : undefined,
    }),
  });
}
