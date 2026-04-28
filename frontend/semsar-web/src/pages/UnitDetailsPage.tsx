import { useState, useMemo, useCallback, useEffect } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useUnit, useProject } from '../hooks/use-properties';
import { useSettings, whatsappLink } from '../hooks/use-settings';
import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import { MobileStickyBar } from '../components/MobileStickyBar';
import { BookViewingModal } from '../components/BookViewingModal';
import { PremiumImage } from '../components/PremiumImage';
import SeoHelmet from '../components/SeoHelmet';
import { useLanguage } from '../i18n/LanguageContext';
import { localizedPath } from '../lib/paths';
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

export default function UnitDetailsPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { data: unit, isLoading, isError } = useUnit(slug || '');
  const { data: project } = useProject(unit?.projectId || '');
  const { data: settings } = useSettings();
  const { t, language, fmtPrice, fmtDate, fmtNum } = useLanguage();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [pickedVariant, setPickedVariant] = useState<Variant | null>(null);
  const [validUrls, setValidUrls] = useState<Set<string> | null>(null);

  useEffect(() => {
    if (!unit) return;
    const all = new Set<string>();
    if (unit.image) all.add(unit.image);
    unit.images?.forEach((u: string) => all.add(u));
    unit.variants?.forEach((v: Variant) => v.images?.forEach((u: string) => all.add(u)));
    validateImageUrls([...all]).then(valid => setValidUrls(new Set(valid)));
  }, [unit]);

  const variants: Variant[] = useMemo(() => unit?.variants || [], [unit]);

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

  const ov = selectedVariant;

  const mediaCollection = useMemo(() => {
    if (!unit) return { items: [], heroIndex: 0 };
    const varImages = ov?.images?.length ? ov.images : undefined;
    const raw = varImages || (unit.images?.length ? unit.images : [unit.image]);
    const filtered = validUrls ? raw.filter((u: string) => validUrls.has(u)) : raw;
    const hero = filtered[0] || unit.image;
    return buildMediaItems(hero, filtered, unit.videos);
  }, [unit, ov, validUrls]);

  const displayPrice = useMemo(() => {
    if (ov) {
      if (ov.price === 0 && (!ov.rentPerMonth || ov.rentPerMonth === 0)) return null;
      if (unit?.listingType === 'rent' && ov.rentPerMonth != null) return ov.rentPerMonth;
      return ov.price;
    }
    if (unit?.listingType === 'rent') return unit?.rentPerMonth ?? null;
    const p = unit?.minPrice ?? unit?.price;
    return p != null && p > 0 ? p : null;
  }, [ov, unit]);

  const priceLabel = useMemo(() => {
    const p = displayPrice;
    if (p == null) return t('properties.priceOnRequest');
    const curr = ov?.currency || unit?.currency || 'EGP';
    return fmtPrice(p, curr);
  }, [ov, displayPrice, unit, fmtPrice, t]);

  const displayBedrooms = ov && ov.bedrooms > 0 ? ov.bedrooms : unit?.bedrooms;
  const displayBathrooms = ov && ov.bathrooms > 0 ? ov.bathrooms : unit?.bathrooms;
  const displayFloor = ov && ov.floor != null ? ov.floor : unit?.floor;
  const displayView = ov && ov.view && ov.view !== 'Unknown' ? ov.view : (unit?.view && unit.view !== 'Unknown' ? unit.view : null);
  const displayFurnished = ov ? ov.isFurnished : unit?.isFurnished;
  const displayUnitNumber = ov?.unitNumber || unit?.unitNumber;
  const displayBuildingNumber = ov?.buildingNumber || unit?.buildingNumber;
  const displayDeliveryDate = ov?.deliveryDate || unit?.deliveryDate;
  const displayFinishingType = ov?.finishingType || unit?.finishingType;
  const displayDeliveryText = language === 'ar'
    ? (unit.deliveryTextAr || unit.deliveryText || project?.deliveryTextAr || project?.deliveryText)
    : (unit.deliveryText || project?.deliveryText);
  const displayConstructionStatus = unit.constructionStatus || project?.constructionStatus;
  const displayHasBalcony = ov ? ov.hasBalcony : unit?.hasBalcony;
  const displayHasParking = ov ? ov.hasParking : unit?.hasParking;

  const allPlans = useMemo(() => {
    return unit?.installments?.length ? unit.installments : (unit?.installment ? [unit.installment] : []);
  }, [unit]);

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

  const title = language === 'ar' ? (unit?.titleAr || unit?.titleEn) : unit?.titleEn;
  const description = language === 'ar' ? (unit?.descriptionAr || unit?.descriptionEn) : unit?.descriptionEn;
  const gallery = unit?.images?.length ? unit.images : (unit?.image ? [unit.image] : []);
  const whatsappNumber = settings?.whatsappNumber || '+201558730895';
  const phoneNumber = settings?.phoneNumber || whatsappNumber;

  const openModal = useCallback(() => setIsModalOpen(true), []);
  const closeModal = useCallback(() => setIsModalOpen(false), []);

  const variantInfoStr = selectedVariant
    ? `${unit.propertyCode ? `[${unit.propertyCode}] ` : ''}${fmtNum(selectedVariant.size)} sqm · ${fmtPrice(selectedVariant.price, selectedVariant.currency)}${selectedVariant.view && selectedVariant.view !== 'Unknown' ? ` · ${selectedVariant.view}` : ''}`
    : '';

  const priceSuffix = unit?.listingType === 'rent' ? ` / ${t('properties.rentSuffix')}` : '';

  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const unitPath = `/units/${unit?.slug}`;

  const jsonLd = useMemo(() => {
    if (!unit) return '';
    const img = unit.images?.[0] ?? unit.image;
    return JSON.stringify({
      '@context': 'https://schema.org',
      '@type': 'RealEstateListing',
      name: ov ? `${title} - ${ov.name}` : title,
      description: description?.slice(0, 200),
      url: `${origin}/units/${unit.slug}`,
      image: img,
      ...(displayPrice != null ? { offers: { '@type': 'Offer', price: displayPrice, priceCurrency: ov?.currency ?? unit.currency ?? 'EGP' } } : {}),
      ...(ov ? { additionalProperty: { '@type': 'PropertyValue', name: 'Variant', value: ov.name } } : {}),
      ...(unit.videos?.length ? { video: unit.videos.map(v => ({ '@type': 'VideoObject', contentUrl: v.url, thumbnailUrl: v.thumbnailUrl, name: title })) } : {}),
    });
  }, [unit, ov, title, description, displayPrice, origin]);

  const seoTitle = useMemo(() => {
    if (language === 'ar' && unit?.seoTitleAr) return unit.seoTitleAr;
    if (unit?.seoTitleEn) return unit.seoTitleEn;
    return ov ? `${title} - ${ov.name}` : title;
  }, [language, unit, ov, title]);

  const seoDescription = useMemo(() => {
    if (language === 'ar' && unit?.seoDescriptionAr) return unit.seoDescriptionAr;
    if (unit?.seoDescriptionEn) return unit.seoDescriptionEn;
    return description?.slice(0, 160);
  }, [language, unit, description]);

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
  if (!unit) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <div className="flex-1 flex items-center justify-center pt-20"><h1 className="text-2xl font-bold">{t('property.notFound')}</h1></div>
        <MobileStickyBar
          whatsappHref={whatsappLink(whatsappNumber, variantInfoStr)}
          phoneHref={`tel:${phoneNumber}`}
          primaryAction={{ label: t('cta.bookViewing'), onClick: openModal }}
        />
        <SiteFooter />
        <BookViewingModal isOpen={isModalOpen} onClose={closeModal} />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet
        title={seoTitle}
        description={seoDescription}
        canonical={`${origin}${localizedPath(unitPath, language)}`}
        image={gallery[0]}
        alternates={[
          { hrefLang: 'en', href: `${origin}${localizedPath(unitPath, 'en')}` },
          { hrefLang: 'ar', href: `${origin}${localizedPath(unitPath, 'ar')}` },
        ]}
        jsonLd={jsonLd}
      />
      <Header />

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
        <button onClick={() => { if (project?.slug) navigate(localizedPath(`/projects/${project.slug}`, language)); else navigate(-1); }} className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 sm:mb-8 font-medium text-sm transition-colors group p-3 -ml-3 min-h-[44px]">
          {language === 'ar' ? <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" /> : <ArrowLeft className="w-4 h-4 group-hover:-translate-x-1 transition-transform" />}
          {t('general.back')}
        </button>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8 lg:gap-12">
          {/* LEFT COLUMN */}
          <div className="lg:col-span-2 space-y-8 sm:space-y-10">

            {/* 1. Media Gallery */}
            {mediaCollection.items.length > 0 && (
              <MediaGallery items={mediaCollection.items} heroIndex={mediaCollection.heroIndex} title={title} />
            )}

            {/* 2. Title + Location + Analytics */}
            <div>
              <div className="flex flex-col md:flex-row md:items-start justify-between gap-4 mb-4">
                <div className="min-w-0">
                  <div className="flex items-center gap-3 flex-wrap mb-1">
                    <h1 className="font-display text-2xl sm:text-3xl md:text-4xl font-bold text-foreground leading-tight">
                      {title}
                    </h1>
                  </div>
                  <div className="text-sm text-muted-foreground font-medium mb-1">
                    {t(`prop_type.${unit.propertyType}`)}
                  </div>
                  <div className="flex items-center gap-2 text-muted-foreground font-medium text-sm">
                    <MapPin className="w-5 h-5 text-amber-600" />
                    <span>{language === 'ar' ? (unit.locationAr || unit.location) : unit.location}</span>
                  </div>
                  <AnalyticsBar viewCount={unit.viewCount} inquiryCount={unit.inquiryCount} favoriteCount={unit.favoriteCount} />
                </div>
              </div>

              <div className="border-b border-border my-5 sm:my-6" />

              {/* 3. VARIANT INFO BAR — name + price */}
              {ov && (
                <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-2 mb-5 mt-5">
                  <span className="font-display text-xl sm:text-2xl font-bold text-foreground">
                    {language === 'ar' ? `${t(`prop_type.${unit.propertyType}`)} ${fmtNum(ov.size)} ${t('general.m2')}` : ov.name}
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
                    {unit.ownershipType && (
                      <OwnershipBadge type={unit.ownershipType} />
                    )}
                  </div>
                </div>
              )}

              {/* 4. Variant Selector */}
              {variants.length > 0 && (
                <div className="mb-5 sm:mb-6">
                  <PremiumVariantSelector
                    variants={variants}
                    selectedVariant={ov}
                    onChange={onVariantChange}
                    listingType={unit.listingType}
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

              {/* 7. Additional Details (floor, furnished, unit#, building#) */}
              {(displayFloor != null || displayFurnished || displayUnitNumber || displayBuildingNumber) && (
                <div className="flex flex-wrap gap-3 mt-5">
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
            {description && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.description')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <ReadMore text={description} />
              </div>
            )}

            {/* 9. Highlights */}
            <UnitHighlights highlights={unit.highlights} highlightsAr={unit.highlightsAr} />

            {/* 10. Nearby Places */}
            <UnitNearbyPlaces places={unit.nearbyPlaces} />

            {/* 11. Floor Plans */}
            {(ov?.floorPlanUrl || (unit.floorPlans && unit.floorPlans.length > 0)) && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('unit.floorPlans')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className={`grid gap-4 ${ov?.floorPlanUrl ? 'grid-cols-1 max-w-md' : 'grid-cols-1 sm:grid-cols-2'}`}>
                  {ov?.floorPlanUrl ? (
                    <div className="bg-muted/20 rounded-xl border border-border overflow-hidden hover:border-gold/30 transition-colors">
                      <PremiumImage src={ov.floorPlanUrl} alt={`Floor plan`} imgClassName="object-contain" />
                    </div>
                  ) : (
                    unit.floorPlans.map((fp, i) => (
                      <div key={i} className="bg-muted/20 rounded-xl border border-border overflow-hidden hover:border-gold/30 transition-colors">
                        <PremiumImage src={fp} alt={`Floor plan ${i + 1}`} imgClassName="object-contain" />
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}

            {/* 12. Virtual Tour */}
            {unit.virtualTourUrl && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('unit.virtualTour')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <a
                  href={unit.virtualTourUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 bg-gradient-to-r from-navy to-navy/90 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:-translate-y-0.5 transition-all text-sm active:scale-[0.98]"
                >
                  <Globe className="w-5 h-5" />
                  {t('unit.virtualTour')}
                </a>
              </div>
            )}

            {/* 13. Construction Timeline */}
            {displayConstructionStatus && (
              <ConstructionTimeline status={displayConstructionStatus} deliveryDate={displayDeliveryDate} />
            )}

            {/* 14. Features */}
            {(language === 'ar' ? (unit.featuresAr?.length ? unit.featuresAr : unit.features) : unit.features)?.length > 0 && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.features')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3">
                  {(language === 'ar' ? (unit.featuresAr?.length ? unit.featuresAr : unit.features) : unit.features).map((feature) => (
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

          {/* RIGHT COLUMN — CTA Card */}
          <div className="lg:col-span-1 space-y-6">
            <div className="bg-card border border-border shadow-2xl rounded-2xl overflow-hidden lg:sticky lg:top-32">
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
                <button onClick={openModal}
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 border-2 border-navy text-navy rounded-xl font-semibold hover:bg-navy hover:text-white hover:shadow-lg transition-all text-sm active:scale-[0.98]">
                  <Calendar className="w-5 h-5" /> {t('cta.bookViewing')}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <StickyMobileSummary
        size={ov?.size ?? null}
        price={displayPrice}
        currency={ov?.currency || unit.currency || 'EGP'}
        downPaymentPercent={installmentSummary?.downPaymentPercent ?? undefined}
        downPaymentAmount={installmentSummary?.downPaymentAmount ?? undefined}
        listingType={unit.listingType}
        onBookViewing={openModal}
      />
      <SiteFooter />
      <BookViewingModal
        isOpen={isModalOpen}
        onClose={closeModal}
        propertyId={unit.rawId && unit.rawId > 0 ? unit.rawId : null}
        unitId={unit.rawUnitId && unit.rawUnitId > 0 ? unit.rawUnitId : null}
        variantName={ov?.name}
        variantSize={ov?.size}
        variantPrice={displayPrice ?? undefined}
        variantPublicKey={ov?.publicKey}
        projectId={unit.projectId}
        unitType={unit.propertyType}
        propertyCode={unit.propertyCode}
      />
    </div>
  );
}
