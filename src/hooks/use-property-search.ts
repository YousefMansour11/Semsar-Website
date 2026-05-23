import { useQuery } from '@tanstack/react-query';
import { API_BASE, adaptProperty } from '../lib/api';
import type { Property } from '../types/property';

export interface SearchFilters {
  locationIds?: number[];
  minPrice?: number;
  maxPrice?: number;
  minSize?: number;
  maxSize?: number;
  bedrooms?: number;
  bathrooms?: number;
  propertyType?: string;
  listingType?: string;
  features?: string[];
  isFurnished?: boolean;
  hasInstallment?: boolean;
  keyword?: string;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

export interface PropertySearchResult {
  id: number;
  publicKey: string;
  slug: string;
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  price: number;
  rentPerMonth?: number;
  currency: string;
  propertyType: string;
  listingType: string;
  location: string;
  size: number;
  bedrooms: number;
  bathrooms: number;
  isFeatured: boolean;
  image?: string;
  images: string[];
  features: string[];
  featuresAr?: string[];
  locationAr?: string;
  code?: string;
  createdAt: string;
  installments?: Array<{
    downPaymentPercent: number;
    years: number;
    monthlyAmount?: number;
    isEnabled?: boolean;
    isDeleted?: boolean;
  }>;
}

export interface PropertySearchResponse {
  data: PropertySearchResult[];
  totalCount: number;
  totalPages: number;
  page: number;
  pageSize: number;
}

function adaptSearchResult(r: PropertySearchResult): Property {
  return adaptProperty(r as unknown as Record<string, unknown>);
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

export async function fetchPropertySearch(filters: SearchFilters): Promise<{ properties: Property[]; totalCount: number; totalPages: number }> {
  const params: Record<string, unknown> = {
    locationIds: filters.locationIds?.length ? filters.locationIds : undefined,
    isFurnished: filters.isFurnished ?? undefined,
    hasInstallment: filters.hasInstallment ?? undefined,
    minPrice: filters.minPrice,
    maxPrice: filters.maxPrice,
    minSize: filters.minSize,
    maxSize: filters.maxSize,
    bedrooms: filters.bedrooms,
    bathrooms: filters.bathrooms,
    propertyType: filters.propertyType,
    listingType: filters.listingType,
    features: filters.features?.length ? filters.features.join(',') : undefined,
    keyword: filters.keyword || undefined,
    sortBy: filters.sortBy || 'newest',
    page: filters.page || 1,
    pageSize: filters.pageSize || 50,
  };

  const res = await fetch(`${API_BASE}/properties/search${qs(params)}`);
  if (!res.ok) throw new Error(`Search failed (${res.status})`);
  const json: PropertySearchResponse = await res.json();
  return {
    properties: json.data.map(adaptSearchResult),
    totalCount: json.totalCount,
    totalPages: json.totalPages,
  };
}

export function usePropertySearch(filters: SearchFilters) {
  return useQuery({
    queryKey: ['property-search', filters],
    queryFn: () => fetchPropertySearch(filters),
    staleTime: 30_000,
  });
}

export { adaptSearchResult };
