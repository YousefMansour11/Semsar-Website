export type PropertyType = 'Studio' | '1BR' | '2BR' | 'Villa';

export type PropertyCategory =
  | 'Apartment' | 'Villa' | 'Office' | 'Shop' | 'Land' | 'Building' | 'Penthouse' | 'Duplex' | 'Studio' | 'Chalet' | 'Other';

export type PropertyListingType = 'Resale' | 'Rental' | 'Project';

export type PropertyStatus = 'Available' | 'Reserved' | 'Sold';

export const PROPERTY_VIEWS = ['Sea', 'Pool', 'Garden', 'Street', 'City', 'Golf', 'Park', 'Lake', 'Mountain', 'BackView', 'SideSeaView', 'PoolSeaView', 'SeaPoolView', 'Unknown'] as const;
export type PropertyView = (typeof PROPERTY_VIEWS)[number];

export const FINISHING_TYPES = ['FullyFinished', 'SemiFinished', 'CoreAndShell'] as const;
export type FinishingType = (typeof FINISHING_TYPES)[number];

export type ListingType = 'sale' | 'rent';

export type AvailabilityStatus = 'Available' | 'Reserved' | 'SoldOut';

export type ConstructionStatus = 'Planned' | 'UnderConstruction' | 'NearDelivery' | 'Delivered';

export type OwnershipType = 'Freehold' | 'GreenContract' | 'Usufruct' | 'SharedOwnership' | 'Other';

export const OWNERSHIP_TYPES: OwnershipType[] = ['Freehold', 'GreenContract', 'Usufruct', 'SharedOwnership', 'Other'];

export const CONSTRUCTION_STATUSES: ConstructionStatus[] = ['Planned', 'UnderConstruction', 'NearDelivery', 'Delivered'];

export interface NearbyPlace {
  name: string;
  nameAr?: string;
  distance: number;
  icon?: string;
}

export interface Variant {
  id: number;
  publicKey?: string;
  name: string;
  nameAr?: string;
  size: number;
  price: number;
  currency: string;
  rentPerMonth?: number;
  bedrooms: number;
  bathrooms: number;
  floor?: number;
  isFurnished: boolean;
  view?: string;
  unitNumber?: string;
  buildingNumber?: string;
  deliveryDate?: string;
  deliveryText?: string;
  finishingType?: string;
  hasBalcony: boolean;
  hasParking: boolean;
  images?: string[];
  floorPlanUrl?: string;
  availabilityStatus?: AvailabilityStatus;
  sortOrder: number;
  isActive: boolean;
  isFeatured?: boolean;
  isRecommended?: boolean;
  viewCount?: number;
  inquiryCount?: number;
  favoriteCount?: number;
}

export interface FinancingCalculation {
  variantPrice: number;
  downPaymentPercent: number;
  years: number;
  downPaymentAmount: number;
  remainingAmount: number;
  monthlyInstallment: number;
  currency: string;
}

export interface InstallmentPlan {
  paymentType?: 'Installment' | 'Cash';
  downPaymentPercent: number;
  discountPercent?: number;
  years: number;
  installmentMonths?: number;
  quarterlyAmount?: number;
  monthlyAmount?: number;
  isEnabled?: boolean;
  isDeleted?: boolean;
}

export interface VideoItem {
  id: number;
  url: string;
  publicId: string;
  thumbnailUrl?: string;
  sortOrder?: number;
}

export interface Property {
  id: string;
  rawId?: number;
  rawUnitId?: number;
  publicKey?: string;
  slug: string;
  propertyCode: string;

  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  title: string;
  description: string;

  type: PropertyType;
  propertyType: PropertyCategory;
  listingType: ListingType;
  listingTypeBackend: PropertyListingType;

  price: number;
  minPrice?: number;
  maxPrice?: number;
  rentPerMonth?: number;
  currency: string;

  location: string;
  locationAr?: string;
  size: number;
  minArea?: number;
  maxArea?: number;
  status: PropertyStatus;
  features: string[];
  featuresAr?: string[];

  bedrooms?: number;
  bathrooms?: number;
  floor?: number;
  totalFloors?: number;
  isFurnished?: boolean;
  view?: PropertyView;
  unitNumber?: string;
  buildingNumber?: string;
  deliveryDate?: string;
  deliveryText?: string;
  deliveryTextAr?: string;
  finishingType?: FinishingType;
  hasBalcony?: boolean;
  hasParking?: boolean;

  image: string;
  images: string[];
  videos?: VideoItem[];
  floorPlans?: string[];
  virtualTourUrl?: string;

  projectId: string | null;

  variants?: Variant[];
  unitType?: string;

  installments: InstallmentPlan[];
  installment?: InstallmentPlan;

  isFeatured?: boolean;
  highlights: string[];
  highlightsAr?: string[];
  nearbyPlaces?: NearbyPlace[];

  seoTitleEn?: string;
  seoTitleAr?: string;
  seoDescriptionEn?: string;
  seoDescriptionAr?: string;

  viewCount?: number;
  inquiryCount?: number;
  favoriteCount?: number;

  ownershipType?: OwnershipType;
  constructionStatus?: ConstructionStatus;
}

export interface Project {
  id: string;
  publicKey?: string;
  slug: string;

  nameEn: string;
  nameAr: string;
  descriptionEn: string;
  descriptionAr: string;

  name: string;
  description: string;

  location: string;
  locationAr?: string;
  developer?: string;
  image: string;
  images: string[];
  videos?: VideoItem[];
  highlights: string[];
  highlightsAr?: string[];
  startingPrice?: number;
  highestPrice?: number;
  propertyTypes?: string[];
  totalArea?: number;
  latitude?: number;
  longitude?: number;
  ownershipType?: string;
  nearbyPlaces?: string[];
  nearbyPlacesAr?: string[];
  unitCount?: number;
  totalAvailableUnits?: number;
  totalReservedUnits?: number;
  totalSoldUnits?: number;
  unitTypesCount?: number;
  units: Property[];
  deliveryText?: string;
  deliveryTextAr?: string;
  constructionStatus?: ConstructionStatus;
}

export interface AdvancedFilterState {
  minPrice: number | '';
  maxPrice: number | '';
  minSize: number | '';
  maxSize: number | '';
  locations: string[];
  types: PropertyCategory[];
}

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
