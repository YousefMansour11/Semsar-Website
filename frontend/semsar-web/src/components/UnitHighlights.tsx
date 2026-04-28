import { useLanguage } from '../i18n/LanguageContext';
import { Check } from 'lucide-react';

interface UnitHighlightsProps {
  highlights: string[];
  highlightsAr?: string[];
}

export function UnitHighlights({ highlights, highlightsAr }: UnitHighlightsProps) {
  const { t, language } = useLanguage();
  const items = language === 'ar' && highlightsAr?.length ? highlightsAr : highlights;
  if (!items?.length) return null;

  return (
    <div>
      <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('unit.highlights')}</h2>
      <div className="w-10 h-1 bg-gold rounded-full mb-5" />
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {items.map((h, i) => (
          <div key={i} className="flex items-center gap-3 bg-gradient-to-r from-gold/5 to-gold/[0.02] px-4 py-3 rounded-xl border border-gold/10 hover:border-gold/30 transition-colors">
            <div className="w-7 h-7 rounded-full bg-gold/10 flex items-center justify-center text-amber-600 shrink-0">
              <Check className="w-3.5 h-3.5" />
            </div>
            <span className="font-medium text-sm">{t(`feature.${h}`, h)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
