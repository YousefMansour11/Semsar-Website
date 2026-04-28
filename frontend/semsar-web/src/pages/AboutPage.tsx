import { Header } from '../components/SiteHeader';
import { SiteFooter } from '../components/SiteFooter';
import SeoHelmet from '../components/SeoHelmet';
import { useLanguage } from '../i18n/LanguageContext';
import { MapPin, Shield, Users, TrendingUp } from 'lucide-react';
import { getSiteUrl } from '../lib/paths';

export default function AboutPage() {
  const { t } = useLanguage();

  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet
        title={t('seo.aboutTitle')}
        description={t('seo.aboutDescription')}
        canonical={typeof window !== 'undefined' ? window.location.href : undefined}
        alternates={[
          { hrefLang: 'en', href: `${getSiteUrl()}/en/about` },
          { hrefLang: 'ar', href: `${getSiteUrl()}/ar/about` },
        ]}
      />
      <Header />

      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12 sm:py-24">
        <h1 className="font-display text-3xl sm:text-5xl font-bold text-foreground mb-4">{t('about.heading')}</h1>
        <div className="w-16 h-1.5 bg-gold rounded-full mb-8" />

        <p className="text-lg text-muted-foreground leading-relaxed mb-12">
          {t('about.paragraph')}
        </p>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-5 mb-12">
          {[
            { icon: <MapPin className="w-6 h-6" />, bg: 'bg-primary/10', color: 'text-primary', title: t('general.whyUs.expertTitle'), desc: t('general.whyUs.expertDesc') },
            { icon: <TrendingUp className="w-6 h-6" />, bg: 'bg-gold/10', color: 'text-amber-600', title: t('general.whyUs.roiTitle'), desc: t('general.whyUs.roiDesc') },
            { icon: <Users className="w-6 h-6" />, bg: 'bg-emerald-500/10', color: 'text-emerald-600', title: t('general.whyUs.supportTitle'), desc: t('general.whyUs.supportDesc') },
            { icon: <Shield className="w-6 h-6" />, bg: 'bg-secondary/10', color: 'text-secondary', title: t('general.whyUs.secureTitle'), desc: t('general.whyUs.secureDesc') },
          ].map((item, i) => (
            <div key={i} className="bg-card border border-border/60 rounded-2xl p-6 space-y-3 transition-all duration-200 hover:border-border hover:shadow-md">
              <div className={`w-12 h-12 rounded-xl ${item.bg} flex items-center justify-center ${item.color}`}>
                {item.icon}
              </div>
              <h2 className="font-bold text-lg">{item.title}</h2>
              <p className="text-muted-foreground text-sm leading-relaxed">{item.desc}</p>
            </div>
          ))}
        </div>
      </div>

      <SiteFooter />
    </div>
  );
}