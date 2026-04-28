import { useMemo } from 'react';
import { useLanguage } from '../i18n/LanguageContext';
import type { Variant } from '../types/property';
import { Check, Sparkles } from 'lucide-react';

interface VariantSelectorProps {
  variants: Variant[];
  selectedVariant: Variant | null;
  onChange: (variant: Variant) => void;
  listingType: 'sale' | 'rent';
  label?: string;
}

interface SizeGroup {
  size: number;
  variants: Variant[];
  minPrice: number;
  currency: string;
}

export function VariantSelector({ variants, selectedVariant, onChange, listingType, label }: VariantSelectorProps) {
  const { t, fmtPrice, fmtNum } = useLanguage();

  const sizeGroups: SizeGroup[] = useMemo(() => {
    const map = new Map<number, Variant[]>();
    variants.forEach(v => {
      const g = map.get(v.size) || [];
      g.push(v);
      map.set(v.size, g);
    });
    return [...map.entries()]
      .map(([size, vv]) => ({
        size,
        variants: vv,
        minPrice: Math.min(...vv.map(x => x.price)),
        currency: vv[0].currency,
      }))
      .sort((a, b) => a.size - b.size);
  }, [variants]);

  const selectedSize = selectedVariant?.size ?? null;

  const matchingVariants = useMemo(
    () => (selectedSize !== null ? variants.filter(v => v.size === selectedSize) : []),
    [variants, selectedSize],
  );

  const hasDistinctViews = matchingVariants.length > 1
    && new Set(matchingVariants.map(v => v.view).filter(Boolean)).size > 1;

  const hasDistinctOptions = matchingVariants.length > 1
    && new Set(matchingVariants.map(v => v.name).filter(Boolean)).size > 1;

  const views = useMemo(() => {
    if (!hasDistinctViews) return [];
    return [...new Set(matchingVariants.map(v => v.view).filter(Boolean))] as string[];
  }, [matchingVariants, hasDistinctViews]);

  const optionNames = useMemo(() => {
    if (!hasDistinctOptions || hasDistinctViews) return [];
    return [...new Set(matchingVariants.map(v => v.name).filter(Boolean))] as string[];
  }, [matchingVariants, hasDistinctOptions, hasDistinctViews]);

  const handleSizeSelect = (size: number) => {
    const group = variants.filter(v => v.size === size);
    if (!group.length) return;
    if (group.length === 1) { onChange(group[0]); return; }
    const sorted = [...group]
      .filter(v => v.isActive && v.availabilityStatus !== 'SoldOut')
      .sort((a, b) => a.price - b.price);
    onChange(sorted.length > 0 ? sorted[0] : group[0]);
  };

  const handleViewSelect = (view: string) => {
    const match = matchingVariants.find(v => v.view === view);
    if (match) onChange(match);
  };

  const handleOptionSelect = (name: string) => {
    const match = matchingVariants.find(v => v.name === name);
    if (match) onChange(match);
  };

  if (!variants.length) return null;

  const totalSizes = sizeGroups.length;
  const isManySizes = totalSizes > 5;

  return (
    <div>
      <div className="text-xs text-muted-foreground uppercase tracking-wide mb-2 font-semibold">
        {label || t('unit.selectSize')}
      </div>
      {isManySizes ? (
        <div className="overflow-x-auto scrollbar-hide pb-1" style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}>
          <div role="radiogroup" aria-label={label || t('unit.selectSize')}
            className="flex gap-2 min-w-max"
          >
            {sizeGroups.map(sg => {
              const displayPrice = selectedVariant && sg.variants.some(v => v.id === selectedVariant.id)
                ? selectedVariant.price
                : sg.minPrice;
              return (
              <SizeChip
                key={sg.size}
                size={sg.size}
                minPrice={displayPrice}
                currency={sg.currency}
                isSelected={sg.size === selectedSize}
                isBestValue={sg.variants.some(v => v.isRecommended)}
                onClick={() => handleSizeSelect(sg.size)}
                fmtPrice={fmtPrice}
                fmtNum={fmtNum}
                t={t}
              />
            );
            })}
          </div>
        </div>
      ) : (
        <div role="radiogroup" aria-label={label || t('unit.selectSize')}
          className="flex flex-wrap gap-2.5"
        >
          {sizeGroups.map(sg => {
            const displayPrice = selectedVariant && sg.variants.some(v => v.id === selectedVariant.id)
              ? selectedVariant.price
              : sg.minPrice;
            return (
            <SizeChip
              key={sg.size}
              size={sg.size}
              minPrice={displayPrice}
              currency={sg.currency}
              isSelected={sg.size === selectedSize}
              isBestValue={sg.variants.some(v => v.isRecommended)}
              onClick={() => handleSizeSelect(sg.size)}
              fmtPrice={fmtPrice}
              fmtNum={fmtNum}
              t={t}
            />
            );
            })}
        </div>
      )}

      {hasDistinctViews && views.length > 0 && (
        <div className="mt-4 pt-4 border-t border-border/50">
          <div className="text-xs text-muted-foreground uppercase tracking-wide mb-2 font-semibold">
            {t('unit.selectView')}
          </div>
          <div role="radiogroup" aria-label={t('unit.selectView')}
            className="flex flex-wrap gap-2.5"
          >
            {views.map(view => {
              const match = matchingVariants.find(v => v.view === view);
              if (!match) return null;
              const sel = match.id === selectedVariant?.id;
              const isDisabled = match.availabilityStatus === 'SoldOut' || match.availabilityStatus === 'Reserved';
              const vPrice = listingType === 'rent' && match.rentPerMonth != null ? match.rentPerMonth : match.price;
              return (
                <button
                  key={view}
                  role="radio"
                  aria-checked={sel}
                  aria-label={`${t(`view.${view}`, view)}, ${fmtPrice(vPrice, match.currency)}`}
                  disabled={isDisabled}
                  onClick={() => !isDisabled && handleViewSelect(view)}
                  className={`
                    flex items-center gap-2 px-4 py-2.5 rounded-xl border-2 min-h-[44px]
                    transition-[color,background-color,border-color,transform] duration-150
                    ${sel
                      ? 'border-gold bg-gold text-navy shadow-sm scale-[1.02]'
                      : isDisabled
                        ? 'border-border/50 bg-muted/20 opacity-50 cursor-not-allowed'
                        : 'border-border bg-card hover:border-gold/40 hover:bg-gold/5 active:scale-[1.02]'
                    }
                  `}
                >
                  <span className={`font-semibold text-sm ${sel ? 'text-navy' : 'text-foreground'}`}>
                    {view === 'Unknown' ? t('general.standard') : t(`view.${view}`, view)}
                  </span>
                  {sel && (
                    <span className="w-4 h-4 rounded-full bg-navy text-gold flex items-center justify-center shrink-0">
                      <Check className="w-2.5 h-2.5" />
                    </span>
                  )}
                  {isDisabled && (
                    <span className="text-[10px] font-bold uppercase tracking-wider">
                      {match.availabilityStatus === 'SoldOut' ? t('unit.availability.SoldOut') : t('unit.availability.Reserved')}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </div>
      )}

      {!hasDistinctViews && hasDistinctOptions && optionNames.length > 0 && (
        <div className="mt-4 pt-4 border-t border-border/50">
          <div className="text-xs text-muted-foreground uppercase tracking-wide mb-2 font-semibold">
            {t('unit.selectBuilding')}
          </div>
          <div role="radiogroup" aria-label={t('unit.selectBuilding')}
            className="flex flex-wrap gap-2.5"
          >
            {optionNames.map(name => {
              const match = matchingVariants.find(v => v.name === name);
              if (!match) return null;
              const sel = match.id === selectedVariant?.id;
              const isDisabled = match.availabilityStatus === 'SoldOut' || match.availabilityStatus === 'Reserved';
              const vPrice = listingType === 'rent' && match.rentPerMonth != null ? match.rentPerMonth : match.price;
              return (
                <button
                  key={name}
                  role="radio"
                  aria-checked={sel}
                  aria-label={`${name}, ${fmtPrice(vPrice, match.currency)}`}
                  disabled={isDisabled}
                  onClick={() => !isDisabled && handleOptionSelect(name)}
                  className={`
                    flex items-center gap-2 px-4 py-2.5 rounded-xl border-2 min-h-[44px]
                    transition-[color,background-color,border-color,transform] duration-150
                    ${sel
                      ? 'border-gold bg-gold text-navy shadow-sm scale-[1.02]'
                      : isDisabled
                        ? 'border-border/50 bg-muted/20 opacity-50 cursor-not-allowed'
                        : 'border-border bg-card hover:border-gold/40 hover:bg-gold/5 active:scale-[1.02]'
                    }
                  `}
                >
                  <span className={`font-semibold text-sm ${sel ? 'text-navy' : 'text-foreground'}`}>
                    {name}
                  </span>
                  {sel && (
                    <span className="w-4 h-4 rounded-full bg-navy text-gold flex items-center justify-center shrink-0">
                      <Check className="w-2.5 h-2.5" />
                    </span>
                  )}
                  {isDisabled && (
                    <span className="text-[10px] font-bold uppercase tracking-wider">
                      {match.availabilityStatus === 'SoldOut' ? t('unit.availability.SoldOut') : t('unit.availability.Reserved')}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

/* ── Size Chip ── */

interface SizeChipProps {
  size: number;
  minPrice: number;
  currency: string;
  isSelected: boolean;
  isBestValue: boolean;
  onClick: () => void;
  fmtPrice: (n: number, c: string) => string;
  fmtNum: (n: number) => string;
  t: (k: string) => string;
}

function SizeChip({ size, minPrice, currency, isSelected, isBestValue, onClick, fmtPrice, fmtNum, t }: SizeChipProps) {
  return (
    <button
      role="radio"
      aria-checked={isSelected}
      aria-label={`${fmtNum(size)} ${t('general.m2')}, ${fmtPrice(minPrice, currency)}`}
      onClick={onClick}
      className={`
        relative flex flex-col items-center px-5 py-3 rounded-xl border-2 min-h-[44px]
        transition-[color,background-color,border-color,transform] duration-150
        ${isSelected
          ? 'border-gold bg-gold text-navy shadow-sm scale-[1.02]'
          : 'border-border bg-card hover:border-gold/40 hover:bg-gold/5 active:scale-[1.02]'
        }
      `}
    >
      <span className="font-bold text-sm whitespace-nowrap leading-tight">
        {fmtNum(size)}
        <span className="font-normal text-[10px] ml-0.5">{t('general.m2')}</span>
      </span>
      <span className={`text-[11px] font-semibold tabular-nums whitespace-nowrap mt-0.5 ${isSelected ? 'text-navy/80' : 'text-muted-foreground'}`}>
        {fmtPrice(minPrice, currency)}
      </span>
      {isSelected && (
        <span className="absolute -top-1 -right-1 w-5 h-5 rounded-full bg-navy text-gold flex items-center justify-center shadow-sm">
          <Check className="w-3 h-3" />
        </span>
      )}
      {isBestValue && (
        <span className={`absolute -top-2.5 px-2 py-0.5 rounded-full text-[9px] font-bold uppercase tracking-wider shadow-sm ${isSelected ? 'bg-navy text-gold' : 'bg-emerald-500 text-white'}`}>
          <Sparkles className="w-2.5 h-2.5 inline-block -mt-0.5 mr-0.5" />
          {t('unit.bestValue')}
        </span>
      )}
    </button>
  );
}
