import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import SeoHelmet from '../components/SeoHelmet';
import { useLanguage } from '../i18n/LanguageContext';
import { MapPin, Shield, Users, TrendingUp } from 'lucide-react';

export default function AboutPage() {
  const { t } = useLanguage();

  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet
        title={t('seo.aboutTitle')}
        description={t('seo.aboutDescription')}
        canonical={typeof window !== 'undefined' ? `${window.location.origin}/about` : undefined}
      />
      <Header />

      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12 sm:py-24">
        <h1 className="font-display text-3xl sm:text-5xl font-bold text-foreground mb-4">{t('about.heading')}</h1>
        <div className="w-16 h-1.5 bg-gold rounded-full mb-8" />

        <p className="text-lg text-muted-foreground leading-relaxed mb-12">
          {t('about.paragraph')}
        </p>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 mb-12">
          <div className="bg-card border border-border rounded-2xl p-6 space-y-3">
            <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center text-primary">
              <MapPin aria-hidden="true" className="w-6 h-6" />
            </div>
            <h2 className="font-bold text-lg">{t('general.whyUs.expertTitle')}</h2>
            <p className="text-muted-foreground text-sm">{t('general.whyUs.expertDesc')}</p>
          </div>
          <div className="bg-card border border-border rounded-2xl p-6 space-y-3">
            <div className="w-12 h-12 rounded-xl bg-gold/10 flex items-center justify-center text-amber-600">
              <TrendingUp aria-hidden="true" className="w-6 h-6" />
            </div>
            <h2 className="font-bold text-lg">{t('general.whyUs.roiTitle')}</h2>
            <p className="text-muted-foreground text-sm">{t('general.whyUs.roiDesc')}</p>
          </div>
          <div className="bg-card border border-border rounded-2xl p-6 space-y-3">
            <div className="w-12 h-12 rounded-xl bg-emerald-500/10 flex items-center justify-center text-emerald-600">
              <Users aria-hidden="true" className="w-6 h-6" />
            </div>
            <h2 className="font-bold text-lg">{t('general.whyUs.supportTitle')}</h2>
            <p className="text-muted-foreground text-sm">{t('general.whyUs.supportDesc')}</p>
          </div>
          <div className="bg-card border border-border rounded-2xl p-6 space-y-3">
            <div className="w-12 h-12 rounded-xl bg-secondary/10 flex items-center justify-center text-secondary">
              <Shield aria-hidden="true" className="w-6 h-6" />
            </div>
            <h2 className="font-bold text-lg">{t('general.whyUs.secureTitle')}</h2>
            <p className="text-muted-foreground text-sm">{t('general.whyUs.secureDesc')}</p>
          </div>
        </div>
      </div>

      <SiteFooter />
    </div>
  );
}