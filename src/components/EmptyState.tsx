import { Search, Home, Heart, ImageOff, MapPin, AlertCircle } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';

const icons = {
  search: Search,
  home: Home,
  heart: Heart,
  imageOff: ImageOff,
  mapPin: MapPin,
  alert: AlertCircle,
} as const;

type IconType = keyof typeof icons;

interface EmptyStateProps {
  icon?: IconType;
  title?: string;
  message?: string;
  actionLabel?: string;
  onAction?: () => void;
  className?: string;
}

export function EmptyState({
  icon = 'search',
  title,
  message,
  actionLabel,
  onAction,
  className = '',
}: EmptyStateProps) {
  const { t } = useLanguage();
  const Icon = icons[icon];

  const defaultTitle: Record<IconType, string> = {
    search: t('general.noProperties'),
    home: t('general.noProperties'),
    heart: t('general.noProperties'),
    imageOff: t('error.somethingWentWrong'),
    mapPin: t('filters.noLocations'),
    alert: t('error.somethingWentWrong'),
  };

  const defaultMessage: Record<IconType, string> = {
    search: t('properties.noResults'),
    home: t('properties.noResults'),
    heart: t('properties.noResults'),
    imageOff: t('error.somethingWentWrong'),
    mapPin: t('filters.noLocations'),
    alert: t('error.somethingWentWrong'),
  };

  return (
    <div className={`bg-card border border-border/60 rounded-2xl p-8 sm:p-12 text-center shadow-sm ${className}`}>
      <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-muted/50 to-muted/30 flex items-center justify-center mx-auto mb-5 ring-1 ring-border/30">
        <Icon className="w-8 h-8 text-muted-foreground/40" />
      </div>
      <h3 className="text-lg font-semibold mb-2 text-foreground">
        {title || defaultTitle[icon]}
      </h3>
      <p className="text-sm text-muted-foreground max-w-xs mx-auto leading-relaxed">
        {message || defaultMessage[icon]}
      </p>
      {actionLabel && onAction && (
        <button
          onClick={onAction}
          className="mt-6 inline-flex items-center gap-2 px-6 py-3 bg-secondary text-white rounded-xl font-semibold hover:bg-secondary/90 transition-all duration-200 shadow-lg shadow-secondary/20 text-sm min-h-[44px] active:scale-[0.97]"
        >
          {actionLabel}
        </button>
      )}
    </div>
  );
}
