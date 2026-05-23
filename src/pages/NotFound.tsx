import { Link } from "react-router-dom";
import { Header } from "../components/SiteHeader";
import { SiteFooter } from "../components/SiteFooter";
import SeoHelmet from "../components/SeoHelmet";
import { useLanguage } from "../i18n/LanguageContext";
import { localizedPath } from "../lib/paths";

const NotFound = () => {
  const { t, language } = useLanguage();
  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet title={t('general.pageNotFound')} description={t('general.pageNotFoundDesc')} noindex />
      <Header />
      <main className="flex items-center justify-center flex-1 min-h-[65vh]">
        <div className="text-center px-4 space-y-6">
          <div className="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-muted/80 ring-1 ring-border/30 mb-2">
            <span className="text-4xl font-display font-bold text-muted-foreground/40">404</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-display font-bold text-foreground">{t('general.oops')}</h1>
          <p className="text-muted-foreground max-w-sm mx-auto">{t('general.pageNotFoundDesc')}</p>
          <Link to={localizedPath('/', language)} className="inline-flex items-center mt-2 px-8 min-h-[48px] bg-navy text-white rounded-xl font-semibold hover:bg-navy-light transition-all duration-200 shadow-lg shadow-navy/20 hover:shadow-xl active:scale-[0.97]">
            {t('general.returnHome')}
          </Link>
        </div>
      </main>
      <SiteFooter />
    </div>
  );
};

export default NotFound;
