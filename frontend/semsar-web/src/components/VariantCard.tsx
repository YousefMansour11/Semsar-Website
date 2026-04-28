import { useLanguage } from '../i18n/LanguageContext';
import type { Variant } from '../types/property';
import { Check } from 'lucide-react';

interface VariantCardProps {
  variant: Variant;
  isSelected: boolean;
  onSelect: () => void;
  listingType: 'sale' | 'rent';
}

const STATUS_STYLES: Record<string, { bg: string; dot: string; text: string }> = {
  Available: { bg: 'bg-emerald-500/10', dot: 'bg-emerald-500', text: 'text-emerald-600' },
  Reserved: { bg: 'bg-amber-500/10', dot: 'bg-amber-500', text: 'text-amber-600' },
  SoldOut: { bg: 'bg-red-500/10', dot: 'bg-red-500', text: 'text-red-600' },
};

export function VariantCard({ variant, isSelected, onSelect, listingType }: VariantCardProps) {
  const { t, fmtPrice, fmtNum } = useLanguage();
  const vPrice = listingType === 'rent' && variant.rentPerMonth != null ? variant.rentPerMonth : variant.price;
  const status = variant.availabilityStatus || 'Available';
  const statusStyle = STATUS_STYLES[status] || STATUS_STYLES.Available;
  const isSold = status === 'SoldOut';
  const isReserved = status === 'Reserved';
  const isDisabled = isSold || isReserved;

  return (
    <button
      onClick={isDisabled ? undefined : onSelect}
      disabled={isDisabled}
      className={`relative flex flex-col gap-2 px-5 py-4 rounded-xl border-2 text-left transition-all ${
        isSelected
          ? 'border-gold bg-gold/5 shadow-md shadow-gold/10'
          : isDisabled
            ? 'border-border/50 bg-muted/20 opacity-60 cursor-not-allowed'
            : 'border-border bg-card hover:border-gold/40 hover:shadow-sm hover:-translate-y-0.5'
      }`}
    >
      {(variant.isFeatured || variant.isRecommended) && (
        <div className="absolute -top-2.5 left-3 flex gap-1.5">
          {variant.isRecommended && (
            <span className="px-2 py-0.5 rounded-full bg-purple-500 text-white text-[10px] font-bold uppercase tracking-wider shadow-sm">
              {t('unit.recommended')}
            </span>
          )}
          {variant.isFeatured && !variant.isRecommended && (
            <span className="px-2 py-0.5 rounded-full bg-gold text-navy text-[10px] font-bold uppercase tracking-wider shadow-sm">
              {t('unit.featured')}
            </span>
          )}
        </div>
      )}

      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <div className="font-bold text-foreground text-base truncate">{variant.name}</div>
          <div className="text-xs text-muted-foreground mt-0.5">
            {fmtNum(variant.size)} {t('general.m2')}
          </div>
        </div>
        {isSelected && (
          <div className="w-6 h-6 rounded-full bg-gold text-navy flex items-center justify-center shrink-0 mt-0.5">
            <Check className="w-3.5 h-3.5" />
          </div>
        )}
      </div>

      <div className="flex items-center justify-between gap-2">
        <div>
          {vPrice > 0 ? (
            <div className="font-bold text-foreground text-sm">{fmtPrice(vPrice, variant.currency)}</div>
          ) : (
            <div className="text-xs text-muted-foreground">{t('properties.priceOnRequest')}</div>
          )}
          {listingType === 'rent' && vPrice > 0 && (
            <div className="text-[10px] text-muted-foreground">/ {t('properties.rentSuffix')}</div>
          )}
        </div>
        <div className={`flex items-center gap-1.5 px-2 py-0.5 rounded-full ${statusStyle.bg} ${statusStyle.text} text-[10px] font-semibold`}>
          <span className={`w-1.5 h-1.5 rounded-full ${statusStyle.dot}`} />
          <span>{t(`unit.availability.${status}`)}</span>
        </div>
      </div>

      {(variant.bedrooms > 0 || variant.bathrooms > 0 || variant.floor != null) && (
        <div className="flex items-center gap-3 text-[11px] text-muted-foreground pt-1 border-t border-border/50 mt-0.5">
          {variant.bedrooms > 0 && <span>{fmtNum(variant.bedrooms)} {t('property.bedrooms')}</span>}
          {variant.bathrooms > 0 && <span>{fmtNum(variant.bathrooms)} {t('property.bathrooms')}</span>}
          {variant.floor != null && <span>{t('property.floor')} {fmtNum(variant.floor)}</span>}
        </div>
      )}
    </button>
  );
}
