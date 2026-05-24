import { useParams, Link } from 'react-router-dom';
import { useProject } from '../hooks/use-properties';
import { useSettings, whatsappLink } from '../hooks/use-settings';
import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import { PropertyCard } from '../components/PropertyCard';
import { MobileStickyBar } from '../components/MobileStickyBar';
import { ReadMore } from '../components/ReadMore';
import SeoHelmet from '../components/SeoHelmet';
import { MapPin, Check, ArrowLeft, ArrowRight } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { localizedPath } from '../lib/paths';
import { PremiumImage } from '../components/PremiumImage';
import { ProjectDetailSkeleton } from '../components/Skeletons';

export default function ProjectDetailsPage() {
  const { slug } = useParams();
  const { data: project, isLoading } = useProject(slug || '');
  const { data: settings } = useSettings();
  const { t, language, fmtNum } = useLanguage();
  const whatsappNumber = settings?.whatsappNumber || '+201558730895';
  const phoneNumber = settings?.phoneNumber || whatsappNumber;
  const heroImg = project?.images?.[0] || project?.image || '/placeholder.svg';

  if (isLoading) {
    return <ProjectDetailSkeleton />;
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

  const name = language === 'ar' ? project.nameAr : project.nameEn;
  const description = language === 'ar' ? project.descriptionAr : project.descriptionEn;
  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const projectPath = `/projects/${project.slug}`;

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
        })}
      />
      <Header />

      {/* Hero */}
      <div className="relative h-[65vh] min-h-[420px] mt-16">
        <PremiumImage src={project.images?.[0] || project.image || ''} alt={name} width={1920} height={1080} options={{ quality: 'best', gravity: 'center', sharpen: 'soft' }} className="absolute inset-0 w-full h-full" imgClassName="object-cover" priority srcsetWidths={[640, 1080, 1600, 1920]} sizes="100vw" />
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
                <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                {t('projects.nowSelling')}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-8">
        <Link to={localizedPath('/', language)} className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 font-medium text-sm transition-colors group p-3 -ml-3 min-h-[44px]">
          {language === 'ar' ? <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" /> : <ArrowLeft className="w-4 h-4 group-hover:-translate-x-1 transition-transform" />}
          {t('nav.home')}
        </Link>
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
                        <div className="w-8 h-8 rounded-full bg-gold/10 flex items-center justify-center text-amber-600 shrink-0"><Check className="w-4 h-4" /></div>
                        <span className="font-medium">{t(`feature.${h}`, h)}</span>
                      </div>
                    ))}
                  </div>
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
                  <div className="flex justify-between items-center pb-5 border-b border-border">
                    <dt className="text-muted-foreground text-sm">{t('projects.location')}</dt>
                    <dd className="font-semibold text-sm">{language === 'ar' ? (project.locationAr || project.location) : project.location}</dd>
                  </div>
                  <div className="flex justify-between items-center pb-5 border-b border-border">
                    <dt className="text-muted-foreground text-sm">{t('projects.status')}</dt>
                    <dd className="font-semibold text-sm text-emerald-600">{t('projects.nowSelling')}</dd>
                  </div>
                  <div className="flex justify-between items-center">
                    <dt className="text-muted-foreground text-sm">{t('projects.availableUnits')}</dt>
                    <dd className="font-semibold text-sm">{fmtNum(project.unitCount ?? project.units?.length ?? 0)}</dd>
                  </div>
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
            {project.units.map(unit => <PropertyCard key={unit.id} property={unit} />)}
          </div>
        </div>
      </section>

      <MobileStickyBar
        whatsappHref={whatsappLink(whatsappNumber, `Hello, I'm interested in project ${name}`)}
        phoneHref={`tel:${phoneNumber}`}
      />
      <SiteFooter />
    </div>
  );
}
