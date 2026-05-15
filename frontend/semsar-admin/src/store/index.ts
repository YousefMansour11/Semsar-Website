/* eslint-disable @typescript-eslint/no-explicit-any */
import { create } from 'zustand';
import { adminApi, getAuthToken } from '@/lib/admin-api';
import { toast } from 'sonner';

// ─── Backend-aligned enums ────────────────────────────────────────────────
export type PropertyType = 'Apartment' | 'Villa' | 'Office' | 'Shop' | 'Land' | 'Building' | 'Penthouse' | 'Duplex' | 'Studio' | 'Chalet' | 'Other';
export type ListingType = 'Resale' | 'Rental' | 'Project';
export type ContactType = 'Owner' | 'Broker';
export type LeadStatus = 'New' | 'Contacted' | 'Interested' | 'ViewingScheduled' | 'Negotiating' | 'ClosedWon' | 'ClosedLost';

export const PROPERTY_VIEWS = ['Sea', 'Pool', 'Garden', 'Street', 'City', 'Golf', 'Park', 'Lake', 'Mountain', 'BackView', 'SideSeaView', 'PoolSeaView', 'SeaPoolView', 'Unknown'] as const;
export type PropertyView = (typeof PROPERTY_VIEWS)[number];

export const FINISHING_TYPES = ['FullyFinished', 'SemiFinished', 'CoreAndShell'] as const;
export type FinishingType = (typeof FINISHING_TYPES)[number];

export const PROPERTY_TYPES: { value: PropertyType; label: string }[] = [
  { value: 'Apartment', label: 'Apartment' },
  { value: 'Villa', label: 'Villa' },
  { value: 'Office', label: 'Office' },
  { value: 'Shop', label: 'Shop' },
  { value: 'Land', label: 'Land' },
  { value: 'Building', label: 'Building' },
  { value: 'Penthouse', label: 'Penthouse' },
  { value: 'Duplex', label: 'Duplex' },
  { value: 'Studio', label: 'Studio' },
  { value: 'Chalet', label: 'Chalet' },
  { value: 'Other', label: 'Other' },
];

export const LISTING_TYPES: { value: ListingType; label: string }[] = [
  { value: 'Resale', label: 'Resale' },
  { value: 'Rental', label: 'Rental' },
  { value: 'Project', label: 'Project' },
];

// ─── Types ────────────────────────────────────────────────────────────────
export interface Contact {
  id: string;
  name: string;
  phone: string;
  type: ContactType;
}

export interface Installment {
  paymentType?: 'Installment' | 'Cash';
  downPaymentPercent: number;
  discountPercent?: number;
  years: number;
  isEnabled: boolean;
}

export interface VideoDto {
  id: number;
  url: string;
  publicId?: string;
  thumbnailUrl?: string;
}

export interface Property {
  id: string;
  code: string;
  title: string;
  titleEn: string;
  titleAr: string;
  description: string;
  descriptionEn: string;
  descriptionAr: string;
  price: number;
  minPrice?: number;
  maxPrice?: number;
  rentPerMonth?: number;
  currency: string;
  location: string;
  propertyType: PropertyType;
  listingType: ListingType;
  size: number;
  minArea?: number;
  maxArea?: number;
  bedrooms?: number;
  bathrooms?: number;
  floor?: number;
  totalFloors?: number;
  isFurnished?: boolean;
  view?: PropertyView;
  unitNumber?: string;
  buildingNumber?: string;
  deliveryDate?: string;
  finishingType?: FinishingType;
  hasBalcony?: boolean;
  hasParking?: boolean;
  features: string[];
  featuresAr?: string[];
  locationAr?: string;
  installments: Installment[];
  contactId: string;
  contactName?: string;
  contactPhone?: string;
  projectId: string | null;
  projectName?: string;
  images: string[];
  isFeatured: boolean;
  isRecommended?: boolean;
  slug: string;
  slugIsAuto: boolean;
  seoTitle?: string;
  seoDescription?: string;
  seoKeywords?: string;
  seoTitleAr?: string;
  seoDescriptionAr?: string;
  seoKeywordsAr?: string;
  canonicalUrl?: string;
  deliveryText?: string;
  deliveryTextAr?: string;
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
  order: number;
  videos?: VideoDto[];
}

export interface Lead {
  id: string;
  name: string;
  phone: string;
  message: string;
  status: LeadStatus;
  source: string;
  medium?: string;
  campaign?: string;
  term?: string;
  content?: string;
  landingPage?: string;
  firstVisitAt?: string;
  currentPage?: string;
  isPaid: boolean;
  referrer?: string;
  userAgent?: string;
  pageViews: number;
  sessionDuration?: number;
  lastReferrer?: string;
  visitHistory?: string;
  propertyCode?: string;
  propertyId?: string;
  createdAt: string;
}

