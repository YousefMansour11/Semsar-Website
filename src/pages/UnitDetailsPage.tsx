import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useUnit, useProject } from '../hooks/use-properties';
import { useSettings, whatsappLink } from '../hooks/use-settings';
import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import { MobileStickyBar } from '../components/MobileStickyBar';
import { BookViewingModal } from '../components/BookViewingModal';
import SeoHelmet from '../components/SeoHelmet';
import { useLanguage } from '../i18n/LanguageContext';
import { localizedPath } from '../lib/paths';
import { MapPin, Square, Check, Eye, MessageCircle, Calendar, PhoneCall, ArrowLeft, ArrowRight, Bed, Bath, ArrowUpDown, Hash, Building, CreditCard, Wallet, ChevronDown, ChevronUp } from 'lucide-react';
import { PremiumImage } from '../components/PremiumImage';
import { ImageLightbox } from '../components/ImageLightbox';
import { PropertyDetailSkeleton } from '../components/Skeletons';

export default function UnitDetailsPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const { data: unit, isLoading } = useUnit(slug || '');
  const { data: project } = useProject(unit?.projectId || '');
  const { data: settings } = useSettings();
  const { t, language, fmtPrice, fmtDate, fmtNum } = useLanguage();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [activeImage, setActiveImage] = useState(0);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [descExpanded, setDescExpanded] = useState(false);
  const whatsappNumber = settings?.whatsappNumber || '+201558730895';
  const phoneNumber = settings?.phoneNumber || whatsappNumber;

  if (isLoading) {
    return <PropertyDetailSkeleton />;
  }
  if (!unit) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <div className="flex-1 flex items-center justify-center pt-20"><h1 className="text-2xl font-bold">{t('property.notFound')}</h1></div>
        <MobileStickyBar
          whatsappHref={whatsappLink(whatsappNumber)}
          phoneHref={`tel:${phoneNumber}`}
          primaryAction={{ label: t('cta.bookViewing'), onClick: () => setIsModalOpen(true) }}
        />
      <SiteFooter />
      </div>
    );
  }

  const title = language === 'ar' ? unit.titleAr : unit.titleEn;
  const description = language === 'ar' ? unit.descriptionAr : unit.descriptionEn;
  const displayPrice = unit.listingType === 'rent' ? (unit.rentPerMonth ?? 0) : unit.price;
  const gallery = unit.images?.length ? unit.images : [unit.image];
  const statusColors: Record<string, string> = {
    Available: 'bg-emerald-500/90 text-white',
    Reserved: 'bg-amber-500/90 text-white',
    Sold: 'bg-rose-500/90 text-white',
  };
  const installmentPlans = unit.installments?.length ? unit.installments : (unit.installment ? [unit.installment] : []);
  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const unitPath = `/units/${unit.slug}`;

  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet
        title={title}
        description={description?.slice(0, 160)}
        canonical={`${origin}${localizedPath(unitPath, language)}`}
        image={gallery[0]}
        alternates={[
          { hrefLang: 'en', href: `${origin}${localizedPath(unitPath, 'en')}` },
          { hrefLang: 'ar', href: `${origin}${localizedPath(unitPath, 'ar')}` },
        ]}
        jsonLd={JSON.stringify({
          '@context': 'https://schema.org',
          '@type': 'RealEstateListing',
          name: title,
          description: description?.slice(0, 200),
          url: `${origin}/units/${unit.slug}`,
          image: gallery[0],
          offers: { '@type': 'Offer', price: displayPrice, priceCurrency: 'EGP' },
        })}
      />
      <Header />

      <div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
        <button onClick={() => { if (project?.slug) navigate(localizedPath(`/projects/${project.slug}`, language)); else navigate(-1); }} className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 sm:mb-8 font-medium text-sm transition-colors group p-3 -ml-3 min-h-[44px]">
          {language === 'ar' ? <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" /> : <ArrowLeft className="w-4 h-4 group-hover:-translate-x-1 transition-transform" />}
          {t('general.back')}
        </button>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8 lg:gap-12">
          <div className="lg:col-span-2 space-y-8 sm:space-y-10">
            <div className="rounded-2xl overflow-hidden shadow-lg border border-border relative group">
              <div className="aspect-[16/10] bg-muted relative overflow-hidden">
                <button onClick={() => setLightboxOpen(true)} className="w-full h-full block cursor-pointer" aria-label={t('gallery.openLightbox')}>
                  <PremiumImage src={gallery[activeImage]} alt={title} width={1600} height={1000} options={{ quality: 'best', gravity: 'center', sharpen: 'soft' }} srcsetWidths={[640, 1080, 1600, 1920]} sizes="(max-width: 640px) 100vw, (max-width: 1024px) 66vw, 66vw" className="w-full h-full" imgClassName="transition-all duration-500" />
                </button>
                <div className={`absolute top-4 ${language === 'ar' ? 'right-4' : 'left-4'}`}>
                  <span className={`px-4 py-1.5 rounded-lg text-sm font-bold backdrop-blur-md shadow-sm ${statusColors[unit.status] || ''}`}>
                    {t(`status.${unit.status}`)}
                  </span>
                </div>
                <div className={`absolute bottom-4 ${language === 'ar' ? 'left-4' : 'right-4'} px-3 py-1.5 rounded-lg bg-black/50 backdrop-blur-sm text-white text-xs font-medium`}>
                  {fmtNum(activeImage + 1)} / {fmtNum(gallery.length)}
                </div>
                {gallery.length > 1 && (
                  <>
                    <button onClick={() => setActiveImage(prev => prev > 0 ? prev - 1 : gallery.length - 1)} aria-label="Previous image"
                      className={`absolute ${language === 'ar' ? 'right-3' : 'left-3'} top-1/2 -translate-y-1/2 w-11 h-11 rounded-full bg-white/80 backdrop-blur-sm shadow-lg flex items-center justify-center text-navy hover:bg-white transition-all opacity-0 group-hover:opacity-100`}>
                      {language === 'ar' ? <ArrowRight className="w-5 h-5" /> : <ArrowLeft className="w-5 h-5" />}
                    </button>
                    <button onClick={() => setActiveImage(prev => prev < gallery.length - 1 ? prev + 1 : 0)} aria-label="Next image"
                      className={`absolute ${language === 'ar' ? 'left-3' : 'right-3'} top-1/2 -translate-y-1/2 w-11 h-11 rounded-full bg-white/80 backdrop-blur-sm shadow-lg flex items-center justify-center text-navy hover:bg-white transition-all opacity-0 group-hover:opacity-100`}>
                      {language === 'ar' ? <ArrowLeft className="w-5 h-5" /> : <ArrowRight className="w-5 h-5" />}
                    </button>
                  </>
                )}
              </div>
              {gallery.length > 1 && (
                <div className="flex gap-2 p-2 overflow-x-auto bg-card">
                  {gallery.map((img, i) => (
                    <button key={i} onClick={() => setActiveImage(i)} aria-label={`${t('gallery.counter', { current: String(i + 1), total: String(gallery.length) })}`} className={`shrink-0 w-20 h-16 rounded-lg overflow-hidden border-2 transition-all ${i === activeImage ? 'border-secondary' : 'border-transparent opacity-70'}`}>
                      <PremiumImage src={img} alt="" width={300} height={200} options={{ quality: 'good' }} srcsetWidths={[160, 320, 640]} sizes="80px" className="w-full h-full" />
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div>
              <div className="flex flex-col md:flex-row md:items-start justify-between gap-4 mb-6 sm:mb-8">
                <div>
                  <h1 className="font-display text-2xl sm:text-3xl md:text-4xl font-bold text-foreground mb-2 leading-tight">{title}</h1>
                  <div className="flex items-center gap-2 text-muted-foreground font-medium text-sm">
                    <MapPin className="w-5 h-5 text-amber-600" />
                    <span>{language === 'ar' ? (unit.locationAr || unit.location) : unit.location}</span>
                  </div>
                </div>
                <div className="text-start md:text-end">
                  <div className="text-2xl sm:text-3xl font-bold text-navy">{fmtPrice(displayPrice, unit.currency)}</div>
                </div>
              </div>

              <div className="flex flex-wrap gap-3 sm:gap-6 py-5 sm:py-6 border-y border-border">
                {[
                  { icon: <Building className="w-5 h-5" />, bg: 'bg-secondary/10', color: 'text-secondary', label: t('property.type'), value: t(`prop_type.${unit.propertyType}`) },
                  { icon: <Square className="w-5 h-5" />, bg: 'bg-gold/10', color: 'text-amber-600', label: t('property.size'), value: `${fmtNum(unit.size)} ${t('general.m2')}` },
                  ...(unit.bedrooms != null ? [{ icon: <Bed className="w-5 h-5" />, bg: 'bg-blue-500/10', color: 'text-blue-600', label: t('property.bedrooms'), value: fmtNum(unit.bedrooms) }] : []),
                  ...(unit.bathrooms != null ? [{ icon: <Bath className="w-5 h-5" />, bg: 'bg-cyan-500/10', color: 'text-cyan-600', label: t('property.bathrooms'), value: fmtNum(unit.bathrooms) }] : []),
                  ...(unit.floor != null ? [{ icon: <ArrowUpDown className="w-5 h-5" />, bg: 'bg-purple-500/10', color: 'text-purple-600', label: t('property.floor'), value: fmtNum(unit.floor) }] : []),
                  ...(unit.view && unit.view !== 'Unknown' ? [{ icon: <Eye className="w-5 h-5" />, bg: 'bg-amber-500/10', color: 'text-amber-600', label: t('property.view'), value: t(`view.${unit.view}`) }] : []),
                ].map((spec, i) => (
                  <div key={i} className="flex items-center gap-3">
                    <div className={`w-11 h-11 sm:w-12 sm:h-12 rounded-xl ${spec.bg} flex items-center justify-center ${spec.color}`}>{spec.icon}</div>
                    <div>
                      <div className="text-xs text-muted-foreground uppercase tracking-wide">{spec.label}</div>
                      <div className="font-bold text-sm sm:text-base">{spec.value}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Additional Details */}
            {(() => {
              const hasDetails = unit.isFurnished || unit.unitNumber || unit.buildingNumber || unit.finishingType || unit.deliveryDate || unit.hasBalcony || unit.hasParking;
              if (!hasDetails) return null;
              return (
                <div>
                  <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.additionalDetails')}</h2>
                  <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                  <div className="flex flex-wrap gap-4 sm:gap-6">
                    {unit.isFurnished && (
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-emerald-500/10 flex items-center justify-center text-emerald-600"><Check className="w-5 h-5" /></div>
                        <div>
                          <div className="text-xs text-muted-foreground uppercase tracking-wide">{t('property.furnished')}</div>
                          <div className="font-bold text-sm sm:text-base">{t('general.yes')}</div>
                        </div>
                      </div>
                    )}
                    {unit.unitNumber && (
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-orange-500/10 flex items-center justify-center text-orange-600"><Hash className="w-5 h-5" /></div>
                        <div>
                          <div className="text-xs text-muted-foreground uppercase tracking-wide">{t('property.unitNumber')}</div>
                          <div className="font-bold text-sm sm:text-base">{unit.unitNumber}</div>
                        </div>
                      </div>
                    )}
                    {unit.buildingNumber && (
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-indigo-500/10 flex items-center justify-center text-indigo-600"><Building className="w-5 h-5" /></div>
                        <div>
                          <div className="text-xs text-muted-foreground uppercase tracking-wide">{t('property.buildingNumber')}</div>
                          <div className="font-bold text-sm sm:text-base">{unit.buildingNumber}</div>
                        </div>
                      </div>
                    )}
                    {unit.finishingType && (
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-pink-500/10 flex items-center justify-center text-pink-600"><Check className="w-5 h-5" /></div>
                        <div>
                          <div className="text-xs text-muted-foreground uppercase tracking-wide">{t('property.finishing')}</div>
                          <div className="font-bold text-sm sm:text-base">{t(`finishing.${unit.finishingType}`)}</div>
                        </div>
                      </div>
                    )}
                    {unit.deliveryDate && (
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-rose-500/10 flex items-center justify-center text-rose-600"><Calendar className="w-5 h-5" /></div>
                        <div>
                          <div className="text-xs text-muted-foreground uppercase tracking-wide">{t('property.delivery')}</div>
                          <div className="font-bold text-sm sm:text-base">{fmtDate(unit.deliveryDate)}</div>
                        </div>
                      </div>
                    )}
                    {unit.hasBalcony && (
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-teal-500/10 flex items-center justify-center text-teal-600"><Check className="w-5 h-5" /></div>
                        <div>
                          <div className="text-xs text-muted-foreground uppercase tracking-wide">{t('property.balcony')}</div>
                          <div className="font-bold text-sm sm:text-base">{t('general.yes')}</div>
                        </div>
                      </div>
                    )}
                    {unit.hasParking && (
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-slate-500/10 flex items-center justify-center text-slate-600"><Check className="w-5 h-5" /></div>
                        <div>
                          <div className="text-xs text-muted-foreground uppercase tracking-wide">{t('property.parking')}</div>
                          <div className="font-bold text-sm sm:text-base">{t('general.yes')}</div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              );
            })()}

            {description && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.description')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <p className="text-muted-foreground text-base sm:text-lg leading-relaxed whitespace-pre-wrap break-words max-w-prose">{descExpanded ? description : `${description?.slice(0, 250)}${description && description.length > 250 ? '...' : ''}`}</p>
                {description && description.length > 250 && (
                  <button onClick={() => setDescExpanded(!descExpanded)} className="flex items-center gap-1 text-sm text-gold mt-2 hover:underline">
                    {descExpanded ? <>Show less <ChevronUp className="w-4 h-4" /></> : <>Show more <ChevronDown className="w-4 h-4" /></>}
                  </button>
                )}
              </div>
            )}

            {(language === 'ar' ? (unit.featuresAr || unit.features) : unit.features)?.length > 0 && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.features')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3">
                  {(language === 'ar' ? (unit.featuresAr || unit.features) : unit.features).map((feature, idx) => (
                    <div key={idx} className="flex items-center gap-3 bg-muted/30 px-3 sm:px-4 py-2.5 sm:py-3 rounded-xl border border-border">
                      <div className="w-6 h-6 rounded-full bg-emerald-500/15 text-emerald-600 flex items-center justify-center shrink-0">
                        <Check className="w-3.5 h-3.5" />
                      </div>
                      <span className="font-medium text-foreground text-sm">{t(`feature.${feature}`, feature)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Installment Plans */}
            {installmentPlans.length > 0 && (
              <div>
                <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('property.installmentPlan')}</h2>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className={`bg-gradient-to-r from-gold/5 to-gold/10 border border-gold/20 rounded-2xl ${installmentPlans.length > 1 ? 'p-3 sm:p-4 space-y-3' : 'p-4 sm:p-6'}`}>
                  {installmentPlans.map((plan, idx) => (
                    <div key={idx}>
                      {installmentPlans.length > 1 && (
                        <div className="text-xs font-bold text-gold uppercase tracking-wider mb-2">{t('installment.plan')} {idx + 1}</div>
                      )}
                      <div className="grid grid-cols-3 gap-2 sm:gap-3">
                        <div className={`text-center bg-white rounded-xl border border-gold/20 ${installmentPlans.length > 1 ? 'p-2' : 'p-3 sm:p-4'}`}>
                          <CreditCard className="w-6 h-6 text-gold mx-auto mb-2" />
                          <div className="text-sm text-muted-foreground mb-1">{t('installment.downPayment')}</div>
                          <div className={`font-bold text-navy ${installmentPlans.length > 1 ? 'text-base' : 'text-xl sm:text-2xl'}`}>{fmtNum(plan.downPaymentPercent)}%</div>
                          <div className="text-sm text-muted-foreground">{fmtPrice(unit.price * plan.downPaymentPercent / 100, unit.currency)}</div>
                        </div>
                        <div className={`text-center bg-white rounded-xl border border-gold/20 ${installmentPlans.length > 1 ? 'p-2' : 'p-3 sm:p-4'}`}>
                          <Calendar className="w-6 h-6 text-gold mx-auto mb-2" />
                          <div className="text-sm text-muted-foreground mb-1">{t('installment.years')}</div>
                          <div className={`font-bold text-navy ${installmentPlans.length > 1 ? 'text-base' : 'text-xl sm:text-2xl'}`}>{fmtNum(plan.years)} {t('installment.years')}</div>
                          <div className="text-sm text-muted-foreground">{t('installment.years')}</div>
                        </div>
                        <div className={`text-center bg-white rounded-xl border border-gold/20 ${installmentPlans.length > 1 ? 'p-2' : 'p-3 sm:p-4'}`}>
                          <Wallet className="w-6 h-6 text-gold mx-auto mb-2" />
                          <div className="text-sm text-muted-foreground mb-1">{t('installment.monthly')}</div>
                          <div className={`font-bold text-navy ${installmentPlans.length > 1 ? 'text-base' : 'text-xl sm:text-2xl'}`}>{fmtPrice(plan.monthlyAmount, unit.currency)}</div>
                          <div className="text-sm text-muted-foreground">{t('installment.perMonth')}</div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          <div className="lg:col-span-1">
            <div className="bg-card border border-border shadow-2xl rounded-2xl overflow-hidden lg:sticky lg:top-32">
              <div className="bg-navy px-5 sm:px-7 py-5 sm:py-6">
                <h2 className="font-display text-lg sm:text-xl font-bold text-white mb-1">{t('property.interested')}</h2>
              </div>
              <div className="p-4 sm:p-6 space-y-3">
                <a href={whatsappLink(whatsappNumber)} target="_blank" rel="noopener noreferrer"
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 bg-green-500 text-white rounded-xl font-semibold shadow-lg shadow-green-500/25 hover:bg-green-600 hover:shadow-xl hover:-translate-y-0.5 transition-all text-sm active:scale-[0.98]">
                  <MessageCircle className="w-5 h-5" /> {t('cta.whatsapp')}
                </a>
                <a href={`tel:${phoneNumber}`}
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 bg-secondary text-white rounded-xl font-semibold shadow-lg shadow-secondary/25 hover:bg-secondary/90 hover:shadow-xl hover:-translate-y-0.5 transition-all text-sm active:scale-[0.98]">
                  <PhoneCall className="w-5 h-5" /> {t('cta.callNow')}
                </a>
                <button onClick={() => setIsModalOpen(true)}
                  className="flex items-center justify-center gap-2 w-full min-h-[48px] py-3 border-2 border-navy text-navy rounded-xl font-semibold hover:bg-navy hover:text-white hover:shadow-lg transition-all text-sm active:scale-[0.98]">
                  <Calendar className="w-5 h-5" /> {t('cta.bookViewing')}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <ImageLightbox
        open={lightboxOpen}
        images={gallery}
        activeIndex={activeImage}
        onClose={() => setLightboxOpen(false)}
        onPrev={() => setActiveImage(prev => prev > 0 ? prev - 1 : gallery.length - 1)}
        onNext={() => setActiveImage(prev => prev < gallery.length - 1 ? prev + 1 : 0)}
        title={title}
      />

        <MobileStickyBar
          whatsappHref={whatsappLink(whatsappNumber)}
          phoneHref={`tel:${phoneNumber}`}
          primaryAction={{ label: t('cta.bookViewing'), onClick: () => setIsModalOpen(true) }}
        />
      </div>
      <SiteFooter />
      <BookViewingModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        propertyId={unit.rawId && unit.rawId > 0 ? unit.rawId : null}
        unitId={unit.rawUnitId && unit.rawUnitId > 0 ? unit.rawUnitId : null}
      />
    </div>
  );
}