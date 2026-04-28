import { useState, useMemo, useCallback, useEffect } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useProperty, useProject } from '../hooks/use-properties';
import { useSettings, whatsappLink } from '../hooks/use-settings';
import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import { MobileStickyBar } from '../components/MobileStickyBar';
import { BookViewingModal } from '../components/BookViewingModal';
import { PremiumImage } from '../components/PremiumImage';
import SeoHelmet from '../components/SeoHelmet';
import { useLanguage } from '../i18n/LanguageContext';
import { localizedPath } from '../lib/paths';
import { safeSessionGet, safeSessionSet, safeSessionRemove } from '../lib/utils';
import { validateImageUrls } from '../lib/image-validator';
import { MapPin, Bed, Bath, Eye, MessageCircle, Calendar, PhoneCall, ArrowLeft, ArrowRight, ArrowUpDown, Hash, Building, Check, Globe, FileText } from 'lucide-react';
import { ReadMore } from '../components/ReadMore';
import { PropertyDetailSkeleton } from '../components/Skeletons';
import { MediaGallery } from '../components/MediaGallery';
import { buildMediaItems } from '../types/media';
import { PremiumVariantSelector } from '../components/PremiumVariantSelector';
import { StickyMobileSummary } from '../components/StickyMobileSummary';
import { PaymentPlanCard } from '../components/PaymentPlanCard';
import { UnitHighlights } from '../components/UnitHighlights';
import { UnitNearbyPlaces } from '../components/UnitNearbyPlaces';
import { ConstructionTimeline } from '../components/ConstructionTimeline';
import { AnalyticsBar } from '../components/AnalyticsBar';
import { OwnershipBadge } from '../components/OwnershipBadge';
import type { Variant } from '../types/property';

