import { useState, memo } from 'react';
import { ChevronDown, ChevronUp } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';

interface ReadMoreProps {
  text: string;
  step?: number;
  className?: string;
}

export const ReadMore = memo(function ReadMore({ text, step = 250, className = '' }: ReadMoreProps) {
  const { t } = useLanguage();
  const [limit, setLimit] = useState(step);
  const full = limit >= text.length;

  if (!text) return null;

  const displayText = full ? text : text.slice(0, limit) + '...';
  const contentId = text.slice(0, 20).replace(/\s+/g, '-').toLowerCase();

  return (
    <div>
      <p
        id={`readmore-${contentId}`}
        className={`text-muted-foreground text-lg leading-relaxed whitespace-pre-wrap break-words max-w-prose ${className}`}
      >
        {displayText}
      </p>
      {text.length > step && (
        <button
          onClick={() => setLimit(full ? step : text.length)}
          aria-expanded={!full}
          aria-controls={`readmore-${contentId}`}
          type="button"
          className="flex items-center gap-1 text-sm text-gold mt-2 hover:underline min-h-[44px] min-w-[44px]"
        >
          {full ? (
            <><ChevronUp className="w-4 h-4" /> {t('general.showLess')}</>
          ) : (
            <><ChevronDown className="w-4 h-4" /> {t('general.showMore')}</>
          )}
        </button>
      )}
    </div>
  );
});
