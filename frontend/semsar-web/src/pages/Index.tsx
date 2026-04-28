import { useState, useEffect, useLayoutEffect, useRef, useCallback, memo } from 'react';
import { useLanguage } from '../i18n/LanguageContext';
import { Header } from '../components/SiteHeader';
import SeoHelmet from '../components/SeoHelmet';
import { SiteFooter } from '../components/SiteFooter';
import { getSiteUrl } from '../lib/paths';
import { ProjectsSlider } from '../components/ProjectsSlider';
import { PropertyCard } from '../components/PropertyCard';
import { PremiumImage } from '../components/PremiumImage';
import { AdvancedFilterPanel } from '../components/AdvancedFilterPanel';
import { LandRequestModal } from '../components/LandRequestModal';
import { useProjects } from '../hooks/use-properties';
import { usePropertySearch, SearchFilters } from '../hooks/use-property-search';
import { useLocationTree, flattenTree } from '../hooks/use-locations';
import { SectionSkeleton, ProjectsSliderSkeleton } from '../components/Skeletons';
import { EmptyState } from '../components/EmptyState';
import { Search, Home, Key, MapPin, X, SlidersHorizontal } from 'lucide-react';
import { safeSessionGet, safeSessionSet, safeSessionRemove } from '../lib/utils';
const SALE_FILTERS: SearchFilters = { listingType: 'Resale' };
const RENT_FILTERS: SearchFilters = { listingType: 'Rental' };

function hasActiveFilters(f: SearchFilters) {
  return (Object.keys(f) as (keyof SearchFilters)[]).some(k => k !== 'listingType' && f[k] !== undefined && f[k] !== null && (Array.isArray(f[k]) ? (f[k] as unknown[]).length > 0 : true));
}

const SectionHeader = memo(function SectionHeader({ label, accent, icon }: { label: string; accent?: string; icon?: React.ReactNode }) {
  return (
    <div>
      {accent && <span className="inline-block text-xs font-bold uppercase tracking-widest text-secondary mb-2">{accent}</span>}
      <h2 className="font-display text-2xl sm:text-3xl md:text-4xl font-bold text-foreground mb-4 flex items-center gap-3">
        {icon}
        {label}
      </h2>
      <div className="w-16 h-1.5 bg-gold rounded-full" />
    </div>
  );
});

function FilterEmptyState({ onReset }: { onReset: () => void }) {
  const { t } = useLanguage();
  return (
    <EmptyState
      icon="search"
      actionLabel={t('filters.reset')}
      onAction={onReset}
    />
  );
}

function FilterChips({ filters, onClear }: { filters: SearchFilters; onClear: (key: string) => void }) {
  const { t, language, fmtNum } = useLanguage();
  const { data: locationTree } = useLocationTree();
  const chips: { key: string; label: string }[] = [];

  if (filters.minPrice !== undefined || filters.maxPrice !== undefined) {
    const label = [filters.minPrice ?? '0', filters.maxPrice ?? '∞']
      .map(v => Number(v).toLocaleString()).join(' - ');
    chips.push({ key: 'price', label: `${label} ${t('general.currency')}` });
  }
  if (filters.minSize !== undefined || filters.maxSize !== undefined) {
    const label = [filters.minSize ?? '0', filters.maxSize ?? '∞'].join(' - ');
    chips.push({ key: 'size', label: `${label} ${t('general.m2')}` });
  }
  if (filters.bedrooms !== undefined) chips.push({ key: 'bedrooms', label: `${fmtNum(filters.bedrooms)}+ ${t('filters.bedrooms')}` });
  if (filters.bathrooms !== undefined) chips.push({ key: 'bathrooms', label: `${fmtNum(filters.bathrooms)}+ ${t('filters.bathrooms')}` });
  if (filters.propertyType) chips.push({ key: 'propertyType', label: t(`prop_type.${filters.propertyType}`, filters.propertyType) });
  if (filters.isFurnished) chips.push({ key: 'isFurnished', label: t('filters.furnished') });
  if (filters.hasInstallment) chips.push({ key: 'hasInstallment', label: t('filters.installments') });
  if (filters.locationIds?.length && locationTree) {
    const names = filters.locationIds
      .map(id => flattenTree(locationTree).find(x => x.node.id === id))
      .filter(Boolean)
      .map(x => language === 'ar' ? (x!.node.nameAr || x!.node.nameEn) : x!.node.nameEn);
    const label = names.length === 1 ? names[0] : `${names.length} ${t('filters.locations')}`;
    chips.push({ key: 'locationIds', label });
  }

  if (!chips.length) return null;

  return (
    <div className="flex flex-wrap items-center gap-2 mb-6">
      <SlidersHorizontal className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
      {chips.map(chip => (
        <span key={chip.key} className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-secondary/10 text-secondary text-xs font-semibold border border-secondary/20">
          {chip.label}
          <button onClick={() => onClear(chip.key)} className="hover:bg-secondary/20 rounded p-0.5 -mr-0.5 transition-colors" aria-label={`Remove ${chip.label}`}>
            <X className="w-3 h-3" />
          </button>
        </span>
      ))}
    </div>
  );
}

