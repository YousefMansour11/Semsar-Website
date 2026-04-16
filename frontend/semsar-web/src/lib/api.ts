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
  Variant,
  VideoItem,
  NearbyPlace,
  SiteSettings,
  BookingPayload,
  LandRequestPayload,
  AvailabilityStatus,
  OwnershipType,
  ConstructionStatus,
} from '../types/property';
import { getInteractionTimestamp, getHoneypotField } from './security';

export const API_BASE = '/api';

const FALLBACK_IMG = '/placeholder.svg';

// --------------------- low-level fetch ---------------------
const REQUEST_TIMEOUT = 15000;

export class ApiError extends Error {
  status: number;
  details?: unknown;
  retryAfterMs?: number;
  constructor(status: number, message: string, details?: unknown, retryAfterMs?: number) {
    super(message);
    this.status = status;
    this.details = details;
    this.retryAfterMs = retryAfterMs;
  }
}

function sanitize(str: string): string {
  return str
    .replace(/<[^>]*>/g, '')
    .replace(/[<>]/g, '')
    .replace(/javascript:/gi, '')
    .replace(/on\w+=/gi, '')
    .trim();
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
      let retryAfterMs: number | undefined;
      if (data && typeof data === 'object') {
        const d = data as Record<string, unknown>;
        msg = (d.title as string) || (d.message as string) || msg;
        retryAfterMs = d.retryAfterMs as number | undefined;
      }
      if (!retryAfterMs && res.status === 429) {
        const retryAfter = res.headers.get('Retry-After');
        if (retryAfter) retryAfterMs = parseInt(retryAfter, 10) * 1000;
      }
      throw new ApiError(res.status, msg, data, retryAfterMs);
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

function isValidImageUrl(url: string): boolean {
  if (!url) return false;
  return url.includes('res.cloudinary.com') ||
    url.startsWith('http://') ||
    url.startsWith('https://') ||
    url.startsWith('/');
}

function adaptImages(raw: any): string[] {
  const arr =
    raw?.images || raw?.imageUrls || raw?.gallery || raw?.photos || [];
  if (!Array.isArray(arr)) return [];
  return arr
    .map((x: any) => (typeof x === 'string' ? x : x?.url || x?.imageUrl || x?.path))
    .filter(Boolean)
    .filter(isValidImageUrl);
}

function adaptVideos(raw: any): VideoItem[] | undefined {
  const arr = raw?.videos;
  if (!Array.isArray(arr) || arr.length === 0) return undefined;
  return arr.map((v: any) => ({
    id: v.id ?? v.Id,
    url: v.url ?? v.Url ?? '',
    publicId: v.publicId ?? v.PublicId ?? '',
    thumbnailUrl: v.thumbnailUrl ?? v.ThumbnailUrl ?? undefined,
  })).filter(v => !!v.url);
}

function adaptVariants(raw: any): Variant[] | undefined {
  const arr = raw?.variants || raw?.Variants;
  if (!Array.isArray(arr) || arr.length === 0) return undefined;
  return arr
    .filter((v: any) => v && v.isActive !== false && !v.isDeleted)
    .map((v: any) => ({
      id: v.id ?? v.Id,
      publicKey: v.publicKey ?? v.PublicKey,
      name: v.name ?? v.Name ?? '',
      nameAr: v.nameAr ?? v.NameAr,
      size: Number(v.size ?? v.Size ?? 0),
      price: Number(v.price ?? v.Price ?? 0),
      images: adaptImages(v),
      currency: v.currency ?? v.Currency ?? 'EGP',
      rentPerMonth: v.rentPerMonth ?? v.RentPerMonth,
      bedrooms: Number(v.bedrooms ?? v.Bedrooms ?? 0),
      bathrooms: Number(v.bathrooms ?? v.Bathrooms ?? 0),
      floor: v.floor ?? v.Floor,
      isFurnished: !!(v.isFurnished ?? v.IsFurnished),
      view: v.view ?? v.View,
      unitNumber: v.unitNumber ?? v.UnitNumber,
      buildingNumber: v.buildingNumber ?? v.BuildingNumber,
      deliveryDate: v.deliveryDate ?? v.DeliveryDate,
      finishingType: v.finishingType ?? v.FinishingType,
      hasBalcony: !!(v.hasBalcony ?? v.HasBalcony),
      hasParking: !!(v.hasParking ?? v.HasParking),
      floorPlanUrl: v.floorPlanUrl ?? v.FloorPlanUrl,
      availabilityStatus: (v.availabilityStatus ?? v.AvailabilityStatus ?? 'Available') as AvailabilityStatus,
      sortOrder: Number(v.sortOrder ?? v.SortOrder ?? 0),
      isActive: v.isActive !== false && !v.IsDeleted,
      isFeatured: !!(v.isFeatured ?? v.IsFeatured),
      isRecommended: !!(v.isRecommended ?? v.IsRecommended),
      viewCount: v.viewCount ?? v.ViewCount,
      inquiryCount: v.inquiryCount ?? v.InquiryCount,
      favoriteCount: v.favoriteCount ?? v.FavoriteCount,
    }))
    .sort((a, b) => {
      if (a.isRecommended !== b.isRecommended) return a.isRecommended ? -1 : 1;
      if (a.isFeatured !== b.isFeatured) return a.isFeatured ? -1 : 1;
      return a.sortOrder - b.sortOrder;
    });
}

function adaptInstallments(raw: any): InstallmentPlan[] {
  const arr = raw?.installments || raw?.installmentPlans || [];
  if (!Array.isArray(arr)) return [];
  return arr
    .filter((p: any) => p && p.isEnabled !== false && !p.isDeleted)
    .map((p: any) => ({
      paymentType: (p.paymentType ?? p.PaymentType) === 'Cash' ? 'Cash' as const : 'Installment' as const,
      downPaymentPercent: Number(p.downPaymentPercent ?? 0),
      discountPercent: (p.discountPercent ?? p.DiscountPercent) != null ? Number(p.discountPercent ?? p.DiscountPercent) : undefined,
      years: Number(p.years ?? 0),
        installmentMonths: (p.installmentMonths ?? p.InstallmentMonths) ?? ((Number(p.years ?? 0) * 12) || undefined),
      quarterlyAmount: p.quarterlyAmount != null ? Number(p.quarterlyAmount) : undefined,
      monthlyAmount: p.monthlyAmount != null ? Number(p.monthlyAmount) || undefined : undefined,
      isEnabled: p.isEnabled !== false,
      isDeleted: !!p.isDeleted,
    }));
}

function adaptNearbyPlaces(raw: any): NearbyPlace[] | undefined {
  const arr = raw?.nearbyPlaces || raw?.NearbyPlaces;
  if (!Array.isArray(arr) || arr.length === 0) return undefined;
  return arr.map((p: any) => ({
    name: typeof p === 'string' ? p : (p.name ?? p.Name ?? ''),
    nameAr: typeof p === 'string' ? undefined : (p.nameAr ?? p.NameAr),
    distance: typeof p === 'number' ? p : Number(p.distance ?? p.Distance ?? 0),
    icon: typeof p === 'string' ? undefined : (p.icon ?? p.Icon),
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

  const variants = adaptVariants(raw);

  const pVariants = variants;
  const pMinPrice = raw?.minPrice != null ? Number(raw.minPrice) : undefined;
  const pMaxPrice = raw?.maxPrice != null ? Number(raw.maxPrice) : undefined;
  const pMinArea = raw?.minArea != null ? Number(raw.minArea) : undefined;
  const pMaxArea = raw?.maxArea != null ? Number(raw.maxArea) : undefined;

  const autoMinPrice = pMinPrice ?? (pVariants?.length ? Math.min(...pVariants.map(v => v.price)) : undefined);
  const autoMaxPrice = pMaxPrice ?? (pVariants?.length ? Math.max(...pVariants.map(v => v.price)) : undefined);
  const autoMinArea = pMinArea ?? (pVariants?.length ? Math.min(...pVariants.map(v => v.size)) : undefined);
  const autoMaxArea = pMaxArea ?? (pVariants?.length ? Math.max(...pVariants.map(v => v.size)) : undefined);

  const floorPlansRaw = raw?.floorPlans || raw?.FloorPlans;
  const floorPlans = Array.isArray(floorPlansRaw) ? floorPlansRaw.filter(Boolean) : undefined;

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

    price: Number(raw?.price ?? autoMinPrice ?? 0) || 0,
    minPrice: autoMinPrice,
    maxPrice: autoMaxPrice,
    rentPerMonth: raw?.rentPerMonth != null ? Number(raw.rentPerMonth) : undefined,
    currency: pickStr(raw, 'currency') || 'EGP',

    location: pickStr(raw, 'location') || '—',
    locationAr: raw?.locationAr || undefined,
    size: Number(raw?.size ?? autoMinArea ?? 0) || 0,
    minArea: autoMinArea,
    maxArea: autoMaxArea,
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
    deliveryText: raw?.deliveryText || raw?.DeliveryText || undefined,
    deliveryTextAr: raw?.deliveryTextAr || raw?.DeliveryTextAr || undefined,
    finishingType: raw?.finishingType || undefined,
    hasBalcony: raw?.hasBalcony != null ? !!raw.hasBalcony : undefined,
    hasParking: raw?.hasParking != null ? !!raw.hasParking : undefined,

    image: primary,
    images: images.length ? images : [primary],
    videos: adaptVideos(raw),
    floorPlans,
    virtualTourUrl: raw?.virtualTourUrl || raw?.VirtualTourUrl || undefined,

    projectId: raw?.projectId != null ? String(raw.projectId) : null,

    variants,
    unitType: pickStr(raw, 'unitType') || undefined,

    installments,
    installment: installments[0],

    isFeatured: !!raw?.isFeatured,
    highlights: Array.isArray(raw?.highlights) ? raw.highlights : [],
    highlightsAr: Array.isArray(raw?.highlightsAr) ? raw.highlightsAr : undefined,
    nearbyPlaces: adaptNearbyPlaces(raw),

    seoTitleEn: raw?.seoTitleEn || raw?.SeoTitleEn || undefined,
    seoTitleAr: raw?.seoTitleAr || raw?.SeoTitleAr || undefined,
    seoDescriptionEn: raw?.seoDescriptionEn || raw?.SeoDescriptionEn || undefined,
    seoDescriptionAr: raw?.seoDescriptionAr || raw?.SeoDescriptionAr || undefined,

    viewCount: raw?.viewCount ?? raw?.ViewCount,
    inquiryCount: raw?.inquiryCount ?? raw?.InquiryCount,
    favoriteCount: raw?.favoriteCount ?? raw?.FavoriteCount,

    ownershipType: (raw?.ownershipType || raw?.OwnershipType) as OwnershipType | undefined,
    constructionStatus: (raw?.constructionStatus || raw?.ConstructionStatus) as ConstructionStatus | undefined,
  };
}

export function adaptProject(raw: any, units: Property[] = []): Project {
  const nameEn = pickStr(raw, 'nameEn', 'name');
  const nameAr = pickStr(raw, 'nameAr') || nameEn;
  const descriptionEn = pickStr(raw, 'descriptionEn', 'description');
  const descriptionAr = pickStr(raw, 'descriptionAr') || descriptionEn;
  const images = adaptImages(raw);
  const primary = raw?.primaryImage || raw?.image || images[0] || FALLBACK_IMG;

  const activeUnits = units.filter(u => u.variants?.length || true);
  const unitVariants = activeUnits.flatMap(u => u.variants || []).filter(Boolean);

  const autoStartingPrice = raw?.startingPrice != null ? Number(raw.startingPrice) : (unitVariants.length ? Math.min(...unitVariants.map(v => v.price)) : undefined);
  const autoHighestPrice = raw?.highestPrice != null ? Number(raw.highestPrice) : (unitVariants.length ? Math.max(...unitVariants.map(v => v.price)) : undefined);
  const allUnitTypes = [...new Set(units.map(u => u.propertyType).filter(Boolean))];

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
    videos: adaptVideos(raw),
    highlights: Array.isArray(raw?.highlights) ? raw.highlights : [],
    highlightsAr: Array.isArray(raw?.highlightsAr) ? raw.highlightsAr : undefined,
    startingPrice: autoStartingPrice,
    highestPrice: autoHighestPrice,
    propertyTypes: raw?.propertyTypes?.length ? raw.propertyTypes : (allUnitTypes.length ? allUnitTypes : undefined),
    totalArea: raw?.totalArea != null ? Number(raw.totalArea) : undefined,
    latitude: raw?.latitude != null ? Number(raw.latitude) : undefined,
    longitude: raw?.longitude != null ? Number(raw.longitude) : undefined,
    ownershipType: raw?.ownershipType ?? undefined,
    nearbyPlaces: Array.isArray(raw?.nearbyPlaces) ? raw.nearbyPlaces : undefined,
    nearbyPlacesAr: Array.isArray(raw?.nearbyPlacesAr) ? raw.nearbyPlacesAr : undefined,
    unitCount: raw?.unitCount ?? units.length,
    totalAvailableUnits: raw?.totalAvailableUnits ?? raw?.unitCount ?? activeUnits.length,
    totalReservedUnits: raw?.totalReservedUnits,
    totalSoldUnits: raw?.totalSoldUnits,
    unitTypesCount: raw?.unitTypesCount ?? allUnitTypes.length,
    units,
    deliveryText: raw?.deliveryText || raw?.DeliveryText || undefined,
    deliveryTextAr: raw?.deliveryTextAr || raw?.DeliveryTextAr || undefined,
    constructionStatus: (raw?.constructionStatus || raw?.ConstructionStatus) as ConstructionStatus | undefined,
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
  const raw = await request<any>(`/Properties/filter${qs(params as any)}`);
  return toArray(raw).map(adaptProperty);
}

export async function fetchPropertyBySlug(slug: string): Promise<Property | null> {
  try {
    const raw = await request<any>(`/Properties/slug/${encodeURIComponent(slug)}`);
    return raw ? adaptProperty(raw) : null;
  } catch (e) {
    if (!(e instanceof ApiError && e.status === 404)) throw e;
  }
  try {
    const raw = await request<any>(`/Units/slug/${encodeURIComponent(slug)}`);
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
    }>(`/Properties/filter/metadata`);
  } catch {
    return { locations: [], locationsAr: [], propertyTypes: [], listingTypes: [] };
  }
}

// ---- Projects ----
export async function fetchProjects(): Promise<Project[]> {
  try {
    const raw = await request<any>(`/Projects?page=1&pageSize=50`);
    const list = toArray<any>(raw);
    return list.map(p => adaptProject(p));
  } catch {
    return [];
  }
}

export async function fetchProjectBySlug(slug: string): Promise<Project | null> {
  try {
    const raw = await request<any>(`/Projects/slug/${encodeURIComponent(slug)}`);
    if (!raw) return null;
    const projectId = raw?.id;
    let units: Property[] = [];
    if (projectId != null) {
      try {
        const unitsRaw = await request<any>(`/Units${qs({ projectId, page: 1, pageSize: 20 })}`);
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
    const raw = await request<any>(`/Units/slug/${encodeURIComponent(slug)}`);
    return raw ? adaptProperty(raw) : null;
  } catch (e) {
    if (!(e instanceof ApiError && e.status === 404)) throw e;
  }
  try {
    const raw = await request<any>(`/Properties/slug/${encodeURIComponent(slug)}`);
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
    raw = await request<any>(`/settings`);
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

function getSecurityFields(submittedAt?: string) {
  const hp = getHoneypotField();
  return {
    [hp.name]: hp.value,
    interactionTimestamp: getInteractionTimestamp() || undefined,
    submittedAt: submittedAt || new Date().toISOString(),
  };
}

// ---- Submissions ----
export async function submitBooking(p: BookingPayload, submittedAt?: string) {
  return request<unknown>(`/bookings`, {
    method: 'POST',
    body: JSON.stringify({
      ...p,
      ...getSecurityFields(submittedAt),
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
  return request<unknown>(`/land-requests`, {
    method: 'POST',
    body: JSON.stringify({
      ...p,
      ...getSecurityFields(),
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
  return request<unknown>(`/leads`, {
    method: 'POST',
    body: JSON.stringify({
      ...p,
      ...getSecurityFields(),
      name: sanitize(p.name),
      phone: sanitize(p.phone),
      message: p.message ? sanitize(p.message) : undefined,
    }),
  });
}
