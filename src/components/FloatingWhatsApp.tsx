import { MessageCircle } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { useSettings, whatsappLink } from '../hooks/use-settings';

export function FloatingWhatsApp() {
  const { t } = useLanguage();
  const { data: settings } = useSettings();
  const link = whatsappLink(settings?.whatsappNumber || '+201558730895');

  return (
    <a
      href={link}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={t('footer.whatsapp')}
      className="fixed bottom-6 right-6 z-50 flex items-center gap-2 bg-green-500 text-white rounded-full shadow-2xl shadow-green-500/40 hover:bg-green-600 hover:scale-105 active:scale-95 transition-all duration-300 group"
    >
      <span className="flex items-center justify-center w-14 h-14">
        <MessageCircle className="w-7 h-7" />
      </span>
      <span className="sm:max-w-0 sm:overflow-hidden sm:group-hover:max-w-[160px] transition-all duration-500 ease-out whitespace-nowrap pr-4 text-sm font-bold sm:opacity-0 sm:group-hover:opacity-100">
        {t('cta.whatsapp')}
      </span>
    </a>
  );
}