const HERO_BG = '/images/hero-bg.jpg';

const stats = [
  { value: '150+', labelKey: 'stats.properties' },
  { value: '5', labelKey: 'stats.locations' },
  { value: '24/7', labelKey: 'stats.support' },
  { value: '100%', labelKey: 'stats.satisfaction' },
];

const Index = () => {
  const { t, fmtNum } = useLanguage();
  const [saleFilters, setSaleFilters] = useState<SearchFilters>(SALE_FILTERS);
  const [rentFilters, setRentFilters] = useState<SearchFilters>(RENT_FILTERS);
  const [landModalOpen, setLandModalOpen] = useState(false);

  const contentRef = useRef<HTMLDivElement>(null);
  const scrollRestored = useRef(false);

  const { data: projects, isLoading: projectsLoading } = useProjects();
  const { data: saleResult, isLoading: saleLoading } = usePropertySearch(saleFilters);
  const { data: rentResult, isLoading: rentLoading } = usePropertySearch(rentFilters);

  // Restore filters from NavContext (set on pointer-down when leaving Index)
  useEffect(() => {
    const saved = safeSessionGet<{ section: string; filters: SearchFilters }>('semsar_nav');
    if (saved) {
      safeSessionRemove('semsar_nav');
      if (saved.filters) {
        if (saved.section === 'for-sale') setSaleFilters(saved.filters);
        else if (saved.section === 'for-rent') setRentFilters(saved.filters);
      }
    }
  }, []);

  // Restore scroll position on back navigation — useLayoutEffect fires before browser paint
  // and BEFORE ScrollManager's useLayoutEffect (child fires before parent wrapper)
  useLayoutEffect(() => {
    if (scrollRestored.current) return;
    if (saleLoading || rentLoading) return;
    const savedY = safeSessionGet<string>('semsar_scroll_y');
    if (savedY) {
      safeSessionRemove('semsar_scroll_y');
      scrollRestored.current = true;
      const y = parseInt(savedY, 10);
      if (y > 0) window.scrollTo(0, y);
    }
  });

  // Fallback: if data wasn't cached, wait for it to load then restore
  useEffect(() => {
    if (scrollRestored.current) return;
    if (saleLoading || rentLoading) return;
    const savedY = safeSessionGet<string>('semsar_scroll_y');
    if (savedY) {
      safeSessionRemove('semsar_scroll_y');
      scrollRestored.current = true;
      const y = parseInt(savedY, 10);
      if (y > 0) requestAnimationFrame(() => window.scrollTo(0, y));
    }
  }, [saleLoading, rentLoading]);

  useEffect(() => {
    const el = contentRef.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.15 }
    );
    el.querySelectorAll('.reveal-on-scroll, .reveal-stagger').forEach(child => observer.observe(child));
    return () => observer.disconnect();
  }, []);

  const scrollTo = useCallback((id: string) => {
    const el = document.getElementById(id);
    if (el) {
      const footer = document.querySelector('footer');
      let top = el.getBoundingClientRect().top + window.scrollY;
      if (footer) {
        const footerTop = footer.getBoundingClientRect().top + window.scrollY;
        top = Math.min(top, footerTop - window.innerHeight);
      }
      window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
    }
  }, []);

  const handleSaleClear = useCallback((key: string) => {
    setSaleFilters(prev => {
      const next = { ...prev };
      if (key === 'price') { delete next.minPrice; delete next.maxPrice; }
      else if (key === 'size') { delete next.minSize; delete next.maxSize; }
      else delete (next as SearchFilters)[key as keyof SearchFilters];
      return next;
    });
  }, []);

  const handleRentClear = useCallback((key: string) => {
    setRentFilters(prev => {
      const next = { ...prev };
      if (key === 'price') { delete next.minPrice; delete next.maxPrice; }
      else if (key === 'size') { delete next.minSize; delete next.maxSize; }
      else delete (next as SearchFilters)[key as keyof SearchFilters];
      return next;
    });
  }, []);

  return (
    <div className="min-h-screen bg-background" ref={contentRef}>
      <SeoHelmet
        title={t('hero.title')}
        description={t('seo.homeDescription')}
        canonical={typeof window !== 'undefined' ? window.location.href : 'https://semsar-alpha.vercel.app/'}
        alternates={[
          { hrefLang: 'en', href: `${getSiteUrl()}/en` },
          { hrefLang: 'ar', href: `${getSiteUrl()}/ar` },
          { hrefLang: 'x-default', href: `${getSiteUrl()}/en` },
        ]}
        jsonLd={JSON.stringify({
          '@context': 'https://schema.org',
          '@type': 'RealEstateAgent',
          name: 'Semsar Real Estate',
          url: typeof window !== 'undefined' ? window.location.origin : 'https://semsar.vercel.app',
          logo: '/semsar-logo.svg',
          telephone: '+201558730895',
          email: 'semsar.realestate@gmail.com',
          address: { '@type': 'PostalAddress', addressCountry: 'EG' },
          areaServed: 'Egypt'
        })}
      />
      <Header />

      {/* ── Hero ──────────────────────────────────── */}
      <section className="relative h-[85vh] sm:h-screen min-h-[500px] sm:min-h-[640px] flex items-center justify-center overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-navy/85 via-navy/50 to-transparent z-10" />
        <div className="absolute inset-0 bg-gradient-to-t from-background via-background/20 to-transparent z-10" />
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,_transparent_40%,_rgba(0,0,0,0.4)_100%)] z-10 pointer-events-none" />
        <PremiumImage
          src={HERO_BG}
          alt={t('hero.alt')}
          width={1920}
          height={1080}
          profile="hero"
          priority
          className="absolute inset-0 w-full h-full will-change-transform"
          imgClassName="w-full h-full object-cover scale-105 will-change-transform"
        />

        <div className="relative z-20 w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center mt-16 sm:mt-20">
          <div className="space-y-4 sm:space-y-6 fade-children">
            <span className="inline-block py-1.5 px-5 rounded-full bg-white/10 backdrop-blur-md border border-white/20 text-white/90 text-xs font-bold tracking-[0.25em] uppercase">
              {t('hero.badge')}
            </span>

            <h1 className="font-display text-3xl sm:text-5xl md:text-7xl font-bold text-white drop-shadow-2xl mx-auto max-w-5xl leading-[1.1]">
              {t('hero.title')}
            </h1>

            <p className="text-base sm:text-xl text-white/80 max-w-2xl mx-auto drop-shadow leading-relaxed">
              {t('hero.subtitle')}
            </p>

              <div className="flex flex-col sm:flex-row gap-3 sm:gap-4 justify-center items-center pt-2">
                <button onClick={() => scrollTo('for-sale')} className="w-full sm:w-auto px-9 py-4 bg-gold text-navy rounded-full font-bold shadow-xl shadow-amber-900/20 hover:bg-gold-dark hover:text-white hover:shadow-2xl hover:-translate-y-0.5 transition-all duration-300 text-sm tracking-wide active:scale-[0.97] will-change-transform">
                  {t('hero.explore')}
                </button>
                <button onClick={() => scrollTo('projects')} className="w-full sm:w-auto px-9 py-4 bg-white/10 backdrop-blur-md border border-white/30 text-white rounded-full font-bold hover:bg-white/20 hover:-translate-y-0.5 transition-all duration-300 text-sm tracking-wide active:scale-[0.97] will-change-transform">
                  {t('hero.projects')}
                </button>
              </div>
          </div>
        </div>
      </section>

      {/* ── Stats ──────────────────────────────────── */}
      <section className="cv-auto bg-navy py-10 sm:py-14">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="reveal-stagger grid grid-cols-2 md:grid-cols-4 gap-6 sm:gap-10 text-center">
            {stats.map(({ value, labelKey }) => (
              <div key={labelKey} className="reveal-item space-y-1.5">
                <div className="text-2xl sm:text-4xl font-display font-bold text-gold">{value}</div>
                <div className="text-white/50 text-xs sm:text-sm font-medium tracking-wide">{t(labelKey)}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Featured Projects ─────────────────────── */}
      <section id="projects" className="cv-auto py-12 sm:py-24 bg-muted/30">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mb-8 sm:mb-12">
          <SectionHeader label={t('projects.featured')} />
        </div>
        {projectsLoading ? (
          <ProjectsSliderSkeleton />
        ) : (
          <ProjectsSlider projects={projects || []} />
        )}
      </section>

      {/* ── Land Request CTA ──────────────────── */}
      <section id="land-request" className="cv-auto py-12 sm:py-24 bg-background">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="reveal-on-scroll relative bg-gradient-to-r from-gold to-gold-dark rounded-2xl sm:rounded-3xl p-8 sm:p-10 md:p-16 text-center overflow-hidden">
            <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'url("data:image/svg+xml,%3Csvg width=\'60\' height=\'60\' viewBox=\'0 0 60 60\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cg fill=\'none\' fill-rule=\'evenodd\'%3E%3Cg fill=\'%23000000\' fill-opacity=\'0.4\'%3E%3Cpath d=\'M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z\'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")' }} />
            <div className="relative z-10">
              <MapPin className="w-10 h-10 sm:w-12 sm:h-12 text-white/80 mx-auto mb-4" />
              <h2 className="font-display text-2xl sm:text-3xl md:text-4xl font-bold text-white mb-4">{t('land.title')}</h2>
              <p className="text-white/80 text-base sm:text-lg mb-6 sm:mb-8 max-w-xl mx-auto">{t('land.subtitle')}</p>
              <button
                onClick={() => setLandModalOpen(true)}
                className="px-8 sm:px-10 py-3 sm:py-4 bg-white text-navy rounded-full font-bold text-base sm:text-lg shadow-2xl shadow-black/20 hover:bg-gray-100 hover:-translate-y-1 transition-all duration-300 will-change-transform"
              >
                {t('land.cta')}
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* ── Properties for Sale ───────────────────── */}
      <section id="for-sale" className="cv-auto py-12 sm:py-24 bg-muted/30">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 sm:gap-6 mb-8 sm:mb-12">
            <div className="flex items-end gap-4">
              <SectionHeader label={t('properties.forSale')} accent={t('nav.forSale')} icon={<Home className="w-6 h-6 sm:w-7 sm:h-7 text-secondary" />} />
              {saleResult && hasActiveFilters(saleFilters) && (
                <div className="flex items-center gap-2 px-4 py-2 rounded-xl bg-gradient-to-r from-secondary/10 to-secondary/5 border border-secondary/20 text-secondary text-sm font-semibold shadow-sm mb-2 sm:mb-0">
                  <Search className="w-3.5 h-3.5" />
                  <span className="tabular-nums">{fmtNum(saleResult.totalCount)}</span>
                  <span>{t('properties.found')}</span>
                </div>
              )}
            </div>
            <AdvancedFilterPanel filters={saleFilters} onApply={setSaleFilters} priceSuffix={t('general.currency')} />
          </div>
          <FilterChips filters={saleFilters} onClear={handleSaleClear} />
          {saleLoading ? (
            <SectionSkeleton />
          ) : saleResult && saleResult.properties.length > 0 ? (
            <div onPointerDown={() => { safeSessionSet('semsar_nav', { section: 'for-sale', filters: saleFilters }); safeSessionSet('semsar_scroll_section', 'for-sale'); safeSessionSet('semsar_scroll_y', window.scrollY); }} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-8" aria-live="polite">
              {saleResult.properties.map(p => <PropertyCard key={p.id} property={p} />)}
            </div>
          ) : (
            <FilterEmptyState onReset={() => setSaleFilters(SALE_FILTERS)} />
          )}
        </div>
      </section>

      {/* ── Properties for Rent ───────────────────── */}
      <section id="for-rent" className="cv-auto py-12 sm:py-24 bg-background">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 sm:gap-6 mb-8 sm:mb-12">
            <div className="flex items-end gap-4">
              <SectionHeader label={t('properties.forRent')} accent={t('nav.forRent')} icon={<Key className="w-6 h-6 sm:w-7 sm:h-7 text-amber-600" />} />
              {rentResult && hasActiveFilters(rentFilters) && (
                <div className="flex items-center gap-2 px-4 py-2 rounded-xl bg-gradient-to-r from-secondary/10 to-secondary/5 border border-secondary/20 text-secondary text-sm font-semibold shadow-sm mb-2 sm:mb-0">
                  <Search className="w-3.5 h-3.5" />
                  <span className="tabular-nums">{fmtNum(rentResult.totalCount)}</span>
                  <span>{t('properties.found')}</span>
                </div>
              )}
            </div>
            <AdvancedFilterPanel filters={rentFilters} onApply={setRentFilters} priceSuffix={t('properties.rentSuffix')} />
          </div>
          <FilterChips filters={rentFilters} onClear={handleRentClear} />
          {rentLoading ? (
            <SectionSkeleton />
          ) : rentResult && rentResult.properties.length > 0 ? (
            <div onPointerDown={() => { safeSessionSet('semsar_nav', { section: 'for-rent', filters: rentFilters }); safeSessionSet('semsar_scroll_section', 'for-rent'); safeSessionSet('semsar_scroll_y', window.scrollY); }} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-8" aria-live="polite">
              {rentResult.properties.map(p => <PropertyCard key={p.id} property={p} />)}
            </div>
          ) : (
            <FilterEmptyState onReset={() => setRentFilters(RENT_FILTERS)} />
          )}
        </div>
      </section>

      <SiteFooter />
      <LandRequestModal isOpen={landModalOpen} onClose={() => setLandModalOpen(false)} />
    </div>
  );
};

export default Index;
