import { memo } from 'react';
import { Link } from 'react-router-dom';
import { MapPin, Bed, Square, Star } from 'lucide-react';
import { Property } from '../types/property';
import { useLanguage } from '../i18n/LanguageContext';
import { formatPrice } from '../lib/utils';
import { localizedPath } from '../lib/paths';
import { PremiumImage } from './PremiumImage';

interface PropertyCardProps {
  property: Property;
}

export const PropertyCard = memo(function PropertyCard({ property }: PropertyCardProps) {
  const { t, language, fmtNum } = useLanguage();

  const locale = language === 'ar' ? 'ar-EG' : 'en-US';

  const displayPrice = property.listingType === 'rent' && property.rentPerMonth
    ? `${formatPrice(property.rentPerMonth, locale)} / ${t('installment.monthly')}`
    : formatPrice(property.minPrice ?? property.price, locale);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Available': return 'bg-emerald-500/15 text-emerald-500 border-emerald-500/25';
      case 'Reserved': return 'bg-amber-500/15 text-amber-500 border-amber-500/25';
      case 'Sold': return 'bg-rose-500/15 text-rose-500 border-rose-500/25';
      default: return 'bg-gray-500/15 text-gray-500 border-gray-500/25';
    }
  };

  const displayTitle = language === 'ar' ? property.titleAr : property.titleEn;
  const imgSrc = property.images?.[0] || property.image || '/placeholder.svg';
  const installmentPlan = property.installment || property.installments?.[0];
  const isFeatured = property.isFeatured;

  return (
    <Link
      to={localizedPath(`/properties/${property.slug}`, language)}
      className="group block bg-card rounded-2xl overflow-hidden border border-border/60 hover:border-border hover:shadow-2xl hover:-translate-y-1.5 transition-[border-color,box-shadow,transform] duration-300 active:scale-[0.98]"
    >
      <div className="relative aspect-[4/3] overflow-hidden">
        <PremiumImage
          src={imgSrc}
          alt={displayTitle}
          width={1200}
          height={900}
          profile="card"
          className="w-full h-full"
          imgClassName="transition-transform duration-700 ease-out group-hover:scale-105"
        />
        <div className={`absolute top-3 sm:top-4 flex flex-col gap-2 ${language === 'ar' ? 'right-3 sm:right-4' : 'left-3 sm:left-4'}`}>
          {isFeatured && (
            <span className="flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] sm:text-xs font-semibold bg-purple-500/90 text-white backdrop-blur-md shadow-sm">
              <Star className="w-3 h-3 fill-white" />
              {t('unit.featured')}
            </span>
          )}
          <span className={`px-2.5 py-1 sm:px-3 rounded-full text-[10px] sm:text-xs font-semibold border backdrop-blur-md ${getStatusColor(property.status)}`}>
            {t(`status.${property.status}`)}
          </span>
          {installmentPlan && (
            <span className="flex items-center gap-1 px-2.5 py-1 sm:px-3 rounded-full text-[10px] sm:text-xs font-semibold bg-gold/90 text-navy backdrop-blur-md shadow-sm">
              {installmentPlan.paymentType === 'Cash' ? `${t('installment.cashDiscount')}` : t('installment.badge')}
            </span>
          )}
        </div>
        <div className="absolute bottom-0 left-0 right-0 p-3 sm:p-4 bg-gradient-to-t from-black/85 via-black/30 to-transparent">
          <p className="text-white font-bold text-lg sm:text-xl drop-shadow-sm">{displayPrice}</p>
          {installmentPlan && installmentPlan.paymentType !== 'Cash' && (
            <p className="text-gold text-[10px] sm:text-xs font-semibold mt-0.5 drop-shadow-sm">
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
            <span>{property.minArea && property.maxArea && property.minArea !== property.maxArea ? `${fmtNum(property.minArea)} - ${fmtNum(property.maxArea)}` : fmtNum(property.minArea ?? property.size)} {t('general.m2')}</span>
          </div>
        </div>

        <span className="block w-full text-center py-3.5 bg-navy text-white rounded-xl text-sm font-semibold shadow-lg shadow-navy/15 group-hover:bg-navy-light group-hover:shadow-xl group-hover:shadow-navy/25 transition-[background-color,box-shadow] duration-200 min-h-[48px] flex items-center justify-center">
          {t('cta.viewDetails')}
        </span>
      </div>
    </Link>
  );
});
