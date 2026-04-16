import { useQuery } from '@tanstack/react-query';
import { fetchSettings } from '../lib/api';
import type { SiteSettings } from '../types/property';

export function useSettings() {
  return useQuery({
    queryKey: ['settings'],
    queryFn: (): Promise<SiteSettings> => fetchSettings(),
    staleTime: 5 * 60 * 1000,
  });
}

// Helper: build a wa.me URL from an E.164 number
export function whatsappLink(num: string, message?: string) {
  const clean = (num || '').replace(/[^\d]/g, '');
  if (!clean) return '';
  return `https://wa.me/${clean}${message ? `?text=${encodeURIComponent(message)}` : ''}`;
}
