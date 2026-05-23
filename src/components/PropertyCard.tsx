import { memo } from 'react';
import { Link } from 'react-router-dom';
import { MapPin, Bed, Square } from 'lucide-react';
import { Property } from '../types/property';
import { useLanguage } from '../i18n/LanguageContext';
import { formatPrice } from '../lib/utils';
import { PremiumImage } from './PremiumImage';

interface PropertyCardProps {
  property: Property;
}

export const PropertyCard = memo(function PropertyCard({ property }: PropertyCardProps) {
  const { t, language, fmtNum } = useLanguage();

  const locale = language === 'ar' ? 'ar-EG' : 'en-US';

  const displayPrice = property.listingType === 'rent' && property.rentPerMonth
    ? `${formatPrice(property.rentPerMonth, locale)} / ${t('installment.monthly')}`
    : formatPrice(property.price, locale);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Available': return 'bg-emerald-500/10 text-emerald-600 border-emerald-500/20';
      case 'Reserved': return 'bg-amber-500/10 text-amber-600 border-amber-500/20';
      case 'Sold': return 'bg-rose-500/10 text-rose-600 border-rose-500/20';
      default: return 'bg-gray-500/10 text-gray-600 border-gray-500/20';
    }
  };

  const displayTitle = language === 'ar' ? property.titleAr : property.titleEn;
  const imgSrc = property.images?.[0] || property.image || '/placeholder.svg';
  const installmentPlan = property.installment || property.installments?.[0];

  return (
    <Link
      to={`/properties/${property.slug}`}
      className="group block bg-card rounded-2xl overflow-hidden border border-border hover:shadow-xl hover:-translate-y-1.5 transition-all duration-300 active:scale-[0.98]"
    >
      <div className="relative aspect-[4/3] overflow-hidden">
        <PremiumImage
          src={imgSrc}
          alt={displayTitle}
          width={800}
          height={600}
          options={{ quality: 'best' }}
          srcsetWidths={[480, 640, 828, 1080, 1200]}
          sizes="(max-width: 640px) 90vw, (max-width: 1024px) 50vw, 33vw"
          className="w-full h-full"
          imgClassName="transition-transform duration-700 group-hover:scale-105"
        />
        <div className={`absolute top-3 sm:top-4 flex flex-col gap-2 ${language === 'ar' ? 'right-3 sm:right-4' : 'left-3 sm:left-4'}`}>
          <span className={`px-2.5 py-1 sm:px-3 rounded-full text-[10px] sm:text-xs font-semibold border backdrop-blur-md ${getStatusColor(property.status)}`}>
            {t(`status.${property.status}`)}
          </span>
          {installmentPlan && (
            <span className="flex items-center gap-1 px-2.5 py-1 sm:px-3 rounded-full text-[10px] sm:text-xs font-semibold bg-gold text-navy backdrop-blur-md">
              {t('installment.badge')}
            </span>
          )}
        </div>
        <div className="absolute bottom-0 left-0 right-0 p-3 sm:p-4 bg-gradient-to-t from-black/80 to-transparent">
          <p className="text-white font-bold text-lg sm:text-xl">{displayPrice}</p>
          {installmentPlan && (
            <p className="text-gold text-[10px] sm:text-xs font-semibold mt-0.5">
              {fmtNum(installmentPlan.downPaymentPercent)}% {t('installment.downPayment')} · {fmtNum(installmentPlan.years)} {t('installment.years')}
            </p>
          )}
        </div>
      </div>

      <div className="p-4 sm:p-5">
        <div className="flex items-center gap-2 text-muted-foreground mb-2 text-xs sm:text-sm">
          <MapPin aria-hidden="true" className="w-3.5 h-3.5 sm:w-4 sm:h-4 shrink-0" />
          <span className="truncate">{language === 'ar' ? (property.locationAr || property.location) : property.location}</span>
          <span className="text-border">·</span>
          <span className="truncate">{t(`prop_type.${property.type}`, property.type)}</span>
        </div>
        <h3 className="font-display font-bold text-base sm:text-lg mb-3 sm:mb-4 text-foreground line-clamp-1 group-hover:text-secondary transition-colors">
          {displayTitle}
        </h3>

        <div className="flex items-center justify-between pt-3 sm:pt-4 border-t border-border/50 text-xs sm:text-sm text-muted-foreground mb-3 sm:mb-4">
          <div className="flex items-center gap-1.5">
            <Bed aria-hidden="true" className="w-3.5 h-3.5 sm:w-4 sm:h-4" />
            <span>{property.bedrooms != null ? `${fmtNum(property.bedrooms)} ${t('property.bedrooms')}` : property.type}</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Square aria-hidden="true" className="w-3.5 h-3.5 sm:w-4 sm:h-4" />
            <span>{fmtNum(property.size)} {t('general.m2')}</span>
          </div>
        </div>

        <span className="block w-full text-center py-4 bg-navy text-white rounded-xl text-sm font-semibold shadow-md shadow-navy/20 group-hover:bg-navy-light group-hover:shadow-lg group-hover:shadow-navy/30 transition-all min-h-[48px] flex items-center justify-center">
          {t('cta.viewDetails')}
        </span>
      </div>
    </Link>
  );
});
