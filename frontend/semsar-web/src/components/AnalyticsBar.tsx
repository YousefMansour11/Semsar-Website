import { useLanguage } from '../i18n/LanguageContext';
import { Eye, MessageCircle, Heart } from 'lucide-react';

interface AnalyticsBarProps {
  viewCount?: number;
  inquiryCount?: number;
  favoriteCount?: number;
}

export function AnalyticsBar({ viewCount, inquiryCount, favoriteCount }: AnalyticsBarProps) {
  const { t, fmtNum } = useLanguage();
  const hasData = (viewCount ?? 0) > 0 || (inquiryCount ?? 0) > 0 || (favoriteCount ?? 0) > 0;
  if (!hasData) return null;

  return (
    <div className="flex items-center gap-4 text-muted-foreground text-xs">
      {(viewCount ?? 0) > 0 && (
        <div className="flex items-center gap-1">
          <Eye className="w-3.5 h-3.5" />
          <span>{fmtNum(viewCount!)} {t('unit.analytics.views')}</span>
        </div>
      )}
      {(inquiryCount ?? 0) > 0 && (
        <div className="flex items-center gap-1">
          <MessageCircle className="w-3.5 h-3.5" />
          <span>{fmtNum(inquiryCount!)} {t('unit.analytics.inquiries')}</span>
        </div>
      )}
      {(favoriteCount ?? 0) > 0 && (
        <div className="flex items-center gap-1">
          <Heart className="w-3.5 h-3.5" />
          <span>{fmtNum(favoriteCount!)} {t('unit.analytics.favorites')}</span>
        </div>
      )}
    </div>
  );
}
