import { MessageCircle, PhoneCall, Calendar } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';

interface MobileStickyBarProps {
  whatsappHref: string;
  phoneHref: string;
  primaryAction: {
    label: string;
    onClick: () => void;
  };
}

export function MobileStickyBar({ whatsappHref, phoneHref, primaryAction }: MobileStickyBarProps) {
  const { dir } = useLanguage();

  return (
    <div className="sticky bottom-0 z-40 lg:hidden" dir={dir}>
      <div className="bg-background/95 backdrop-blur-lg border-t border-border/40 shadow-[0_-4px_20px_rgba(0,0,0,0.06)] rounded-t-2xl px-3 pt-2 pb-[max(env(safe-area-inset-bottom,0px),6px)]">
        <div className="flex items-center gap-2">
          <a
            href={whatsappHref}
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 flex items-center justify-center gap-1.5 h-11 rounded-xl bg-green-500/10 text-green-600 dark:text-green-400 font-medium text-xs transition-colors hover:bg-green-500/20 active:bg-green-500/25"
          >
            <MessageCircle className="w-4 h-4 shrink-0" />
            <span className="truncate">WhatsApp</span>
          </a>
          <a
            href={phoneHref}
            className="flex-1 flex items-center justify-center gap-1.5 h-11 rounded-xl bg-primary/10 text-primary font-medium text-xs transition-colors hover:bg-primary/20 active:bg-primary/25"
          >
            <PhoneCall className="w-4 h-4 shrink-0" />
            <span className="truncate">Call</span>
          </a>
          <button
            onClick={primaryAction.onClick}
            className="flex-1 flex items-center justify-center gap-1.5 h-11 rounded-xl bg-navy dark:bg-gold text-white dark:text-navy font-semibold text-xs shadow-sm transition-all hover:opacity-90 active:scale-[0.97]"
          >
            <Calendar className="w-4 h-4 shrink-0" />
            <span className="truncate">{primaryAction.label}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
