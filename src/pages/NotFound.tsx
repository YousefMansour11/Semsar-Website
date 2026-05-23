import { Link } from "react-router-dom";
import { Header } from "../components/SiteHeader";
import { SiteFooter } from "../components/SiteFooter";
import SeoHelmet from "../components/SeoHelmet";
import { useLanguage } from "../i18n/LanguageContext";

const NotFound = () => {
  const { t } = useLanguage();
  return (
    <div className="min-h-screen bg-background pt-20">
      <SeoHelmet title={t('general.pageNotFound')} description={t('general.pageNotFoundDesc')} noindex />
      <Header />
      <main className="flex items-center justify-center bg-muted flex-1 min-h-[60vh]">
        <div className="text-center px-4">
          <h1 className="mb-4 text-7xl sm:text-9xl font-bold text-foreground">404</h1>
          <p className="mb-4 text-xl sm:text-2xl text-muted-foreground">{t('general.oops')}</p>
          <Link to="/" className="inline-flex items-center mt-2 px-8 min-h-[48px] bg-navy text-white rounded-xl font-semibold hover:bg-navy-light transition-all hover:shadow-lg active:scale-[0.98]">
            {t('general.returnHome')}
          </Link>
        </div>
      </main>
      <SiteFooter />
    </div>
  );
};

export default NotFound;
