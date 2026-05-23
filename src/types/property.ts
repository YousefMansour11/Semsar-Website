// =====================================================================
// Backend-aligned types (mirror Semsar User Website API contract).
// Display-only fields (`title`, `description`, `image`, `type`) are kept
// for current UI; bilingual + slug + images[] + installments[] mirror
// the backend response so an API adapter is plug-and-play later.
// =====================================================================

// UI-side unit category (used for cards/filters — NOT the backend enum)
export type PropertyType = 'Studio' | '1BR' | '2BR' | 'Villa';

// Backend enums (USER-WEBSITE-API.md)
export type PropertyCategory =
  | 'Apartment' | 'Villa' | 'Office' | 'Shop' | 'Land' | 'Building' | 'Penthouse' | 'Duplex' | 'Studio' | 'Chalet' | 'Other';
export type PropertyListingType = 'Resale' | 'Rental' | 'Project';

export type PropertyStatus = 'Available' | 'Reserved' | 'Sold';

export const PROPERTY_VIEWS = ['Sea', 'Pool', 'Garden', 'Street', 'City', 'Golf', 'Park', 'Lake', 'Mountain', 'Unknown'] as const;
export type PropertyView = (typeof PROPERTY_VIEWS)[number];

export const FINISHING_TYPES = ['FullyFinished', 'SemiFinished', 'CoreAndShell'] as const;
export type FinishingType = (typeof FINISHING_TYPES)[number];

// Legacy frontend listing type — kept for current UI logic
export type ListingType = 'sale' | 'rent';

export interface InstallmentPlan {
  downPaymentPercent: number;
  years: number;
  monthlyAmount?: number;
  isEnabled?: boolean;
  isDeleted?: boolean;
}

export interface Property {
  // Core
  id: string;
  rawId?: number;
  rawUnitId?: number;
  publicKey?: string;
  slug: string;
  propertyCode: string;

  // Bilingual
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;

  // Display aliases (resolved via language) — kept for components
  title: string;
  description: string;

  // Categorisation
  type: PropertyType;                  // UI display category
  propertyType: PropertyCategory;      // Backend enum
  listingType: ListingType;            // UI ('sale' | 'rent')
  listingTypeBackend: PropertyListingType; // Backend ('Resale' | 'Rental' | 'Project')

  // Pricing
  price: number;
  rentPerMonth?: number;
  currency: string;

  // Location & meta
  location: string;
  locationAr?: string;
  size: number;
  status: PropertyStatus;
  features: string[];
  featuresAr?: string[];

  // Real estate details
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

  // Media
  image: string;          // primary thumbnail (= images[0])
  images: string[];       // gallery (backend `images[]`)

  // Relations
  projectId: string | null;

  // Plans
  installments: InstallmentPlan[];     // backend returns array
  installment?: InstallmentPlan;       // legacy single — first of installments[]

  // Flags
  isFeatured?: boolean;
}

export interface Project {
  id: string;
  publicKey?: string;
  slug: string;

  nameEn: string;
  nameAr: string;
  descriptionEn: string;
  descriptionAr: string;

  // Display aliases
  name: string;
  description: string;

  location: string;
  locationAr?: string;
  developer?: string;
  image: string;
  images: string[];
  highlights: string[];
  highlightsAr?: string[];
  unitCount?: number;
  units: Property[];
}

export interface AdvancedFilterState {
  minPrice: number | '';
  maxPrice: number | '';
  minSize: number | '';
  maxSize: number | '';
  locations: string[];
  types: PropertyCategory[];
}

// =====================================================================
// Settings (mirror GET /api/settings)
// =====================================================================
export interface SiteSettings {
  whatsappNumber: string;
  phoneNumber: string;
  companyName: string;
  email?: string;
  socialLinks: {
    facebook?: string;
    instagram?: string;
    tiktok?: string;
  };
}

// =====================================================================
// Booking & Lead payloads (mirror POST /api/bookings, /api/land-requests)
// =====================================================================
export interface BookingPayload {
  propertyId: number | null;
  unitId: number | null;
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
}

export interface LandRequestPayload {
  name: string;
  phone: string;
  location: string;
  minPrice?: number;
  maxPrice?: number;
  minArea?: number;
  maxArea?: number;
  notes?: string;
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
}
