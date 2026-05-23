import { Helmet } from "react-helmet-async";
import { useLanguage } from "../i18n/LanguageContext";
import { optimizeCloudinaryUrl } from "../lib/utils";

interface SeoHelmetProps {
  title: string;
  description: string;
  canonical?: string;
  image?: string;
  alternates?: { hrefLang: string; href: string }[];
  jsonLd?: string;
  noindex?: boolean;
}

const SITE_NAME = "Semsar";
const DEFAULT_IMAGE = "/semsar-logo.svg";

export default function SeoHelmet({
  title,
  description,
  canonical,
  image = DEFAULT_IMAGE,
  alternates,
  jsonLd,
  noindex,
}: SeoHelmetProps) {
  const { localeStr } = useLanguage();
  const ogLocale = localeStr === 'ar-EG' ? 'ar_EG' : 'en_US';
  const fullTitle = `${title} | ${SITE_NAME}`;
  const ogImage = optimizeCloudinaryUrl(image, 1200);

  return (
    <Helmet>
      <title>{fullTitle}</title>
      <meta name="description" content={description} />

      <meta property="og:title" content={fullTitle} />
      <meta property="og:description" content={description} />
      <meta property="og:image" content={ogImage} />
      <meta property="og:site_name" content={SITE_NAME} />
      <meta property="og:type" content="website" />
      <meta property="og:locale" content={ogLocale} />

      <meta name="twitter:title" content={fullTitle} />
      <meta name="twitter:description" content={description} />
      <meta name="twitter:image" content={ogImage} />
      <meta name="twitter:card" content="summary_large_image" />

      {canonical && <link rel="canonical" href={canonical} />}
      {noindex && <meta name="robots" content="noindex,nofollow" />}

      {alternates?.map((alt) => (
        <link key={alt.hrefLang} rel="alternate" hrefLang={alt.hrefLang} href={alt.href} />
      ))}

      {jsonLd && (
        <script type="application/ld+json">{jsonLd}</script>
      )}
    </Helmet>
  );
}