export interface BookingRequest {
  id: string;
  name: string;
  phone: string;
  message: string;
  propertyCode: string;
  propertyTitle: string;
  propertyLocation: string;
  propertyId: string;
  preferredDate: string;
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

export interface LandRequest {
  id: string;
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

export interface Project {
  id: string;
  name: string;
  nameEn: string;
  nameAr: string;
  slug: string;
  location: string;
  locationAr?: string;
  developer: string;
  unitCount: number;
  description: string;
  descriptionEn: string;
  descriptionAr: string;
  image: string;
  highlights: string[];
  highlightsAr?: string[];
  startingPrice?: number;
  nearbyPlaces?: string[];
  nearbyPlacesAr?: string[];
  propertyTypes?: string[];
  latitude?: number;
  longitude?: number;
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
  order: number;
  seoTitle?: string;
  seoDescription?: string;
  seoKeywords?: string;
  seoTitleAr?: string;
  seoDescriptionAr?: string;
  seoKeywordsAr?: string;
  videos?: VideoDto[];
}

// ─── Helpers ──────────────────────────────────────────────────────────────
function generateSlug(s: string): string {
  if (!s) return '';
  return s.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
}

function generateCode(existing: Property[], projectId: string | null): string {
  if (projectId) {
    const n = existing.filter(p => p.projectId === projectId).length + 1;
    return `UNIT-${String(n).padStart(3, '0')}`;
  }
  const n = existing.filter(p => !p.projectId).length + 1;
  return `PROP-${String(n).padStart(3, '0')}`;
}

// ─── Adapters ─────────────────────────────────────────────────────────────
export function apiPropertyToStore(raw: any): Property {
  const id = String(raw.id ?? raw.publicKey ?? '');
  return {
    id,
    code: raw.code || raw.propertyCode || '',
    title: raw.titleEn || raw.title || '',
    titleEn: raw.titleEn || raw.title || '',
    titleAr: raw.titleAr || '',
    description: raw.descriptionEn || raw.description || '',
    descriptionEn: raw.descriptionEn || raw.description || '',
    descriptionAr: raw.descriptionAr || '',
    price: Number(raw.price ?? raw.minPrice ?? 0),
    minPrice: raw.minPrice != null ? Number(raw.minPrice) : undefined,
    maxPrice: raw.maxPrice != null ? Number(raw.maxPrice) : undefined,
    rentPerMonth: raw.rentPerMonth != null ? Number(raw.rentPerMonth) : undefined,
    currency: raw.currency || 'EGP',
    location: raw.location || '',
    propertyType: raw.type || raw.propertyType || 'Apartment',
    listingType: raw.listingType || 'Resale',
    size: Number(raw.size ?? raw.minArea ?? 0),
    minArea: raw.minArea != null ? Number(raw.minArea) : undefined,
    maxArea: raw.maxArea != null ? Number(raw.maxArea) : undefined,
    bedrooms: raw.bedrooms != null ? Number(raw.bedrooms) : undefined,
    bathrooms: raw.bathrooms != null ? Number(raw.bathrooms) : undefined,
    floor: raw.floor != null ? Number(raw.floor) : undefined,
    totalFloors: raw.totalFloors != null ? Number(raw.totalFloors) : undefined,
    isFurnished: raw.isFurnished != null ? !!raw.isFurnished : undefined,
    view: raw.view || undefined,
    unitNumber: raw.unitNumber || undefined,
    buildingNumber: raw.buildingNumber || undefined,
    deliveryDate: raw.deliveryDate ? raw.deliveryDate.slice(0, 10) : undefined,
    finishingType: raw.finishingType || undefined,
    hasBalcony: raw.hasBalcony != null ? !!raw.hasBalcony : undefined,
    hasParking: raw.hasParking != null ? !!raw.hasParking : undefined,
    features: Array.isArray(raw.features) ? raw.features : [],
    featuresAr: Array.isArray(raw.featuresAr) ? raw.featuresAr : undefined,
    locationAr: raw.locationAr || undefined,
    installments: (() => {
      const inst = raw.installments ?? raw.Installments ?? (raw.installment ? [raw.installment] : null);
      return Array.isArray(inst) ? inst.map((i: any) => ({
        paymentType: (i.paymentType ?? i.PaymentType) === 'Cash' ? 'Cash' : 'Installment',
        downPaymentPercent: Number(i.downPaymentPercent ?? i.DownPaymentPercent ?? 0),
        discountPercent: (i.discountPercent ?? i.DiscountPercent) != null ? Number(i.discountPercent ?? i.DiscountPercent) : undefined,
        years: Number(i.years ?? i.Years ?? 0),
        isEnabled: (i.isEnabled ?? i.IsEnabled) !== false,
      })) : [];
    })(),
    contactId: raw.contactId != null ? String(raw.contactId) : '',
    contactName: raw.contactName || raw.contact?.name || undefined,
    contactPhone: raw.contactPhone || raw.contact?.phone || undefined,
    projectId: raw.projectId != null ? String(raw.projectId) : null,
    projectName: raw.projectName || undefined,
    images: Array.isArray(raw.images) ? [...new Set(raw.images.filter(Boolean))] : raw.image ? [raw.image] : [],
    isFeatured: !!raw.isFeatured,
    isRecommended: raw.isRecommended ?? undefined,
    deliveryText: raw.deliveryText ?? undefined,
    deliveryTextAr: raw.deliveryTextAr ?? undefined,
    constructionStatus: raw.constructionStatus ?? undefined,
    availabilityStatus: raw.availabilityStatus ?? undefined,
    ownershipType: raw.ownershipType ?? undefined,
    viewCount: raw.viewCount != null ? Number(raw.viewCount) : undefined,
    inquiryCount: raw.inquiryCount != null ? Number(raw.inquiryCount) : undefined,
    favoriteCount: raw.favoriteCount != null ? Number(raw.favoriteCount) : undefined,
    virtualTourUrl: raw.virtualTourUrl ?? undefined,
    highlightsAr: Array.isArray(raw.highlightsAr) ? raw.highlightsAr : undefined,
    nearbyPlaces: Array.isArray(raw.nearbyPlaces) ? raw.nearbyPlaces : undefined,
    nearbyPlacesAr: Array.isArray(raw.nearbyPlacesAr) ? raw.nearbyPlacesAr : undefined,
    slug: raw.slug || '',
    slugIsAuto: raw.slugIsAuto !== false,
    seoTitle: raw.seoTitle || undefined,
    seoDescription: raw.seoDescription || undefined,
    seoKeywords: raw.seoKeywords || undefined,
    seoTitleAr: raw.seoTitleAr || undefined,
    seoDescriptionAr: raw.seoDescriptionAr || undefined,
    seoKeywordsAr: raw.seoKeywordsAr || undefined,
    canonicalUrl: raw.canonicalUrl || undefined,
    createdAt: raw.createdAt || new Date().toISOString(),
    order: raw.sortOrder ?? 0,
    videos: Array.isArray(raw.videos) ? raw.videos.map((v: any) => ({ id: v.id, url: v.url, publicId: v.publicId })) : undefined,
  };
}

function apiProjectToStore(raw: any): Project {
  const id = String(raw.id ?? raw.publicKey ?? '');
  return {
    id,
    name: raw.nameEn || raw.name || '',
    nameEn: raw.nameEn || raw.name || '',
    nameAr: raw.nameAr || '',
    slug: raw.slug || '',
    location: raw.location || '',
    locationAr: raw.locationAr || undefined,
    developer: raw.developer || '',
    unitCount: raw.unitCount ?? 0,
    description: raw.descriptionEn || raw.description || '',
    descriptionEn: raw.descriptionEn || raw.description || '',
    descriptionAr: raw.descriptionAr || '',
    image: raw.image || '',
    highlights: Array.isArray(raw.highlights) ? raw.highlights : [],
    highlightsAr: Array.isArray(raw.highlightsAr) ? raw.highlightsAr : undefined,
    startingPrice: raw.startingPrice != null ? Number(raw.startingPrice) : undefined,
    nearbyPlaces: Array.isArray(raw.nearbyPlaces) ? raw.nearbyPlaces : undefined,
    nearbyPlacesAr: Array.isArray(raw.nearbyPlacesAr) ? raw.nearbyPlacesAr : undefined,
    propertyTypes: Array.isArray(raw.propertyTypes) ? raw.propertyTypes : undefined,
    latitude: raw.latitude != null ? Number(raw.latitude) : undefined,
    longitude: raw.longitude != null ? Number(raw.longitude) : undefined,
    totalArea: raw.totalArea != null ? Number(raw.totalArea) : undefined,
    ownershipType: raw.ownershipType ?? undefined,
    deliveryText: raw.deliveryText ?? undefined,
    deliveryTextAr: raw.deliveryTextAr ?? undefined,
    isRecommended: raw.isRecommended ?? undefined,
    constructionStatus: raw.constructionStatus ?? undefined,
    availabilityStatus: raw.availabilityStatus ?? undefined,
    viewCount: raw.viewCount != null ? Number(raw.viewCount) : undefined,
    inquiryCount: raw.inquiryCount != null ? Number(raw.inquiryCount) : undefined,
    favoriteCount: raw.favoriteCount != null ? Number(raw.favoriteCount) : undefined,
    virtualTourUrl: raw.virtualTourUrl ?? undefined,
    order: raw.order ?? 0,
    videos: Array.isArray(raw.videos) ? raw.videos.map((v: any) => ({ id: v.id, url: v.url, publicId: v.publicId })) : undefined,
  };
}

const CONTACT_TYPE_MAP: Record<string | number, ContactType> = {
  0: 'Owner',
  1: 'Broker',
  Owner: 'Owner',
  Broker: 'Broker',
};

function apiContactToStore(raw: any): Contact {
  return {
    id: String(raw.id),
    name: raw.name,
    phone: raw.phone,
    type: CONTACT_TYPE_MAP[raw.type] || 'Owner',
  };
}

function apiLeadToStore(raw: any): Lead {
  return {
    id: String(raw.id),
    name: raw.name,
    phone: raw.phone,
    message: raw.message || '',
    status: raw.status || 'New',
    source: raw.source || 'direct',
    medium: raw.medium || undefined,
    campaign: raw.campaign || undefined,
    term: raw.term || undefined,
    content: raw.content || undefined,
    landingPage: raw.landingPage || undefined,
    firstVisitAt: raw.firstVisitAt || undefined,
    currentPage: raw.currentPage || undefined,
    isPaid: !!raw.isPaid,
    referrer: raw.referrer || undefined,
    userAgent: raw.userAgent || undefined,
    pageViews: raw.pageViews ?? 0,
    sessionDuration: raw.sessionDuration || undefined,
    lastReferrer: raw.lastReferrer || undefined,
    visitHistory: raw.visitHistory || undefined,
    propertyCode: raw.propertyCode,
    propertyId: raw.propertyId ? String(raw.propertyId) : undefined,
    createdAt: raw.createdAt,
  };
}

function apiBookingToStore(raw: any): BookingRequest {
  return {
    id: String(raw.id),
    name: raw.name,
    phone: raw.phone,
    message: raw.message || '',
    propertyCode: raw.propertyCode || '',
    propertyTitle: raw.propertyTitle || '',
    propertyLocation: raw.propertyLocation || '',
    propertyId: raw.propertyId ? String(raw.propertyId) : '',
    preferredDate: raw.preferredDate || '',
    source: raw.source || 'direct',
    medium: raw.medium || undefined,
    campaign: raw.campaign || undefined,
    term: raw.term || undefined,
    content: raw.content || undefined,
    landingPage: raw.landingPage || undefined,
    firstVisitAt: raw.firstVisitAt || undefined,
    currentPage: raw.currentPage || undefined,
    referrer: raw.referrer || undefined,
    userAgent: raw.userAgent || undefined,
    pageViews: raw.pageViews ?? 0,
    sessionDuration: raw.sessionDuration || undefined,
    lastReferrer: raw.lastReferrer || undefined,
    visitHistory: raw.visitHistory || undefined,
    createdAt: raw.createdAt,
  };
}

function apiLandRequestToStore(raw: any): LandRequest {
  return {
    id: String(raw.id),
    name: raw.name,
    phone: raw.phone,
    location: raw.location || '',
    minPrice: Number(raw.minPrice ?? 0),
    maxPrice: Number(raw.maxPrice ?? 0),
    minArea: Number(raw.minArea ?? 0),
    maxArea: Number(raw.maxArea ?? 0),
    notes: raw.notes || '',
    source: raw.source || 'direct',
    medium: raw.medium || undefined,
    campaign: raw.campaign || undefined,
    term: raw.term || undefined,
    content: raw.content || undefined,
    landingPage: raw.landingPage || undefined,
    firstVisitAt: raw.firstVisitAt || undefined,
    currentPage: raw.currentPage || undefined,
    referrer: raw.referrer || undefined,
    userAgent: raw.userAgent || undefined,
    pageViews: raw.pageViews ?? 0,
    sessionDuration: raw.sessionDuration || undefined,
    lastReferrer: raw.lastReferrer || undefined,
    visitHistory: raw.visitHistory || undefined,
    createdAt: raw.createdAt,
  };
}

function mergeProperties(existing: Property[], incoming: Property[]): Property[] {
  const map = new Map(existing.map(p => [p.id, p]));
  for (const p of incoming) map.set(p.id, { ...map.get(p.id), ...p });
  return [...map.values()];
}

// ─── Store ────────────────────────────────────────────────────────────────
interface StoreState {
  // Auth (delegated to useAuthStore; kept here for backward compat)
  isAuthenticated: boolean;
  adminName: string;
  login: (username: string, password: string) => boolean;
  logout: () => void;

