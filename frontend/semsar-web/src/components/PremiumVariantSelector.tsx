import { useMemo, useCallback, useState } from 'react';
import { useLanguage } from '../i18n/LanguageContext';
import type { Variant } from '../types/property';
import { Check, Sparkles } from 'lucide-react';

interface PremiumVariantSelectorProps {
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

export function PremiumVariantSelector({ variants, selectedVariant, onChange, listingType, label }: PremiumVariantSelectorProps) {
  const { t, fmtPrice, fmtNum, language } = useLanguage();

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

  const handleSizeSelect = useCallback((size: number) => {
    const group = variants.filter(v => v.size === size);
    if (!group.length) return;
    if (group.length === 1) { onChange(group[0]); return; }
    const sorted = [...group]
      .filter(v => v.isActive && v.availabilityStatus !== 'SoldOut')
      .sort((a, b) => a.price - b.price);
    onChange(sorted.length > 0 ? sorted[0] : group[0]);
  }, [variants, onChange]);

  const handleViewSelect = useCallback((view: string) => {
    const group = matchingVariants.filter(v => v.view === view);
    if (!group.length) return;
    if (group.length === 1) { onChange(group[0]); return; }
    const sorted = [...group]
      .filter(v => v.isActive && v.availabilityStatus !== 'SoldOut')
      .sort((a, b) => a.price - b.price);
    onChange(sorted.length > 0 ? sorted[0] : group[0]);
  }, [matchingVariants, onChange]);

  const [, setSizeNavFocus] = useState(0);

  const handleSizeKeyDown = useCallback((e: React.KeyboardEvent, idx: number, total: number) => {
    let nextIdx = idx;
    if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
      e.preventDefault();
      nextIdx = (idx + 1) % total;
    } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
      e.preventDefault();
      nextIdx = (idx - 1 + total) % total;
    } else if (e.key === 'Home') {
      e.preventDefault();
      nextIdx = 0;
    } else if (e.key === 'End') {
      e.preventDefault();
      nextIdx = total - 1;
    } else return;
    setSizeNavFocus(nextIdx);
    const btns = document.querySelectorAll<HTMLButtonElement>('[data-size-chip]');
    btns[nextIdx]?.focus();
  }, []);

  const handleViewKeyDown = useCallback((e: React.KeyboardEvent, idx: number, total: number) => {
    let nextIdx = idx;
    if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
      e.preventDefault();
      nextIdx = (idx + 1) % total;
    } else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
      e.preventDefault();
      nextIdx = (idx - 1 + total) % total;
    } else return;
    const btns = document.querySelectorAll<HTMLButtonElement>('[data-view-chip]');
    btns[nextIdx]?.focus();
  }, []);

  if (!variants.length) return null;

  return (
    <div>
      <div className="text-xs text-muted-foreground uppercase tracking-wide mb-2.5 font-semibold">
        {label || t('unit.availableOptions')}
      </div>
      <div role="radiogroup" aria-label={t('unit.selectSize')}
        className="flex flex-wrap gap-2.5"
      >
        {sizeGroups.map((sg, idx) => {
          const isSelected = sg.size === selectedSize;
          const isBestValue = sg.variants.some(v => v.isRecommended);
          const displayPrice = selectedVariant && sg.variants.some(v => v.id === selectedVariant.id)
            ? selectedVariant.price
            : sg.minPrice;
          return (
            <SizeChip
              key={sg.size}
              size={sg.size}
              minPrice={displayPrice}
              currency={sg.currency}
              isSelected={isSelected}
              isBestValue={isBestValue}
              onClick={() => handleSizeSelect(sg.size)}
              onKeyDown={(e) => handleSizeKeyDown(e, idx, sizeGroups.length)}
              fmtPrice={fmtPrice}
              fmtNum={fmtNum}
              t={t}
            />
          );
        })}
      </div>

      {hasDistinctViews && views.length > 0 && (
        <div className="mt-4 pt-4 border-t border-border/50">
          <div className="text-xs text-muted-foreground uppercase tracking-wide mb-2.5 font-semibold">
            {t('unit.selectView')}
          </div>
          <div role="radiogroup" aria-label={t('unit.selectView')}
            className="flex flex-wrap gap-2.5"
          >
            {views.map((view) => {
              const viewVariants = matchingVariants.filter(v => v.view === view);
              if (!viewVariants.length) return null;
              const isMultiOption = viewVariants.length > 1;
              const firstMatch = viewVariants[0];
              return (
                <div key={view} className="flex items-stretch gap-1.5">
                  {isMultiOption ? (
                    viewVariants.map((match) => {
                      const isActive = match.id === selectedVariant?.id;
                      const isDisabled = match.availabilityStatus === 'SoldOut' || match.availabilityStatus === 'Reserved';
                      const vPrice = listingType === 'rent' && match.rentPerMonth != null ? match.rentPerMonth : match.price;
                      return (
                        <button
                          key={match.id ?? match.name}
                          data-view-chip
                          role="radio"
                          aria-checked={isActive}
                          aria-label={`${t(`view.${view}`, view)} ${language === 'ar' ? (match.nameAr || match.name) : match.name}, ${fmtPrice(vPrice, match.currency)}`}
                          disabled={isDisabled}
                          onClick={() => !isDisabled && onChange(match)}
                          className={`
                            relative flex flex-col items-center gap-0.5 px-3.5 py-2.5 rounded-xl border-2 min-h-[52px]
                            transition-[color,background-color,border-color,transform] duration-150
                            ${isActive
                              ? 'border-gold bg-gold text-navy shadow-sm scale-[1.02]'
                              : isDisabled
                                ? 'border-border/50 bg-muted/20 opacity-50 cursor-not-allowed'
                                : 'border-border bg-card hover:border-gold/40 hover:bg-gold/5 active:scale-[1.02]'
                            }
                          `}
                        >
                          <span className="text-xs font-semibold whitespace-nowrap">
                            {t(`view.${view}`, view)}
                          </span>
                          {(() => {
                            const raw = language === 'ar' ? (match.nameAr || match.name) : match.name;
                            const m = raw.match(/\(([^)]+)\)/);
                            const tierLabel = m ? `(${m[1]})` : null;
                            return tierLabel ? (
                              <span className={`text-[10px] font-medium whitespace-nowrap ${isActive ? 'text-navy/70' : 'text-muted-foreground'}`}>
                                {tierLabel}
                              </span>
                            ) : null;
                          })()}
                          <span className={`text-[10px] font-semibold tabular-nums ${isActive ? 'text-navy/80' : 'text-muted-foreground'}`}>
                            {fmtPrice(vPrice, match.currency)}
                          </span>
                          {isActive && (
                            <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-navy text-gold flex items-center justify-center shadow-sm">
                              <Check className="w-2.5 h-2.5" />
                            </span>
                          )}
                          {isDisabled && (
                            <span className="text-[9px] font-bold uppercase tracking-wider">
                              {match.availabilityStatus === 'SoldOut' ? t('unit.availability.SoldOut') : t('unit.availability.Reserved')}
                            </span>
                          )}
                        </button>
                      );
                    })
                  ) : (
                    (() => {
                      const match = firstMatch;
                      const isActive = match.id === selectedVariant?.id;
                      const isDisabled = match.availabilityStatus === 'SoldOut' || match.availabilityStatus === 'Reserved';
                      const vPrice = listingType === 'rent' && match.rentPerMonth != null ? match.rentPerMonth : match.price;
                      return (
                        <button
                          key={view}
                          data-view-chip
                          role="radio"
                          aria-checked={isActive}
                          aria-label={`${t(`view.${view}`, view)}, ${fmtPrice(vPrice, match.currency)}`}
                          disabled={isDisabled}
                          onClick={() => !isDisabled && handleViewSelect(view)}
                          onKeyDown={(e) => handleViewKeyDown(e, views.indexOf(view), views.length)}
                          className={`
                            flex items-center gap-2 px-4 py-2.5 rounded-xl border-2 min-h-[44px]
                            transition-[color,background-color,border-color,transform] duration-150
                            ${isActive
                              ? 'border-gold bg-gold text-navy shadow-sm scale-[1.02]'
                              : isDisabled
                                ? 'border-border/50 bg-muted/20 opacity-50 cursor-not-allowed'
                                : 'border-border bg-card hover:border-gold/40 hover:bg-gold/5 active:scale-[1.02]'
                            }
                          `}
                        >
                          <span className={`font-semibold text-sm ${isActive ? 'text-navy' : 'text-foreground'}`}>
                            {view === 'Unknown' ? t('general.standard') : t(`view.${view}`, view)}
                          </span>
                          {isActive && (
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
                    })()
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {!hasDistinctViews && hasDistinctOptions && optionNames.length > 0 && (
        <div className="mt-4 pt-4 border-t border-border/50">
          <div className="text-xs text-muted-foreground uppercase tracking-wide mb-2.5 font-semibold">
            {t('unit.selectBuilding')}
          </div>
          <div role="radiogroup" aria-label={t('unit.selectBuilding')}
            className="flex flex-wrap gap-2.5"
          >
            {optionNames.map((name) => {
              const match = matchingVariants.find(v => v.name === name);
              if (!match) return null;
              const isActive = match.id === selectedVariant?.id;
              const isDisabled = match.availabilityStatus === 'SoldOut' || match.availabilityStatus === 'Reserved';
              const vPrice = listingType === 'rent' && match.rentPerMonth != null ? match.rentPerMonth : match.price;
              const view = match.view && match.view !== 'Unknown' ? match.view : null;
              const rawName = language === 'ar' ? (match.nameAr || match.name) : match.name;
              const tierMatch = rawName.match(/\(([^)]+)\)/);
              const tierLabel = tierMatch ? `(${tierMatch[1]})` : null;
              return (
                <button
                  key={name}
                  role="radio"
                  aria-checked={isActive}
                  aria-label={`${language === 'ar' ? (match.nameAr || name) : name}, ${fmtPrice(vPrice, match.currency)}`}
                  disabled={isDisabled}
                  onClick={() => !isDisabled && onChange(match)}
                  className={`
                    relative flex flex-col items-center gap-0.5 px-3.5 py-2.5 rounded-xl border-2 min-h-[52px]
                    transition-[color,background-color,border-color,transform] duration-150
                    ${isActive
                      ? 'border-gold bg-gold text-navy shadow-sm scale-[1.02]'
                      : isDisabled
                        ? 'border-border/50 bg-muted/20 opacity-50 cursor-not-allowed'
                        : 'border-border bg-card hover:border-gold/40 hover:bg-gold/5 active:scale-[1.02]'
                    }
                  `}
                >
                  {view && (
                    <span className="text-xs font-semibold whitespace-nowrap">
                      {t(`view.${view}`, view)}
                    </span>
                  )}
                  {tierLabel && (
                    <span className={`text-[10px] font-medium whitespace-nowrap ${isActive ? 'text-navy/70' : 'text-muted-foreground'}`}>
                      {tierLabel}
                    </span>
                  )}
                  <span className={`text-[10px] font-semibold tabular-nums ${isActive ? 'text-navy/80' : 'text-muted-foreground'}`}>
                    {fmtPrice(vPrice, match.currency)}
                  </span>
                  {isActive && (
                    <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-navy text-gold flex items-center justify-center shadow-sm">
                      <Check className="w-2.5 h-2.5" />
                    </span>
                  )}
                  {isDisabled && (
                    <span className="text-[9px] font-bold uppercase tracking-wider">
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
  onKeyDown: (e: React.KeyboardEvent) => void;
  fmtPrice: (n: number, c: string) => string;
  fmtNum: (n: number) => string;
  t: (k: string) => string;
}

function SizeChip({ size, minPrice, currency, isSelected, isBestValue, onClick, onKeyDown, fmtPrice, fmtNum, t }: SizeChipProps) {
  return (
    <button
      data-size-chip
      role="radio"
      aria-checked={isSelected}
      aria-label={`${fmtNum(size)} ${t('general.m2')}, ${fmtPrice(minPrice, currency)}`}
      onClick={onClick}
      onKeyDown={onKeyDown}
      className={`
        relative flex flex-col items-center px-5 py-3 rounded-xl border-2 min-h-[54px]
        transition-[color,background-color,border-color,transform] duration-150
        ${isSelected
          ? 'border-gold bg-gold text-navy shadow-sm scale-[1.04]'
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
