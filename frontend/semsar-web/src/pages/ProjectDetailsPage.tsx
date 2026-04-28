import { useParams, useNavigate } from 'react-router-dom';
import { useCallback, useMemo, useEffect, useState } from 'react';
import { useProject } from '../hooks/use-properties';
import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import { PropertyCard } from '../components/PropertyCard';
import { ReadMore } from '../components/ReadMore';
import SeoHelmet from '../components/SeoHelmet';
import { MapPin, Check, ArrowLeft, ArrowRight, DollarSign, Ruler, Layers, TrendingUp, Home, Shield } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { localizedPath } from '../lib/paths';
import { safeSessionGet, safeSessionRemove } from '../lib/utils';
import { PremiumImage } from '../components/PremiumImage';
import { validateImageUrls } from '../lib/image-validator';
import { ProjectDetailSkeleton } from '../components/Skeletons';
import { MediaGallery } from '../components/MediaGallery';
import { buildProjectMediaItems } from '../types/media';

export default function ProjectDetailsPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const { data: project, isLoading, isError } = useProject(slug || '');
  const { t, language, fmtNum, fmtPrice } = useLanguage();
  const [validUrls, setValidUrls] = useState<Set<string> | null>(null);

  useEffect(() => {
    if (!project) return;
    const all = new Set<string>();
    if (project.image) all.add(project.image);
    project.images?.forEach((u: string) => all.add(u));
    validateImageUrls([...all]).then(valid => setValidUrls(new Set(valid)));
  }, [project]);

  const name = useMemo(() => language === 'ar' ? project?.nameAr : project?.nameEn, [language, project?.nameAr, project?.nameEn]);
  const description = useMemo(() => language === 'ar' ? project?.descriptionAr : project?.descriptionEn, [language, project?.descriptionAr, project?.descriptionEn]);

  const handleBack = useCallback(() => {
    const savedY = safeSessionGet<string>('semsar_scroll_y');
    if (savedY) safeSessionRemove('semsar_scroll_y');
    navigate(localizedPath('/', language), {
      state: { scrollTo: 'projects', ...(savedY ? { restoreScrollY: parseInt(savedY, 10) } : {}) },
    });
  }, [navigate, language]);

  if (isLoading) {
    return <ProjectDetailSkeleton />;
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

  if (!project) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <div className="flex-1 flex items-center justify-center pt-20"><h1 className="text-2xl font-bold">{t('general.projectNotFound')}</h1></div>
        <SiteFooter />
      </div>
    );
  }
  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const projectPath = `/projects/${project.slug}`;

  const hasStats = (project.totalReservedUnits ?? 0) > 0 || (project.totalSoldUnits ?? 0) > 0;
  return (
    <div className="min-h-screen bg-background">
      <SeoHelmet
        title={name}
        description={description?.slice(0, 160)}
        canonical={`${origin}${localizedPath(projectPath, language)}`}
        image={project.image}
        alternates={[
          { hrefLang: 'en', href: `${origin}${localizedPath(projectPath, 'en')}` },
          { hrefLang: 'ar', href: `${origin}${localizedPath(projectPath, 'ar')}` },
        ]}
        jsonLd={JSON.stringify({
          '@context': 'https://schema.org',
          '@type': 'RealEstateProject',
          name,
          description: description?.slice(0, 200),
          url: `${origin}/projects/${project.slug}`,
          image: project.image,
          location: { '@type': 'Place', address: project.location },
          ...(project.videos?.length ? { video: project.videos.map(v => ({ '@type': 'VideoObject', contentUrl: v.url, thumbnailUrl: v.thumbnailUrl, name })) } : {}),
        })}
      />
      <Header />

      {/* Hero */}
      <div className="relative h-[65vh] min-h-[420px] mt-16">
        <PremiumImage src={project.images?.[0] || project.image || '/placeholder.svg'} alt={name || ''} width={1920} height={1080} profile="hero" className="absolute inset-0 w-full h-full" imgClassName="object-cover" priority fallback="/placeholder.svg" />
        <div className="absolute inset-0 bg-gradient-to-t from-background via-background/50 to-navy/20" />
        <div className="absolute inset-0 flex flex-col justify-end pb-16">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
            <div className="max-w-3xl">
              <div className="flex items-center gap-2 text-amber-600 font-medium mb-3">
                <MapPin className="w-5 h-5" />
                <span className="text-base">{language === 'ar' ? (project.locationAr || project.location) : project.location}</span>
              </div>
              <h1 className="font-display text-4xl sm:text-5xl md:text-6xl font-bold text-foreground mb-4 leading-tight">{name}</h1>
              <span className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-emerald-500/15 text-emerald-600 text-sm font-semibold border border-emerald-500/30">
                <span className="w-2 h-2 rounded-full bg-emerald-500" />
                {t('projects.nowSelling')}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-8">
        <button onClick={handleBack} className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 font-medium text-sm transition-colors group p-3 -ml-3 min-h-[44px]">
          {language === 'ar' ? <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" /> : <ArrowLeft className="w-4 h-4 group-hover:-translate-x-1 transition-transform" />}
          {t('general.back')}
        </button>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
          <div className="lg:col-span-2 space-y-12">
            <section>
              <h2 className="font-display text-3xl font-bold mb-2">{t('projects.about')}</h2>
              <div className="w-12 h-1 bg-gold rounded-full mb-6" />
              <ReadMore text={description || ''} />
            </section>

            {(() => {
              const highlights = language === 'ar' ? (project.highlightsAr?.length ? project.highlightsAr : project.highlights) : project.highlights;
              if (!highlights?.length) return null;
              return (
                <section>
                  <h2 className="font-display text-2xl font-bold mb-2">{t('projects.highlights')}</h2>
                  <div className="w-12 h-1 bg-gold rounded-full mb-6" />
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    {highlights.map((h, i) => (
                      <div key={i} className="flex items-center gap-3 bg-muted/40 p-4 rounded-xl border border-border hover:border-gold/30 transition-colors">
                        <div className="w-8 h-8 rounded-full bg-gold/10 flex items-center justify-center text-amber-600 shrink-0"><Check className="w-4 h-4" aria-hidden="true" /></div>
                        <span className="font-medium">{t(`feature.${h}`, h)}</span>
                      </div>
                    ))}
                  </div>
                </section>
              );
            })()}

            {project.propertyTypes?.length > 0 && (
              <section>
                <h2 className="font-display text-2xl font-bold mb-2">{t('projects.propertyTypes')}</h2>
                <div className="w-12 h-1 bg-gold rounded-full mb-6" />
                <div className="flex flex-wrap gap-3">
                  {project.propertyTypes.map((pt, i) => (
                    <span key={i} className="px-4 py-2 bg-muted/40 rounded-full border border-border text-sm font-medium hover:border-gold/30 transition-colors">{t(`prop_type.${pt}`)}</span>
                  ))}
                </div>
              </section>
            )}

            {(() => {
              const nearby = language === 'ar' ? (project.nearbyPlacesAr?.length ? project.nearbyPlacesAr : project.nearbyPlaces) : project.nearbyPlaces;
              if (!nearby?.length) return null;
              return (
                <section>
                  <h2 className="font-display text-2xl font-bold mb-2">{t('projects.nearbyPlaces')}</h2>
                  <div className="w-12 h-1 bg-gold rounded-full mb-6" />
                  <div className="flex flex-wrap gap-3">
                    {nearby.map((p, i) => (
                      <span key={i} className="px-4 py-2 bg-muted/40 rounded-full border border-border text-sm font-medium hover:border-gold/30 transition-colors">{p}</span>
                    ))}
                  </div>
                </section>
              );
            })()}

            {(() => {
              const filteredImages = validUrls ? project.images.filter((u: string) => validUrls.has(u)) : project.images;
              const media = buildProjectMediaItems(filteredImages, project.videos);
              const hasExtraContent = (project.videos?.length ?? 0) > 0 || filteredImages.length > 1;
              if (!hasExtraContent || media.items.length === 0) return null;
              return (
                <section>
                  <MediaGallery items={media.items} heroIndex={media.heroIndex} title={name} />
                </section>
              );
            })()}
          </div>

          <div>
            <div className="bg-card border border-border shadow-xl rounded-2xl overflow-hidden sticky top-32">
              <div className="bg-navy px-7 py-5">
                <h3 className="font-display text-lg font-bold text-white">{t('projects.overview')}</h3>
              </div>
              <div className="px-7 py-6">
                <dl className="space-y-5">
                  <div className="pb-5 border-b border-border">
                    <dt className="text-muted-foreground text-sm mb-1.5">{t('projects.location')}</dt>
                    <dd className="font-semibold text-sm leading-relaxed">{language === 'ar' ? (project.locationAr || project.location) : project.location}</dd>
                  </div>
                  <div className="flex justify-between items-center pb-5 border-b border-border">
                    <dt className="text-muted-foreground text-sm">{t('projects.status')}</dt>
                    <dd className="font-semibold text-sm text-emerald-600">{t('projects.nowSelling')}</dd>
                  </div>

                  {/* Stats */}
                  {hasStats && (
                    <div className="space-y-3 pb-5 border-b border-border">
                      {(project.totalReservedUnits ?? 0) > 0 && (
                        <div className="flex justify-between items-center">
                          <dt className="text-muted-foreground text-sm flex items-center gap-1.5">
                            <Shield className="w-3.5 h-3.5 text-amber-500" />
                            {t('project.totalReserved')}
                          </dt>
                          <dd className="font-semibold text-sm text-amber-600">{fmtNum(project.totalReservedUnits!)}</dd>
                        </div>
                      )}
                      {(project.totalSoldUnits ?? 0) > 0 && (
                        <div className="flex justify-between items-center">
                          <dt className="text-muted-foreground text-sm flex items-center gap-1.5">
                            <TrendingUp className="w-3.5 h-3.5 text-red-500" />
                            {t('project.totalSold')}
                          </dt>
                          <dd className="font-semibold text-sm text-red-600">{fmtNum(project.totalSoldUnits!)}</dd>
                        </div>
                      )}
                    </div>
                  )}

                  <div className="flex justify-between items-center pb-5 border-b border-border">
                    <dt className="text-muted-foreground text-sm flex items-center gap-1.5">
                      <Home className="w-3.5 h-3.5 text-gold" />
                      {t('projects.availableUnits')}
                    </dt>
                    <dd className="font-semibold text-sm">{fmtNum(project.totalAvailableUnits ?? 0)}</dd>
                  </div>

                  {project.unitTypesCount != null && (
                    <div className="flex justify-between items-center pt-5 border-t border-border">
                      <dt className="text-muted-foreground text-sm flex items-center gap-1.5">
                        <Layers className="w-3.5 h-3.5 text-gold" />
                        {t('project.unitTypes')}
                      </dt>
                      <dd className="font-semibold text-sm">{fmtNum(project.unitTypesCount)}</dd>
                    </div>
                  )}

                  {project.ownershipType && (
                    <div className="flex justify-between items-center pt-5 border-t border-border">
                      <dt className="text-muted-foreground text-sm">{t('projects.ownershipType')}</dt>
                      <dd className="font-semibold text-sm">{t(`ownership.${project.ownershipType}`)}</dd>
                    </div>
                  )}

                  {project.startingPrice != null && (
                    <div className="flex justify-between items-center pt-5 border-t border-border">
                      <dt className="text-muted-foreground text-sm flex items-center gap-1.5">
                        <DollarSign className="w-3.5 h-3.5 text-emerald-500" />
                        {t('projects.startingPrice')}
                      </dt>
                      <dd className="font-semibold text-sm text-emerald-600">
                        {fmtPrice(project.startingPrice!)}
                      </dd>
                    </div>
                  )}
                  {project.totalArea != null && (
                    <div className="flex justify-between items-center pt-5 border-t border-border">
                      <dt className="text-muted-foreground text-sm flex items-center gap-1.5">
                        <Ruler className="w-3.5 h-3.5 text-gold" />
                        {t('projects.totalArea')}
                      </dt>
                      <dd className="font-semibold text-sm">{fmtNum(project.totalArea)} {t('general.m2')}</dd>
                    </div>
                  )}
                  {project.latitude != null && project.longitude != null && (
                    <div className="flex justify-between items-center pt-5 border-t border-border">
                      <dt className="text-muted-foreground text-sm">{t('projects.coordinates')}</dt>
                      <dd className="font-semibold text-sm text-muted-foreground">{project.latitude.toFixed(4)}, {project.longitude.toFixed(4)}</dd>
                    </div>
                  )}
                </dl>
              </div>
            </div>
          </div>
        </div>
      </div>

      <section id="units" className="bg-muted/30 py-12 sm:py-24">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <h2 className="font-display text-3xl font-bold mb-2">{t('projects.nowSelling')}</h2>
          <div className="w-16 h-1.5 bg-gold rounded-full mb-10" />
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
            {(project.units ?? []).map(unit => <PropertyCard key={unit.id} property={unit} />)}
          </div>
        </div>
      </section>

      <SiteFooter />
    </div>
  );
}