  // Loading (counter-based to support parallel fetches)
  loadingCount: number;
  loading: boolean;
  apiError: string | null;

  contacts: Contact[];
  properties: Property[];
  units: Property[];
  leads: Lead[];
  projects: Project[];
  bookings: BookingRequest[];
  landRequests: LandRequest[];
  previewMode: boolean;

  setPreviewMode: (mode: boolean) => void;
  togglePreviewMode: () => void;

  updateContact: (id: string, data: Partial<Contact>) => void;
  deleteContact: (id: string) => void;

  addProperty: (p: Omit<Property, 'id' | 'createdAt' | 'code' | 'order' | 'isFeatured' | 'slug' | 'slugIsAuto' | 'currency'> & Partial<Pick<Property, 'currency' | 'slug'>>) => void;
  updateProperty: (id: string, data: Partial<Omit<Property, 'code'>>) => void;
  deleteProperty: (id: string) => void;
  deleteProperties: (ids: string[]) => void;
  toggleFeaturedBulk: (ids: string[]) => void;
  reorderProperties: (activeId: string, overId: string) => void;
  reorderUnits: (activeId: string, overId: string) => void;
  deleteUnit: (id: string) => void;
  toggleFeatured: (id: string) => void;

  addLead: (lead: Omit<Lead, 'id' | 'createdAt'>) => void;
  deleteLead: (id: string) => void;

