import { Search, MapPin } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { memo } from 'react';

const icons = {
  search: Search,
  mapPin: MapPin,
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

export const EmptyState = memo(function EmptyState({
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
    mapPin: t('filters.noLocations'),
  };

  const defaultMessage: Record<IconType, string> = {
    search: t('properties.noResults'),
    mapPin: t('filters.noLocations'),
  };

  return (
    <div className={`bg-card border border-border/60 rounded-2xl p-8 sm:p-12 text-center shadow-sm ${className}`} role="status" aria-live="polite">
      <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-muted/50 to-muted/30 flex items-center justify-center mx-auto mb-5 ring-1 ring-border/30">
        <Icon className="w-8 h-8 text-muted-foreground/40" aria-hidden="true" />
      </div>
      <h3 className="text-lg font-semibold mb-2 text-foreground">
        {title || defaultTitle[icon]}
      </h3>
      <p className="text-sm text-muted-foreground max-w-xs mx-auto leading-relaxed">
        {message || defaultMessage[icon]}
      </p>
      {actionLabel && onAction && (
        <button
          type="button"
          onClick={onAction}
          className="mt-6 inline-flex items-center gap-2 px-6 py-3 bg-secondary text-white rounded-xl font-semibold hover:bg-secondary/90 transition-colors duration-200 shadow-lg shadow-secondary/20 text-sm min-h-[44px] active:scale-[0.97]"
        >
          {actionLabel}
        </button>
      )}
    </div>
  );
});
