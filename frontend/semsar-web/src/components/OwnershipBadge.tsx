import { useLanguage } from '../i18n/LanguageContext';
import { Shield } from 'lucide-react';
import type { OwnershipType } from '../types/property';

interface OwnershipBadgeProps {
  type?: OwnershipType;
}

const OWNERSHIP_COLORS: Record<string, string> = {
  Freehold: 'bg-emerald-500/10 text-emerald-600 border-emerald-500/20',
  GreenContract: 'bg-green-500/10 text-green-600 border-green-500/20',
  Usufruct: 'bg-blue-500/10 text-blue-600 border-blue-500/20',
  SharedOwnership: 'bg-purple-500/10 text-purple-600 border-purple-500/20',
  Other: 'bg-gray-500/10 text-gray-600 border-gray-500/20',
};

export function OwnershipBadge({ type }: OwnershipBadgeProps) {
  const { t } = useLanguage();
  if (!type) return null;

  return (
    <div className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full border text-xs font-semibold ${OWNERSHIP_COLORS[type] || OWNERSHIP_COLORS.Other}`}>
      <Shield className="w-3.5 h-3.5" />
      {t(`ownership.${type}`)}
    </div>
  );
}