  addProject: (p: Omit<Project, 'id' | 'order' | 'slug'>) => void;
  updateProject: (id: string, data: Partial<Project>) => void;
  deleteProject: (id: string) => void;
  reorderProjects: (activeId: string, overId: string) => void;

  addBooking: (b: Omit<BookingRequest, 'id' | 'createdAt'>) => void;
  deleteBooking: (id: string) => void;

  addLandRequest: (r: Omit<LandRequest, 'id' | 'createdAt'>) => void;
  deleteLandRequest: (id: string) => void;

  // Pagination
  propertiesTotal: number;
  propertiesPage: number;
  propertiesPageSize: number;
  unitsTotal: number;
  unitsPage: number;
  unitsPageSize: number;
  projectsTotal: number;
  projectsPage: number;
  projectsPageSize: number;
  leadsTotal: number;
  leadsPage: number;
  leadsPageSize: number;
  bookingsTotal: number;
  bookingsPage: number;
  bookingsPageSize: number;
  landRequestsTotal: number;
  landRequestsPage: number;
  landRequestsPageSize: number;
  contactsTotal: number;
  contactsPage: number;
  contactsPageSize: number;
  setPropertiesPage: (page: number) => Promise<void>;
  setUnitsPage: (page: number) => Promise<void>;
  setProjectsPage: (page: number) => Promise<void>;
  setLeadsPage: (page: number) => Promise<void>;
  setBookingsPage: (page: number) => Promise<void>;
  setLandRequestsPage: (page: number) => Promise<void>;
  setContactsPage: (page: number) => Promise<void>;