export default function PropertyDetailsPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { data: property, isLoading, isError } = useProperty(slug || '');
  const { data: project } = useProject(property?.projectId || '');
  const { data: settings } = useSettings();
  const { t, language, fmtPrice, fmtDate, fmtNum } = useLanguage();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [pickedVariant, setPickedVariant] = useState<Variant | null>(null);
  const [validUrls, setValidUrls] = useState<Set<string> | null>(null);

  useEffect(() => {
    if (!property) return;
    const all = new Set<string>();
    if (property.image) all.add(property.image);
    property.images?.forEach((u: string) => all.add(u));
    property.variants?.forEach((v: Variant) => v.images?.forEach((u: string) => all.add(u)));
    validateImageUrls([...all]).then(valid => setValidUrls(new Set(valid)));
  }, [property]);

  const variants: Variant[] = useMemo(() => property?.variants || [], [property]);

  const selectedVariant: Variant | null = useMemo(() => {
    if (pickedVariant) return pickedVariant;
    if (!variants.length) return null;
    const fromUrl = searchParams.get('variant');
    if (fromUrl) {
      const match = variants.find(v => v.publicKey === fromUrl);
      if (match) return match;
    }
    const active = [...variants].filter(v => v.isActive && v.availabilityStatus !== 'SoldOut').sort((a, b) => a.price - b.price);
    if (active.length) return active[0];
    return variants[0];
  }, [variants, searchParams, pickedVariant]);

  const onVariantChange = useCallback((v: Variant) => {
    setPickedVariant(v);
    const params = new URLSearchParams(searchParams);
    if (v.publicKey) params.set('variant', v.publicKey);
    else params.delete('variant');
    const qs = params.toString();
    window.history.replaceState(null, '', qs ? `?${qs}` : window.location.pathname);
  }, [searchParams]);

  const mediaCollection = useMemo(() => {
    if (!property) return { items: [], heroIndex: 0 };
    const varImages = selectedVariant?.images?.length ? selectedVariant.images : undefined;
    const raw = varImages || (property.images?.length ? property.images : [property.image]);
    const filtered = validUrls ? raw.filter((u: string) => validUrls.has(u)) : raw;
    const hero = filtered[0] || property.image;
    return buildMediaItems(hero, filtered, property.videos);
  }, [property, selectedVariant, validUrls]);

  const openModal = useCallback(() => setIsModalOpen(true), []);
  const closeModal = useCallback(() => setIsModalOpen(false), []);

  const title = useMemo(() => language === 'ar' ? (property?.titleAr || property?.titleEn) : property?.titleEn, [language, property?.titleAr, property?.titleEn]);
  const description = useMemo(() => language === 'ar' ? (property?.descriptionAr || property?.descriptionEn) : property?.descriptionEn, [language, property?.descriptionAr, property?.descriptionEn]);

  const ov = selectedVariant;

  const displayPrice = useMemo(() => {
    if (ov) {
      if (ov.price === 0 && (!ov.rentPerMonth || ov.rentPerMonth === 0)) return null;
      if (property?.listingType === 'rent' && ov.rentPerMonth != null) return ov.rentPerMonth;
      return ov.price;
    }
    if (property?.listingType === 'rent') return property?.rentPerMonth ?? null;
    const p = property?.minPrice ?? property?.price;
    return p != null && p > 0 ? p : null;
  }, [ov, property]);

  const priceLabel = useMemo(() => {
    const p = displayPrice;
    if (p == null) return t('properties.priceOnRequest');
    const curr = ov?.currency || property?.currency || 'EGP';
    return fmtPrice(p, curr);
  }, [ov, displayPrice, property, fmtPrice, t]);

  const displayBedrooms = ov && ov.bedrooms > 0 ? ov.bedrooms : property?.bedrooms;
  const displayBathrooms = ov && ov.bathrooms > 0 ? ov.bathrooms : property?.bathrooms;
  const displayFloor = ov && ov.floor != null ? ov.floor : property?.floor;
  const displayView = ov && ov.view && ov.view !== 'Unknown' ? ov.view : (property?.view && property.view !== 'Unknown' ? property.view : null);
  const displayFurnished = ov ? ov.isFurnished : property?.isFurnished;
  const displayUnitNumber = ov?.unitNumber || property?.unitNumber;
  const displayBuildingNumber = ov?.buildingNumber || property?.buildingNumber;
  const displayDeliveryDate = ov?.deliveryDate || property?.deliveryDate;
  const displayFinishingType = ov?.finishingType || property?.finishingType;
  const displayDeliveryText = language === 'ar'
    ? (property?.deliveryTextAr || property?.deliveryText || project?.deliveryTextAr || project?.deliveryText)
    : (property?.deliveryText || project?.deliveryText);
  const displayConstructionStatus = property?.constructionStatus || project?.constructionStatus;
  const displayHasBalcony = ov ? ov.hasBalcony : property?.hasBalcony;
  const displayHasParking = ov ? ov.hasParking : property?.hasParking;
  const displayFeatures = language === 'ar'
    ? ((property?.featuresAr?.length ? property.featuresAr : property?.features) ?? [])
    : (property?.features ?? []);

  const gallery = property?.images?.length ? property.images : (property?.image ? [property.image] : []);

  const allPlans = useMemo(() => {
    return property?.installments?.length ? property.installments : (property?.installment ? [property.installment] : []);
  }, [property]);

  const firstPlan = useMemo(() => {
    return allPlans.length > 0 ? allPlans[0] : null;
  }, [allPlans]);

  const installmentSummary = useMemo(() => {
    if (!firstPlan || !ov) return null;
    const bp = ov.price;
    const dpAmt = bp * (firstPlan.downPaymentPercent / 100);
    const remaining = bp - dpAmt;
    const totalMonths = firstPlan.installmentMonths || (firstPlan.years * 12);
    const monthly = firstPlan.monthlyAmount && firstPlan.monthlyAmount > 0
      ? firstPlan.monthlyAmount
      : (bp - dpAmt) / totalMonths;
    return {
      downPaymentPercent: firstPlan.downPaymentPercent,
      downPaymentAmount: dpAmt,
      remainingAmount: remaining,
      monthlyAmount: monthly,
      years: firstPlan.years,
    };
  }, [firstPlan, ov]);

  const backSection = property?.listingType === 'rent' ? 'for-rent' : 'for-sale';

  const handleBack = useCallback(() => {
    if (project?.slug) navigate(localizedPath(`/projects/${project.slug}`, language));
    else {
      safeSessionSet('semsar_nav', { section: backSection });
      const savedY = safeSessionGet<string>('semsar_scroll_y');
      if (savedY) safeSessionRemove('semsar_scroll_y');
      navigate(localizedPath('/', language), {
        state: { scrollTo: backSection, ...(savedY ? { restoreScrollY: parseInt(savedY, 10) } : {}) },
      });
    }
  }, [navigate, project?.slug, language, backSection]);

  const variantInfoStr = selectedVariant
    ? `${property?.propertyCode ? `[${property.propertyCode}] ` : ''}${fmtNum(selectedVariant.size)} sqm · ${fmtPrice(selectedVariant.price, selectedVariant.currency)}${selectedVariant.view && selectedVariant.view !== 'Unknown' ? ` · ${selectedVariant.view}` : ''}`
    : '';

  const priceSuffix = property?.listingType === 'rent' ? ` / ${t('properties.rentSuffix')}` : '';

  const whatsappNumber = settings?.whatsappNumber || '+201558730895';
  const phoneNumber = settings?.phoneNumber || whatsappNumber;

  if (isLoading) {
    return <PropertyDetailSkeleton />;
  }
  if (isError) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <div className="flex-1 flex items-center justify-center pt-20">
          <div className="text-center">
            <h1 className="text-2xl font-bold mb-4">{t('error.somethingWentWrong')}</h1>
            <button type="button" onClick={() => window.location.reload()} className="text-sm text-muted-foreground hover:text-foreground underline">{t('error.reloadPage')}</button>
          </div>
        </div>
        <SiteFooter />
      </div>
    );
  }
  if (!property) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <div className="flex-1 flex items-center justify-center pt-20"><h1 className="text-2xl font-bold">{t('property.notFound')}</h1></div>
        <MobileStickyBar whatsappHref={whatsappLink(whatsappNumber, variantInfoStr)}
          phoneHref={`tel:${phoneNumber}`}
          primaryAction={{ label: t('cta.bookViewing'), onClick: openModal }}
        />
        <SiteFooter />
      </div>
    );
  }

  const locationParts = (language === 'ar' ? (property?.locationAr || property?.location) : property?.location)
    ?.split(',').map(s => s.trim()).filter(Boolean) || [];

  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const propPath = `/properties/${property.slug}`;

  const seoTitle = language === 'ar' ? (property?.seoTitleAr || title) : (property?.seoTitleEn || title);
  const seoDescription = language === 'ar' ? (property?.seoDescriptionAr || description) : (property?.seoDescriptionEn || description);

  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet
        title={seoTitle}
        description={seoDescription?.slice(0, 160)}
        canonical={`${origin}${localizedPath(propPath, language)}`}
        image={gallery[0]}
        alternates={[
          { hrefLang: 'en', href: `${origin}${localizedPath(propPath, 'en')}` },
          { hrefLang: 'ar', href: `${origin}${localizedPath(propPath, 'ar')}` },
        ]}
        jsonLd={JSON.stringify({
          '@context': 'https://schema.org',
          '@type': 'RealEstateListing',
          name: ov ? `${title} - ${ov.name}` : title,
          description: description?.slice(0, 200),
          url: `${origin}${localizedPath(propPath, language)}`,
          image: gallery[0],
          ...(displayPrice != null ? { offers: { '@type': 'Offer', price: displayPrice, priceCurrency: ov?.currency ?? property?.currency ?? 'EGP' } } : {}),
          ...(ov ? { additionalProperty: { '@type': 'PropertyValue', name: 'Variant', value: ov.name } } : {}),
          ...(property?.videos?.length ? { video: property.videos.map(v => ({ '@type': 'VideoObject', contentUrl: v.url, thumbnailUrl: v.thumbnailUrl, name: title })) } : {}),
        })}
      />
      <Header />

      <div>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
        <button onClick={handleBack} className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 sm:mb-8 font-medium text-sm transition-colors group p-3 -ml-3 min-h-[44px]">
          {language === 'ar' ? <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" /> : <ArrowLeft className="w-4 h-4 group-hover:-translate-x-1 transition-transform" />}
          {t('general.back')}
        </button>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8 lg:gap-12">
          {/* LEFT COLUMN — Main Content */}
          <div className="lg:col-span-2 space-y-8 sm:space-y-10">

            {/* 1. HERO — Gallery */}
            {mediaCollection.items.length > 0 && (
              <MediaGallery items={mediaCollection.items} heroIndex={mediaCollection.heroIndex} title={title} />
            )}

            {/* 2. PRIMARY INFO — Title, Location, Analytics */}
            <div>
              <h1 className="font-display text-2xl sm:text-3xl md:text-4xl font-bold text-foreground leading-tight mb-3">
                {t(`prop_type.${property?.propertyType}`)} {project ? <>{t('general.at')} {language === 'ar' ? project.nameAr : project.nameEn} </> : ''}
              </h1>

              <div className="flex items-center gap-1.5 text-muted-foreground text-sm mb-3">
                <MapPin className="w-4 h-4 text-amber-600 shrink-0" />
                <span>{locationParts.join(' • ')}</span>
              </div>

              <AnalyticsBar viewCount={property?.viewCount} inquiryCount={property?.inquiryCount} favoriteCount={property?.favoriteCount} />

            {/* 3. VARIANT INFO BAR — name + price */}
            {ov && (
              <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-2 mb-5 mt-5">
                <span className="font-display text-xl sm:text-2xl font-bold text-foreground">
                  {language === 'ar' ? `${t(`prop_type.${property?.propertyType}`)} ${fmtNum(ov.size)} ${t('general.m2')}` : ov.name}
                </span>
                <div className="flex items-center gap-3 shrink-0 flex-wrap">
                  <div key={ov?.id ?? 'base'} className="text-3xl sm:text-4xl font-bold text-navy animate-price-update">
                    {priceLabel}{priceSuffix}
                  </div>
                  {ov?.isRecommended && (
                    <span className="px-2.5 py-1 rounded-full bg-purple-500 text-white text-[10px] font-bold uppercase tracking-wider shrink-0">
                      {t('unit.recommended')}
                    </span>
                  )}
                  {property?.ownershipType && (
                    <OwnershipBadge type={property.ownershipType} />
                  )}
                </div>
              </div>
            )}

            {/* 4. VARIANT SELECTOR */}
            {variants.length > 0 && (
              <div className="mb-5">
                <PremiumVariantSelector
                  variants={variants}
                  selectedVariant={ov}
                  onChange={onVariantChange}
                  listingType={property?.listingType || 'sale'}
                />
              </div>
            )}

            {/* ALL PAYMENT PLANS */}
            {allPlans.length > 0 && ov && (
              <div className="mb-5 space-y-3">
                <h3 className="text-sm font-semibold text-foreground">{t('installment.allPlans')}</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {[...allPlans]
                    .sort((a) => (a.paymentType === 'Cash' ? -1 : 1))
                    .map((plan, i) => (
                    <PaymentPlanCard
                      key={i}
                      plan={plan}
                      basePrice={ov.price}
                      currency={ov.currency}
                      planIndex={i}
                      totalPlans={allPlans.length}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* 6. META CHIPS — View, Availability, Bathrooms, Bedrooms, Balcony, Parking, Finishing */}
            {(displayView || ov?.availabilityStatus || displayBathrooms != null || displayBedrooms != null || displayHasBalcony || displayHasParking || displayFinishingType) && (
              <div className="flex flex-wrap items-center gap-3 mb-5">
                {displayView && (
                  <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-amber-500/10 border border-amber-500/20 shrink-0">
                    <Eye className="w-4 h-4 text-amber-600" />
                    <div>
                      <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.view')}</div>
                      <div className="font-semibold text-sm text-foreground whitespace-nowrap">{t(`view.${displayView}`)}</div>
                    </div>
                  </div>
                )}
                {ov?.availabilityStatus && (
                  <div className={`flex items-center gap-2 px-3.5 py-2.5 rounded-xl border shrink-0 ${
                    ov.availabilityStatus === 'SoldOut' ? 'bg-rose-500/10 border-rose-500/20' :
                    ov.availabilityStatus === 'Reserved' ? 'bg-amber-500/10 border-amber-500/20' :
                    'bg-emerald-500/10 border-emerald-500/20'
                  }`}>
                    <span className={`w-2.5 h-2.5 rounded-full ${
                      ov.availabilityStatus === 'SoldOut' ? 'bg-rose-500' :
                      ov.availabilityStatus === 'Reserved' ? 'bg-amber-500' :
                      'bg-emerald-500'
                    }`} />
                    <div>
                      <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.availability')}</div>
                      <div className="font-semibold text-sm text-foreground whitespace-nowrap">{t(`unit.availability.${ov.availabilityStatus}`)}</div>
                    </div>
                  </div>
                )}
                {displayBathrooms != null && (
                  <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-cyan-500/10 border border-cyan-500/20 shrink-0">
                    <Bath className="w-4 h-4 text-cyan-600" />
                    <div>
                      <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.bathrooms')}</div>
                      <div className="font-semibold text-sm text-foreground whitespace-nowrap">{fmtNum(displayBathrooms)}</div>
                    </div>
                  </div>
                )}
                {displayBedrooms != null && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-blue-500/10 border border-blue-500/20 shrink-0">
                      <Bed className="w-4 h-4 text-blue-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.bedrooms')}</div>
                        <div className="font-semibold text-sm text-foreground whitespace-nowrap">{fmtNum(displayBedrooms)}</div>
                      </div>
                    </div>
                  )}
                  {displayFinishingType && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-pink-500/10 border border-pink-500/20 shrink-0">
                      <Check className="w-4 h-4 text-pink-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.finishing')}</div>
                        <div className="font-semibold text-sm text-foreground whitespace-nowrap">{t(`finishing.${displayFinishingType}`)}</div>
                      </div>
                    </div>
                  )}
                  {displayHasBalcony && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-teal-500/10 border border-teal-500/20 shrink-0">
                      <Check className="w-4 h-4 text-teal-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.balcony')}</div>
                        <div className="font-semibold text-sm text-foreground whitespace-nowrap">{t('general.yes')}</div>
                      </div>
                    </div>
                  )}
                  {displayHasParking && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-slate-500/10 border border-slate-500/20 shrink-0">
                      <Check className="w-4 h-4 text-slate-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.parking')}</div>
                        <div className="font-semibold text-sm text-foreground whitespace-nowrap">{t('general.yes')}</div>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* Delivery badge */}
            {displayDeliveryDate && (
              <div className="flex items-center gap-2 mb-5">
                <Calendar className="w-4 h-4 text-rose-600" />
                <span className="text-sm font-medium text-foreground">{t('property.delivery')}: {fmtDate(displayDeliveryDate)}</span>
              </div>
            )}

            {displayDeliveryText && (
              <div className="flex items-center gap-3 p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/20 mb-5">
                <FileText className="w-4 h-4 text-amber-600 shrink-0" />
                <span className="text-sm text-foreground">{displayDeliveryText}</span>
              </div>
            )}

            {/* 7. ADDITIONAL SPECS — floor, furnished, unit#, building# */}
              {(displayFloor != null || displayFurnished || displayUnitNumber || displayBuildingNumber) && (
                <div className="flex flex-wrap gap-3 mb-5">
                  {displayFloor != null && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-purple-500/10 border border-purple-500/20">
                      <ArrowUpDown className="w-4 h-4 text-purple-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.floor')}</div>
                        <div className="font-semibold text-sm text-foreground">{fmtNum(displayFloor)}</div>
                      </div>
                    </div>
                  )}
                  {displayFurnished && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20">
                      <Check className="w-4 h-4 text-emerald-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.furnished')}</div>
                        <div className="font-semibold text-sm text-foreground">{t('general.yes')}</div>
                      </div>
                    </div>
                  )}
                  {displayUnitNumber && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-orange-500/10 border border-orange-500/20">
                      <Hash className="w-4 h-4 text-orange-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.unitNumber')}</div>
                        <div className="font-semibold text-sm text-foreground">{displayUnitNumber}</div>
                      </div>
                    </div>
                  )}
                  {displayBuildingNumber && (
                    <div className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl bg-indigo-500/10 border border-indigo-500/20">
                      <Building className="w-4 h-4 text-indigo-600" />
                      <div>
                        <div className="text-[10px] text-muted-foreground uppercase tracking-wide">{t('property.buildingNumber')}</div>
                        <div className="font-semibold text-sm text-foreground">{displayBuildingNumber}</div>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* 8. Description */}
            <div>
              <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.description')}</h2>
              <div className="w-10 h-1 bg-gold rounded-full mb-5" />
              <ReadMore text={description || ''} />
            </div>

            {/* 9. Highlights */}
            <UnitHighlights highlights={property?.highlights} highlightsAr={property?.highlightsAr} />

            {/* 10. Nearby Places */}
            <UnitNearbyPlaces places={property?.nearbyPlaces} />

            {/* 11. Floor Plans */}
            {(ov?.floorPlanUrl || (property?.floorPlans && property.floorPlans.length > 0)) && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('unit.floorPlans')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className={`grid gap-4 ${ov?.floorPlanUrl ? 'grid-cols-1 max-w-md' : 'grid-cols-1 sm:grid-cols-2'}`}>
                  {ov?.floorPlanUrl ? (
                    <div className="bg-muted/20 rounded-xl border border-border overflow-hidden hover:border-gold/30 transition-colors">
                      <PremiumImage src={ov.floorPlanUrl} alt="Floor plan" imgClassName="object-contain" />
                    </div>
                  ) : (
                    property?.floorPlans.map((fp, i) => (
                      <div key={i} className="bg-muted/20 rounded-xl border border-border overflow-hidden hover:border-gold/30 transition-colors">
                        <PremiumImage src={fp} alt={`Floor plan ${i + 1}`} imgClassName="object-contain" />
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}

            {/* 12. Virtual Tour */}
            {property?.virtualTourUrl && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('unit.virtualTour')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <a
                  href={property.virtualTourUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 bg-gradient-to-r from-navy to-navy/90 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:-translate-y-0.5 transition-all text-sm active:scale-[0.98]"
                >
                  <Globe className="w-5 h-5" />
                  {t('unit.virtualTour')}
                </a>
              </div>
            )}

            {/* 13. Construction Status */}
            {displayConstructionStatus && (
              <ConstructionTimeline status={displayConstructionStatus} deliveryDate={displayDeliveryDate} />
            )}

            {/* 14. Features / Amenities */}
            {(displayFeatures.length > 0) && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.features')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3">
                  {displayFeatures.map((feature) => (
                    <div key={feature} className="flex items-center gap-3 bg-muted/30 px-3 sm:px-4 py-2.5 sm:py-3 rounded-xl border border-border">
                      <div className="w-6 h-6 rounded-full bg-emerald-500/15 text-emerald-600 flex items-center justify-center shrink-0">
                        <Check className="w-3.5 h-3.5" />
                      </div>
                      <span className="font-medium text-foreground text-sm">{t(`feature.${feature}`, feature)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

          </div>

          {/* RIGHT COLUMN — Contact CTA (desktop only) */}
          <div className="lg:col-span-1 space-y-6">
            <div className="bg-card border border-border shadow-2xl rounded-2xl overflow-hidden lg:sticky lg:top-32 hidden lg:block">
              <div className="bg-navy px-5 sm:px-7 py-5 sm:py-6">
                <h2 className="font-display text-lg sm:text-xl font-bold text-white mb-1">{t('property.interested')}</h2>
              </div>
              <div className="p-4 sm:p-6 space-y-3">
                <a href={whatsappLink(whatsappNumber, variantInfoStr)} target="_blank" rel="noopener noreferrer"
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 bg-green-500 text-white rounded-xl font-semibold shadow-lg shadow-green-500/25 hover:bg-green-600 hover:shadow-xl hover:-translate-y-0.5 transition-all text-sm active:scale-[0.98]">
                  <MessageCircle className="w-5 h-5" /> {t('cta.whatsapp')}
                </a>
                <a href={`tel:${phoneNumber}`}
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 bg-secondary text-white rounded-xl font-semibold shadow-lg shadow-secondary/25 hover:bg-secondary/90 hover:shadow-xl hover:-translate-y-0.5 transition-all text-sm active:scale-[0.98]">
                  <PhoneCall className="w-5 h-5" /> {t('cta.callNow')}
                </a>
                <button type="button" onClick={openModal}
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 border-2 border-navy text-navy rounded-xl font-semibold hover:bg-navy hover:text-white hover:shadow-lg transition-all text-sm active:scale-[0.98]">
                  <Calendar className="w-5 h-5" /> {t('cta.bookViewing')}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

        {/* Mobile Sticky Summary with variant info */}
        <StickyMobileSummary
          size={ov?.size ?? null}
          price={displayPrice}
          currency={ov?.currency || property?.currency || 'EGP'}
          downPaymentPercent={installmentSummary?.downPaymentPercent ?? undefined}
          downPaymentAmount={installmentSummary?.downPaymentAmount ?? undefined}
          listingType={property?.listingType || 'sale'}
          onBookViewing={openModal}
        />
      </div>
      <SiteFooter />
      <BookViewingModal
        isOpen={isModalOpen}
        onClose={closeModal}
        propertyId={property?.rawId && property.rawId > 0 ? property.rawId : null}
        unitId={property?.rawUnitId && property.rawUnitId > 0 ? property.rawUnitId : null}
        variantName={ov?.name}
        variantSize={ov?.size}
        variantPrice={displayPrice ?? undefined}
        variantPublicKey={ov?.publicKey}
        projectId={property?.projectId}
        unitType={property?.propertyType}
        propertyCode={property?.propertyCode}
      />
    </div>
  );
}
