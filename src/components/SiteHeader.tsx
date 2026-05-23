import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useLanguage } from '../i18n/LanguageContext';
import { type Language } from '../i18n/translations';
import { localizedPath, stripLanguagePrefix } from '../lib/paths';
import { Menu, X, Globe } from 'lucide-react';
import { useState, useEffect } from 'react';
import { LogoIcon } from './LogoIcon';

export function Header() {
  const { language, setLanguage, t } = useLanguage();
  const [isScrolled, setIsScrolled] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const location = useLocation();

  const isHomePage = stripLanguagePrefix(location.pathname) === '/';
  const showSolid = !isHomePage || isScrolled;

  useEffect(() => {
    const handleScroll = () => setIsScrolled(window.scrollY > 40);
    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  useEffect(() => {
    setIsMobileMenuOpen(false);
  }, [location]);

  const languages: { code: Language; label: string }[] = [
    { code: 'en', label: 'EN' },
    { code: 'ar', label: 'ع' },
  ];

  const navLinks = [
    { label: t('nav.home'), href: '/', isRoute: true },
    { label: t('nav.projects'), href: '#projects', isRoute: false },
    { label: t('nav.forSale'), href: '#for-sale', isRoute: false },
    { label: t('nav.forRent'), href: '#for-rent', isRoute: false },
    { label: t('nav.lands'), href: '#land-request', isRoute: false },
  ];

  const navigate = useNavigate();

  const handleNavClick = (href: string, isRoute: boolean) => {
    if (isRoute) return;
    const sectionId = href.replace('#', '');
    if (isHomePage) {
      const el = document.getElementById(sectionId);
      el?.scrollIntoView({ behavior: 'smooth' });
    } else {
      navigate('/', { state: { scrollTo: sectionId } });
    }
  };

  return (
    <>
      <a href="#main-content" className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-50 focus:px-4 focus:py-2 focus:bg-background focus:text-foreground focus:rounded-lg focus:ring-2 focus:ring-ring">
        {t('general.skipToContent')}
      </a>
    <header
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-500 ${
        showSolid
          ? 'bg-white/95 backdrop-blur-xl shadow-sm border-b border-gray-100/80 py-2 sm:py-3'
          : 'bg-transparent py-3 sm:py-5'
      }`}
    >
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-[max(env(safe-area-inset-top,0px),0px)]">
        <div className="flex items-center justify-between gap-4">
          <Link to={localizedPath('/', language)} className="flex items-center gap-2 group shrink-0">
            <div className={`w-8 h-8 sm:w-10 sm:h-10 rounded-xl flex items-center justify-center p-1 transition-all duration-300 ${
              showSolid
                ? 'bg-[#0A1628]'
                : 'bg-white/12 border border-white/20'
            }`}>
              <LogoIcon variant={showSolid ? 'gold' : 'navy'} />
            </div>
            <div className="flex flex-col leading-none gap-0.5">
              <span className={`font-display font-extrabold text-base sm:text-lg tracking-tight transition-colors duration-300 leading-none ${
                showSolid ? 'text-[#0A1628]' : 'text-white drop-shadow-sm'
              }`}>
                Semsar
              </span>
              <span className="font-bold leading-none text-[#C9A84C] transition-colors duration-300" style={{ fontSize: '10px', letterSpacing: '0.15em' }}>
                سمسار
              </span>
            </div>
          </Link>

          <nav className="hidden md:flex items-center gap-0.5 flex-1 justify-center">
            {navLinks.map(({ label, href, isRoute }) =>
              isRoute ? (
                <Link
                  key={href}
                  to={localizedPath(href, language)}
                  className={`relative px-3 lg:px-4 py-2 text-sm font-medium transition-all duration-200 group rounded-lg ${
                    showSolid ? 'text-gray-500 hover:text-navy' : 'text-white/75 hover:text-white'
                  }`}
                >
                  {label}
                  <span className="absolute bottom-0.5 left-4 right-4 h-0.5 rounded-full bg-gold scale-x-0 group-hover:scale-x-100 transition-transform duration-200 origin-left" />
                </Link>
              ) : (
                <a
                  key={href}
                  href={isHomePage ? href : `/${href}`}
                  onClick={(e) => { e.preventDefault(); handleNavClick(href, false); }}
                  className={`relative px-3 lg:px-4 py-2 text-sm font-medium transition-all duration-200 group rounded-lg cursor-pointer ${
                    showSolid ? 'text-gray-500 hover:text-navy' : 'text-white/75 hover:text-white'
                  }`}
                >
                  {label}
                  <span className="absolute bottom-0.5 left-4 right-4 h-0.5 rounded-full bg-gold scale-x-0 group-hover:scale-x-100 transition-transform duration-200 origin-left" />
                </a>
              )
            )}
          </nav>

          <div className="hidden md:flex items-center gap-2.5 shrink-0">
            <div className={`flex items-center gap-0.5 p-1 rounded-xl transition-colors ${
              showSolid ? 'bg-gray-100' : 'bg-white/10'
            }`}>
              <Globe className={`w-3 h-3 mx-1 ${showSolid ? 'text-gray-400' : 'text-white/50'}`} />
              {languages.map(lang => (
                <button
                  key={lang.code}
                  onClick={() => {
                    setLanguage(lang.code);
                    navigate(localizedPath(stripLanguagePrefix(location.pathname), lang.code), { replace: true });
                  }}
                  className={`text-xs font-bold px-3 min-h-[44px] rounded-lg transition-all duration-200 ${
                    language === lang.code
                      ? 'bg-gold text-white shadow-sm'
                      : showSolid
                        ? 'text-gray-500 hover:text-gray-800 hover:bg-white'
                        : 'text-white/60 hover:text-white hover:bg-white/10'
                  }`}
                >
                  {lang.label}
                </button>
              ))}
            </div>
          </div>

          <button
            className={`md:hidden min-h-[44px] min-w-[44px] flex items-center justify-center rounded-xl border transition-all duration-200 ${
              showSolid
                ? 'border-gray-200 text-navy bg-white hover:bg-gray-50'
                : 'border-white/20 text-white bg-white/10 hover:bg-white/20'
            }`}
            onClick={() => setIsMobileMenuOpen(v => !v)}
            aria-label="Toggle menu"
          >
            {isMobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </div>
      </div>

      {/* Mobile Menu */}
      <div className={`md:hidden overflow-hidden transition-all duration-300 ease-in-out ${
        isMobileMenuOpen ? 'max-h-[500px] opacity-100' : 'max-h-0 opacity-0 invisible'
      }`} aria-hidden={!isMobileMenuOpen}>
        <div className="bg-navy mt-2 mx-4 rounded-2xl border border-white/10 shadow-2xl overflow-hidden">
          <div className="p-3 space-y-0.5">
              {navLinks.map(({ label, href, isRoute }) =>
              isRoute ? (
                <Link
                  key={href}
                  to={localizedPath(href, language)}
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="flex items-center px-4 py-3.5 text-white/80 hover:text-white hover:bg-white/5 rounded-xl transition-colors font-medium text-sm"
                >
                  {label}
                </Link>
              ) : (
                <a
                  key={href}
                  href={href}
                  onClick={(e) => { e.preventDefault(); setIsMobileMenuOpen(false); handleNavClick(href, false); }}
                  className="flex items-center px-4 py-3.5 text-white/80 hover:text-white hover:bg-white/5 rounded-xl transition-colors font-medium text-sm"
                >
                  {label}
                </a>
              )
            )}
          </div>
          <div className="px-3 pb-3 pt-1 border-t border-white/10">
            <div className="grid grid-cols-2 gap-1.5">
              {languages.map(lang => (
                <button
                  key={lang.code}
                  onClick={() => {
                    setLanguage(lang.code);
                    setIsMobileMenuOpen(false);
                    navigate(localizedPath(stripLanguagePrefix(location.pathname), lang.code), { replace: true });
                  }}
                  className={`py-2.5 rounded-xl text-sm font-bold transition-colors ${
                    language === lang.code
                      ? 'bg-gold text-white shadow-sm'
                      : 'bg-white/10 text-white/60 hover:bg-white/15'
                  }`}
                >
                  {lang.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>
    </header>
      <div id="main-content" />
    </>
  );
}
