import { useRef, useCallback } from 'react';
import { useLanguage } from '../i18n/LanguageContext';
import type { Variant } from '../types/property';
import { Check } from 'lucide-react';

interface VariantSliderProps {
  variants: Variant[];
  selectedId: number;
  onChange: (variant: Variant) => void;
  listingType: 'sale' | 'rent';
}

export function VariantSlider({ variants, selectedId, onChange, listingType }: VariantSliderProps) {
  const { t, fmtPrice, fmtNum } = useLanguage();
  const ref = useRef<HTMLDivElement>(null);
  const dragRef = useRef({ isDragging: false, startX: 0, scrollLeft: 0 });

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    if (!ref.current) return;
    dragRef.current = { isDragging: false, startX: e.pageX, scrollLeft: ref.current.scrollLeft };
    const onMove = (ev: MouseEvent) => {
      if (!ref.current) return;
      const dx = ev.pageX - dragRef.current.startX;
      if (Math.abs(dx) > 4) dragRef.current.isDragging = true;
      ref.current.scrollLeft = dragRef.current.scrollLeft - dx;
    };
    const onUp = () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  }, []);

  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    const touch = e.touches[0];
    dragRef.current = { isDragging: false, startX: touch.clientX, scrollLeft: ref.current?.scrollLeft ?? 0 };
  }, []);

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    const touch = e.touches[0];
    const dx = touch.clientX - dragRef.current.startX;
    if (Math.abs(dx) > 4) dragRef.current.isDragging = true;
    if (ref.current) ref.current.scrollLeft = dragRef.current.scrollLeft - dx;
  }, []);

  const handleClick = useCallback((v: Variant) => {
    if (dragRef.current.isDragging) return;
    onChange(v);
  }, [onChange]);

  if (!variants.length) return null;

  return (
    <div
      ref={ref}
      onMouseDown={handleMouseDown}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      role="radiogroup"
      aria-label={t('unit.selectSize')}
      className="flex gap-2 overflow-x-auto cursor-grab active:cursor-grabbing scrollbar-hide select-none overscroll-x-contain -mb-1 pb-1"
      style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
    >
      {variants.map((v) => {
        const sel = v.id === selectedId;
        const isSold = v.availabilityStatus === 'SoldOut';
        const isReserved = v.availabilityStatus === 'Reserved';
        const isDisabled = isSold || isReserved;
        const vPrice = listingType === 'rent' && v.rentPerMonth != null ? v.rentPerMonth : v.price;

        return (
          <button
            key={v.id}
            role="radio"
            aria-checked={sel}
            aria-label={`${fmtNum(v.size)} ${t('general.m2')}, ${fmtPrice(vPrice, v.currency)}`}
            disabled={isDisabled}
            onClick={() => handleClick(v)}
            className={`
              flex-shrink-0 flex items-center gap-1.5 px-3 py-2 rounded-xl border-2
              min-h-[44px] text-left transition-[color,background-color,border-color,transform] duration-150
              ${sel
                ? 'border-gold bg-gold text-navy shadow-sm scale-[1.02]'
                : isDisabled
                  ? 'border-border/50 bg-muted/20 opacity-50 cursor-not-allowed'
                  : 'border-border bg-card hover:border-gold/40 hover:bg-gold/5 active:scale-[1.02]'
              }
            `}
          >
            <span className="font-bold text-sm whitespace-nowrap leading-none">
              {fmtNum(v.size)}
              <span className="font-normal text-[10px] ml-0.5">{t('general.m2')}</span>
            </span>
            {vPrice > 0 && (
              <span className={`text-[11px] font-semibold tabular-nums whitespace-nowrap ${sel ? 'text-navy/80' : 'text-muted-foreground'}`}>
                {fmtPrice(vPrice, v.currency)}
              </span>
            )}
            {sel && (
              <span className="w-4 h-4 rounded-full bg-navy text-gold flex items-center justify-center shrink-0">
                <Check className="w-2.5 h-2.5" />
              </span>
            )}
            {(isSold || isReserved) && (
              <span className="text-[10px] font-bold uppercase tracking-wider ml-0.5">
                {isSold ? t('unit.availability.SoldOut') : t('unit.availability.Reserved')}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
