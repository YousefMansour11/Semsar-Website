import { MessageCircle, PhoneCall, Calendar } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';

interface MobileStickyBarProps {
  whatsappHref: string;
  phoneHref: string;
  primaryAction?: {
    label: string;
    onClick: () => void;
  } | null;
}

export function MobileStickyBar({ whatsappHref, phoneHref, primaryAction }: MobileStickyBarProps) {
  const { t, dir } = useLanguage();

  return (
    <div className="sticky bottom-0 z-40 lg:hidden" dir={dir}>
      <div className="bg-white/95 backdrop-blur-xl border-t border-border/30 shadow-[0_-4px_24px_rgba(0,0,0,0.08)] rounded-t-2xl px-3 pt-2.5 pb-[max(env(safe-area-inset-bottom,0px),8px)]">
        <div className="flex items-center gap-2.5">
          <a
            href={whatsappHref}
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 flex items-center justify-center gap-1.5 h-12 rounded-xl bg-green-500/10 text-green-600 font-medium text-xs transition-colors duration-200 hover:bg-green-500/20 hover:shadow-sm active:scale-[0.97]"
          >
            <MessageCircle className="w-4.5 h-4.5 shrink-0" />
            <span className="truncate">{t('cta.whatsapp')}</span>
          </a>
          <a
            href={phoneHref}
            className="flex-1 flex items-center justify-center gap-1.5 h-12 rounded-xl bg-primary/10 text-primary font-medium text-xs transition-colors duration-200 hover:bg-primary/20 hover:shadow-sm active:scale-[0.97]"
          >
            <PhoneCall className="w-4.5 h-4.5 shrink-0" />
            <span className="truncate">{t('cta.callNow')}</span>
          </a>
          {primaryAction && (
            <button
              onClick={primaryAction.onClick}
              className="flex-1 flex items-center justify-center gap-1.5 h-12 rounded-xl bg-navy text-white font-semibold text-xs shadow-md shadow-navy/20 transition-colors duration-200 hover:bg-navy-light hover:shadow-lg active:scale-[0.97]"
            >
              <Calendar className="w-4.5 h-4.5 shrink-0" />
              <span className="truncate">{primaryAction.label}</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
