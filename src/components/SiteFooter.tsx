import { useLanguage } from '../i18n/LanguageContext';
import { Phone, Mail, MapPin, Instagram, Facebook, MessageCircle } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { LogoIcon } from './LogoIcon';
import { useSettings, whatsappLink } from '../hooks/use-settings';
import { useProjects } from '../hooks/use-properties';

export function SiteFooter() {
  const { t, language, dir } = useLanguage();
  const navigate = useNavigate();
  const { data: settings } = useSettings();
  const { data: projects, isLoading: projectsLoading } = useProjects();
  const phone = settings?.phoneNumber || '+201558730895';
  const wa = whatsappLink(settings?.whatsappNumber || phone);
  const email = settings?.email || 'semsar.realestate@gmail.com';
  const social = settings?.socialLinks || {};

  const quickLinks: ({ label: string; to: string } | { label: string; href: string; scrollTo: string })[] = [
    { label: t('nav.home'), to: '/' },
    { label: t('nav.about'), to: '/about' },
    { label: t('nav.contact'), to: '/contact' },
    { label: t('nav.forSale'), href: '#for-sale', scrollTo: 'for-sale' },
    { label: t('nav.forRent'), href: '#for-rent', scrollTo: 'for-rent' },
    { label: t('land.cta'), href: '#land-request', scrollTo: 'land-request' },
  ];

  const contactItems = [
    { icon: <Phone className="w-4 h-4" />, content: phone, href: `tel:${phone}`, dir: 'ltr' as const },
    { icon: <MessageCircle className="w-4 h-4" />, content: t('footer.whatsapp'), href: wa },
    { icon: <Mail className="w-4 h-4" />, content: email, href: `mailto:${email}` },
    { icon: <MapPin className="w-4 h-4" />, content: t('footer.address') },
  ];

  const socialLinks = [
    { href: social.facebook, icon: <Facebook className="w-4 h-4" />, label: 'Facebook' },
    { href: social.instagram, icon: <Instagram className="w-4 h-4" />, label: 'Instagram' },
    { href: wa || null, icon: <MessageCircle className="w-4 h-4" />, label: 'WhatsApp' },
  ].filter(s => s.href);

  return (
    <footer id="site-footer" className="bg-navy text-white" dir={dir}>
      <div className="max-w-7xl mx-auto px-5 sm:px-8 lg:px-12 pt-8 sm:pt-12 lg:pt-16 pb-8 sm:pb-10">

        {/* Main grid: mobile=2col, tablet=3col, desktop=4col */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-x-5 sm:gap-x-8 lg:gap-x-12 gap-y-8 sm:gap-y-10 mb-8 sm:mb-12">

          {/* Brand — full width on mobile/tablet, 1col on desktop */}
          <div className="col-span-2 sm:col-span-3 lg:col-span-1">
            <Link to="/" className="inline-flex items-center gap-3 mb-3 group">
              <div className="w-10 h-10 sm:w-12 sm:h-12 shrink-0 transition-transform duration-300 group-hover:scale-105">
                <LogoIcon variant="gold" />
              </div>
              <div className="flex flex-col">
                <span className="font-display font-bold text-lg tracking-wider text-white leading-none">Semsar</span>
                <span className="text-gold text-sm font-bold leading-tight mt-0.5">سمسار</span>
              </div>
            </Link>
            <p className="text-white/45 text-xs sm:text-sm leading-[1.7] max-w-xs">
              {t('footer.aboutText')}
            </p>
            {socialLinks.length > 0 && (
              <div className="flex items-center gap-2.5 mt-4 sm:mt-5">
                {socialLinks.map(({ href, icon, label }) => (
                  <a
                    key={label}
                    href={href!}
                    target={label === 'WhatsApp' ? '_blank' : undefined}
                    rel={label === 'WhatsApp' ? 'noopener noreferrer' : undefined}
                    aria-label={label}
                    className="w-10 h-10 sm:w-11 sm:h-11 rounded-full border border-white/15 flex items-center justify-center text-white/40 hover:text-white hover:border-gold/60 hover:bg-gold/10 transition-all duration-300"
                  >
                    {icon}
                  </a>
                ))}
              </div>
            )}
          </div>

          {/* Quick Links — 1col */}
          <div>
            <h4 className="font-bold text-[11px] sm:text-xs uppercase tracking-[0.15em] text-gold/80 mb-3 sm:mb-5">
              {t('footer.quickLinks')}
            </h4>
            <ul className="space-y-2 sm:space-y-2.5">
              {quickLinks.map((item) => (
                <li key={item.label}>
                  {'to' in item ? (
                    <Link to={item.to!} className="text-white/55 hover:text-white text-xs sm:text-sm transition-colors duration-200 inline-block py-1">{item.label}</Link>
                  ) : (
                    <button onClick={() => navigate('/', { state: { scrollTo: 'scrollTo' in item ? item.scrollTo : null } })} className="text-white/55 hover:text-white text-xs sm:text-sm transition-colors duration-200 text-start py-1">{item.label}</button>
                  )}
                </li>
              ))}
            </ul>
          </div>

          {/* Projects — 1col */}
          <div>
            <h4 className="font-bold text-[11px] sm:text-xs uppercase tracking-[0.15em] text-gold/80 mb-3 sm:mb-5">
              {t('footer.projects')}
            </h4>
            <ul className="space-y-2 sm:space-y-2.5">
              {projectsLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <li key={i}><div className="h-3 sm:h-3.5 w-20 sm:w-24 bg-white/8 animate-pulse rounded" /></li>
                ))
              ) : projects?.length ? (
                projects.slice(0, 5).map(p => (
                  <li key={p.slug}>
                    <Link to={`/projects/${p.slug}`} className="text-white/55 hover:text-white text-xs sm:text-sm transition-colors duration-200 inline-block py-1">
                      {language === 'ar' ? p.nameAr || p.nameEn || p.name : p.nameEn || p.name}
                    </Link>
                  </li>
                ))
              ) : (
                <li className="text-white/30 text-xs sm:text-sm">{t('footer.noProjects')}</li>
              )}
            </ul>
          </div>

          {/* Contact — full width on mobile, 1col on tablet/desktop */}
          <div className="col-span-2 sm:col-span-1">
            <h4 className="font-bold text-[11px] sm:text-xs uppercase tracking-[0.15em] text-gold/80 mb-3 sm:mb-5">
              {t('footer.contactUs')}
            </h4>
            <div className="grid grid-cols-2 sm:grid-cols-1 gap-2 sm:gap-3">
              {contactItems.map((item, i) => (
                <div key={i}>
                  {'href' in item && item.href ? (
                    <a href={item.href} target={item.href.startsWith('http') ? '_blank' : undefined} rel={item.href.startsWith('http') ? 'noopener noreferrer' : undefined} className="flex items-center gap-2.5 sm:gap-3 text-white/55 hover:text-white transition-colors duration-200 group py-1.5 min-h-[44px]">
                      <span className="w-9 h-9 sm:w-10 sm:h-10 rounded-full border border-white/10 flex items-center justify-center text-gold shrink-0 group-hover:border-gold/40 transition-colors duration-200">
                        {item.icon}
                      </span>
                      <span className="text-xs sm:text-sm leading-tight" {...(item.dir ? { dir: item.dir } : {})}>{item.content}</span>
                    </a>
                  ) : (
                    <span className="flex items-center gap-2.5 sm:gap-3 text-white/55 py-1.5 min-h-[44px]">
                      <span className="w-9 h-9 sm:w-10 sm:h-10 rounded-full border border-white/10 flex items-center justify-center text-gold shrink-0">
                        {item.icon}
                      </span>
                      <span className="text-xs sm:text-sm leading-tight">{item.content}</span>
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>

        </div>

        {/* Bottom bar */}
        <div className="pt-5 sm:pt-8 border-t border-white/8 flex flex-col sm:flex-row items-center justify-between gap-3">
          <p className="text-white/30 text-[11px] sm:text-xs">{t('footer.rights')}</p>
        </div>
      </div>
    </footer>
  );
}
