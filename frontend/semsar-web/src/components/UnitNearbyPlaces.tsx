import { useLanguage } from '../i18n/LanguageContext';
import { MapPin } from 'lucide-react';
import type { NearbyPlace } from '../types/property';

interface UnitNearbyPlacesProps {
  places?: NearbyPlace[];
}

const PLACE_ICONS: Record<string, string> = {
  Beach: '🏖️',
  Airport: '✈️',
  Marina: '⛵',
  Mall: '🛍️',
  Hospital: '🏥',
  School: '🏫',
  Park: '🌳',
  Restaurant: '🍽️',
  Mosque: '🕌',
  Church: '⛪',
  Supermarket: '🛒',
  Gym: '💪',
  Spa: '🧖',
  Golf: '🏌️',
};

export function UnitNearbyPlaces({ places }: UnitNearbyPlacesProps) {
  const { t, language, fmtNum } = useLanguage();
  const items = places?.filter(p => p.name) || [];
  if (!items.length) return null;

  return (
    <div>
      <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('unit.nearbyPlaces')}</h2>
      <div className="w-10 h-1 bg-gold rounded-full mb-5" />
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {items.map((place, i) => {
          const displayName = language === 'ar' && place.nameAr ? place.nameAr : place.name;
          const icon = PLACE_ICONS[place.name] || PLACE_ICONS[place.icon || ''] || '📍';
          return (
            <div key={i} className="flex items-center gap-3 bg-muted/30 px-4 py-3 rounded-xl border border-border hover:border-gold/20 transition-colors">
              <span className="text-xl shrink-0">{icon}</span>
              <div className="flex-1 min-w-0">
                <div className="font-medium text-sm truncate">{displayName}</div>
                {place.distance > 0 && (
                  <div className="text-xs text-muted-foreground">{fmtNum(place.distance)} km</div>
                )}
              </div>
              <MapPin className="w-4 h-4 text-amber-600 shrink-0" />
            </div>
          );
        })}
      </div>
    </div>
  );
}
