import { useLanguage } from '../i18n/LanguageContext';
import { Calendar } from 'lucide-react';

interface StickyMobileSummaryProps {
  size: number | null;
  price: number | null;
  currency: string;
  downPaymentPercent?: number;
  downPaymentAmount?: number;
  listingType: 'sale' | 'rent';
  onBookViewing: () => void;
}

export function StickyMobileSummary({
  size, price, currency, downPaymentPercent, downPaymentAmount,
  listingType, onBookViewing,
}: StickyMobileSummaryProps) {
  const { t, fmtPrice, fmtNum } = useLanguage();

  if (price == null) return null;

  return (
    <div className="fixed bottom-0 left-0 right-0 z-40 bg-white/95 backdrop-blur-lg border-t border-border shadow-2xl shadow-black/10 lg:hidden"
      style={{ paddingBottom: 'env(safe-area-inset-bottom, 0px)' }}
    >
      <div className="flex items-center justify-between px-4 py-3 max-w-7xl mx-auto gap-3">
        <div className="min-w-0 flex-1 flex items-center gap-3">
          {size != null && (
            <span className="font-bold text-sm text-foreground whitespace-nowrap">
              {fmtNum(size)} {t('general.m2')}
            </span>
          )}
          <span className="font-bold text-base text-navy tabular-nums whitespace-nowrap">
            {fmtPrice(price, currency)}{listingType === 'rent' ? `/${t('properties.rentSuffix')}` : ''}
          </span>
          {downPaymentPercent != null && downPaymentAmount != null && downPaymentAmount > 0 && (
            <span className="hidden xs:inline text-[11px] text-muted-foreground whitespace-nowrap">
              {fmtNum(downPaymentPercent)}% {t('installment.downPayment')}
            </span>
          )}
        </div>
        <button
          onClick={onBookViewing}
          className="flex items-center justify-center gap-2 min-h-[44px] px-5 py-2.5 bg-secondary text-white rounded-xl font-semibold shadow-lg shadow-secondary/25 hover:bg-secondary/90 transition-all text-sm active:scale-[0.98] shrink-0"
        >
          <Calendar className="w-4 h-4" />
          <span className="hidden xs:inline">{t('cta.bookViewing')}</span>
        </button>
      </div>
    </div>
  );
}
