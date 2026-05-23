import { useQuery, keepPreviousData } from '@tanstack/react-query';
import {
  fetchProperties,
  fetchPropertyBySlug,
  fetchUnitBySlug,
  fetchProjects,
  fetchProjectBySlug,
  type PropertyFilterParams,
} from '../lib/api';
import type { AdvancedFilterState, Project, Property } from '../types/property';

function toFilterParams(
  f: AdvancedFilterState | undefined,
  listingType: 'Resale' | 'Rental' | 'Project',
): PropertyFilterParams {
  return {
    listingType,
    minPrice: f && f.minPrice !== '' ? Number(f.minPrice) : undefined,
    maxPrice: f && f.maxPrice !== '' ? Number(f.maxPrice) : undefined,
    minSize: f && f.minSize !== '' ? Number(f.minSize) : undefined,
    maxSize: f && f.maxSize !== '' ? Number(f.maxSize) : undefined,
    locations: f?.locations?.length ? f.locations : undefined,
    propertyType: f?.types?.[0] || undefined,
    pageSize: 50,
  };
}

export function useStandaloneProperties(filters?: AdvancedFilterState) {
  return useQuery({
    queryKey: ['properties', 'sale', filters],
    queryFn: (): Promise<Property[]> => fetchProperties(toFilterParams(filters, 'Resale')),
    placeholderData: keepPreviousData,
  });
}

export function useRentalProperties(filters?: AdvancedFilterState) {
  return useQuery({
    queryKey: ['properties', 'rent', filters],
    queryFn: (): Promise<Property[]> => fetchProperties(toFilterParams(filters, 'Rental')),
    placeholderData: keepPreviousData,
  });
}

export function useProperty(slug: string) {
  return useQuery({
    queryKey: ['property', slug],
    queryFn: (): Promise<Property | null> => fetchPropertyBySlug(slug),
    enabled: !!slug,
  });
}

export function useProjects() {
  return useQuery({
    queryKey: ['projects'],
    queryFn: (): Promise<Project[]> => fetchProjects(),
    placeholderData: keepPreviousData,
  });
}

export function useProject(slug: string) {
  return useQuery({
    queryKey: ['project', slug],
    queryFn: (): Promise<Project | null> => fetchProjectBySlug(slug),
    enabled: !!slug,
  });
}

export function useUnit(slug: string) {
  return useQuery({
    queryKey: ['unit', slug],
    queryFn: (): Promise<Property | null> => fetchUnitBySlug(slug),
    enabled: !!slug,
  });
}

export function useFeaturedProperties() {
  return useQuery({
    queryKey: ['properties', 'featured'],
    queryFn: (): Promise<Property[]> => fetchProperties({ isFeatured: true, pageSize: 12 }),
  });
}

export function useInstallmentProperties() {
  return useQuery({
    queryKey: ['properties', 'installment'],
    queryFn: (): Promise<Property[]> => fetchProperties({ hasInstallment: true, pageSize: 12 }),
  });
}
