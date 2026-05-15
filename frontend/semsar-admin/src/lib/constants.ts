export const PROPERTY_TYPE_MAP: Record<string, number> = {
  Apartment: 0, Villa: 1, Office: 2, Shop: 3, Land: 4,
  Building: 5, Penthouse: 6, Duplex: 7, Studio: 8, Chalet: 9, Other: 10,
};

export const LISTING_TYPE_MAP: Record<string, number> = {
  Resale: 0, Rental: 1, Project: 2,
};

export const CONTACT_TYPE_MAP: Record<string, number> = {
  Owner: 0, Broker: 1,
};

export const toApiEnum = (map: Record<string, number>, val: string) => map[val] ?? 0;

export const LISTING_BADGE: Record<string, string> = {
  Resale: 'border-status-contacted text-status-contacted',
  Rental: 'border-status-closed text-status-closed',
  Project: 'border-primary text-primary',
};

export const DEFAULT_CURRENCY = 'EGP';
export const DEFAULT_VIEW = 'Unknown';
export const DEFAULT_PROPERTY_TYPE = 'Apartment';
export const DEFAULT_LISTING_TYPE = 'Project';

export interface VariantFormItem {
  name: string;
  nameAr?: string;
  size: string;
  price: string;
  currency: string;
  rentPerMonth: string;
  bedrooms: string;
  bathrooms: string;
  floor: string;
  isFurnished: boolean;
  view: string;
  unitNumber: string;
  buildingNumber: string;
  deliveryDate: string;
  finishingType: string;
  hasBalcony: boolean;
  hasParking: boolean;
  images: string;
  floorPlanUrl: string;
  availabilityStatus: string;
  sortOrder: string;
  isActive: boolean;
  isFeatured?: boolean;
  isRecommended?: boolean;
  deliveryText?: string;
  deliveryTextAr?: string;
}

export const defaultVariantFormItem: VariantFormItem = {
  name: '', nameAr: '', size: '', price: '', currency: 'EGP', rentPerMonth: '',
  bedrooms: '0', bathrooms: '0', floor: '', isFurnished: false, view: 'Unknown',
  unitNumber: '', buildingNumber: '', deliveryDate: '', finishingType: '',
  hasBalcony: false, hasParking: false, images: '', floorPlanUrl: '', availabilityStatus: 'Available',
  sortOrder: '0', isActive: true, isFeatured: false, isRecommended: false, deliveryText: '', deliveryTextAr: '',
};

export interface DefaultUnitForm {
  titleEn: string; titleAr: string;
  descriptionEn: string; descriptionAr: string;
  rentPerMonth: string; currency: string;
  location: string; locationAr: string;
  governorate: string; city: string; area: string;
  governorateAr: string; cityAr: string; areaAr: string;
  bedrooms: string; bathrooms: string; floor: string;
  isFurnished: boolean; view: string;
  unitNumber: string; buildingNumber: string; deliveryDate: string;
  finishingType: string; hasBalcony: boolean; hasParking: boolean;
  propertyType: string; listingType: string;
  features: string[]; featuresAr: string[]; featuresInput: string; featuresArInput: string;
  contactId: string; images: string[];
  installments: { paymentType: 'Installment' | 'Cash'; downPaymentPercent: string; discountPercent: string; years: string; isEnabled: boolean }[];
  variants: VariantFormItem[];
  isRecommended?: boolean;
  deliveryText?: string;
  deliveryTextAr?: string;
  constructionStatus?: string;
  availabilityStatus?: string;
  ownershipType?: string;
  virtualTourUrl?: string;
  highlightsAr?: string[];
  nearbyPlaces?: string[];
  nearbyPlacesAr?: string[];
  slug: string; slugIsAuto: boolean;
  seoTitle: string; seoDescription: string; seoKeywords: string;
  seoTitleAr: string; seoDescriptionAr: string; seoKeywordsAr: string;
}

export const defaultUnitForm: DefaultUnitForm = {
  titleEn: '', titleAr: '',
  descriptionEn: '', descriptionAr: '',
  rentPerMonth: '', currency: DEFAULT_CURRENCY,
  location: '', locationAr: '',
  governorate: '', city: '', area: '',
  governorateAr: '', cityAr: '', areaAr: '',
  bedrooms: '', bathrooms: '', floor: '',
  isFurnished: false, view: DEFAULT_VIEW,
  unitNumber: '', buildingNumber: '', deliveryDate: '',
  finishingType: '', hasBalcony: false, hasParking: false,
  propertyType: DEFAULT_PROPERTY_TYPE, listingType: DEFAULT_LISTING_TYPE,
  features: [], featuresAr: [], featuresInput: '', featuresArInput: '',
  contactId: '', images: [],
  installments: [],
  variants: [],
  isRecommended: false,
  deliveryText: '',
  deliveryTextAr: '',
  constructionStatus: '',
  availabilityStatus: 'Available',
  ownershipType: '',
  virtualTourUrl: '',
  highlightsAr: [],
  nearbyPlaces: [],
  nearbyPlacesAr: [],
  slug: '', slugIsAuto: true,
  seoTitle: '', seoDescription: '', seoKeywords: '',
  seoTitleAr: '', seoDescriptionAr: '', seoKeywordsAr: '',
};
