import { useLanguage } from '../i18n/LanguageContext';
import { Construction, Ship, Home } from 'lucide-react';
import type { ConstructionStatus } from '../types/property';

interface ConstructionTimelineProps {
  status: ConstructionStatus;
  deliveryDate?: string;
}

const STATUS_ORDER: ConstructionStatus[] = ['Planned', 'UnderConstruction', 'NearDelivery', 'Delivered'];

const STATUS_ICONS = {
  Planned: Construction,
  UnderConstruction: Construction,
  NearDelivery: Ship,
  Delivered: Home,
};

export function ConstructionTimeline({ status, deliveryDate }: ConstructionTimelineProps) {
  const { t, fmtDate } = useLanguage();
  const currentIdx = STATUS_ORDER.indexOf(status);
  if (currentIdx === -1) return null;

  return (
    <div>
      <h2 className="font-display text-xl sm:text-2xl font-bold mb-2">{t('unit.construction')}</h2>
      <div className="w-10 h-1 bg-gold rounded-full mb-5" />
      <div className="bg-card border border-border rounded-2xl p-5">
        <div className="flex items-center justify-between relative">
          {STATUS_ORDER.map((s, i) => {
            const Icon = STATUS_ICONS[s];
            const isComplete = i <= currentIdx;
            const isCurrent = i === currentIdx;
            return (
              <div key={s} className="flex flex-col items-center relative z-10">
                <div className={`w-10 h-10 rounded-full flex items-center justify-center transition-all ${
                  isComplete ? 'bg-gold text-navy shadow-md shadow-gold/20' : 'bg-muted text-muted-foreground'
                } ${isCurrent ? 'ring-4 ring-gold/20 scale-110' : ''}`}>
                  <Icon className="w-5 h-5" />
                </div>
                <div className={`text-[10px] mt-1.5 font-semibold whitespace-nowrap ${
                  isComplete ? 'text-foreground' : 'text-muted-foreground'
                }`}>
                  {t(`construction.${s}`)}
                </div>
              </div>
            );
          })}
          <div className="absolute top-5 left-0 right-0 h-0.5 bg-muted -translate-y-1/2 z-0">
            <div
              className="h-full bg-gold transition-all duration-700"
              style={{ width: `${(currentIdx / (STATUS_ORDER.length - 1)) * 100}%` }}
            />
          </div>
        </div>
        {deliveryDate && status !== 'Delivered' && (
          <div className="mt-4 pt-4 border-t border-border text-center">
            <div className="text-sm text-muted-foreground">{t('unit.delivery')}: <span className="font-semibold text-foreground">{fmtDate(deliveryDate)}</span></div>
          </div>
        )}
      </div>
    </div>
  );
}