  // API loaders
  loadProperties: (page?: number) => Promise<void>;
  loadUnits: (page?: number) => Promise<void>;
  loadProjects: (page?: number) => Promise<void>;
  loadLeads: (page?: number) => Promise<void>;
  loadBookings: (page?: number) => Promise<void>;
  loadLandRequests: (page?: number) => Promise<void>;
  loadContacts: (page?: number) => Promise<void>;
  loadMoreProperties: () => Promise<void>;
  loadMoreUnits: () => Promise<void>;
  loadMoreProjects: () => Promise<void>;
  loadMoreContacts: () => Promise<void>;
  loadMoreLeads: () => Promise<void>;
  loadMoreBookings: () => Promise<void>;
  loadMoreLandRequests: () => Promise<void>;
  clearApiError: () => void;
}

let nextId = 1000;
const genId = (prefix: string) => `${prefix}${nextId++}`;

export const useStore = create<StoreState>((set, get) => ({
  // Auth (backward compat)
  isAuthenticated: false,
  adminName: 'Admin',
  login: () => true,
  logout: () => {},

  loadingCount: 0,
  loading: false,
  apiError: null,

  contacts: [],
  properties: [],
  units: [],
  leads: [],
  projects: [],
  bookings: [],
  landRequests: [],
  previewMode: false,

  propertiesTotal: 0,
  propertiesPage: 1,
  propertiesPageSize: 100,
  unitsTotal: 0,
  unitsPage: 1,
  unitsPageSize: 100,
  projectsTotal: 0,
  projectsPage: 1,
  projectsPageSize: 100,
  leadsTotal: 0,
  leadsPage: 1,
  leadsPageSize: 100,
  bookingsTotal: 0,
  bookingsPage: 1,
  bookingsPageSize: 100,
  landRequestsTotal: 0,
  landRequestsPage: 1,
  landRequestsPageSize: 100,
  contactsTotal: 0,
  contactsPage: 1,
  contactsPageSize: 100,

  setPreviewMode: (mode) => set({ previewMode: mode }),
  togglePreviewMode: () => set((s) => ({ previewMode: !s.previewMode })),

  updateContact: (id, data) => set((s) => ({ contacts: s.contacts.map(c => c.id === id ? { ...c, ...data } : c) })),
  deleteContact: (id) => set((s) => ({ contacts: s.contacts.filter(c => c.id !== id) })),

  addProperty: (property) => {
    const { properties } = get();
    const code = generateCode(properties, property.projectId);
    const order = properties.length;
    const titleEn = property.titleEn || property.title;
    const newProp: Property = {
      ...property,
      title: titleEn,
      titleEn,
      titleAr: property.titleAr || '',
      description: property.descriptionEn || property.description || '',
      descriptionEn: property.descriptionEn || property.description || '',
      descriptionAr: property.descriptionAr || '',
      id: genId('p'),
      code,
      createdAt: new Date().toISOString(),
      order,
      isFeatured: false,
      currency: property.currency || 'EGP',
      slug: property.slug || generateSlug(titleEn),
      slugIsAuto: true,
      size: property.size || 0,
      features: property.features || [],
      installments: property.installments || [],
    };
    set((s) => ({ properties: [...s.properties, newProp] }));
  },

  updateProperty: (id, data) => {
    const mergeFn = (p: Property) => {
      if (p.id !== id) return p;
      const merged = { ...p, ...data };
      if (data.titleEn !== undefined) merged.title = data.titleEn;
      if (data.descriptionEn !== undefined) merged.description = data.descriptionEn;
      if (data.titleEn !== undefined && merged.slugIsAuto) merged.slug = generateSlug(data.titleEn);
      return merged;
    };
    set((s) => ({
      properties: s.properties.map(mergeFn),
      units: s.units.map(mergeFn),
    }));
  },

  deleteProperty: (id) => set((s) => ({ properties: s.properties.filter((p) => p.id !== id) })),
  deleteProperties: (ids) => set((s) => ({ properties: s.properties.filter((p) => !ids.includes(p.id)) })),

  toggleFeaturedBulk: (ids) => {
    set((s) => ({ properties: s.properties.map(p => ids.includes(p.id) ? { ...p, isFeatured: !p.isFeatured } : p) }));
  },

  reorderProperties: (activeId, overId) => {
    set((s) => {
      const oldIndex = s.properties.findIndex((p) => p.id === activeId);
      const newIndex = s.properties.findIndex((p) => p.id === overId);
      if (oldIndex === -1 || newIndex === -1) return s;
      const updated = [...s.properties];
      const [moved] = updated.splice(oldIndex, 1);
      updated.splice(newIndex, 0, moved);
      return { properties: updated.map((p, i) => ({ ...p, order: i })) };
    });
  },

  reorderUnits: (activeId, overId) => {
    set((s) => {
      const oldIndex = s.units.findIndex((p) => p.id === activeId);
      const newIndex = s.units.findIndex((p) => p.id === overId);
      if (oldIndex === -1 || newIndex === -1) return s;
      const updated = [...s.units];
      const [moved] = updated.splice(oldIndex, 1);
      updated.splice(newIndex, 0, moved);
      return { units: updated.map((p, i) => ({ ...p, order: i })) };
    });
  },

  deleteUnit: (id) => set((s) => ({ units: s.units.filter((u) => u.id !== id) })),

  toggleFeatured: (id) => {
    set((s) => ({ properties: s.properties.map((p) => p.id === id ? { ...p, isFeatured: !p.isFeatured } : p) }));
  },

  addLead: (lead) => {
    set((s) => ({ leads: [...s.leads, { ...lead, id: genId('l'), createdAt: new Date().toISOString() }] }));
  },

  deleteLead: (id) => set((s) => ({ leads: s.leads.filter(l => l.id !== id) })),

  addProject: (project) => {
    const order = get().projects.length;
    const nameEn = project.nameEn || project.name;
    const slug = generateSlug(nameEn);
    set((s) => ({
      projects: [
        ...s.projects,
        {
          ...project,
          name: nameEn,
          nameEn,
          nameAr: project.nameAr || '',
          description: project.descriptionEn || project.description || '',
          descriptionEn: project.descriptionEn || project.description || '',
          descriptionAr: project.descriptionAr || '',
          startingPrice: project.startingPrice != null ? Number(project.startingPrice) : undefined,
          nearbyPlaces: Array.isArray(project.nearbyPlaces) ? project.nearbyPlaces : undefined,
          propertyTypes: Array.isArray(project.propertyTypes) ? project.propertyTypes : undefined,
          latitude: project.latitude != null ? Number(project.latitude) : undefined,
          longitude: project.longitude != null ? Number(project.longitude) : undefined,
          totalArea: project.totalArea != null ? Number(project.totalArea) : undefined,
          id: genId('pr'),
          order,
          slug,
        },
      ],
    }));
  },

  updateProject: (id, data) => {
    set((s) => ({
      projects: s.projects.map(p => {
        if (p.id !== id) return p;
        const merged = { ...p, ...data };
        if (data.nameEn !== undefined) {
          merged.name = data.nameEn;
          merged.slug = generateSlug(data.nameEn);
        }
        if (data.descriptionEn !== undefined) merged.description = data.descriptionEn;
        return merged;
      }),
    }));
  },

  deleteProject: (id) => set((s) => ({ projects: s.projects.filter(p => p.id !== id) })),

  reorderProjects: (activeId, overId) => {
    set((s) => {
      const oldIndex = s.projects.findIndex((p) => p.id === activeId);
      const newIndex = s.projects.findIndex((p) => p.id === overId);
      if (oldIndex === -1 || newIndex === -1) return s;
      const updated = [...s.projects];
      const [moved] = updated.splice(oldIndex, 1);
      updated.splice(newIndex, 0, moved);
      return { projects: updated.map((p, i) => ({ ...p, order: i })) };
    });
  },

  addBooking: (booking) => {
    set((s) => ({ bookings: [...s.bookings, { ...booking, id: genId('b'), createdAt: new Date().toISOString() }] }));
  },
  deleteBooking: (id) => set((s) => ({ bookings: s.bookings.filter(b => b.id !== id) })),

  addLandRequest: (req) => {
    set((s) => ({ landRequests: [...s.landRequests, { ...req, id: genId('lr'), createdAt: new Date().toISOString() }] }));
  },
  deleteLandRequest: (id) => set((s) => ({ landRequests: s.landRequests.filter(r => r.id !== id) })),

  setPropertiesPage: async (page) => { await get().loadProperties(page); },
  setUnitsPage: async (page) => { await get().loadUnits(page); },
  setProjectsPage: async (page) => { await get().loadProjects(page); },
  setLeadsPage: async (page) => { await get().loadLeads(page); },
  setBookingsPage: async (page) => { await get().loadBookings(page); },
  setLandRequestsPage: async (page) => { await get().loadLandRequests(page); },
  setContactsPage: async (page) => { await get().loadContacts(page); },

  clearApiError: () => set({ apiError: null }),

  loadProperties: async (page) => {
    if (!getAuthToken()) return;
    set((s) => ({ loadingCount: s.loadingCount + 1, loading: true }));
    try {
      const pg = page ?? 1;
      const pageSize = 100;
      const res = await adminApi.getProperties({ page: pg, pageSize });
      const incoming = Array.isArray(res.data) ? res.data.map(apiPropertyToStore) : [];
      set((s) => ({
        properties: pg === 1 ? incoming : mergeProperties(s.properties, incoming),
        propertiesTotal: res.total,
        propertiesPage: pg,
        propertiesPageSize: pageSize,
      }));
    } catch (err) {
      if (import.meta.env.DEV) console.warn('loadProperties failed:', err);
      set({ apiError: 'Failed to load properties' });
      toast.error('Failed to load properties');
    } finally {
      const count = (get().loadingCount ?? 1) - 1;
      set({ loadingCount: count, loading: count > 0 });
    }
  },

  loadMoreProperties: async () => {
    const s = get();
    if (s.loading || !s.propertiesTotal || s.properties.length >= s.propertiesTotal) return;
    await s.loadProperties((s.propertiesPage ?? 1) + 1);
  },

  loadUnits: async (page) => {
    if (!getAuthToken()) return;
    set((s) => ({ loadingCount: s.loadingCount + 1, loading: true }));
    try {
      const pg = page ?? 1;
      const pageSize = 100;
      const res = await adminApi.getUnits({ page: pg, pageSize });
      const incomingUnits = Array.isArray(res.data) ? res.data.map(apiPropertyToStore).map(u => ({ ...u, id: `u-${u.id}` })) : [];
      set((s) => ({
        units: pg === 1 ? incomingUnits.map(u => {
          const existing = s.units.find(e => e.id === u.id);
          if (!existing) return u;
          return {
            ...u,
            locationAr: existing.locationAr ?? u.locationAr,
            featuresAr: existing.featuresAr ?? u.featuresAr,
            seoTitle: existing.seoTitle ?? u.seoTitle,
            seoDescription: existing.seoDescription ?? u.seoDescription,
            seoKeywords: existing.seoKeywords ?? u.seoKeywords,
            seoTitleAr: existing.seoTitleAr ?? u.seoTitleAr,
            seoDescriptionAr: existing.seoDescriptionAr ?? u.seoDescriptionAr,
            seoKeywordsAr: existing.seoKeywordsAr ?? u.seoKeywordsAr,
            canonicalUrl: existing.canonicalUrl ?? u.canonicalUrl,
            titleAr: existing.titleAr ?? u.titleAr,
            descriptionAr: existing.descriptionAr ?? u.descriptionAr,
            bedrooms: existing.bedrooms ?? u.bedrooms,
            bathrooms: existing.bathrooms ?? u.bathrooms,
            floor: existing.floor ?? u.floor,
            totalFloors: existing.totalFloors ?? u.totalFloors,
            isFurnished: existing.isFurnished ?? u.isFurnished,
            view: existing.view ?? u.view,
            unitNumber: existing.unitNumber ?? u.unitNumber,
            buildingNumber: existing.buildingNumber ?? u.buildingNumber,
            deliveryDate: existing.deliveryDate ?? u.deliveryDate,
            finishingType: existing.finishingType ?? u.finishingType,
            hasBalcony: existing.hasBalcony ?? u.hasBalcony,
            hasParking: existing.hasParking ?? u.hasParking,
            contactName: existing.contactName ?? u.contactName,
            contactPhone: existing.contactPhone ?? u.contactPhone,
            projectName: existing.projectName ?? u.projectName,
            videos: existing.videos ?? u.videos,
            isRecommended: existing.isRecommended ?? u.isRecommended,
            deliveryText: existing.deliveryText ?? u.deliveryText,
            deliveryTextAr: existing.deliveryTextAr ?? u.deliveryTextAr,
            constructionStatus: existing.constructionStatus ?? u.constructionStatus,
            availabilityStatus: existing.availabilityStatus ?? u.availabilityStatus,
            ownershipType: existing.ownershipType ?? u.ownershipType,
            virtualTourUrl: existing.virtualTourUrl ?? u.virtualTourUrl,
            highlightsAr: existing.highlightsAr ?? u.highlightsAr,
            nearbyPlaces: existing.nearbyPlaces ?? u.nearbyPlaces,
            nearbyPlacesAr: existing.nearbyPlacesAr ?? u.nearbyPlacesAr,
          };
        }) : mergeProperties(s.units, incomingUnits),
        unitsTotal: res.total,
        unitsPage: pg,
        unitsPageSize: pageSize,
      }));
    } catch (err) {
      if (import.meta.env.DEV) console.warn('loadUnits failed:', err);
      set({ apiError: 'Failed to load units' });
      toast.error('Failed to load units');
    } finally {
      const count = (get().loadingCount ?? 1) - 1;
      set({ loadingCount: count, loading: count > 0 });
    }
  },

  loadMoreUnits: async () => {
    const s = get();
    if (s.loading || !s.unitsTotal || s.units.length >= s.unitsTotal) return;
    await s.loadUnits((s.unitsPage ?? 1) + 1);
  },

  loadProjects: async (page) => {
    if (!getAuthToken()) return;
    set((s) => ({ loadingCount: s.loadingCount + 1, loading: true }));
    try {
      const pg = page ?? 1;
      const pageSize = 100;
      const res = await adminApi.getProjects({ page: pg, pageSize });
      const mapped = Array.isArray(res.data) ? res.data.map(apiProjectToStore) : [];
      set((s) => ({
        projects: pg === 1 ? mapped : [...s.projects, ...mapped],
        projectsTotal: res.total,
        projectsPage: pg,
        projectsPageSize: pageSize,
      }));
    } catch (err) {
      if (import.meta.env.DEV) console.warn('loadProjects failed:', err);
      set({ apiError: 'Failed to load projects' });
      toast.error('Failed to load projects');
    } finally {
      const count = (get().loadingCount ?? 1) - 1;
      set({ loadingCount: count, loading: count > 0 });
    }
  },

  loadMoreProjects: async () => {
    const s = get();
    if (s.loading || !s.projectsTotal || s.projects.length >= s.projectsTotal) return;
    await s.loadProjects((s.projectsPage ?? 1) + 1);
  },

  loadContacts: async (page) => {
    if (!getAuthToken()) return;
    set((s) => ({ loadingCount: s.loadingCount + 1, loading: true }));
    try {
      const pg = page ?? 1;
      const pageSize = 100;
      const res = await adminApi.getContacts({ page: pg, pageSize });
      const mapped = Array.isArray(res.data) ? res.data.map(apiContactToStore) : [];
      set((s) => ({
        contacts: pg === 1 ? mapped : [...s.contacts, ...mapped],
        contactsTotal: res.total,
        contactsPage: pg,
        contactsPageSize: pageSize,
      }));
    } catch (err) {
      if (import.meta.env.DEV) console.warn('loadContacts failed:', err);
      set({ apiError: 'Failed to load contacts' });
      toast.error('Failed to load contacts');
    } finally {
      const count = (get().loadingCount ?? 1) - 1;
      set({ loadingCount: count, loading: count > 0 });
    }
  },

  loadMoreContacts: async () => {
    const s = get();
    if (s.loading || !s.contactsTotal || s.contacts.length >= s.contactsTotal) return;
    await s.loadContacts((s.contactsPage ?? 1) + 1);
  },

  loadLeads: async (page) => {
    if (!getAuthToken()) return;
    set((s) => ({ loadingCount: s.loadingCount + 1, loading: true }));
    try {
      const pg = page ?? 1;
      const pageSize = 100;
      const res = await adminApi.getLeads({ page: pg, pageSize });
      const mapped = Array.isArray(res.data) ? res.data.map(apiLeadToStore) : [];
      set((s) => ({
        leads: pg === 1 ? mapped : [...s.leads, ...mapped],
        leadsTotal: res.total,
        leadsPage: pg,
        leadsPageSize: pageSize,
      }));
    } catch (err) {
      if (import.meta.env.DEV) console.warn('loadLeads failed:', err);
      set({ apiError: 'Failed to load leads' });
      toast.error('Failed to load leads');
    } finally {
      const count = (get().loadingCount ?? 1) - 1;
      set({ loadingCount: count, loading: count > 0 });
    }
  },

  loadMoreLeads: async () => {
    const s = get();
    if (s.loading || !s.leadsTotal || s.leads.length >= s.leadsTotal) return;
    await s.loadLeads((s.leadsPage ?? 1) + 1);
  },

  loadBookings: async (page) => {
    if (!getAuthToken()) return;
    set((s) => ({ loadingCount: s.loadingCount + 1, loading: true }));
    try {
      const pg = page ?? 1;
      const pageSize = 100;
      const res = await adminApi.getBookings({ page: pg, pageSize });
      const mapped = Array.isArray(res.data) ? res.data.map(apiBookingToStore) : [];
      set((s) => ({
        bookings: pg === 1 ? mapped : [...s.bookings, ...mapped],
        bookingsTotal: res.total,
        bookingsPage: pg,
        bookingsPageSize: pageSize,
      }));
    } catch (err) {
      if (import.meta.env.DEV) console.warn('loadBookings failed:', err);
      set({ apiError: 'Failed to load bookings' });
      toast.error('Failed to load bookings');
    } finally {
      const count = (get().loadingCount ?? 1) - 1;
      set({ loadingCount: count, loading: count > 0 });
    }
  },

  loadMoreBookings: async () => {
    const s = get();
    if (s.loading || !s.bookingsTotal || s.bookings.length >= s.bookingsTotal) return;
    await s.loadBookings((s.bookingsPage ?? 1) + 1);
  },

  loadLandRequests: async (page) => {
    if (!getAuthToken()) return;
    set((s) => ({ loadingCount: s.loadingCount + 1, loading: true }));
    try {
      const pg = page ?? 1;
      const pageSize = 100;
      const res = await adminApi.getLandRequests({ page: pg, pageSize });
      const mapped = Array.isArray(res.data) ? res.data.map(apiLandRequestToStore) : [];
      set((s) => ({
        landRequests: pg === 1 ? mapped : [...s.landRequests, ...mapped],
        landRequestsTotal: res.total,
        landRequestsPage: pg,
        landRequestsPageSize: pageSize,
      }));
    } catch (err) {
      if (import.meta.env.DEV) console.warn('loadLandRequests failed:', err);
      set({ apiError: 'Failed to load land requests' });
      toast.error('Failed to load land requests');
    } finally {
      const count = (get().loadingCount ?? 1) - 1;
      set({ loadingCount: count, loading: count > 0 });
    }
  },

  loadMoreLandRequests: async () => {
    const s = get();
    if (s.loading || !s.landRequestsTotal || s.landRequests.length >= s.landRequestsTotal) return;
    await s.loadLandRequests((s.landRequestsPage ?? 1) + 1);
  },
}));
